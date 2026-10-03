using Hotdrop.Config;

namespace Hotdrop.Match
{
    // Spec section 5: complete match flow, server-authoritative phases.
    public enum MatchPhase { Preparation, Countdown, InProgress, Overtime, Results }

    /// <summary>
    /// Drives the phase transitions the server enforces around a Relay match:
    /// 8s preparation (no damage, no pad placement), 3s countdown, then live
    /// play, handing scoring off to RelayScoringController.
    /// </summary>
    public class MatchFlowController
    {
        readonly MatchRulesConfig _rules;

        public MatchPhase Phase { get; private set; } = MatchPhase.Preparation;
        public float PhaseElapsed { get; private set; }
        public float MatchTime { get; private set; } // only advances once InProgress/Overtime

        public MatchFlowController(MatchRulesConfig rules)
        {
            _rules = rules;
        }

        public bool DamageAllowed => Phase == MatchPhase.InProgress || Phase == MatchPhase.Overtime;
        public bool PadPlacementAllowed => Phase == MatchPhase.InProgress || Phase == MatchPhase.Overtime;
        public bool WeaponSelectAllowed => Phase == MatchPhase.Preparation;

        public void Tick(float deltaTime)
        {
            PhaseElapsed += deltaTime;

            switch (Phase)
            {
                case MatchPhase.Preparation:
                    if (PhaseElapsed >= _rules.preparationSeconds)
                        AdvanceTo(MatchPhase.Countdown);
                    break;

                case MatchPhase.Countdown:
                    if (PhaseElapsed >= _rules.countdownSeconds)
                        AdvanceTo(MatchPhase.InProgress);
                    break;

                case MatchPhase.InProgress:
                case MatchPhase.Overtime:
                    MatchTime += deltaTime;
                    break;
            }
        }

        /// <summary>Called externally once RelayScoringController reaches an outcome.</summary>
        public void NotifyMatchEnded()
        {
            AdvanceTo(MatchPhase.Results);
        }

        public void NotifyOvertimeStarted()
        {
            AdvanceTo(MatchPhase.Overtime);
        }

        void AdvanceTo(MatchPhase phase)
        {
            Phase = phase;
            PhaseElapsed = 0f;
        }
    }
}


