using NUnit.Framework;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.UI;

namespace Hotdrop.Tests
{
    /// <summary>Small, config-backed UI regressions. These do not assert a mockup or balance literal.</summary>
    public class GameplayHudPresentationTests
    {
        [Test]
        public void PreparationCopy_StatesTheActualPreMatchLocks()
        {
            string copy = MatchHUD.PreparationCopy(8f);
            StringAssert.Contains("8s", copy);
            StringAssert.Contains("SPAWN LOCKED", copy);
            StringAssert.Contains("DAMAGE BLOCKED", copy);
            StringAssert.Contains("PADS UNAVAILABLE", copy);
        }

        [Test]
        public void LoadoutStats_NeverSplitAValueFromItsUnit()
        {
            // Regression: the BRICK SCATTERGUN row wrapped as "9 DMG · 8 PELLETS · 75" / "RPM",
            // leaving a fire rate whose unit sat on the next line. Wrapping is still enabled on the
            // stat text so an over-long row folds instead of escaping the card, so the copy has to
            // break where a reader expects it to - never between a number and its unit.
            foreach (int pellets in new[] { 0, 8 })
            {
                var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                weapon.bodyDamage = 9f;
                weapon.pelletsPerShot = pellets;
                weapon.roundsPerMinute = 75f;
                weapon.magazineSize = 6;
                weapon.reloadSeconds = 2f;
                string copy = MatchHUD.FormatLoadoutStats(weapon);
                foreach (string line in copy.Split('\n'))
                {
                    string trimmed = line.Trim();
                    Assert.IsNotEmpty(trimmed, $"pellets={pellets}: a stat row must not be blank");
                    Assert.IsFalse(char.IsDigit(trimmed[trimmed.Length - 1]),
                        $"pellets={pellets}: stat row '{trimmed}' ends on a bare number, so its unit " +
                        "was pushed to another line and the value is easy to misread");
                }
                Object.DestroyImmediate(weapon);
            }
        }

        [Test]
        public void LoadoutStats_GiveAPelletCountItsOwnLineSoTheRateStaysReadable()
        {
            // Without a pellet count the two-row layout fits and is kept: a card that always showed
            // three rows would waste space on the weapons that do not need it.
            var plain = ScriptableObject.CreateInstance<WeaponDefinition>();
            plain.bodyDamage = 18f; plain.pelletsPerShot = 1;
            plain.roundsPerMinute = 480f; plain.magazineSize = 24; plain.reloadSeconds = 1.6f;
            Assert.AreEqual(2, MatchHUD.FormatLoadoutStats(plain).Split('\n').Length,
                "a non-pellet weapon's stats fit two rows");
            Object.DestroyImmediate(plain);

            var shotgun = ScriptableObject.CreateInstance<WeaponDefinition>();
            shotgun.bodyDamage = 9f; shotgun.pelletsPerShot = 8;
            shotgun.roundsPerMinute = 75f; shotgun.magazineSize = 6; shotgun.reloadSeconds = 2f;
            string copy = MatchHUD.FormatLoadoutStats(shotgun);
            Assert.AreEqual(3, copy.Split('\n').Length,
                "the pellet count adds a row rather than overrunning the damage row");
            StringAssert.Contains("8 PELLETS\n75 RPM", copy);
            Object.DestroyImmediate(shotgun);
        }

        [Test]
        public void LoadoutStats_ReadTheWeaponDefinitionRatherThanAUiPreset()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.bodyDamage = 17.5f;
            weapon.pelletsPerShot = 8;
            weapon.roundsPerMinute = 77f;
            weapon.magazineSize = 6;
            weapon.reloadSeconds = 2.25f;
            string copy = MatchHUD.FormatLoadoutStats(weapon);
            StringAssert.Contains("18 DMG", copy); // UI rounds display only; simulation keeps fractional values.
            StringAssert.Contains("8 PELLETS", copy);
            StringAssert.Contains("77 RPM", copy);
            StringAssert.Contains("6 MAG", copy);
            StringAssert.Contains("2.3s RELOAD", copy);
            Object.DestroyImmediate(weapon);
        }

        [Test]
        public void MatchFlow_OnlyOfflineCallerMayPauseTime_OnlinePhasesRemainLive()
        {
            var rules = ScriptableObject.CreateInstance<MatchRulesConfig>();
            var flow = new MatchFlowController(rules);
            Assert.IsFalse(flow.DamageAllowed);
            Assert.IsFalse(flow.PadPlacementAllowed);
            flow.Tick(rules.preparationSeconds);
            Assert.AreEqual(MatchPhase.Countdown, flow.Phase);
            Assert.IsFalse(flow.DamageAllowed);
            flow.Tick(rules.countdownSeconds);
            Assert.AreEqual(MatchPhase.InProgress, flow.Phase);
            Assert.IsTrue(flow.DamageAllowed);
            Object.DestroyImmediate(rules);
        }
    }
}
