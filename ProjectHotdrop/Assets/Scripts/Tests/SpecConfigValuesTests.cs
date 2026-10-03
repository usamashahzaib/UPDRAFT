using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Tests
{
    /// <summary>
    /// Gate B: the shipped config assets (not hand-built test configs) hold the exact
    /// authoritative values from the master specification.
    /// </summary>
    public class SpecConfigValuesTests
    {
        static T Load<T>(string name) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"Assets/Resources/Config/{name}.asset");
            Assert.IsNotNull(asset, $"Missing config asset {name}");
            return asset;
        }

        [TestCase("Weapon_ARC", WeaponId.Arc, 18f, 1, 480f, 24, 1.6f, 20f, 45f, 10f, 55f, 0.45f, 0.75f, 1.2f, 0.55f)]
        [TestCase("Weapon_ZIP", WeaponId.Zip, 13f, 1, 780f, 30, 1.5f, 10f, 28f, 6f, 35f, 1.0f, 1.3f, 2.0f, 0.30f)]
        [TestCase("Weapon_BRICK", WeaponId.Brick, 9f, 8, 75f, 6, 2.0f, 7f, 20f, 3f, 25f, 4.0f, 4.0f, 4.5f, 2.0f)]
        public void Weapons_MatchSection7(string asset, WeaponId id, float body, int pellets, float rpm, int mag, float reload,
            float full, float falloffEnd, float falloffDamage, float max, float still, float moving, float air, float recoil)
        {
            var w = Load<WeaponDefinition>(asset);
            Assert.AreEqual(id, w.id);
            Assert.AreEqual(body, w.bodyDamage);
            Assert.AreEqual(pellets, w.pelletsPerShot);
            Assert.AreEqual(rpm, w.roundsPerMinute);
            Assert.AreEqual(mag, w.magazineSize);
            Assert.AreEqual(reload, w.reloadSeconds, 1e-5f);
            Assert.AreEqual(full, w.fullDamageDistance);
            Assert.AreEqual(falloffEnd, w.falloffEndpointDistance);
            Assert.AreEqual(falloffDamage, w.damageAtFalloffEndpoint);
            Assert.AreEqual(max, w.maxRange);
            Assert.AreEqual(1.25f, w.headMultiplier, 1e-5f);
            Assert.AreEqual(still, w.spreadStationaryGrounded, 1e-5f);
            Assert.AreEqual(moving, w.spreadMovingGrounded, 1e-5f);
            Assert.AreEqual(air, w.spreadAirborne, 1e-5f);
            Assert.AreEqual(recoil, w.verticalRecoilDegreesPerShot, 1e-5f);
        }

        [Test]
        public void Movement_MatchesSection5()
        {
            var m = Load<MovementConfig>("MovementConfig");
            Assert.AreEqual(100f, m.maxHealth);
            Assert.AreEqual(6f, m.groundSpeed);
            Assert.AreEqual(5.1f, m.speedWhileFiring, 1e-5f);
            Assert.AreEqual(35f, m.groundAcceleration);
            Assert.AreEqual(20f, m.gravity);
            Assert.AreEqual(6f, m.jumpUpwardVelocity);
            Assert.AreEqual(0.9f, m.jumpUpwardVelocity * m.jumpUpwardVelocity / (2f * m.gravity), 0.01f, "Jump height ~0.9 m");
            Assert.AreEqual(1.8f, m.capsuleHeight, 1e-5f);
            Assert.AreEqual(0.35f, m.capsuleRadius, 1e-5f);
            Assert.AreEqual(0.35f, m.stepHeight, 1e-5f);
            Assert.AreEqual(4f, m.respawnDelaySeconds);
            Assert.AreEqual(5f, m.recoveryDelayAfterDamage);
            Assert.AreEqual(25f, m.recoveryPerSecond);
            Assert.AreEqual(1.5f, m.respawnProtectionSeconds, 1e-5f);
        }

        [Test]
        public void Pads_MatchSection6()
        {
            var p = Load<PadConfig>("PadConfig");
            Assert.AreEqual(1, p.chargeCapacity);
            Assert.AreEqual(16f, p.rechargeSeconds);
            Assert.AreEqual(18f, p.lifetimeSeconds);
            Assert.AreEqual(0.75f, p.armingSeconds, 1e-5f);
            Assert.AreEqual(60f, p.health);
            Assert.AreEqual(1.4f, p.diameter, 1e-5f);
            Assert.AreEqual(14f, p.launchHorizontalVelocity);
            Assert.AreEqual(12f, p.launchUpwardVelocity);
            Assert.AreEqual(1, p.maxActivePadsPerOwner);
            Assert.AreEqual(6, p.maxActivePadsPerMatch);
            Assert.AreEqual(1f, p.perPlayerActivationLockoutSeconds);
            Assert.AreEqual(1.6f, p.placementForwardOffset, 1e-5f);
            Assert.AreEqual(0.18f, p.previewHoldThresholdSeconds, 1e-5f);
            Assert.AreEqual(5f, p.maxSurfaceTiltDegrees);
            Assert.AreEqual(5f, p.noPlacementNearSpawnExitRadius);
            Assert.AreEqual(30f, p.airSteerDegreesPerSecond);
        }

        [Test]
        public void MatchRules_MatchSections3And4()
        {
            var r = Load<MatchRulesConfig>("MatchRulesConfig");
            Assert.AreEqual(180f, r.regulationDurationSeconds);
            Assert.AreEqual(30f, r.maxOvertimeSeconds);
            Assert.AreEqual(3f, r.overtimeHoldSeconds);
            Assert.AreEqual(120, r.scoreToWin);
            Assert.AreEqual(6f, r.zoneCaptureRadius);
            Assert.AreEqual(1.5f, r.zoneMaxFeetHeight, 1e-5f);
            Assert.AreEqual(10f, r.nextZoneWarningSeconds);
            Assert.AreEqual(8f, r.preparationSeconds);
            Assert.AreEqual(3f, r.countdownSeconds);
            Assert.AreEqual(30f, r.loadTimeoutSeconds);
            Assert.AreEqual(4f, r.respawnDelaySeconds);
            ZoneId[] expected = { ZoneId.Center, ZoneId.West, ZoneId.East, ZoneId.Center, ZoneId.East, ZoneId.West };
            Assert.AreEqual(expected.Length, r.schedule.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(i * 30f, r.schedule[i].startTime);
                Assert.AreEqual(i * 30f + 30f, r.schedule[i].endTime);
                Assert.AreEqual(expected[i], r.schedule[i].zone);
            }
        }

        [Test]
        public void Map_MatchesSection9()
        {
            var m = Load<MapConfig>("MapConfig");
            Assert.AreEqual(new Vector2(-48, -40), m.boundsMin);
            Assert.AreEqual(new Vector2(48, 40), m.boundsMax);
            Assert.AreEqual(Vector3.zero, m.centerZone);
            Assert.AreEqual(new Vector3(-22, 0, 0), m.westZone);
            Assert.AreEqual(new Vector3(22, 0, 0), m.eastZone);
            Assert.AreEqual(6f, m.zoneRadius);
            Assert.AreEqual(new Vector3(0, 0, 35), m.northSpawnCenter);
            Assert.AreEqual(new Vector3(0, 0, -35), m.southSpawnCenter);
            Assert.AreEqual(new Vector2(20, 8), m.spawnRoomSize);
            Assert.AreEqual(3, m.exitsPerSpawn);
            Assert.AreEqual(3f, m.platformHeight);
            Assert.AreEqual(new Vector2(10, 8), m.platformSize);
            Assert.AreEqual(4, m.platformCenters.Length);
            foreach (var c in m.platformCenters)
            {
                Assert.AreEqual(20f, Mathf.Abs(c.x));
                Assert.AreEqual(14f, Mathf.Abs(c.z));
            }
            Assert.AreEqual(20f, m.sightlineBlockerZ);
        }

        [Test]
        public void Bots_MatchSection12()
        {
            var b = Load<BotConfig>("BotConfig");
            Assert.AreEqual(0.6f, b.easyPracticeReactionSeconds, 1e-5f);
            Assert.AreEqual(0.35f, b.standardReactionSeconds, 1e-5f);
            Assert.IsTrue(b.useLineOfSightOnly);
            Assert.Greater(b.viewConeHalfAngleDegrees, 0f);
            Assert.Less(b.viewConeHalfAngleDegrees, 180f, "Bots must not have all-round vision.");
        }

        [Test]
        public void MirroredSpawnExits_ForBothTeams()
        {
            var m = Load<MapConfig>("MapConfig");
            for (int exit = 0; exit < m.exitsPerSpawn; exit++)
            {
                Pose north = m.SpawnExit(0, exit), south = m.SpawnExit(1, exit);
                Assert.AreEqual(north.position.x, south.position.x, 1e-4f);
                Assert.AreEqual(north.position.y, south.position.y, 1e-4f);
                Assert.AreEqual(-north.position.z, south.position.z, 1e-4f);
                Assert.IsTrue(m.IsInsideSpawnRoom(0, north.position));
                Assert.IsTrue(m.IsInsideSpawnRoom(1, south.position));
                // Both face the arena.
                Assert.Less((north.rotation * Vector3.forward).z, -0.99f);
                Assert.Greater((south.rotation * Vector3.forward).z, 0.99f);
            }
        }
    }
}


