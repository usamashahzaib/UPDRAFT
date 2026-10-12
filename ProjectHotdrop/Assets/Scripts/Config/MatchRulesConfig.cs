using UnityEngine;

namespace Hotdrop.Config
{
    public enum ZoneId { Center, West, East }

    [System.Serializable]
    public struct ZoneScheduleEntry
    {
        public float startTime;
        public float endTime;
        public ZoneId zone;
    }

    // Spec section 4: Relay mode rules, and section 5: match flow timings.
    [CreateAssetMenu(fileName = "MatchRulesConfig", menuName = "Hotdrop/Config/Match Rules Config")]
    public class MatchRulesConfig : ScriptableObject
    {
        [Header("Duration")]
        public float regulationDurationSeconds = 180f;
        public float maxOvertimeSeconds = 30f;
        public float overtimeHoldSeconds = 3f;

        [Header("Scoring")]
        public int scoreToWin = 120;
        public float zoneCaptureRadius = 6f;
        public float zoneMaxFeetHeight = 1.5f;

        [Header("Zone Schedule (matches spec table exactly)")]
        public ZoneScheduleEntry[] schedule = new[]
        {
            new ZoneScheduleEntry { startTime = 0f,   endTime = 30f,  zone = ZoneId.Center },
            new ZoneScheduleEntry { startTime = 30f,  endTime = 60f,  zone = ZoneId.West },
            new ZoneScheduleEntry { startTime = 60f,  endTime = 90f,  zone = ZoneId.East },
            new ZoneScheduleEntry { startTime = 90f,  endTime = 120f, zone = ZoneId.Center },
            new ZoneScheduleEntry { startTime = 120f, endTime = 150f, zone = ZoneId.East },
            new ZoneScheduleEntry { startTime = 150f, endTime = 180f, zone = ZoneId.West },
        };
        public float nextZoneWarningSeconds = 10f;

        [Header("Match Flow")]
        public float preparationSeconds = 8f;
        public float countdownSeconds = 3f;
        public float loadTimeoutSeconds = 30f;
        public float respawnDelaySeconds = 4f;

        [Header("Adaptive Music (Design Bible estimates)")]
        public float musicPreUnlockSilenceSeconds = 1.5f;
        public float musicRhythmStartSeconds = 20f;
        public float musicLeadStartSeconds = 60f;
        public float musicBaseTempoBpm = 120f;
        public float musicEndgameTempoBonusBpm = 4f;
        public float endgameHudTrimSeconds = 60f;

        [Header("Disconnect")]
        public float botSubstitutionDelaySeconds = 5f;

        [Header("Pings")]
        public float pingLifetimeSeconds = 8f;
        public float pingCooldownSeconds = 2f;

        [Header("Spatial Weapon Audio")]
        [Min(0.01f)] public float spatialAudioMinDistanceMeters = 3f;
        [Min(0.01f)] public float weaponFullMixDistanceMeters = 15f;
        [Min(0.01f)] public float weaponDistantReportDistanceMeters = 40f;
        [Min(0.01f)] public float spatialAudioMaxDistanceMeters = 50f;
        [Min(1f)] public float weaponMidDistanceLowPassHz = 1800f;
        [Min(0.01f)] public float nearbyWeaponCaptionMeters = 10f;

        [Header("Cockpit HUD Fade (HUD spec estimates)")]
        [Min(0f)] public float cockpitCombatRangeMeters = 30f;
        [Range(0f, 1f)] public float cockpitFadedOpacity = 0.15f;
        [Min(0.01f)] public float cockpitFadeDurationSeconds = 0.2f;
        [Min(0f)] public float rosterIdleFadeSeconds = 5f;

        [Header("Damage Feedback (Bible 2.5 presentation estimates)")]
        [Min(0.05f)] public float hitArcDurationSeconds = 0.8f;
        [Range(0f, 1f)] public float hitArcMaxOpacity = 0.9f;
        [Range(10f, 180f)] public float hitArcWidthDegrees = 50f;
        [Range(0.05f, 1f)] public float lowHealthThresholdFraction = 0.3f;
        [Range(0f, 1f)] public float lowHealthVignetteMaxOpacity = 0.55f;
        [Min(0.05f)] public float lowHealthCueDurationSeconds = 1.2f;
        [Min(0.05f)] public float lowHealthAudioRepeatIntervalSeconds = 1.2f;
        [Range(0f, 100f)] public float lowHealthDesaturationAmount = 30f;
        [Range(0f, 1f)] public float flightSpeedLinesMaxOpacity = 0.14f;
        [Header("Pad Launch Feedback (Bible 2.5 presentation estimates)")]
        [Range(0f, 10f)] public float padLaunchFovKickDegrees = 2f;
        [Range(0f, 1f)] public float padLaunchChromaticIntensity = 0.08f;
        [Range(0.01f, 1f)] public float padLaunchChromaticDurationSeconds = 0.15f;
        [Min(1)] public int shakeMaxEventsPerSecond = 2;

        [Header("Aim")]
        [Min(0.5f)] public float minimumAimPointDistanceMeters = 0.5f;
        [Min(0.5f)] public float cameraAimRayDistanceMeters = 100f;

        public void ValidateSpatialWeaponAudio()
        {
            ValidatePositiveFinite(nameof(spatialAudioMinDistanceMeters), spatialAudioMinDistanceMeters);
            ValidatePositiveFinite(nameof(weaponFullMixDistanceMeters), weaponFullMixDistanceMeters);
            ValidatePositiveFinite(nameof(weaponDistantReportDistanceMeters), weaponDistantReportDistanceMeters);
            ValidatePositiveFinite(nameof(spatialAudioMaxDistanceMeters), spatialAudioMaxDistanceMeters);
            if (!(spatialAudioMinDistanceMeters < weaponFullMixDistanceMeters
                && weaponFullMixDistanceMeters < weaponDistantReportDistanceMeters
                && weaponDistantReportDistanceMeters < spatialAudioMaxDistanceMeters))
                throw new System.InvalidOperationException(
                    "MatchRulesConfig weapon audio distances must satisfy spatialMin < fullMix < distantReport < spatialMax.");

            ValidatePositiveFinite(nameof(weaponMidDistanceLowPassHz), weaponMidDistanceLowPassHz);
            ValidatePositiveFinite(nameof(nearbyWeaponCaptionMeters), nearbyWeaponCaptionMeters);
            if (nearbyWeaponCaptionMeters > spatialAudioMaxDistanceMeters)
                throw new System.InvalidOperationException(
                    "MatchRulesConfig.nearbyWeaponCaptionMeters cannot exceed spatialAudioMaxDistanceMeters.");
        }

        static void ValidatePositiveFinite(string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new System.InvalidOperationException($"MatchRulesConfig.{field} must be finite and positive.");
        }

        public ZoneId? GetActiveZone(float matchTime)
        {
            foreach (var entry in schedule)
                if (matchTime >= entry.startTime && matchTime < entry.endTime)
                    return entry.zone;
            return null;
        }

        /// <summary>The zone that scores at this time. Overtime continues on the last regulation zone.</summary>
        public ZoneId GetScoringZone(float matchTime)
        {
            var active = GetActiveZone(matchTime);
            if (active.HasValue) return active.Value;
            return matchTime >= regulationDurationSeconds && schedule.Length > 0 ? schedule[schedule.Length - 1].zone : ZoneId.Center;
        }

        /// <summary>Seconds until the active zone changes, or -1 when no later zone is scheduled.</summary>
        public float SecondsUntilRotation(float matchTime)
        {
            foreach (var entry in schedule)
                if (matchTime >= entry.startTime && matchTime < entry.endTime)
                    return entry.endTime < regulationDurationSeconds ? entry.endTime - matchTime : -1f;
            return -1f;
        }

        public ZoneId? GetUpcomingZone(float matchTime)
        {
            foreach (var entry in schedule)
            {
                float warnAt = entry.startTime - nextZoneWarningSeconds;
                if (matchTime >= warnAt && matchTime < entry.startTime)
                    return entry.zone;
            }
            return null;
        }
    }
}


