using NUnit.Framework;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Movement;

namespace Hotdrop.Tests
{
    public class PlayerMovementSimulatorTests
    {
        static MovementConfig NewConfig()
        {
            var config = ScriptableObject.CreateInstance<MovementConfig>();
            config.groundSpeed = 6f;
            config.speedWhileFiring = 5.1f;
            config.groundAcceleration = 35f;
            config.gravity = 20f;
            config.jumpUpwardVelocity = 6f;
            return config;
        }

        [Test]
        public void Launch_ReplacesVelocity_AndPreservesHorizontalSpeedDuringTick()
        {
            var movement = new PlayerMovementSimulator(NewConfig());
            movement.ApplyLaunchVelocity(new Vector3(0f, 12f, 14f));

            movement.Tick(new MovementInput { moveAxis = Vector2.left }, Vector3.forward, Vector3.right, 0.1f);

            Assert.AreEqual(14f, new Vector2(movement.Velocity.x, movement.Velocity.z).magnitude, 0.001f);
            Assert.AreEqual(10f, movement.Velocity.y, 0.001f);
        }

        [Test]
        public void AirSteer_RotatesWithoutChangingHorizontalSpeed()
        {
            var movement = new PlayerMovementSimulator(NewConfig());
            movement.ApplyLaunchVelocity(new Vector3(0f, 12f, 14f));

            movement.ApplyAirSteer(Vector3.right, 30f, 1f);

            Assert.AreEqual(14f, new Vector2(movement.Velocity.x, movement.Velocity.z).magnitude, 0.001f);
            Assert.Greater(movement.Velocity.x, 0f);
        }
    }
}


