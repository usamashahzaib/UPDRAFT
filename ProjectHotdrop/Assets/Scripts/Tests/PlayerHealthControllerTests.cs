using NUnit.Framework;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Movement;

namespace Hotdrop.Tests
{
    public class PlayerHealthControllerTests
    {
        static MovementConfig NewConfig()
        {
            var c = ScriptableObject.CreateInstance<MovementConfig>();
            c.maxHealth = 100f;
            c.recoveryDelayAfterDamage = 5f;
            c.recoveryPerSecond = 25f;
            return c;
        }

        [Test]
        public void Damage_ReducesHealth_AndDoesNotGoNegative()
        {
            var health = new PlayerHealthController(NewConfig());
            health.ApplyDamage(150f);
            Assert.AreEqual(0f, health.CurrentHealth);
            Assert.IsFalse(health.IsAlive);
        }

        [Test]
        public void Regen_DoesNotStart_BeforeDelayElapses()
        {
            var health = new PlayerHealthController(NewConfig());
            health.ApplyDamage(50f);
            health.Tick(4.9f);
            Assert.AreEqual(50f, health.CurrentHealth, 0.01f);
        }

        [Test]
        public void Regen_StartsAfterDelay_At25PerSecond()
        {
            var health = new PlayerHealthController(NewConfig());
            health.ApplyDamage(50f);
            health.Tick(5f);   // crosses the delay
            health.Tick(1f);   // one full second of regen

            Assert.AreEqual(75f, health.CurrentHealth, 0.01f);
        }

        [Test]
        public void NewDamage_InterruptsRegenAndResetsDelay()
        {
            var health = new PlayerHealthController(NewConfig());
            health.ApplyDamage(50f);
            health.Tick(6f); // now regenerating
            health.ApplyDamage(10f); // interrupts, resets delay
            health.Tick(4.9f); // still within new delay window

            Assert.AreEqual(65f, health.CurrentHealth, 0.01f); // 100-50+25 = 75, then -10 = 65, no further regen yet
        }

        [Test]
        public void Regen_NeverExceedsMaxHealth()
        {
            var health = new PlayerHealthController(NewConfig());
            health.ApplyDamage(10f);
            health.Tick(100f); // plenty of time to fully regen
            Assert.AreEqual(100f, health.CurrentHealth, 0.01f);
        }
    }
}


