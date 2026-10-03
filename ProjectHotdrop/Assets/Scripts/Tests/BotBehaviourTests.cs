using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Hotdrop.Bots;
using Hotdrop.Config;

namespace Hotdrop.Tests
{
    /// <summary>Gate B, spec section 12: real reaction delay, retreat when hurt, LOS-only perception.</summary>
    public class BotBehaviourTests
    {
        BotConfig config;
        const float Dt = 1f / 60f;

        [SetUp]
        public void SetUp() => config = AssetDatabase.LoadAssetAtPath<BotConfig>("Assets/Resources/Config/BotConfig.asset");

        static BotDecisionInputs Calm => new BotDecisionInputs { currentHealthFraction = 1f };
        static BotDecisionInputs EnemyVisible => new BotDecisionInputs { currentHealthFraction = 1f, hasVisibleEnemy = true };
        static BotDecisionInputs Hurt => new BotDecisionInputs { currentHealthFraction = 0.2f, hasVisibleEnemy = true };

        /// <summary>Seconds from the stimulus until the new intent is committed.</summary>
        static float ReactionTime(BotDecisionMaker bot, BotDecisionInputs stimulus, BotIntent expected, bool easy)
        {
            float t = 0f;
            while (t < 5f)
            {
                if (bot.Tick(stimulus, Dt, easy) == expected) return t;
                t += Dt;
            }
            return float.PositiveInfinity;
        }

        [TestCase(false, 0.35f)]
        [TestCase(true, 0.6f)]
        public void NewIntent_WaitsForTheConfiguredReactionDelay(bool easy, float seconds)
        {
            var bot = new BotDecisionMaker(config, 1);
            Assert.AreEqual(BotIntent.RotateToObjective, bot.Tick(Calm, Dt, easy));
            float reaction = ReactionTime(bot, EnemyVisible, BotIntent.EngageEnemy, easy);
            Assert.AreEqual(seconds, reaction, Dt + 1e-4f, "The bot must keep its old intent for the whole reaction delay.");
        }

        [Test]
        public void StimulusThatVanishesBeforeTheDelay_IsNeverActedOn()
        {
            var bot = new BotDecisionMaker(config, 2);
            bot.Tick(Calm, Dt, false);
            for (float t = 0f; t < 0.2f; t += Dt)
                Assert.AreEqual(BotIntent.RotateToObjective, bot.Tick(EnemyVisible, Dt, false));
            for (float t = 0f; t < 1f; t += Dt)
                Assert.AreEqual(BotIntent.RotateToObjective, bot.Tick(Calm, Dt, false));
        }

        [Test]
        public void Hurt_RetreatsAfterReacting_AndReturnsWhenHealed()
        {
            var bot = new BotDecisionMaker(config, 3);
            bot.Tick(EnemyVisible, Dt, false);
            Assert.AreEqual(BotIntent.EngageEnemy, bot.Committed);
            Assert.AreEqual(0.35f, ReactionTime(bot, Hurt, BotIntent.Retreat, false), Dt + 1e-4f);
            var healed = new BotDecisionInputs { currentHealthFraction = 0.31f };
            Assert.Less(ReactionTime(bot, healed, BotIntent.RotateToObjective, false), 1f);
        }

        [Test]
        public void RetreatThreshold_IsThirtyPercent()
        {
            var bot = new BotDecisionMaker(config, 4);
            Assert.AreEqual(BotIntent.Retreat, bot.Tick(new BotDecisionInputs { currentHealthFraction = 0.3f }, Dt, false));
            bot.Reset();
            Assert.AreEqual(BotIntent.RotateToObjective, bot.Tick(new BotDecisionInputs { currentHealthFraction = 0.31f }, Dt, false));
        }

        [Test]
        public void FocusFireDesync_IsBoundedAndSometimesZero()
        {
            var bot = new BotDecisionMaker(config, 5);
            int desynced = 0;
            for (int i = 0; i < 1000; i++)
            {
                float d = bot.FocusFireDesyncSeconds();
                if (d > 0f) { desynced++; Assert.That(d, Is.InRange(config.focusFireDesyncMinSeconds, config.focusFireDesyncMaxSeconds)); }
            }
            Assert.That(desynced, Is.InRange(250, 450), "About 35% of new targets are delayed.");
        }

        // ---- Line of sight (real physics) ----

        GameObject wall;

        [TearDown]
        public void TearDown() { if (wall != null) Object.DestroyImmediate(wall); }

        [Test]
        public void Sight_IsBlockedByCover_NeverAWallhack()
        {
            var physics = Physics.defaultPhysicsScene;
            Vector3 eye = new Vector3(0, 1.6f, -100f), target = new Vector3(0, 1.4f, -80f);
            int mask = BotPerception.OccluderMask;
            Assert.AreNotEqual(0, mask, "The occluder mask must never be empty.");
            Assert.IsTrue(BotPerception.CanSee(physics, eye, Vector3.forward, target, 50f, 60f, mask));

            foreach (var layer in new[] { "Cover", "Ground", "SpawnBarrier" })
            {
                if (wall != null) Object.DestroyImmediate(wall);
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.layer = LayerMask.NameToLayer(layer);
                wall.transform.position = new Vector3(0, 1.5f, -90f);
                wall.transform.localScale = new Vector3(4, 3, 0.5f);
                Physics.SyncTransforms();
                Assert.IsFalse(BotPerception.CanSee(physics, eye, Vector3.forward, target, 50f, 60f, mask), $"{layer} must block sight");
            }
        }

        [Test]
        public void Sight_IsLimitedToRangeAndViewCone()
        {
            var physics = Physics.defaultPhysicsScene;
            int mask = BotPerception.OccluderMask;
            Vector3 eye = new Vector3(0, 1.6f, -100f);
            Assert.IsFalse(BotPerception.CanSee(physics, eye, Vector3.forward, eye + Vector3.forward * 51f, 50f, 60f, mask), "beyond range");
            Assert.IsFalse(BotPerception.CanSee(physics, eye, Vector3.forward, eye - Vector3.forward * 10f, 50f, 60f, mask), "behind");
            Vector3 side = Quaternion.Euler(0, 70f, 0) * Vector3.forward * 10f;
            Assert.IsFalse(BotPerception.CanSee(physics, eye, Vector3.forward, eye + side, 50f, 60f, mask), "outside the cone");
            Vector3 inCone = Quaternion.Euler(0, 50f, 0) * Vector3.forward * 10f;
            Assert.IsTrue(BotPerception.CanSee(physics, eye, Vector3.forward, eye + inCone, 50f, 60f, mask));
        }
    }
}


