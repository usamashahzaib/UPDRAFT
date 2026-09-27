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

        [Header("Perception")]
        public bool useLineOfSightOnly = true; // never read hidden enemy positions for aiming
    }
}
