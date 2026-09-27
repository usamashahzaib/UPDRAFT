using UnityEngine;
using Updraft.Config;

namespace Updraft.Pads
{
    public enum PadState { Arming, Armed, Destroyed, Expired }

    /// <summary>
    /// Spec section 7. One instance per placed pad. Pure state/timer logic;
    /// a thin MonoBehaviour/NetworkBehaviour presents this on top.
    /// </summary>
    public class LaunchPad
    {
        readonly PadConfig _config;

        public int OwnerId { get; }
        public int TeamId { get; }
        public Vector3 Position { get; }
        public Vector3 ForwardDirection { get; }
        public float Health { get; private set; }
        public PadState State { get; private set; } = PadState.Arming;

        float _age;

        public LaunchPad(PadConfig config, int ownerId, int teamId, Vector3 position, Vector3 forward)
        {
            _config = config;
            OwnerId = ownerId;
            TeamId = teamId;
            Position = position;
            ForwardDirection = forward;
            Health = config.health;
        }

        public void Tick(float deltaTime)
        {
            if (State == PadState.Destroyed || State == PadState.Expired) return;

            _age += deltaTime;

            if (State == PadState.Arming && _age >= _config.armingSeconds)
                State = PadState.Armed;

            if (_age >= _config.lifetimeSeconds)
                State = PadState.Expired;
        }

        public void ApplyDamage(float amount)
        {
            if (State == PadState.Destroyed || State == PadState.Expired) return;
            Health -= amount;
            if (Health <= 0f) State = PadState.Destroyed;
        }

        /// <summary>Anyone (either team) can trigger this while armed. Friendly fire never damages pads.</summary>
        public bool CanBeUsed => State == PadState.Armed;

        public Vector3 GetLaunchVelocity()
        {
            Vector3 horizontal = ForwardDirection.normalized * _config.launchHorizontalVelocity;
            return horizontal + Vector3.up * _config.launchUpwardVelocity;
        }
    }
}
