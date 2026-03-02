using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Mirror;
using System.Linq;
using System.Collections.Generic;

namespace SpectatorCamera
{
    [BepInPlugin("com.kingcox22.sbg.autospectator", "Auto Spectator Camera", "1.0")]
    public class SpectatorCameraPlugin : BaseUnityPlugin
    {
        private static ConfigEntry<float> _configSpecInterval;

        private float _specTimer = 0f;
        private Transform _cachedHole;
        private OrbitCameraModule _cachedOrbit;
        private bool _isCustomSpectatorActive = false;

        private void Awake()
        {
            _configSpecInterval = Config.Bind("Spectator", "Update Interval", 3f, "Seconds between camera updates."); 

            PlayerSpectator.LocalPlayerIsSpectatingChanged += OnGameSpectateChanged; 
            PlayerSpectator.LocalPlayerStoppedSpectating += OnGameSpectateStopped; 

            var harmony = new Harmony("com.kingcox22.sbg.autospectator");
            harmony.PatchAll();
        }

        private void Update()
        {
            _specTimer += Time.deltaTime;
            if (_specTimer >= _configSpecInterval.Value)
            {
                _specTimer = 0f;
                RefreshSpectatorLogic();
            }
        }

        private void OnGameSpectateChanged()  
        {
            var localSpec = GameObject.FindObjectsByType<PlayerSpectator>(FindObjectsSortMode.None)
                .FirstOrDefault(s => s.isLocalPlayer);
            
            if (localSpec != null)
            {
                _isCustomSpectatorActive = localSpec.IsSpectating;
            }
        }

        private void OnGameSpectateStopped()
        {
            _isCustomSpectatorActive = false;
            ResetCamera();
        }

        private void ResetCamera()
        {
            if (_cachedOrbit == null) _cachedOrbit = GameObject.FindFirstObjectByType<OrbitCameraModule>();
            if (_cachedOrbit != null)
            {
                _cachedOrbit.subject = null;
                var localGolfer = GameObject.FindObjectsByType<PlayerGolfer>(FindObjectsSortMode.None)
                    .FirstOrDefault(g => g.isLocalPlayer);
                if (localGolfer != null) _cachedOrbit.subject = localGolfer.transform;
            }
            _cachedHole = null;
        }

        private void RefreshSpectatorLogic()
        {
            if (!_isCustomSpectatorActive) return;

            if (_cachedHole == null)
            {
                GameObject hole = GameObject.Find("Hole") ?? GameObject.Find("Main hole");
                if (hole != null) _cachedHole = hole.transform;
            }

            if (_cachedOrbit == null) _cachedOrbit = GameObject.FindFirstObjectByType<OrbitCameraModule>();
            if (_cachedHole == null || _cachedOrbit == null) return;

            var golfers = GameObject.FindObjectsByType<PlayerGolfer>(FindObjectsSortMode.None)
                .Where(g => g != null && !CourseManager.IsPlayerSpectator(g))
                .ToList();

            if (golfers.Count == 0) return;

            var leader = golfers
                .OrderBy(g => Vector3.Distance(g.transform.position, _cachedHole.position))
                .FirstOrDefault();

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

            _cachedOrbit.transform.position = player.position + (lookDir * 8.5f) + (Vector3.up * 4.5f);
            _cachedOrbit.transform.LookAt(_cachedHole.position + (Vector3.up * 1.5f));
        }
    }
}