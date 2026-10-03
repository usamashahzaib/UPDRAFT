using UnityEngine;

namespace Hotdrop.Config
{
    // Final spec section 24: soft-launch decision thresholds. These are
    // decision inputs for humans reviewing telemetry at Milestone D/G, not
    // runtime gameplay behavior — kept as a config asset so the numbers are
    // versioned alongside everything else rather than living in a slide deck.
    [CreateAssetMenu(fileName = "KpiGateConfig", menuName = "Hotdrop/Config/KPI Gate Config")]
    public class KpiGateConfig : ScriptableObject
    {
        [Range(0f, 1f)] public float minTutorialCompletionRate = 0.80f;
        [Range(0f, 1f)] public float minD1Retention = 0.35f;
        [Range(0f, 1f)] public float minD7Retention = 0.15f;
        [Range(0f, 1f)] public float minD30Retention = 0.05f;
        [Range(0f, 1f)] public float minCrashFreeSessionRate = 0.995f;
        [Range(0f, 1f)] public float minVoluntaryInstantRematchRate = 0.50f;
        public float maxMedianPublicQueueSeconds = 45f;

        [Header("Kill/Pivot Rule")]
        // Spec section 24: "two failed KPI passes -> fix core loop or pivot
        // (Phase 2 re-skin), no sunk-cost spending." Tracked externally per
        // test round; this just records the agreed threshold count.
        public int failedPassesBeforePivotDecision = 2;
    }
}


