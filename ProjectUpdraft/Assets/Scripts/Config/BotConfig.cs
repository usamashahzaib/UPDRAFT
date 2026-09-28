using UnityEngine;

namespace Updraft.Config
{
    // Final spec section 12: bots for tutorial, offline practice, disconnect
    // substitution, AND public-lobby fill once queue time exceeds
    // MatchmakingConfig.botFillQueueThresholdSeconds (this is a change from the
    // earlier draft, which kept public matchmaking human-only).
    [CreateAssetMenu(fileName = "BotConfig", menuName = "Updraft/Config/Bot Config")]
    public class BotConfig : ScriptableObject
    {
        [Header("Reaction Delay")]
        public float easyPracticeReactionSeconds = 0.6f;
        public float standardReactionSeconds = 0.35f; // standard practice / disconnect substitution

        [Header("Behavior")]
        [Range(0f, 1f)] public float retreatHealthFraction = 0.3f;
        [Range(0f, 1f)] public float focusFireDesyncChance = 0.35f; // avoid perfectly synced team focus fire
        public float focusFireDesyncMinSeconds = 0.1f;
        public float focusFireDesyncMaxSeconds = 0.35f;

        [Header("Perception")]
        public bool useLineOfSightOnly = true; // never read hidden enemy positions for aiming
        public float sightRangeMeters = 50f;
        // Horizontal half-angle of what a bot can see; a human sees roughly the camera's view.
        public float viewConeHalfAngleDegrees = 60f;
        // Gunfire is audible within this range; bots move toward the sound's origin, never the hidden player.
        public float hearingRangeMeters = 30f;
        public float heardShotMemorySeconds = 0.5f;

        [Header("Imperfect Tracking")]
        public float initialAimErrorEasyDegrees = 7f;
        public float initialAimErrorStandardDegrees = 4.5f;
        public float minimumAimErrorEasyDegrees = 2.5f;
        public float minimumAimErrorStandardDegrees = 1.2f;
        public float aimErrorRecoveryDegreesPerSecond = 3f;
    }
}
