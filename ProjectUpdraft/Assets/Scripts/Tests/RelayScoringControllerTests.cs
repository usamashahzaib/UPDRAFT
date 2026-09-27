using NUnit.Framework;
using Updraft.Config;
using Updraft.Match;
using UnityEngine;

namespace Updraft.Tests
{
    // Requires Unity's Test Framework package (com.unity.test-framework) and
    // an EditMode assembly definition referencing NUnit. Not yet runnable in
    // this environment (no Unity Editor installed) — verify once available.
    public class RelayScoringControllerTests
    {
        static MatchRulesConfig NewRules()
        {
            var rules = ScriptableObject.CreateInstance<MatchRulesConfig>();
            rules.scoreToWin = 5; // small number for fast tests
            rules.regulationDurationSeconds = 10f;
            rules.overtimeHoldSeconds = 3f;
            rules.maxOvertimeSeconds = 30f;
            return rules;
        }

        [Test]
        public void UncontestedOccupation_ScoresOnePointPerSecond()
        {
            var controller = new RelayScoringController(NewRules());
            for (int i = 0; i < 3; i++)
                controller.Tick(teamAPresent: true, teamBPresent: false, matchTime: i, deltaTime: 1f);

            Assert.AreEqual(3, controller.ScoreA);
            Assert.AreEqual(0, controller.ScoreB);
        }

        [Test]
        public void ContestedZone_DoesNotScore()
        {
            var controller = new RelayScoringController(NewRules());
            controller.Tick(true, true, 0f, 1f);
            controller.Tick(true, true, 1f, 1f);

            Assert.AreEqual(0, controller.ScoreA);
            Assert.AreEqual(0, controller.ScoreB);
        }

        [Test]
        public void LeavingZone_ResetsIncompleteInterval()
        {
            var controller = new RelayScoringController(NewRules());
            controller.Tick(true, false, 0f, 0.9f);   // 0.9s occupied, no point yet
            controller.Tick(false, false, 0.9f, 0.5f); // leaves — resets
            controller.Tick(true, false, 1.4f, 0.9f);  // occupies again, still < 1s total

            Assert.AreEqual(0, controller.ScoreA);
        }

        [Test]
        public void ScoreLimit_EndsMatchImmediately()
        {
            var controller = new RelayScoringController(NewRules());
            for (int i = 0; i < 5 && controller.Outcome == MatchOutcome.InProgress; i++)
                controller.Tick(true, false, i, 1f);

            Assert.AreEqual(MatchOutcome.TeamAWins, controller.Outcome);
        }

        [Test]
        public void TiedAtRegulationEnd_EntersOvertimeThenAwardsHold()
        {
            var rules = NewRules();
            rules.scoreToWin = 999;
            rules.regulationDurationSeconds = 2f;
            var controller = new RelayScoringController(rules);

            // Tie at 0-0 through regulation.
            controller.Tick(false, false, 0f, 1f);
            controller.Tick(false, false, 1f, 1f);
            controller.Tick(false, false, 2f, 1f); // crosses regulation end -> overtime starts

            Assert.AreEqual(MatchOutcome.InProgress, controller.Outcome);

            // Team A holds uncontested for the full overtime window.
            controller.Tick(true, false, 2f, 1f);
            controller.Tick(true, false, 2f, 1f);
            controller.Tick(true, false, 2f, 1f);

            Assert.AreEqual(MatchOutcome.TeamAWins, controller.Outcome);
        }
    }
}
