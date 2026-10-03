using UnityEngine;

namespace Hotdrop.Config
{
    // Spec section 14: matchmaking/skill model.
    [CreateAssetMenu(fileName = "MatchmakingConfig", menuName = "Hotdrop/Config/Matchmaking Config")]
    public class MatchmakingConfig : ScriptableObject
    {
        [Header("Party")]
        public int maxPartySize = 3;
        public int inviteCodeLength = 8;

        [Header("Skill Model")]
        public int startingRating = 1000;
        public int eloK = 24;

        [Header("Search Widening")]
        public float evaluationIntervalSeconds = 2f;
        public int initialRatingWindow = 150;
        public float widenAfterSeconds = 20f;
        public int widenStepEverySeconds = 10;
        public int widenStepAmount = 50;
        public int maxRatingWindow = 350;
        public int preferredMaxLatencyMs = 120;

        [Header("UX")]
        public float showStillSearchingAfterSeconds = 60f;

        [Header("Bot Fill (final spec section 12/13)")]
        // Public lobbies fill remaining slots with labeled bots once queue time
        // exceeds this threshold, so a match still starts. Bot-filled matches
        // are flagged in analytics and excluded from balance/rating updates.
        public float botFillQueueThresholdSeconds = 45f;
    }
}


