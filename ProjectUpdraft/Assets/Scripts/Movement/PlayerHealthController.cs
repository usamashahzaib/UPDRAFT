using Updraft.Config;

namespace Updraft.Movement
{
    /// <summary>
    /// Spec section 6, health recovery rules. Pure logic — the server calls
    /// this authoritatively; the client only predicts for feedback.
    /// </summary>
    public class PlayerHealthController
    {
        readonly MovementConfig _config;

        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;

        float _timeSinceLastDamage;

        public PlayerHealthController(MovementConfig config)
        {
            _config = config;
            CurrentHealth = config.maxHealth;
            _timeSinceLastDamage = float.MaxValue;
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive) return;
            CurrentHealth = UnityEngine.Mathf.Max(0f, CurrentHealth - amount);
            _timeSinceLastDamage = 0f;
        }

        public void ResetForRespawn()
        {
            CurrentHealth = _config.maxHealth;
            _timeSinceLastDamage = float.MaxValue;
        }

        public void Tick(float deltaTime)
        {
            if (!IsAlive) return;

            float previousTimeSinceDamage = _timeSinceLastDamage;
            _timeSinceLastDamage += deltaTime;

            if (CurrentHealth >= _config.maxHealth) return;

            // Only the portion of this tick that falls after the recovery delay
            // counts as regen time — a single large deltaTime that straddles the
            // delay boundary must not be credited as a full second of healing.
            float regenSeconds = _timeSinceLastDamage - UnityEngine.Mathf.Max(previousTimeSinceDamage, _config.recoveryDelayAfterDamage);
            if (regenSeconds > 0f)
            {
                CurrentHealth = UnityEngine.Mathf.Min(
                    _config.maxHealth,
                    CurrentHealth + _config.recoveryPerSecond * regenSeconds);
            }
        }
    }
}
