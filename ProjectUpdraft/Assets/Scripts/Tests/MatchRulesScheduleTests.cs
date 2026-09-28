using NUnit.Framework;
using UnityEngine;
using Updraft.Config;

namespace Updraft.Tests
{
    public class MatchRulesScheduleTests
    {
        MatchRulesConfig rules;

        [SetUp] public void SetUp() => rules = ScriptableObject.CreateInstance<MatchRulesConfig>();
        [TearDown] public void TearDown() => Object.DestroyImmediate(rules);

        [TestCase(0f, ZoneId.Center)]
        [TestCase(29.9f, ZoneId.Center)]
        [TestCase(30f, ZoneId.West)]
        [TestCase(60f, ZoneId.East)]
        [TestCase(90f, ZoneId.Center)]
        [TestCase(120f, ZoneId.East)]
        [TestCase(150f, ZoneId.West)]
        public void ScoringZone_FollowsSpecSchedule(float time, ZoneId expected) =>
            Assert.AreEqual(expected, rules.GetScoringZone(time));

        [TestCase(180f)]
        [TestCase(195f)]
        [TestCase(209.9f)]
        public void Overtime_StaysOnTheZoneActiveAtRegulationEnd(float time) =>
            Assert.AreEqual(ZoneId.West, rules.GetScoringZone(time));

        [Test]
        public void NextZone_IsAnnouncedTenSecondsEarly_WithCountdown()
        {
            Assert.IsNull(rules.GetUpcomingZone(19.9f));
            Assert.AreEqual(ZoneId.West, rules.GetUpcomingZone(20f));
            Assert.AreEqual(8f, rules.SecondsUntilRotation(22f), 0.001f);
            Assert.AreEqual(-1f, rules.SecondsUntilRotation(170f), "No rotation is announced after the last regulation zone.");
        }
    }
}
