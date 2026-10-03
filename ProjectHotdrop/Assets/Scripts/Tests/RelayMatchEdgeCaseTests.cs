using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Match;

namespace Hotdrop.Tests
{
    /// <summary>Gate B, spec sections 3-4: every zone transition, contested/empty, score limit, timer, overtime, draw and phase permissions.</summary>
    public class RelayMatchEdgeCaseTests
    {
        MatchRulesConfig rules;
        const float Dt = 1f / 30f;

        [SetUp]
        public void SetUp() => rules = AssetDatabase.LoadAssetAtPath<MatchRulesConfig>("Assets/Resources/Config/MatchRulesConfig.asset");

        static void Run(RelayScoringController s, bool a, bool b, ref float time, float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps && s.Outcome == MatchOutcome.InProgress; i++)
            {
                time += Dt;
                s.Tick(a, b, time, Dt);
            }
        }

        [Test]
        public void EveryZoneTransition_FollowsScheduleAndWarnsTenSecondsEarly()
        {
            ZoneId[] expected = { ZoneId.Center, ZoneId.West, ZoneId.East, ZoneId.Center, ZoneId.East, ZoneId.West };
            for (int i = 0; i < expected.Length; i++)
            {
                float start = i * 30f;
                Assert.AreEqual(expected[i], rules.GetScoringZone(start), $"at {start}s");
                Assert.AreEqual(expected[i], rules.GetScoringZone(start + 29.99f), $"just before {start + 30}s");
                if (i > 0)
                {
                    Assert.AreEqual(expected[i], rules.GetUpcomingZone(start - 10f), "announced 10 s early");
                    Assert.IsNull(rules.GetUpcomingZone(start - 10.01f), "not announced earlier than 10 s");
                }
            }
            Assert.AreEqual(ZoneId.West, rules.GetScoringZone(185f), "Overtime stays on the zone active at 180 s.");
            Assert.AreEqual(-1f, rules.SecondsUntilRotation(170f), "No rotation after the final segment.");
        }

        [Test]
        public void ContestedAndEmpty_NeverScore_ForTheWholeMatch()
        {
            var s = new RelayScoringController(rules);
            float t = 0f;
            Run(s, true, true, ref t, 60f);
            Run(s, false, false, ref t, 60f);
            Assert.AreEqual(0, s.ScoreA);
            Assert.AreEqual(0, s.ScoreB);
        }

        [Test]
        public void ContestedInterruption_ResetsPartialSecond()
        {
            var s = new RelayScoringController(rules);
            float t = 0f;
            Run(s, true, false, ref t, 0.9f);
            Run(s, true, true, ref t, 0.2f);
            Run(s, true, false, ref t, 0.9f);
            Assert.AreEqual(0, s.ScoreA, "Two 0.9 s holds split by contest are not a point.");
        }

        [Test]
        public void ScoreLimit_WinsAtExactly120()
        {
            var s = new RelayScoringController(rules);
            float t = 0f;
            Run(s, false, true, ref t, 119.5f);
            Assert.AreEqual(MatchOutcome.InProgress, s.Outcome);
            Assert.AreEqual(119, s.ScoreB);
            Run(s, false, true, ref t, 1f);
            Assert.AreEqual(MatchOutcome.TeamBWins, s.Outcome);
            Assert.AreEqual(120, s.ScoreB);
        }

        [Test]
        public void Timer_HigherScoreWinsAt180()
        {
            var s = new RelayScoringController(rules);
            float t = 0f;
            Run(s, true, false, ref t, 10f);
            Run(s, false, false, ref t, 175f);
            Assert.AreEqual(MatchOutcome.TeamAWins, s.Outcome);
            Assert.GreaterOrEqual(t, 180f - Dt);
            Assert.IsFalse(s.IsInOvertime);
        }

        [Test]
        public void Tie_Overtime_ContestedAndEmptyResetHold_ThenHoldWins()
        {
            var s = new RelayScoringController(rules);
            float t = 0f;
            Run(s, false, false, ref t, 181f);
            Assert.IsTrue(s.IsInOvertime);
            Run(s, true, false, ref t, 2.5f);
            Run(s, true, true, ref t, 0.5f);   // contested resets
            Run(s, true, false, ref t, 2.5f);
            Run(s, false, false, ref t, 0.5f); // empty resets
            Assert.AreEqual(MatchOutcome.InProgress, s.Outcome);
            Run(s, false, true, ref t, 3.1f);
            Assert.AreEqual(MatchOutcome.TeamBWins, s.Outcome);
            Assert.AreEqual(0, s.ScoreA);
            Assert.AreEqual(0, s.ScoreB, "Overtime is decided by a hold, not points.");
        }

        [Test]
        public void Tie_ThirtySecondsWithoutHold_IsDraw()
        {
            var s = new RelayScoringController(rules);
            float t = 0f;
            Run(s, false, false, ref t, 180.5f);
            Assert.IsTrue(s.IsInOvertime);
            Run(s, true, true, ref t, 29f);
            Assert.AreEqual(MatchOutcome.InProgress, s.Outcome);
            Run(s, true, true, ref t, 2f);
            Assert.AreEqual(MatchOutcome.Draw, s.Outcome);
        }

        [Test]
        public void Occupancy_RequiresAliveFeetInsideLowAndUnprotected()
        {
            Vector3 zone = Vector3.zero;
            Assert.IsTrue(ZoneOccupancy.IsValid(new Vector3(5.9f, 0f, 0f), zone, 0f, rules, true, false));
            Assert.IsFalse(ZoneOccupancy.IsValid(new Vector3(6.1f, 0f, 0f), zone, 0f, rules, true, false), "outside radius");
            Assert.IsTrue(ZoneOccupancy.IsValid(new Vector3(0f, 1.5f, 0f), zone, 0f, rules, true, false));
            Assert.IsFalse(ZoneOccupancy.IsValid(new Vector3(0f, 1.6f, 0f), zone, 0f, rules, true, false), "flying over does not capture");
            Assert.IsFalse(ZoneOccupancy.IsValid(Vector3.zero, zone, 0f, rules, false, false), "dead");
            Assert.IsFalse(ZoneOccupancy.IsValid(Vector3.zero, zone, 0f, rules, true, true), "spawn-protected");
        }

        [Test]
        public void Phases_PrepThenCountdownThenLive_WithPermissions()
        {
            var flow = new MatchFlowController(rules);
            Assert.AreEqual(MatchPhase.Preparation, flow.Phase);
            Assert.IsTrue(flow.WeaponSelectAllowed);
            Assert.IsFalse(flow.DamageAllowed);
            Assert.IsFalse(flow.PadPlacementAllowed);
            for (int i = 0; i < Mathf.RoundToInt(7.9f / Dt); i++) flow.Tick(Dt);
            Assert.AreEqual(MatchPhase.Preparation, flow.Phase);
            for (int i = 0; i < 4; i++) flow.Tick(Dt);
            Assert.AreEqual(MatchPhase.Countdown, flow.Phase);
            Assert.IsFalse(flow.WeaponSelectAllowed);
            Assert.IsFalse(flow.DamageAllowed);
            Assert.AreEqual(0f, flow.MatchTime, "The authoritative match clock starts after the countdown.");
            for (int i = 0; i < Mathf.RoundToInt(3f / Dt) + 1; i++) flow.Tick(Dt);
            Assert.AreEqual(MatchPhase.InProgress, flow.Phase);
            Assert.IsTrue(flow.DamageAllowed);
            Assert.IsTrue(flow.PadPlacementAllowed);
            flow.NotifyMatchEnded();
            Assert.AreEqual(MatchPhase.Results, flow.Phase);
            Assert.IsFalse(flow.DamageAllowed, "Scoring and damage stop instantly at match end.");
            Assert.IsFalse(flow.PadPlacementAllowed);
        }
    }
}


