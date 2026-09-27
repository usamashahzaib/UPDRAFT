using Updraft.Config;

namespace Updraft.Pads
{
    /// <summary>
    /// Spec section 7 ownership rules: one charge, 16s recharge, death does not
    /// refresh cooldown, placing a replacement removes the owner's previous pad,
    /// one-second per-player activation lockout.
    /// </summary>
    public class PlayerPadManager
    {
        readonly PadConfig _config;

        public LaunchPad ActivePad { get; private set; }
        public bool HasCharge { get; private set; } = true;

        float _rechargeRemaining;
        float _activationLockoutRemaining;

        public PlayerPadManager(PadConfig config)
        {
            _config = config;
        }

        public void Tick(float deltaTime)
        {
            ActivePad?.Tick(deltaTime);

            if (ActivePad != null &&
                (ActivePad.State == PadState.Destroyed || ActivePad.State == PadState.Expired))
            {
                ActivePad = null;
            }

            if (!HasCharge)
            {
                _rechargeRemaining -= deltaTime;
                if (_rechargeRemaining <= 0f) HasCharge = true;
            }

            if (_activationLockoutRemaining > 0f)
                _activationLockoutRemaining -= deltaTime;
        }

        /// <summary>Called once a validated placement lands. Replaces any existing pad owned by this player.</summary>
        public LaunchPad PlaceNew(int ownerId, int teamId, UnityEngine.Vector3 position, UnityEngine.Vector3 forward)
        {
            if (!HasCharge) return null;

            ActivePad = new LaunchPad(_config, ownerId, teamId, position, forward);
            HasCharge = false;
            _rechargeRemaining = _config.rechargeSeconds;
            return ActivePad;
        }

        public bool CanActivate => _activationLockoutRemaining <= 0f;

        public void NotifyActivated()
        {
            _activationLockoutRemaining = _config.perPlayerActivationLockoutSeconds;
        }
    }
}
