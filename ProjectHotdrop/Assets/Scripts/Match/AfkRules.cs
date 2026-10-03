using UnityEngine;

namespace Hotdrop.Match
{
    /// <summary>
    /// Spec 18 activity rules. QA-018: aiming is activity — a stationary player actively
    /// tracking opponents must not be warned (30 s) or replaced by a bot (45 s).
    /// </summary>
    public static class AfkRules
    {
        /// <summary>Smaller changes are crosshair jitter (e.g. the aim point sliding over moving targets).</summary>
        public const float MinAimChangeDegrees = 1f;

        public static bool AimChanged(float previousYaw, float previousPitch, float yaw, float pitch) =>
            Mathf.Abs(Mathf.DeltaAngle(previousYaw, yaw)) >= MinAimChangeDegrees ||
            Mathf.Abs(Mathf.DeltaAngle(previousPitch, pitch)) >= MinAimChangeDegrees;

        public static bool IsActivity(Vector2 move, bool anyButton, float previousYaw, float previousPitch, float yaw, float pitch) =>
            move.sqrMagnitude > 0.01f || anyButton || AimChanged(previousYaw, previousPitch, yaw, pitch);
    }
}

