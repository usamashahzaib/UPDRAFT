using NUnit.Framework;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Pads;

namespace Hotdrop.Tests
{
    public class LaunchPadTests
    {
        static PadConfig NewConfig()
        {
            var c = ScriptableObject.CreateInstance<PadConfig>();
            c.armingSeconds = 0.75f;
            c.lifetimeSeconds = 18f;
            c.health = 60f;
            c.launchHorizontalVelocity = 14f;
            c.launchUpwardVelocity = 12f;
            c.rechargeSeconds = 16f;
            c.perPlayerActivationLockoutSeconds = 1f;
            c.maxActivePadsPerMatch = 6;
            return c;
        }

        [Test]
        public void Pad_StartsArming_ThenBecomesArmed()
        {
            var pad = new LaunchPad(NewConfig(), ownerId: 1, teamId: 0, Vector3.zero, Vector3.forward);
            Assert.AreEqual(PadState.Arming, pad.State);
            Assert.IsFalse(pad.CanBeUsed);

            pad.Tick(0.8f);

            Assert.AreEqual(PadState.Armed, pad.State);
            Assert.IsTrue(pad.CanBeUsed);
        }

        [Test]
        public void Pad_ExpiresAfterLifetime()
        {
            var pad = new LaunchPad(NewConfig(), 1, 0, Vector3.zero, Vector3.forward);
            pad.Tick(18.1f);
            Assert.AreEqual(PadState.Expired, pad.State);
        }

        [Test]
        public void Pad_DestroyedWhenHealthReachesZero()
        {
            var pad = new LaunchPad(NewConfig(), 1, 0, Vector3.zero, Vector3.forward);
            pad.ApplyDamage(60f);
            Assert.AreEqual(PadState.Destroyed, pad.State);
        }

        [Test]
        public void LaunchVelocity_CombinesConfiguredHorizontalAndUpward()
        {
            var config = NewConfig();
            var pad = new LaunchPad(config, 1, 0, Vector3.zero, Vector3.forward);
            var velocity = pad.GetLaunchVelocity();

            Assert.AreEqual(config.launchHorizontalVelocity, velocity.z, 0.001f);
            Assert.AreEqual(config.launchUpwardVelocity, velocity.y, 0.001f);
        }

        [Test]
        public void Pad_UsableByBothTeams_WhenArmed()
        {
            var config = NewConfig();
            var pad = new LaunchPad(config, ownerId: 1, teamId: 0, Vector3.zero, Vector3.forward);
            pad.Tick(0.8f); // Arm pad

            Assert.AreEqual(PadState.Armed, pad.State);
            // Owner team (team 0) and enemy team (team 1) can both use the armed pad
            Assert.IsTrue(pad.CanBeUsed, "Pad must be usable by friendly and enemy players alike");
        }

        [Test]
        public void PlayerPadManager_ReplacingPad_RemovesPrevious()
        {
            var manager = new PlayerPadManager(NewConfig());
            var first = manager.PlaceNew(1, 0, Vector3.zero, Vector3.forward);
            Assert.IsNotNull(first);
            Assert.IsFalse(manager.HasCharge); // consumed the single charge

            // No charge left, so a second placement before recharge must fail.
            var second = manager.PlaceNew(1, 0, Vector3.one, Vector3.forward);
            Assert.IsNull(second);
            Assert.AreSame(first, manager.ActivePad);
        }

        [Test]
        public void PlayerPadManager_RechargesAfterConfiguredDelay()
        {
            var manager = new PlayerPadManager(NewConfig());
            manager.PlaceNew(1, 0, Vector3.zero, Vector3.forward);
            Assert.IsFalse(manager.HasCharge);

            manager.Tick(16.1f);

            Assert.IsTrue(manager.HasCharge);
        }

        [Test]
        public void PlayerPadManager_ReplacingAfterRecharge_RetiresPreviousPad()
        {
            var manager = new PlayerPadManager(NewConfig());
            var first = manager.PlaceNew(1, 0, Vector3.zero, Vector3.forward);
            manager.Tick(16.1f);

            var second = manager.PlaceNew(1, 0, Vector3.one, Vector3.forward);

            Assert.IsNotNull(second);
            Assert.AreEqual(PadState.Expired, first.State);
            Assert.AreSame(second, manager.ActivePad);
        }

        [Test]
        public void PlayerPadManager_CanKeepReplacingAfterSixPlacements()
        {
            var manager = new PlayerPadManager(NewConfig());
            for (int i = 0; i < 6; i++)
            {
                Assert.IsNotNull(manager.PlaceNew(1, 0, Vector3.right * i, Vector3.forward));
                manager.Tick(16.1f);
            }

            Assert.IsNotNull(manager.PlaceNew(1, 0, Vector3.right * 7, Vector3.forward));
            Assert.AreEqual(7, manager.PlacementsThisMatch);
        }
    }
}


