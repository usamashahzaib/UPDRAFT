using UnityEngine;
using Updraft.Config;

namespace Updraft.Movement
{
    public struct MovementInput
    {
        public Vector2 moveAxis; // -1..1 on each axis, local to facing
        public bool jumpOrLaunchPressed;
        public bool isFiring;
    }

    /// <summary>
    /// Server-authoritative ground/air movement matching spec section 6 exactly.
    /// This is the math only — no Unity CharacterController/Fusion KCC
    /// dependency — so it can be ticked identically by the dedicated server,
    /// by offline practice, and by client-side prediction. A thin adapter
    /// (built once Unity's Simple KCC is available, per section 21) resolves
    /// the resulting velocity against real scene collision.
    /// </summary>
    public class PlayerMovementSimulator
    {
        readonly MovementConfig _config;

        public Vector3 Velocity { get; private set; }
        public bool IsGrounded { get; set; } = true;
        public bool IsLaunched { get; private set; }

        public PlayerMovementSimulator(MovementConfig config)
        {
            _config = config;
        }

        public void Tick(MovementInput input, Vector3 facingForward, Vector3 facingRight, float deltaTime)
        {
            float targetSpeed = input.isFiring ? _config.speedWhileFiring : _config.groundSpeed;

            Vector3 wishDir = (facingForward * input.moveAxis.y + facingRight * input.moveAxis.x);
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            Vector3 currentHorizontal = new Vector3(Velocity.x, 0f, Velocity.z);
            Vector3 newHorizontal = currentHorizontal;

            if (!IsLaunched)
            {
                Vector3 targetHorizontalVelocity = wishDir * targetSpeed;
                newHorizontal = Vector3.MoveTowards(
                    currentHorizontal, targetHorizontalVelocity, _config.groundAcceleration * deltaTime);
            }

            float verticalVelocity = Velocity.y;

            if (IsGrounded)
            {
                IsLaunched = false;
                verticalVelocity = 0f;
                if (input.jumpOrLaunchPressed)
                    verticalVelocity = _config.jumpUpwardVelocity;
            }
            else
            {
                verticalVelocity -= _config.gravity * deltaTime;
            }

            Velocity = new Vector3(newHorizontal.x, verticalVelocity, newHorizontal.z);
        }

        /// <summary>Spec section 7: "Launch velocity replaces current velocity; it never stacks."</summary>
        public void ApplyLaunchVelocity(Vector3 launchVelocity)
        {
            Velocity = launchVelocity;
            IsGrounded = false;
            IsLaunched = true;
        }

        /// <summary>Spec section 7: limited air steering while preserving launch horizontal speed.</summary>
        public void ApplyAirSteer(Vector3 desiredHorizontalDir, float airSteerDegreesPerSecond, float deltaTime)
        {
            if (IsGrounded) return;

            Vector3 currentHorizontal = new Vector3(Velocity.x, 0f, Velocity.z);
            float speed = currentHorizontal.magnitude;
            if (speed < 0.01f || desiredHorizontalDir.sqrMagnitude < 0.01f) return;

            float maxDegrees = airSteerDegreesPerSecond * deltaTime;
            Vector3 newDir = Vector3.RotateTowards(
                currentHorizontal.normalized, desiredHorizontalDir.normalized, maxDegrees * Mathf.Deg2Rad, 0f);

            Vector3 newHorizontal = newDir * speed; // horizontal speed preserved, only direction steers
            Velocity = new Vector3(newHorizontal.x, Velocity.y, newHorizontal.z);
        }

        public void Reset()
        {
            Velocity = Vector3.zero;
            IsGrounded = true;
            IsLaunched = false;
        }
    }
}
