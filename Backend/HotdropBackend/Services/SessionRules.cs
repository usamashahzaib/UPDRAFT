namespace HotdropBackend.Services;

/// <summary>
/// Playable-session rules from the 2026-10-01 amendment (measurement section):
/// a session ends after 30 minutes with no gameplay activity, and
/// <c>same_day_return</c> is a new playable session at least 30 minutes after the
/// prior session ended. Brief background/foreground switches are therefore never a
/// return visit, and unique people are counted separately from session counts.
///
/// Deliberately pure and side-effect free so both the retention rollup and the tests
/// use one definition; there is no second place where "30 minutes" is written.
/// </summary>
public static class SessionRules
{
    /// <summary>Idle time that ends a session (amendment: 30 minutes with no gameplay activity).</summary>
    public static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromMinutes(30);

    /// <summary>Minimum gap between the end of one session and the start of a return visit.</summary>
    public static readonly TimeSpan ReturnGap = TimeSpan.FromMinutes(30);

    /// <summary>
    /// One continuous stretch of play. <paramref name="Voluntary"/> is false when any
    /// activity in the session came from a scheduled/incentivized tester, so those
    /// sessions are never counted as voluntary play.
    /// </summary>
    public sealed record PlaySession(DateTimeOffset StartedAt, DateTimeOffset EndedAt, bool Voluntary);

    /// <summary>
    /// Splits one player's activity timestamps (ascending or not) into sessions.
    /// Returns nothing when there is no activity; a single timestamp is one session.
    /// </summary>
    public static List<PlaySession> BuildSessions(IEnumerable<DateTimeOffset> activityTimes, bool incentivized = false)
    {
        var ordered = activityTimes.OrderBy(t => t).ToList();
        var sessions = new List<PlaySession>();
        if (ordered.Count == 0) return sessions;

        DateTimeOffset start = ordered[0], last = ordered[0];
        for (int i = 1; i < ordered.Count; i++)
        {
            if (ordered[i] - last > SessionIdleTimeout)
            {
                sessions.Add(new PlaySession(start, last, !incentivized));
                start = ordered[i];
            }
            last = ordered[i];
        }
        sessions.Add(new PlaySession(start, last, !incentivized));
        return sessions;
    }

    /// <summary>
    /// A return visit: the player had already stopped (<paramref name="returnStart"/> minus
    /// <paramref name="previousEnd"/> is at least the gap) and came back on the same UTC day.
    /// </summary>
    public static bool IsSameDayReturn(DateTimeOffset previousEnd, DateTimeOffset returnStart) =>
        returnStart - previousEnd >= ReturnGap &&
        DateOnly.FromDateTime(returnStart.UtcDateTime) == DateOnly.FromDateTime(previousEnd.UtcDateTime);

    /// <summary>
    /// Session rollup for one player: how many sessions they played, how many of those
    /// were same-day returns, and how many were voluntary. A player's first session is
    /// never a return, because there is no prior session to return from.
    /// </summary>
    public sealed record SessionSummary(int Sessions, int SameDayReturns, int VoluntarySessions);

    public static SessionSummary Summarize(IReadOnlyList<PlaySession> sessions)
    {
        if (sessions.Count == 0) return new SessionSummary(0, 0, 0);
        int returns = 0;
        for (int i = 1; i < sessions.Count; i++)
            if (IsSameDayReturn(sessions[i - 1].EndedAt, sessions[i].StartedAt)) returns++;
        return new SessionSummary(sessions.Count, returns, sessions.Count(s => s.Voluntary));
    }
}