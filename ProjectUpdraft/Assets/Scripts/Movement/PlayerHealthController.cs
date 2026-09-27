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

            _timeSinceLastDamage += deltaTime;

            if (_timeSinceLastDamage >= _config.recoveryDelayAfterDamage && CurrentHealth < _config.maxHealth)
            {
                CurrentHealth = UnityEngine.Mathf.Min(
                    _config.maxHealth,
                    CurrentHealth + _config.recoveryPerSecond * deltaTime);
            }
        }
    }
}
