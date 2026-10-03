using NUnit.Framework;
using UnityEngine;
using Hotdrop.Match;

namespace Hotdrop.Tests
{
    /// <summary>QA-018 regression: aiming alone keeps a player active; idle and jitter do not.</summary>
    public class AfkRulesTests
    {
        [Test]
        public void AimingOnly_IsActivity() =>
            Assert.IsTrue(AfkRules.IsActivity(Vector2.zero, false, 90f, 0f, 93f, 0f));

        [Test]
        public void PitchOnly_IsActivity() =>
            Assert.IsTrue(AfkRules.IsActivity(Vector2.zero, false, 90f, 0f, 90f, -4f));

        [Test]
        public void YawWrapAround_IsMeasuredTheShortWay()
        {
            Assert.IsTrue(AfkRules.AimChanged(359.5f, 0f, 1.5f, 0f), "2 degrees across north");
            Assert.IsFalse(AfkRules.AimChanged(359.8f, 0f, 0.1f, 0f), "0.3 degrees across north is jitter");
        }

        [Test]
        public void TrulyIdle_IsNotActivity() =>
            Assert.IsFalse(AfkRules.IsActivity(Vector2.zero, false, 90f, 5f, 90.4f, 5.2f));

        [Test]
        public void MovementAndButtons_StillCount()
        {
            Assert.IsTrue(AfkRules.IsActivity(new Vector2(0.5f, 0f), false, 0f, 0f, 0f, 0f));
            Assert.IsTrue(AfkRules.IsActivity(Vector2.zero, true, 0f, 0f, 0f, 0f));
        }
    }
}


