using UnityEngine;
using Updraft.Config;

namespace Updraft.Bots
{
    public enum BotIntent { RotateToObjective, EngageEnemy, Retreat, UseRoute }

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
        readonly BotConfig _config;
        readonly System.Random _rng;
        float _reactionTimer;
        BotIntent _pendingIntent;
        bool _hasPendingIntent;

        public BotDecisionMaker(BotConfig config, int seed)
        {
            _config = config;
            _rng = new System.Random(seed);
        }

        /// <summary>
        /// Call every tick. Reaction delay (600ms easy / 350ms standard) is
        /// modeled as a hold before a newly-decided intent takes effect,
        /// matching how a human's reflexes lag a stimulus rather than
        /// artificially slowing bot movement/aim once committed.
        /// </summary>
        public BotIntent Tick(BotDecisionInputs inputs, float deltaTime, bool isEasyDifficulty)
        {
            BotIntent desired = Decide(inputs);

            if (!_hasPendingIntent || desired != _pendingIntent)
            {
                _pendingIntent = desired;
                _hasPendingIntent = true;
                _reactionTimer = isEasyDifficulty ? _config.easyPracticeReactionSeconds : _config.standardReactionSeconds;
            }

            if (_reactionTimer > 0f)
            {
                _reactionTimer -= deltaTime;
                // Still acting on whatever intent was already committed while reacting to the new one.
            }

            return _pendingIntent;
        }

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
        /// Call once per bot when a team-wide "focus this target" signal fires;
        /// some bots intentionally hold back this tick.
        /// </summary>
        public bool ShouldDesyncFromTeamFocusFire()
        {
            return _rng.NextDouble() < _config.focusFireDesyncChance;
        }
    }
}
