using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Weapons
{
    public enum MovementState { StationaryGrounded, MovingGrounded, Airborne }

    public struct PelletResult
    {
        public Vector3 direction;
    }

    /// <summary>
    /// Spec section 8. Deterministic given the same seed, so the server and a
    /// reconciling client compute identical spread patterns (server is authoritative
    /// per spec section 22 — this resolver is what the server actually evaluates).
    /// </summary>
    public static class ShotResolver
    {
        public static float GetSpreadHalfAngle(WeaponDefinition weapon, MovementState state) => state switch
        {
            MovementState.StationaryGrounded => weapon.spreadStationaryGrounded,
            MovementState.MovingGrounded => weapon.spreadMovingGrounded,
            _ => weapon.spreadAirborne,
        };

        /// <summary>
        /// Generates pellet directions using a server-issued shot seed. Scattergun
        /// pellets use an evenly distributed rotation (spec: "avoid extreme random
        /// clustering") rather than pure uniform random sampling.
        /// </summary>
        public static PelletResult[] ResolvePellets(WeaponDefinition weapon, Vector3 aimDirection, uint shotSeed, MovementState state)
        {
            var results = new PelletResult[weapon.pelletsPerShot];
            ResolvePellets(weapon, aimDirection, shotSeed, state, results);
            return results;
        }

        public static int ResolvePellets(WeaponDefinition weapon, Vector3 aimDirection, uint shotSeed, MovementState state, PelletResult[] results)
        {
            int count = Mathf.Min(weapon.pelletsPerShot, results.Length);
            float halfAngle = GetSpreadHalfAngle(weapon, state);
            uint rng = shotSeed == 0 ? 0x9E3779B9u : shotSeed;
            aimDirection = aimDirection.normalized;
            // Any axis perpendicular to the aim works; fall back when aiming straight up/down.
            Vector3 tiltAxis = Vector3.Cross(aimDirection, Vector3.up);
            if (tiltAxis.sqrMagnitude < 1e-6f) tiltAxis = Vector3.Cross(aimDirection, Vector3.right);
            tiltAxis.Normalize();

            // Even angular rotation offset per pellet plus a seeded jitter keeps the
            // pattern from clustering while still varying shot-to-shot.
            float baseRotation = Next01(ref rng) * 360f;

            for (int i = 0; i < count; i++)
            {
                float ring = weapon.pelletsPerShot > 1 ? Mathf.Sqrt((i + 0.5f) / weapon.pelletsPerShot) : Next01(ref rng);

                float angleFromCenter = ring * halfAngle;
                float rotationAroundAxis = baseRotation + (360f / Mathf.Max(1, weapon.pelletsPerShot)) * i;

                Quaternion spreadRot = Quaternion.AngleAxis(rotationAroundAxis, aimDirection) *
                                       Quaternion.AngleAxis(angleFromCenter, tiltAxis);

                results[i] = new PelletResult { direction = spreadRot * aimDirection };
            }

            return count;
        }

        static float Next01(ref uint state)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (state & 0x00FFFFFFu) / 16777216f;
        }

        public static float GetDamage(WeaponDefinition weapon, float distance, bool isHeadshot)
        {
            float baseDamage = weapon.GetDamageAtDistance(distance);
            return isHeadshot ? baseDamage * weapon.headMultiplier : baseDamage;
        }
    }
}


