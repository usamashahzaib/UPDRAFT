namespace Hotdrop.Weapons
{
    /// <summary>
    /// QA-009: fixed-step fire cadence that carries the fractional remainder between shots,
    /// so a 30 Hz simulation still delivers the configured rounds per minute on average
    /// (ARC 0.125 s = 3.75 ticks, ZIP 0.0769 s = 2.31 ticks). Restarting a whole-tick timer
    /// on every shot rounds each interval up and silently lowers the fire rate.
    /// Same rule as the offline <see cref="WeaponState"/>: no banking while the trigger is up.
    /// </summary>
    public static class FireCadence
    {
        const float Epsilon = 1e-4f;

        /// <summary>
        /// Advances <paramref name="debt"/> (seconds until the next shot may fire) by one step.
        /// Returns true when a shot fires this step; at most one per step.
        /// </summary>
        public static bool Step(ref float debt, float deltaTime, bool triggerHeld, float interval)
        {
            debt -= deltaTime;
            if (!triggerHeld || debt > Epsilon)
            {
                if (!triggerHeld && debt < 0f) debt = 0f;
                return false;
            }
            // Keep the (negative) remainder so the next interval starts on time, but never let
            // a long stall bank more than one step of credit.
            if (debt < -deltaTime) debt = -deltaTime;
            debt += interval;
            return true;
        }
    }
}

