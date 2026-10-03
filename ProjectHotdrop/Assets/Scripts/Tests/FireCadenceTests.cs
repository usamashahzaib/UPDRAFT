using NUnit.Framework;
using UnityEditor;
using Hotdrop.Config;
using Hotdrop.Weapons;

namespace Hotdrop.Tests
{
    /// <summary>
    /// QA-009 regression: at the 30 Hz network simulation rate, sustained fire must deliver
    /// the configured rounds per minute. A whole-tick timer restarted per shot rounds every
    /// interval up (ARC 3.75 -> 4 ticks = 450 RPM; ZIP 2.31 -> 3 ticks = 600 RPM).
    /// </summary>
    public class FireCadenceTests
    {
        const float Tick = 1f / 30f;

        static WeaponDefinition Load(string id) =>
            AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"Assets/Resources/Config/Weapon_{id}.asset");

        static int ShotsInOneMinute(float interval)
        {
            float debt = 0f;
            int shots = 0;
            for (int i = 0; i < 30 * 60; i++)
                if (FireCadence.Step(ref debt, Tick, true, interval)) shots++;
            return shots;
        }

        [TestCase("ARC")]
        [TestCase("ZIP")]
        [TestCase("BRICK")]
        public void SustainedFire_AtThirtyHz_MatchesConfiguredRpm(string id)
        {
            var weapon = Load(id);
            Assert.IsNotNull(weapon, id);
            float configuredRpm = 60f / weapon.SecondsPerShot;
            Assert.AreEqual(configuredRpm, ShotsInOneMinute(weapon.SecondsPerShot), 1.0,
                $"{id} must fire {configuredRpm:0} rounds per minute online");
        }

        [Test]
        public void FirstShot_IsImmediate_AndIdleTimeIsNotBanked()
        {
            float debt = 0f;
            Assert.IsTrue(FireCadence.Step(ref debt, Tick, true, 0.125f), "a fresh trigger pull fires at once");
            for (int i = 0; i < 90; i++) FireCadence.Step(ref debt, Tick, false, 0.125f); // 3 s idle
            Assert.IsTrue(FireCadence.Step(ref debt, Tick, true, 0.125f));
            Assert.IsFalse(FireCadence.Step(ref debt, Tick, true, 0.125f), "idle time must not allow a burst");
        }
    }
}


