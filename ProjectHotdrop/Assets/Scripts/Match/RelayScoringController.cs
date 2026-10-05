using Hotdrop.Config;

namespace Hotdrop.Match
{
    public enum TeamId { TeamA, TeamB }
    public enum MatchOutcome { InProgress, TeamAWins, TeamBWins, Draw }

    /// <summary>How a finished match ended. Presentation uses this instead of guessing from scores (C14).</summary>
    public enum MatchEndReason { None, ScoreLimit, TimeExpired, OvertimeHold, OvertimeDraw }

    /// <summary>
    /// Pure, engine-agnostic implementation of spec section 4 (Relay mode).
    /// Runs on the authoritative server tick (see spec section 22, step 6:
    /// "Evaluate occupancy and scoring"). No Unity/Fusion dependency so it can
    /// be unit tested and reused between local practice and the dedicated server.
    /// </summary>
    public class RelayScoringController
    {
        readonly MatchRulesConfig _rules;

        public int ScoreA { get; private set; }
        public int ScoreB { get; private set; }
        public MatchOutcome Outcome { get; private set; } = MatchOutcome.InProgress;
        public MatchEndReason EndReason { get; private set; } = MatchEndReason.None;
        public bool IsInOvertime => _inOvertime;

        // Continuous occupation timer, reset whenever occupation becomes invalid,
        // contested, or the controlling team changes (spec: "resets the incomplete
        // one-second scoring interval").
        float _occupationSeconds;
        TeamId? _controllingTeam;

        // Overtime hold tracking (first to 3 continuous uncontested seconds wins).
        bool _inOvertime;
        float _overtimeElapsed;
        float _overtimeHoldSeconds;
        TeamId? _overtimeHoldingTeam;

        public RelayScoringController(MatchRulesConfig rules)
        {
            _rules = rules;
        }

        /// <param name="teamAPresent">True if TeamA has >=1 validly-occupying player in the active zone.</param>
        /// <param name="teamBPresent">True if TeamB has >=1 validly-occupying player in the active zone.</param>
        /// <param name="matchTime">Elapsed regulation time in seconds (not used once overtime starts).</param>
        /// <param name="deltaTime">Server tick delta seconds.</param>
        public void Tick(bool teamAPresent, bool teamBPresent, float matchTime, float deltaTime)
        {
            if (Outcome != MatchOutcome.InProgress) return;

            if (!_inOvertime)
            {
                TickRegulationScoring(teamAPresent, teamBPresent, deltaTime);

                if (ScoreA >= _rules.scoreToWin) { Outcome = MatchOutcome.TeamAWins; EndReason = MatchEndReason.ScoreLimit; return; }
                if (ScoreB >= _rules.scoreToWin) { Outcome = MatchOutcome.TeamBWins; EndReason = MatchEndReason.ScoreLimit; return; }

                if (matchTime >= _rules.regulationDurationSeconds)
                {
                    if (ScoreA > ScoreB) { Outcome = MatchOutcome.TeamAWins; EndReason = MatchEndReason.TimeExpired; }
                    else if (ScoreB > ScoreA) { Outcome = MatchOutcome.TeamBWins; EndReason = MatchEndReason.TimeExpired; }
                    else StartOvertime();
                }
            }
            else
            {
                TickOvertime(teamAPresent, teamBPresent, deltaTime);
            }
        }

        void TickRegulationScoring(bool teamAPresent, bool teamBPresent, float deltaTime)
        {
            TeamId? current = teamAPresent && teamBPresent ? (TeamId?)null // contested: no scoring
                             : teamAPresent ? TeamId.TeamA
                             : teamBPresent ? TeamId.TeamB
                             : (TeamId?)null; // empty: no scoring

            if (current != _controllingTeam)
            {
                _controllingTeam = current;
                _occupationSeconds = 0f;
            }

            if (current == null) return;

            _occupationSeconds += deltaTime;

            // Award one point per continuous full second of valid uncontested occupation.
            while (_occupationSeconds >= 1f)
            {
                _occupationSeconds -= 1f;
                if (current == TeamId.TeamA) ScoreA++; else ScoreB++;
            }
        }

        void StartOvertime()
        {
            _inOvertime = true;
            _overtimeElapsed = 0f;
            _overtimeHoldSeconds = 0f;
            _overtimeHoldingTeam = null;
        }

        void TickOvertime(bool teamAPresent, bool teamBPresent, float deltaTime)
        {
            _overtimeElapsed += deltaTime;

            TeamId? current = teamAPresent && teamBPresent ? (TeamId?)null
                             : teamAPresent ? TeamId.TeamA
                             : teamBPresent ? TeamId.TeamB
                             : (TeamId?)null;

            if (current != _overtimeHoldingTeam)
            {
                _overtimeHoldingTeam = current;
                _overtimeHoldSeconds = 0f;
            }

            if (current != null)
            {
                _overtimeHoldSeconds += deltaTime;
                if (_overtimeHoldSeconds >= _rules.overtimeHoldSeconds)
                {
                    Outcome = current == TeamId.TeamA ? MatchOutcome.TeamAWins : MatchOutcome.TeamBWins;
                    EndReason = MatchEndReason.OvertimeHold;
                    return;
                }
            }

            if (_overtimeElapsed >= _rules.maxOvertimeSeconds)
            {
                Outcome = MatchOutcome.Draw;
                EndReason = MatchEndReason.OvertimeDraw;
            }
        }
    }
}


