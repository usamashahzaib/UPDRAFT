using HotdropBackend.Services;
using Xunit;

namespace HotdropBackend.Tests;

// The 2026-10-01 amendment defines `same_day_return` and the session boundary.
// These tests pin the definition so it cannot drift silently into "any second launch".
public class SessionRulesTests
{
    static DateTimeOffset At(int hour, int minute = 0) =>
        new DateTimeOffset(2026, 10, 3, hour, minute, 0, TimeSpan.Zero);

    [Fact]
    public void NoActivity_IsNoSession()
    {
        var summary = SessionRules.Summarize(SessionRules.BuildSessions([]));
        Assert.Equal(0, summary.Sessions);
        Assert.Equal(0, summary.SameDayReturns);
    }

    [Fact]
    public void EventsInsideThirtyMinutes_AreOneSession()
    {
        var sessions = SessionRules.BuildSessions([At(10), At(10, 20), At(10, 50)]);
        var session = Assert.Single(sessions);
        Assert.Equal(At(10), session.StartedAt);
        Assert.Equal(At(10, 50), session.EndedAt);
    }

    [Fact]
    public void IdleLongerThanThirtyMinutes_StartsANewSession()
    {
        // Amendment: "a session ends after 30 minutes with no gameplay activity".
        var sessions = SessionRules.BuildSessions([At(10), At(10, 31)]);
        Assert.Equal(2, sessions.Count);
    }

    [Fact]
    public void BriefBackgroundSwitch_IsNotAReturnVisit()
    {
        // The player backgrounded the app for five minutes: still one session, no return.
        var sessions = SessionRules.BuildSessions([At(10), At(10, 5)]);
        Assert.Single(sessions);
        Assert.Equal(0, SessionRules.Summarize(sessions).SameDayReturns);
    }

    [Fact]
    public void ComingBackAfterThirtyMinutes_SameDay_IsASameDayReturn()
    {
        var sessions = SessionRules.BuildSessions([At(10), At(11, 5)]);
        var summary = SessionRules.Summarize(sessions);
        Assert.Equal(2, summary.Sessions);
        Assert.Equal(1, summary.SameDayReturns);
    }

    [Fact]
    public void ReturningTheNextDay_IsNotASameDayReturn()
    {
        var nextDay = At(10).AddDays(1);
        Assert.False(SessionRules.IsSameDayReturn(At(23, 50), At(0, 30).AddDays(1)));
        Assert.False(SessionRules.IsSameDayReturn(At(10), nextDay));
    }

    [Fact]
    public void FirstSessionOfAPlayer_IsNeverAReturn()
    {
        var summary = SessionRules.Summarize(SessionRules.BuildSessions([At(10), At(11, 5)]));
        Assert.Equal(1, summary.SameDayReturns);
        Assert.Equal(2, summary.Sessions);
    }

    [Fact]
    public void ThreeSeparatePlayingsOnOneDay_CountTwoReturnsAndThreeSessions()
    {
        var sessions = SessionRules.BuildSessions([At(9), At(9, 10), At(11), At(15), At(15, 5)]);
        var summary = SessionRules.Summarize(sessions);
        Assert.Equal(3, summary.Sessions);
        Assert.Equal(2, summary.SameDayReturns);
    }

    [Fact]
    public void IncentivizedSessions_AreCountedButNotAsVoluntary()
    {
        var sessions = SessionRules.BuildSessions([At(10)], incentivized: true);
        var summary = SessionRules.Summarize(sessions);
        Assert.Equal(1, summary.Sessions);
        Assert.Equal(0, summary.VoluntarySessions);
    }

    [Fact]
    public void UnorderedActivity_IsStillOneSession()
    {
        var sessions = SessionRules.BuildSessions([At(10, 50), At(10), At(10, 20)]);
        Assert.Single(sessions);
        Assert.Equal(At(10, 50), sessions[0].EndedAt);
    }
}