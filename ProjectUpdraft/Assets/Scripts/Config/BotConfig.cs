using UnityEngine;

namespace Updraft.Config
{
    // Spec section 13: bots for tutorial, offline practice, and disconnect substitution only.
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
