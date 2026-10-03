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

        [Header("Disconnect")]
        public float botSubstitutionDelaySeconds = 5f;

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


