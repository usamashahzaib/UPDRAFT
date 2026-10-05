using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Hotdrop.Input;
using Hotdrop.Player;

namespace Hotdrop.Tests
{
    /// <summary>
    /// C15: aim assist is advertised in the pause menu, so its exact spec rule is pinned here —
    /// slowdown only (the look direction is never bent toward a target), a 3 degree cone, a 25 m
    /// limit, 70% of normal sensitivity inside the region, and a working off switch. Deciding that
    /// a target is inside the region needs a live scene with real colliders; what is pinned here
    /// is the rule itself plus the wiring that applies it, so the advertised claim cannot drift.
    /// </summary>
    public class AimAssistRuleTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Rules_MatchTheSpecNumbers()
        {
            Assert.AreEqual(3f, AimAssistRules.ConeDegrees, "spec: aim assist cone is at most 3 degrees");
            Assert.AreEqual(25f, AimAssistRules.MaxDistance, "spec: aim assist only applies within 25 m");
            Assert.AreEqual(0.7f, AimAssistRules.SensitivityMultiplier,
                "spec: 70% sensitivity inside the region (slowdown only, no rotation assist)");
        }

        [Test]
        public void SensitivityMultiplier_IsSlowdownOnly_NeverAnAmplifier()
        {
            Assert.Less(AimAssistRules.SensitivityMultiplier, 1f,
                "aim assist may only slow aiming down, never speed it up");
            Assert.Greater(AimAssistRules.SensitivityMultiplier, 0f,
                "a zero multiplier would freeze aiming instead of slowing it");
            // The only two scales that can ever be produced: full speed, or the slowed one.
            Assert.AreEqual(0.7f, AimAssistRules.SensitivityScale(true), "inside the region: 70%");
            Assert.AreEqual(1f, AimAssistRules.SensitivityScale(false), "outside the region: untouched");
        }

        [Test]
        public void Qualifies_RespectsConeDistanceAndLineOfSight()
        {
            Vector3 eye = Vector3.zero;
            Vector3 forward = Vector3.forward;

            // Straight ahead inside the cone and range.
            Assert.IsTrue(AimAssistRules.Qualifies(eye, forward, new Vector3(0, 0, 10f), true),
                "a target dead ahead within 25 m must qualify");

            // Just outside the 3 degree cone.
            float outsideCone = AimAssistRules.ConeDegrees + 1f;
            Vector3 angled = Quaternion.AngleAxis(outsideCone, Vector3.up) * Vector3.forward * 10f;
            Assert.IsFalse(AimAssistRules.Qualifies(eye, forward, angled, true),
                "a target beyond the 3 degree cone must not qualify");

            // Just inside the cone.
            float insideCone = AimAssistRules.ConeDegrees - 0.5f;
            Vector3 nearAxis = Quaternion.AngleAxis(insideCone, Vector3.up) * Vector3.forward * 10f;
            Assert.IsTrue(AimAssistRules.Qualifies(eye, forward, nearAxis, true),
                "a target just inside the cone must qualify");

            // Beyond 25 m, dead ahead.
            Assert.IsFalse(AimAssistRules.Qualifies(eye, forward, Vector3.forward * 30f, true),
                "a target beyond 25 m must not qualify");
            Assert.IsTrue(AimAssistRules.Qualifies(eye, forward, Vector3.forward * AimAssistRules.MaxDistance, true),
                "the limit itself is inclusive");

            // Cover never grants assist ("no tracking through cover").
            Assert.IsFalse(AimAssistRules.Qualifies(eye, forward, Vector3.forward * 10f, false),
                "an obstructed target must never qualify");
        }

        [Test]
        public void Qualifies_IgnoresDegenerateZeroDistance()
        {
            Assert.IsFalse(AimAssistRules.Qualifies(Vector3.zero, Vector3.forward, Vector3.zero, true),
                "a zero-length direction has no meaningful angle");
        }

        [Test]
        public void PauseMenu_ToggleIsLabelledSlowdownOnlyAndDefaultsOn()
        {
            string source = ReadProjectSource("Core/OfflinePauseMenu.cs");
            StringAssert.Contains("AIM ASSIST (SLOWDOWN ONLY)", source,
                "the player-facing claim must state what the assist actually does");
            StringAssert.Contains("\"AimAssistEnabled\", true", source, "the assist defaults to on");
            StringAssert.Contains("SetAimAssistEnabled", source, "the toggle must reach the player controllers");
        }

        [Test]
        public void BothOfflineAndOnlineCallTheSharedQualifyHelper()
        {
            // One definition for both paths: an offline/online divergence would make the
            // advertised assist mean two different things. Both must route the decision
            // through AimAssistRules.Qualifies/SensitivityScale rather than their own copy.
            foreach (var path in new[] { "Network/NetClientPresenter.cs", "Player/PlayerController.cs" })
            {
                string source = ReadProjectSource(path);
                StringAssert.Contains("AimAssistRules.Qualifies", source, path + " must use the shared rule");
                StringAssert.Contains("AimAssistRules.SensitivityScale", source, path + " must use the shared scale");
            }
        }

        [Test]
        public void AimAssist_IsARealSwitch_NotHardcodedOn()
        {
            var go = new GameObject("AimAssistSwitch");
            go.SetActive(false);
            try
            {
                var player = go.AddComponent<PlayerController>();
                var field = typeof(PlayerController).GetField("aimAssistEnabled", Hidden);
                Assert.IsNotNull(field, "aim assist must be a real field, not an always-on hardcode");

                player.SetAimAssistEnabled(false);
                Assert.AreEqual(false, field.GetValue(player), "the off switch must actually disable the assist");
                player.SetAimAssistEnabled(true);
                Assert.AreEqual(true, field.GetValue(player), "the assist must be re-enableable mid-match");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Reads a script from the project folder; EditMode has no asset bundles.</summary>
        static string ReadProjectSource(string relativePath)
        {
            string data = Application.dataPath.Replace('\\', '/');
            string projectRoot = data.EndsWith("/Assets") ? data.Substring(0, data.Length - "/Assets".Length) : data;
            string[] candidates =
            {
                Path.Combine(projectRoot, "Assets/Scripts/" + relativePath),
                Path.Combine(projectRoot, "ProjectHotdrop/Assets/Scripts/" + relativePath),
            };
            foreach (var path in candidates)
                if (File.Exists(path)) return File.ReadAllText(path);
            Assert.Fail("Could not locate " + relativePath + " under " + projectRoot);
            return "";
        }
    }
}