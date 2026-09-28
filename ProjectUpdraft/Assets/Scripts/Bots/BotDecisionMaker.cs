using UnityEngine;
using Updraft.Config;

namespace Updraft.Bots
{
    public enum BotIntent { RotateToObjective, EngageEnemy, Retreat }

    public struct BotDecisionInputs
    {
        public float currentHealthFraction;
        public bool hasVisibleEnemy;
        public Vector3 enemyPosition;
        public Vector3 selfPosition;
        public Vector3 activeObjectivePosition;
    }

    /// <summary>
    /// Spec section 12: attack/rotate toward the objective, retreat when hurt,
    /// avoid perfectly synchronized team focus fire, imperfect tracking rather
    /// than secretly reduced damage, never tuned to guarantee wins. This is
    /// the decision layer only — it picks an intent; movement/aim execution
    /// reuses the same PlayerMovementSimulator/ShotResolver humans use.
    /// </summary>
    public class BotDecisionMaker
    {
        const float Epsilon = 1e-5f;

        readonly BotConfig _config;
        readonly System.Random _rng;

        BotIntent _committed;
        bool _hasCommitted;
        BotIntent _pending;
        bool _hasPending;
        float _reactionRemaining;

        public BotDecisionMaker(BotConfig config, int seed)
        {
            _config = config;
            _rng = new System.Random(seed);
        }

        /// <summary>The intent the bot is acting on right now.</summary>
        public BotIntent Committed => _committed;

        /// <summary>
        /// Call every tick. Reaction delay (easy ~600 ms / standard ~350 ms) is a
        /// real hold: when the situation changes, the bot keeps acting on its
        /// previous intent until the delay has elapsed, like a human's reflexes
        /// lagging a stimulus. A change that reverts before the delay ends is
        /// never acted on.
        /// </summary>
        public BotIntent Tick(BotDecisionInputs inputs, float deltaTime, bool isEasyDifficulty)
        {
            BotIntent desired = Decide(inputs);

            if (!_hasCommitted)
            {
                // A freshly spawned bot starts on its first read of the situation.
                _committed = desired;
                _hasCommitted = true;
                return _committed;
            }

            if (desired == _committed)
            {
                _hasPending = false;
                return _committed;
            }

            if (!_hasPending || desired != _pending)
            {
                // New stimulus: start the reaction clock on the following ticks.
                _pending = desired;
                _hasPending = true;
                _reactionRemaining = ReactionSeconds(isEasyDifficulty);
                return _committed;
            }

            _reactionRemaining -= deltaTime;
            if (_reactionRemaining <= Epsilon)
            {
                _committed = _pending;
                _hasPending = false;
            }
            return _committed;
        }

        /// <summary>Forget the current intent (e.g. after respawning).</summary>
        public void Reset()
        {
            _hasCommitted = false;
            _hasPending = false;
        }

        public float ReactionSeconds(bool isEasyDifficulty) =>
            isEasyDifficulty ? _config.easyPracticeReactionSeconds : _config.standardReactionSeconds;

        BotIntent Decide(BotDecisionInputs inputs)
        {
            if (inputs.currentHealthFraction <= _config.retreatHealthFraction)
                return BotIntent.Retreat;

            if (inputs.hasVisibleEnemy)
                return BotIntent.EngageEnemy;

            return BotIntent.RotateToObjective;
        }

        /// <summary>
        /// Spec section 12: "avoid perfectly synchronized team focus fire."
        /// Extra delay (0 when this bot does not desync) added to a new target's reaction.
        /// </summary>
        public float FocusFireDesyncSeconds()
        {
            if (_rng.NextDouble() >= _config.focusFireDesyncChance) return 0f;
            return Mathf.Lerp(_config.focusFireDesyncMinSeconds, _config.focusFireDesyncMaxSeconds, (float)_rng.NextDouble());
        }
    }
}
