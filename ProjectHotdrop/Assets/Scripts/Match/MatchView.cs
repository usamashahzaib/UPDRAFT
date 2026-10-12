using System.Collections.Generic;
using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Match
{
    /// <summary>What presentation (HUD, pings, zone visuals) may read about one player.</summary>
    public interface IMatchParticipant
    {
        int ParticipantId { get; }
        string DisplayName { get; }
        int TeamId { get; }
        bool IsBot { get; }
        bool IsAlive { get; }
        bool IsConnected { get; }
        float HealthFraction { get; }
        float RespawnRemaining { get; }
        int Eliminations { get; }
        int Deaths { get; }
        int PadLaunches { get; }
        bool HasPadCharge { get; }
        float PadRechargeRemaining { get; }
        float ObjectiveSeconds { get; }
        Vector3 Position { get; }
        Vector3 FacingForward { get; }
    }

    /// <summary>
    /// Read-only view of a running match. Offline practice (MatchController) and
    /// the networked match (NetMatch) both implement it, so presentation code is
    /// identical whichever authority runs the rules (spec section 21).
    /// </summary>
    public interface IMatchView
    {
        MatchRulesConfig Rules { get; }
        MapConfig MapConfig { get; }
        MatchPhase CurrentPhase { get; }
        float MatchTime { get; }
        bool IsEndgame { get; }
        string StudyCohort { get; }
        bool PadsEnabled { get; }
        ZoneId ScoringZone { get; }
        Vector3 ActiveObjectivePosition { get; }
        bool IsLiveMatch { get; }
        IMatchParticipant LocalParticipant { get; }
        IReadOnlyList<IMatchParticipant> Participants { get; }
        IMatchParticipant SpectatedTeammate { get; }
    }

    /// <summary>Optional label for local drill objectives; Relay keeps its actual zone name.</summary>
    public interface IObjectivePresentation
    {
        string ActiveObjectiveLabel { get; }
    }

    public static class MatchView
    {
        public static IMatchView Current { get; private set; }

        public static void Register(IMatchView view) => Current = view;

        public static void Unregister(IMatchView view)
        {
            if (ReferenceEquals(Current, view)) Current = null;
        }

        public static bool IsFriendly(int teamId) => teamId == (Current?.LocalParticipant?.TeamId ?? 0);
    }
}


