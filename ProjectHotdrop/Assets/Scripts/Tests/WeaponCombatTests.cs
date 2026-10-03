using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Weapons;

namespace Hotdrop.Tests
{
    /// <summary>Gate B, spec section 7: cadence, reload, spread, falloff and the BRICK ceiling, on the shipped assets.</summary>
    public class WeaponCombatTests
    {
        static WeaponDefinition Weapon(string name) =>
            AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"Assets/Resources/Config/{name}.asset");

        static readonly string[] All = { "Weapon_ARC", "Weapon_ZIP", "Weapon_BRICK" };

        // ---- Cadence ----

        [Test]
        public void HeldTrigger_FiresAtConfiguredRpm_AtCommonFrameRates([ValueSource(nameof(All))] string name, [Values(30, 60, 45)] int fps)
        {
            var w = Weapon(name);
            var state = new WeaponState();
            state.Equip(w);
            float dt = 1f / fps;
            // Hold long enough to empty all but the last round (the first shot fires at t = 0).
            int wanted = w.magazineSize - 1;
            float holdSeconds = (wanted - 1) * w.SecondsPerShot + 0.5f * dt;
            int shots = 0;
            for (float t = 0f; t < holdSeconds; t += dt) shots += state.Tick(dt, true);
            int expected = Mathf.FloorToInt(holdSeconds / w.SecondsPerShot) + 1;
            Assert.That(shots, Is.InRange(expected - 1, expected), $"{w.displayName} at {fps} fps fired {shots}, expected ~{expected}");
        }

        [Test]
        public void Tap_FiresImmediately_AfterIdle_ButNeverFasterThanCadence([ValueSource(nameof(All))] string name)
        {
            var w = Weapon(name);
            var state = new WeaponState();
            state.Equip(w);
            Assert.AreEqual(1, state.Tick(0.016f, true), "The first press fires at once.");
            state.Tick(0.016f, false);
            Assert.AreEqual(0, state.Tick(0.016f, true), "A second tap inside the interval must not fire.");
            state.Tick(w.SecondsPerShot, false);
            Assert.AreEqual(1, state.Tick(0.016f, true), "After a full interval of idle the next tap fires at once.");
        }

        [Test]
        public void IdleTrigger_NeverBanksShots()
        {
            var w = Weapon("Weapon_ZIP");
            var state = new WeaponState();
            state.Equip(w);
            state.Tick(10f, false);
            Assert.AreEqual(1, state.Tick(0.02f, true), "Ten idle seconds must not become a burst.");
        }

        // ---- Reload ----

        [Test]
        public void EmptyMagazine_AutoReloads_AndReloadBlocksFiring([ValueSource(nameof(All))] string name)
        {
            var w = Weapon(name);
            var state = new WeaponState();
            state.Equip(w);
            int fired = 0;
            for (int i = 0; i < 2000 && !state.IsReloading; i++) fired += state.Tick(0.01f, true);
            Assert.AreEqual(w.magazineSize, fired);
            Assert.IsTrue(state.IsReloading, "Empty magazine reloads automatically.");

            float elapsed = 0f;
            int firedWhileReloading = 0;
            while (state.IsReloading && elapsed < 10f)
            {
                int shots = state.Tick(0.01f, true);
                elapsed += 0.01f;
                if (state.IsReloading) firedWhileReloading += shots;
            }
            Assert.AreEqual(0, firedWhileReloading, "Reload blocks firing.");
            Assert.AreEqual(w.reloadSeconds, elapsed, 0.011f);
            // The step that completes the reload may already fire with the time left over.
            Assert.That(state.Ammo, Is.InRange(w.magazineSize - 1, w.magazineSize), "Magazine refilled after reload.");
        }

        [Test]
        public void ManualReload_OnlyWhenNotFull_AndUnlimitedReserve()
        {
            var state = new WeaponState();
            state.Equip(Weapon("Weapon_ARC"));
            Assert.IsFalse(state.RequestReload(), "A full magazine does not reload.");
            state.Tick(0.01f, true);
            Assert.IsTrue(state.RequestReload());
            Assert.IsFalse(state.RequestReload(), "Already reloading.");
            for (int cycle = 0; cycle < 20; cycle++)
            {
                state.Tick(5f, false);
                Assert.AreEqual(24, state.Ammo, "Reserve ammo is unlimited.");
                state.Tick(0.01f, true);
                state.RequestReload();
            }
        }

        [Test]
        public void ReequippingWeapon_ResetsMagazineAndReload()
        {
            var state = new WeaponState();
            state.Equip(Weapon("Weapon_ARC"));
            state.Tick(0.01f, true);
            state.RequestReload();
            state.Equip(Weapon("Weapon_BRICK"));
            Assert.AreEqual(6, state.Ammo);
            Assert.IsFalse(state.IsReloading);
        }

        // ---- Damage ----

        [TestCase("Weapon_ARC", 20f, 18f)]
        [TestCase("Weapon_ARC", 32.5f, 14f)]
        [TestCase("Weapon_ARC", 45f, 10f)]
        [TestCase("Weapon_ARC", 55.01f, 0f)]
        [TestCase("Weapon_ZIP", 10f, 13f)]
        [TestCase("Weapon_ZIP", 19f, 9.5f)]
        [TestCase("Weapon_ZIP", 28f, 6f)]
        [TestCase("Weapon_ZIP", 35.01f, 0f)]
        [TestCase("Weapon_BRICK", 7f, 9f)]
        [TestCase("Weapon_BRICK", 13.5f, 6f)]
        [TestCase("Weapon_BRICK", 20f, 3f)]
        [TestCase("Weapon_BRICK", 25.01f, 0f)]
        public void Falloff_IsLinearBetweenThresholds_ExactValues(string name, float distance, float expected)
        {
            Assert.AreEqual(expected, Weapon(name).GetDamageAtDistance(distance), 1e-4f);
        }

        [Test]
        public void Headshots_ApplyExactly125Percent([ValueSource(nameof(All))] string name)
        {
            var w = Weapon(name);
            foreach (float d in new[] { 1f, w.fullDamageDistance + 1f, w.falloffEndpointDistance })
                Assert.AreEqual(ShotResolver.GetDamage(w, d, false) * 1.25f, ShotResolver.GetDamage(w, d, true), 1e-4f);
        }

        [Test]
        public void Brick_CannotOneShotFullHealth_EvenAllPelletsHead()
        {
            var brick = Weapon("Weapon_BRICK");
            var movement = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Resources/Config/MovementConfig.asset");
            float max = brick.pelletsPerShot * ShotResolver.GetDamage(brick, 0f, true);
            Assert.AreEqual(90f, max, 1e-4f);
            Assert.Less(max, movement.maxHealth);
        }

        // ---- Spread ----

        [Test]
        public void Pellets_StayInsideTheStateSpreadCone([ValueSource(nameof(All))] string name,
            [Values(MovementState.StationaryGrounded, MovementState.MovingGrounded, MovementState.Airborne)] MovementState moveState)
        {
            var w = Weapon(name);
            float half = ShotResolver.GetSpreadHalfAngle(w, moveState);
            Vector3 aim = new Vector3(0.3f, 0.1f, 1f).normalized;
            for (uint seed = 1; seed < 200; seed++)
                foreach (var pellet in ShotResolver.ResolvePellets(w, aim, seed, moveState))
                    Assert.LessOrEqual(Vector3.Angle(aim, pellet.direction), half + 1e-3f);
        }

        [Test]
        public void BrickPellets_AreEvenlyDistributed_NotClustered()
        {
            var brick = Weapon("Weapon_BRICK");
            for (uint seed = 1; seed < 100; seed++)
            {
                var pellets = ShotResolver.ResolvePellets(brick, Vector3.forward, seed, MovementState.StationaryGrounded);
                float minSeparation = float.MaxValue;
                for (int i = 0; i < pellets.Length; i++)
                    for (int j = i + 1; j < pellets.Length; j++)
                        minSeparation = Mathf.Min(minSeparation, Vector3.Angle(pellets[i].direction, pellets[j].direction));
                Assert.Greater(minSeparation, brick.spreadStationaryGrounded * 0.2f, $"seed {seed} clustered pellets");
            }
        }

        [Test]
        public void Spread_IsFiniteWhenAimingStraightUpOrDown()
        {
            var brick = Weapon("Weapon_BRICK");
            foreach (var aim in new[] { Vector3.up, Vector3.down })
                foreach (var pellet in ShotResolver.ResolvePellets(brick, aim, 5, MovementState.Airborne))
                {
                    Assert.IsFalse(float.IsNaN(pellet.direction.x) || float.IsNaN(pellet.direction.y) || float.IsNaN(pellet.direction.z));
                    Assert.LessOrEqual(Vector3.Angle(aim, pellet.direction), brick.spreadAirborne + 1e-3f);
                }
        }

        [Test]
        public void Spread_OrdersStationaryMovingAirborne([ValueSource(nameof(All))] string name)
        {
            var w = Weapon(name);
            Assert.LessOrEqual(ShotResolver.GetSpreadHalfAngle(w, MovementState.StationaryGrounded), ShotResolver.GetSpreadHalfAngle(w, MovementState.MovingGrounded));
            Assert.Less(ShotResolver.GetSpreadHalfAngle(w, MovementState.MovingGrounded), ShotResolver.GetSpreadHalfAngle(w, MovementState.Airborne));
        }
    }
}


