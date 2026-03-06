using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using System.Linq;
using Mirror;

namespace SpectatorCamera
{
    [BepInPlugin("com.kingcox22.sbg.autospectator", "Auto Spectator", "1.3.4")]
    public class SpectatorCameraPlugin : BaseUnityPlugin
    {
        private float _specTimer = 0f;
        
        // Config Entry
        private ConfigEntry<float> _configUpdateInterval;

        private void Awake()
        {
            // Bind the config variable
            _configUpdateInterval = Config.Bind("Spectator", 
                                                "Update Interval", 
                                                3f, 
                                                "Seconds between camera updates.");

            Logger.LogInfo("Auto Spectator 1.3.4 (Cinematic View) Loaded.");
        }

        private void Update()
        {
            // Only process logic if we are actually in a networked match
            if (!NetworkClient.active) return;

            _specTimer += Time.deltaTime;
            
            // Use the config value for the timer check
            if (_specTimer >= _configUpdateInterval.Value)
            {
                _specTimer = 0f;
                MatchLeaderOfficially();
            }
        }

        private void MatchLeaderOfficially()
        {
            var lp = GameManager.LocalPlayerInfo;
            if (lp == null || lp.AsSpectator == null || !lp.AsSpectator.IsSpectating) return;

            var spectator = lp.AsSpectator;
            
            // Search for the 'course' hole/flag location
            Transform holeTrans = GolfHoleManager.MainHole?.transform;
            if (holeTrans == null) return;

            // Find the closest golfer to the flag
            var leader = Object.FindObjectsOfType<PlayerGolfer>()
                .Where(g => g != null && !g.isLocalPlayer && g.gameObject.activeInHierarchy)
                .OrderBy(g => Vector3.Distance(g.transform.position, holeTrans.position))
                .FirstOrDefault();

            if (leader == null) return;
            PlayerInfo leaderInfo = leader.GetComponent<PlayerInfo>();

            // Official Cycle: Step through targets until the game's internal state matches the leader
            int safetyBreak = 0;
            while (spectator.TargetPlayer != leaderInfo && safetyBreak < 20)
            {
                spectator.CycleNextTarget(false);
                safetyBreak++;
            }
        }

        private void LateUpdate()
        {
            var lp = GameManager.LocalPlayerInfo;
            if (lp == null || lp.AsSpectator == null || !lp.AsSpectator.IsSpectating) return;

            Transform playerTransform = lp.AsSpectator.Target; 
            Transform holeTransform = GolfHoleManager.MainHole?.transform;

            if (playerTransform == null || holeTransform == null) return;

            if (CameraModuleController.TryGetOrbitModule(out var orbitModule))
            {
                // 1. HORIZONTAL SYNC (The "Flipped" fix)
                Vector3 playerToHole = (holeTransform.position - playerTransform.position).normalized;
                float targetYaw = Mathf.Atan2(playerToHole.x, playerToHole.z) * Mathf.Rad2Deg;
                orbitModule.SetYaw(targetYaw);

                // 2. VERTICAL LOCK (The "Y Direction" fix)
                // 0 is level with the horizon, 90 is looking straight down.
                // 15f gives a nice slight downward tilt.
                orbitModule.SetPitch(15f); 
            }
        }
    }
}