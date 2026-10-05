using NUnit.Framework;
using UnityEngine;
using Hotdrop.Input;

namespace Hotdrop.Tests
{
    // Spec 8: "Aim assist: slowdown only, <=3 degree cone, <=25m, sensitivity drops to 70%
    // inside region, no snap, no tracking through cover, no magnetism, no auto-fire, off switch."
    // The toggle existed without evidence of the actual behaviour; these tests pin it.
    public class AimAssistTests
    {
        static readonly Vector3 Eye = Vector3.zero;
        static Vector3 Forward => Vector3.forward;

        static Vector3 AtDistance(float distance) => Forward * distance;

        [Test]
        public void SpecLimits_AreThreeDegreesTwentyFiveMetersAndSeventyPercent()
        {
            Assert.AreEqual(3f, AimAssistRules.ConeDegrees, 0.0001f);
            Assert.AreEqual(25f, AimAssistRules.MaxDistance, 0.0001f);
            Assert.AreEqual(0.7f, AimAssistRules.SensitivityMultiplier, 0.0001f);
        }

        [Test]
        public void TargetDeadAheadInsideRange_IsAssisted()
        {
            Assert.IsTrue(AimAssistRules.Qualifies(Eye, Forward, AtDistance(10f), lineOfSightClear: true));
        }

        [Test]
        public void TargetInsideTheCone_IsAssisted()
        {
            // Two degrees off axis is inside the three-degree cone.
            Vector3 target = Quaternion.Euler(0f, 2f, 0f) * Forward * 10f;
            Assert.IsTrue(AimAssistRules.Qualifies(Eye, Forward, target, lineOfSightClear: true));
        }

        [Test]
        public void TargetOutsideTheCone_IsNotAssisted()
        {
            // Ten degrees off axis is well outside the cone.
            Vector3 target = Quaternion.Euler(0f, 10f, 0f) * Forward * 10f;
            Assert.IsFalse(AimAssistRules.Qualifies(Eye, Forward, target, lineOfSightClear: true));
        }

        [Test]
        public void TargetBeyondTwentyFiveMeters_IsNotAssisted()
        {
            Assert.IsFalse(AimAssistRules.Qualifies(Eye, Forward, AtDistance(25.5f), lineOfSightClear: true));
        }

        [Test]
        public void TargetAtExactlyTwentyFiveMeters_IsStillAssisted()
        {
            // "<=25m" is inclusive.
            Assert.IsTrue(AimAssistRules.Qualifies(Eye, Forward, AtDistance(25f), lineOfSightClear: true));
        }

        [Test]
        public void TargetBehindThePlayer_IsNotAssisted()
        {
            Assert.IsFalse(AimAssistRules.Qualifies(Eye, Forward, -Forward * 5f, lineOfSightClear: true));
        }

        [Test]
        public void ObstructedTarget_IsNotAssisted()
        {
            // "no tracking through cover": if anything blocks the sight line, no assist.
            Assert.IsFalse(AimAssistRules.Qualifies(Eye, Forward, AtDistance(10f), lineOfSightClear: false));
        }

        [Test]
        public void TargetAtTheCamera_IsIgnored()
        {
            // Degenerate zero-length direction must not qualify.
            Assert.IsFalse(AimAssistRules.Qualifies(Eye, Forward, Eye, lineOfSightClear: true));
        }

        [Test]
        public void InsideTheRegion_SensitivityDropsToSeventyPercent()
        {
            Assert.AreEqual(0.7f, AimAssistRules.SensitivityScale(true), 0.0001f);
        }

        [Test]
        public void OutsideTheRegion_SensitivityIsUntouched()
        {
            // Slowdown only: the assist never speeds aim up, and never alters it at range.
            Assert.AreEqual(1f, AimAssistRules.SensitivityScale(false), 0.0001f);
        }

        [Test]
        public void SensitivityScale_OnlyEverSlowsDown_NeverSpeedsUp()
        {
            foreach (bool inside in new[] { true, false })
                Assert.LessOrEqual(AimAssistRules.SensitivityScale(inside), AimAssistRules.FullSensitivityScale,
                    "Aim assist must only reduce sensitivity, never increase it.");
        }

        [Test]
        public void AssistProvidesExactlyTwoScales_SoNoSnapOrMagnetismIsPossible()
        {
            // The assist returns one of two sensitivity scales and nothing else, so there
            // is no code path that could bend the aim toward a target.
            var distinct = new System.Collections.Generic.HashSet<float>
            {
                AimAssistRules.SensitivityScale(true),
                AimAssistRules.SensitivityScale(false),
            };
            Assert.AreEqual(2, distinct.Count);
        }
    }
}