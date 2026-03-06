using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SpectatorCamera
{
    [BepInPlugin("com.kingcox22.sbg.autospectator", "Auto Spectator", "1.1.1")]
    public class SpectatorCameraPlugin : BaseUnityPlugin
    {
        private static ConfigEntry<float> _configSpecInterval;
        private float _specTimer = 0f;
        private Transform _cachedHole;
        private OrbitCameraModule _cachedOrbit;
        private bool _isCustomSpectatorActive = false;
        private string _lastScene = "";

        private void Awake()
        {
            _configSpecInterval = Config.Bind("Spectator", "Update Interval", 3f, "Seconds between camera updates."); 

            // Keep your event hooks
            PlayerSpectator.LocalPlayerIsSpectatingChanged += OnGameSpectateChanged; 
            PlayerSpectator.LocalPlayerStoppedSpectating += OnGameSpectateStopped; 

            var harmony = new Harmony("com.kingcox22.sbg.autospectator");
            harmony.PatchAll();
            
            Logger.LogInfo("Auto Spectator Loaded. Watching for Round Ends...");
        }

        private void Update()
        {
            // --- NEW: END OF ROUND RESET TRIGGERS ---
            
            // 1. Reset if we enter the Driving Range or Lobby
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != _lastScene)
            {
                if (currentScene.Contains("DrivingRange") || currentScene.Contains("Lobby"))
                {
                    OnGameSpectateStopped();
                }
                _lastScene = currentScene;
            }

            // 2. Reset if a new Hole Overview starts
            if (HoleOverviewCameraUi.HasInstance && HoleOverviewCameraUi.Instance.NetworkisVisible)
            {
                if (_isCustomSpectatorActive) OnGameSpectateStopped();
            }

            // --- END RESET TRIGGERS ---

            if (!NetworkClient.active) return;

            _specTimer += Time.deltaTime;
            if (_specTimer >= _configSpecInterval.Value)
            {
                _specTimer = 0f;
                
                // Safety: If the event failed to fire, poll the state manually
                CheckSpectatorStateManually();
                
                RefreshSpectatorLogic();
            }
        }

        private void CheckSpectatorStateManually()
        {
            var lp = GameManager.LocalPlayerInfo;
            if (lp != null && lp.AsSpectator != null)
            {
                _isCustomSpectatorActive = lp.AsSpectator.IsSpectating;
            }
        }

        private void OnGameSpectateChanged()  
        {
            var lp = GameManager.LocalPlayerInfo;
            if (lp != null && lp.AsSpectator != null)
            {
                _isCustomSpectatorActive = lp.AsSpectator.IsSpectating;
            }
        }

        private void OnGameSpectateStopped()
        {
            Logger.LogInfo("Round end or Manual Stop detected. Resetting camera to player.");
            _isCustomSpectatorActive = false;
            ResetCamera();
        }

        private void ResetCamera()
        {
            if (_cachedOrbit == null) _cachedOrbit = GameObject.FindFirstObjectByType<OrbitCameraModule>();
            if (_cachedOrbit != null)
            {
                var localGolfer = GameObject.FindObjectsByType<PlayerGolfer>(FindObjectsSortMode.None)
                    .FirstOrDefault(g => g.isLocalPlayer);
                
                if (localGolfer != null)
                {
                    _cachedOrbit.subject = localGolfer.transform;
                }
            }
            _cachedHole = null;
        }

        private void RefreshSpectatorLogic()
        {
            // 1. Guard Clause: Don't run if the feature is toggled off
            if (!_isCustomSpectatorActive) return;

            // 2. Resolve Hole Location using the Game's Internal Singleton
            // We check MainHole directly. If the level is loading or no hole is set, this returns null.
            if (GolfHoleManager.MainHole != null)
            {
                _cachedHole = GolfHoleManager.MainHole.transform;
            }
            else
            {
                // Fallback: If manager is empty, try a quick Type search or just wait for the next frame
                var fallbackHole = GameObject.FindFirstObjectByType<GolfHole>();
                _cachedHole = fallbackHole?.transform;
            }

            // 3. Resolve Camera Module
            if (_cachedOrbit == null) 
            {
                _cachedOrbit = GameObject.FindFirstObjectByType<OrbitCameraModule>();
            }

            // 4. Safety Check: If we still don't have a hole or a camera, we can't do math
            if (_cachedHole == null || _cachedOrbit == null) return;

            // 5. Gather all other golfers (excluding the local player)
            var golfers = GameObject.FindObjectsByType<PlayerGolfer>(FindObjectsSortMode.None)
                .Where(g => g != null && !g.isLocalPlayer && g.gameObject.activeInHierarchy)
                .ToList();

            if (golfers.Count == 0) return;

            // 6. Find the Leader (The golfer closest to the GolfHoleManager.MainHole)
            var leader = golfers
                .OrderBy(g => Vector3.Distance(g.transform.position, _cachedHole.position))
                .FirstOrDefault();

            // 7. Update Camera Subject
            // Only update if the subject has actually changed to prevent camera jitter
            if (leader != null && _cachedOrbit.subject != leader.transform) 
            {
                _cachedOrbit.subject = leader.transform;
            }
        }

        private void LateUpdate()
        {
            if (!_isCustomSpectatorActive || _cachedHole == null || _cachedOrbit == null || _cachedOrbit.subject == null) return;

            Transform player = _cachedOrbit.subject;
            Vector3 lookDir = (player.position - _cachedHole.position).normalized;
            lookDir.y = 0;

            // Positioning logic
            _cachedOrbit.transform.position = player.position + (lookDir * 8.5f) + (Vector3.up * 4.5f);
            _cachedOrbit.transform.LookAt(_cachedHole.position + (Vector3.up * 1.5f));
        }
    }
}