using NUnit.Framework;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Weapons;

namespace Hotdrop.Tests
{
    public class ShotResolverTests
    {
        static WeaponDefinition NewArc()
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.bodyDamage = 18f;
            w.pelletsPerShot = 1;
            w.fullDamageDistance = 20f;
            w.falloffEndpointDistance = 45f;
            w.damageAtFalloffEndpoint = 10f;
            w.maxRange = 55f;
            w.headMultiplier = 1.25f;
            return w;
        }

        static WeaponDefinition NewBrick()
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.bodyDamage = 9f;
            w.pelletsPerShot = 8;
            w.fullDamageDistance = 7f;
            w.falloffEndpointDistance = 20f;
            w.damageAtFalloffEndpoint = 3f;
            w.maxRange = 25f;
            w.headMultiplier = 1.25f;
            return w;
        }

        [Test]
        public void FullDamage_WithinFullDamageDistance()
        {
            var arc = NewArc();
            Assert.AreEqual(18f, arc.GetDamageAtDistance(10f), 0.001f);
            Assert.AreEqual(18f, arc.GetDamageAtDistance(20f), 0.001f);
        }

        [Test]
        public void DamageFallsLinearly_BetweenThresholds()
        {
            var arc = NewArc();
            // Halfway between 20m and 45m -> halfway between 18 and 10 damage.
            float midDistance = (20f + 45f) / 2f;
            float expected = (18f + 10f) / 2f;
            Assert.AreEqual(expected, arc.GetDamageAtDistance(midDistance), 0.01f);
        }

        [Test]
        public void MinimumDamage_HoldsPastFalloffEndpointUntilMaxRange()
        {
            var arc = NewArc();
            Assert.AreEqual(10f, arc.GetDamageAtDistance(50f), 0.001f);
        }

        [Test]
        public void NoDamage_BeyondMaxRange()
        {
            var arc = NewArc();
            Assert.AreEqual(0f, arc.GetDamageAtDistance(60f));
        }

        [Test]
        public void Brick_CannotOneShotAFullHealthPlayer_EvenAllHeadshots()
        {
            var brick = NewBrick();
            float maxPossibleDamage = 0f;
            for (int i = 0; i < brick.pelletsPerShot; i++)
                maxPossibleDamage += ShotResolver.GetDamage(brick, distance: 0f, isHeadshot: true);

            Assert.Less(maxPossibleDamage, 100f, "BRICK must never one-shot a 100 HP player per spec section 8.");
        }

        [Test]
        public void HeadshotMultiplier_AppliesToBaseDamage()
        {
            var arc = NewArc();
            float body = ShotResolver.GetDamage(arc, 10f, isHeadshot: false);
            float head = ShotResolver.GetDamage(arc, 10f, isHeadshot: true);
            Assert.AreEqual(body * 1.25f, head, 0.001f);
        }

        [Test]
        public void Pellets_AreGeneratedForEachConfiguredCount()
        {
            var brick = NewBrick();
            var pellets = ShotResolver.ResolvePellets(brick, Vector3.forward, shotSeed: 42, MovementState.StationaryGrounded);
            Assert.AreEqual(8, pellets.Length);
        }

        [Test]
        public void SameSeed_ProducesIdenticalPellets_ForServerClientAgreement()
        {
            var brick = NewBrick();
            var a = ShotResolver.ResolvePellets(brick, Vector3.forward, 7, MovementState.MovingGrounded);
            var b = ShotResolver.ResolvePellets(brick, Vector3.forward, 7, MovementState.MovingGrounded);

            for (int i = 0; i < a.Length; i++)
                Assert.AreEqual(a[i].direction, b[i].direction);
        }
    }
}


