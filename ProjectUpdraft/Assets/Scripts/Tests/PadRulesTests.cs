using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Updraft.Config;
using Updraft.Movement;
using Updraft.Pads;

namespace Updraft.Tests
{
    /// <summary>
    /// Gate B, spec section 6: placement validation against real colliders, and the pad
    /// lifecycle (arming, lifetime, HP, recharge through death, replacement, lockout, no stacking).
    /// </summary>
    public class PadRulesTests
    {
        PadConfig pad;
        MapConfig map;
        MovementConfig movement;
        readonly List<GameObject> created = new List<GameObject>();
        PhysicsScene Physics => UnityEngine.Physics.defaultPhysicsScene;

        [SetUp]
        public void SetUp()
        {
            pad = AssetDatabase.LoadAssetAtPath<PadConfig>("Assets/Resources/Config/PadConfig.asset");
            map = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/Resources/Config/MapConfig.asset");
            movement = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Resources/Config/MovementConfig.asset");
            // Arena floor, top at Y = 0.
            Box("Floor", new Vector3(0, -0.25f, 0), new Vector3(200, 0.5f, 200), "Ground");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created) Object.DestroyImmediate(go);
            created.Clear();
        }

        GameObject Box(string name, Vector3 position, Vector3 scale, string layer, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = LayerMask.NameToLayer(layer);
            go.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            go.transform.localScale = scale;
            created.Add(go);
            UnityEngine.Physics.SyncTransforms();
            return go;
        }

        string Validate(Vector3 feet, Vector3 forward, int otherPads = 0)
        {
            string reason = PadPlacementRules.ResolvePosition(Physics, feet, forward, pad, out var position);
            return reason ?? PadPlacementRules.Validate(Physics, position, forward, otherPads, pad, map, movement);
        }

        static readonly Vector3 OpenFloor = new Vector3(-36f, 0f, -12f);

        [Test] public void OpenLevelFloor_IsValid() => Assert.IsNull(Validate(OpenFloor, Vector3.right));

        [Test]
        public void PlacesAboutOnePointSixMetresAhead()
        {
            Assert.IsNull(PadPlacementRules.ResolvePosition(Physics, OpenFloor, Vector3.right, pad, out var position));
            Assert.AreEqual(1.6f, position.x - OpenFloor.x, 1e-3f);
        }

        [Test] public void MidAir_HasNoFloor() => Assert.AreEqual("NO FLOOR", Validate(OpenFloor + Vector3.up * 10f, Vector3.right));

        [Test]
        public void SteepSurface_IsRejected()
        {
            Box("Slope", new Vector3(-36f, 2f, 20f), new Vector3(10, 0.5f, 10), "Ground", Quaternion.Euler(0, 0, 10f));
            Vector3 feet = new Vector3(-37.6f, 2.4f, 20f); // pad lands on the slab's centre (top ~Y 2.25)
            Assert.AreEqual("TOO STEEP", Validate(feet, Vector3.right));
        }

        [Test]
        public void SurfaceJustInsideFiveDegrees_IsAllowed()
        {
            Box("Ramp", new Vector3(-36f, 2f, 20f), new Vector3(10, 0.5f, 10), "Ground", Quaternion.Euler(0, 0, 4.8f));
            Vector3 feet = new Vector3(-37.6f, 2.4f, 20f);
            Assert.IsNull(Validate(feet, Vector3.right), "A legal tilt must also pass the fits-fully rim check.");
        }

        [Test] public void InsideSpawnRoom_IsRejected() => Assert.AreEqual("SPAWN ROOM", Validate(new Vector3(-1.6f, 0f, 34f), Vector3.right));

        [Test]
        public void WithinFiveMetresOfSpawnExit_IsRejected()
        {
            Assert.AreEqual("TOO CLOSE TO SPAWN EXIT", Validate(new Vector3(4.4f, 0f, 27.5f), Vector3.right));
            Assert.AreEqual("TOO CLOSE TO SPAWN EXIT", Validate(new Vector3(4.4f, 0f, -27.5f), Vector3.right), "South exits too.");
        }

        [Test]
        public void InsideOrOverlappingCaptureCircle_IsRejected()
        {
            foreach (var zone in new[] { map.centerZone, map.westZone, map.eastZone })
            {
                Assert.AreEqual("INSIDE A CAPTURE ZONE", Validate(zone + new Vector3(-1.6f, 0f, 2f), Vector3.right));
                // Centre 6.5 m out: the 1.4 m disc would still cross the 6 m ring.
                Assert.AreEqual("INSIDE A CAPTURE ZONE", Validate(zone + new Vector3(-1.6f, 0f, 6.5f), Vector3.right));
            }
        }

        [Test]
        public void OverlappingAnotherPad_IsRejected()
        {
            Box("Existing pad", OpenFloor + new Vector3(1.6f + 1f, 0.05f, 0f), new Vector3(1.4f, 0.2f, 1.4f), "Pad");
            Assert.AreEqual("OVERLAPS A PAD", Validate(OpenFloor, Vector3.right));
        }

        [Test]
        public void MustFitFullyOnTheSurface()
        {
            // A small raised crate: a pad centred near its corner would hang over the edge.
            Box("Crate", new Vector3(-36f, 0.5f, 20f), new Vector3(2f, 1f, 2f), "Ground");
            Vector3 feet = new Vector3(-36.6f - 1.6f + 1.4f, 1f, 20.8f);
            Assert.AreEqual("DOESN'T FIT", Validate(feet, Vector3.right));
        }

        [Test]
        public void LaunchPathIntoOverheadGeometry_IsRejected()
        {
            Vector3 launch = (Vector3.right * pad.launchHorizontalVelocity + Vector3.up * pad.launchUpwardVelocity).normalized;
            Vector3 padPos = OpenFloor + Vector3.right * 1.6f;
            Box("Overhang", padPos + Vector3.up * 0.5f + launch * 3f, new Vector3(3f, 0.5f, 3f), "Cover");
            Assert.AreEqual("LAUNCH PATH BLOCKED", Validate(OpenFloor, Vector3.right));
        }

        /// <summary>
        /// QA-005: a 0.3 m hole inside the footprint but between the old centre + 8 rim rays.
        /// The raised platform has nothing within a metre below, so the hole is unsupported.
        /// </summary>
        [Test]
        public void HoleBetweenRimSamples_IsRejected()
        {
            Vector3 padPos = new Vector3(OpenFloor.x + 1.6f, 3f, OpenFloor.z);
            float hx0 = padPos.x + 0.2f, hx1 = padPos.x + 0.5f, hz0 = padPos.z - 0.03f, hz1 = padPos.z + 0.27f;
            float x0 = padPos.x - 3f, x1 = padPos.x + 3f, z0 = padPos.z - 3f, z1 = padPos.z + 3f;
            void Slab(float ax, float bx, float az, float bz) =>
                Box("Platform", new Vector3((ax + bx) * 0.5f, 2.75f, (az + bz) * 0.5f), new Vector3(bx - ax, 0.5f, bz - az), "Ground");
            Slab(x0, hx0, z0, z1);
            Slab(hx1, x1, z0, z1);
            Slab(hx0, hx1, z0, hz0);
            Slab(hx0, hx1, hz1, z1);

            Assert.AreEqual("DOESN'T FIT", Validate(new Vector3(OpenFloor.x, 3f, OpenFloor.z), Vector3.right));
        }

        /// <summary>QA-005: a beam at head height over the pad, behind the launch direction.</summary>
        [Test]
        public void HeadHeightBeamOverPad_IsRejected()
        {
            Vector3 padPos = OpenFloor + Vector3.right * 1.6f;
            Box("Beam", padPos + new Vector3(-0.15f, 1.6f, 0f), new Vector3(0.3f, 0.2f, 3f), "Cover");
            Assert.AreEqual("NO HEADROOM", Validate(OpenFloor, Vector3.right));
        }

        /// <summary>
        /// QA-006: online rejections travel as a one-byte code. Every reason the placement rules
        /// (or the offline/online pre-checks) can return must survive that round trip unchanged.
        /// </summary>
        [Test]
        public void EveryRejectReason_RoundTripsThroughNetworkCode()
        {
            // Only the placement paths: other methods in these files return unrelated uppercase
            // labels (e.g. the death-direction "FRONT"), which are not pad reasons.
            var source = System.IO.File.ReadAllText("Assets/Scripts/Pads/PadPlacementRules.cs")
                + MethodBody("Assets/Scripts/Network/NetPlayer.cs", "string EvaluatePadPlacement(")
                + MethodBody("Assets/Scripts/Player/PlayerController.cs", "string ResolvePadPlacement(");
            var returned = System.Text.RegularExpressions.Regex.Matches(source, "return \"([A-Z' ]+)\"");
            Assert.Greater(returned.Count, 8, "expected to find the placement reason literals");
            foreach (System.Text.RegularExpressions.Match m in returned)
            {
                string reason = m.Groups[1].Value;
                byte code = PadPlacementRules.CodeOf(reason);
                Assert.AreEqual(reason, PadPlacementRules.ReasonOf(code), $"'{reason}' has no dedicated network code");
            }
            Assert.AreEqual(0, PadPlacementRules.CodeOf(null));
            Assert.IsNull(PadPlacementRules.ReasonOf(0));
        }

        static string MethodBody(string path, string signature)
        {
            string text = System.IO.File.ReadAllText(path);
            int start = text.IndexOf(signature, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, $"'{signature}' not found in {path}");
            int open = text.IndexOf('{', start);
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return text.Substring(open, i - open + 1);
            }
            Assert.Fail($"unbalanced braces after '{signature}' in {path}");
            return null;
        }

        [Test]
        public void ArenaCap_SixActivePadsFromOthers()
        {
            Assert.IsNull(Validate(OpenFloor, Vector3.right, otherPads: 5));
            Assert.AreEqual("ARENA PAD LIMIT", Validate(OpenFloor, Vector3.right, otherPads: 6));
        }

        // ---- Lifecycle (pure) ----

        [Test]
        public void ArmsAt075_ExpiresAt18_DiesAt60Damage()
        {
            var p = new LaunchPad(pad, 1, 0, Vector3.zero, Vector3.forward);
            p.Tick(0.74f);
            Assert.IsFalse(p.CanBeUsed, "Not usable while arming.");
            p.Tick(0.02f);
            Assert.IsTrue(p.CanBeUsed);
            p.ApplyDamage(59.9f);
            Assert.IsTrue(p.CanBeUsed);
            p.ApplyDamage(0.2f);
            Assert.AreEqual(PadState.Destroyed, p.State);

            var q = new LaunchPad(pad, 1, 0, Vector3.zero, Vector3.forward);
            q.Tick(17.9f);
            Assert.IsTrue(q.CanBeUsed);
            q.Tick(0.2f);
            Assert.AreEqual(PadState.Expired, q.State);
            Assert.IsFalse(q.CanBeUsed);
        }

        [Test]
        public void DeathDoesNotRefreshCooldown_AndPadOutlivesOwner()
        {
            var owner = new PlayerPadManager(pad);
            var placed = owner.PlaceNew(1, 0, Vector3.zero, Vector3.forward);
            owner.Tick(4f); // owner dies here; respawn does not touch the pad manager
            Assert.IsFalse(owner.HasCharge);
            Assert.AreEqual(12f, owner.RechargeRemaining, 1e-3f, "Cooldown keeps running through death, never refreshed.");
            Assert.AreEqual(PadState.Armed, placed.State, "The pad outlives its owner's death.");
            owner.Tick(12.01f);
            Assert.IsTrue(owner.HasCharge);
        }

        [Test]
        public void NewPlacement_RemovesOldPad_OnePerOwner()
        {
            var owner = new PlayerPadManager(pad);
            var first = owner.PlaceNew(1, 0, Vector3.zero, Vector3.forward);
            owner.Tick(16.01f);
            var second = owner.PlaceNew(1, 0, Vector3.right * 5f, Vector3.forward);
            Assert.AreEqual(PadState.Expired, first.State);
            Assert.AreSame(second, owner.ActivePad);
        }

        [Test]
        public void ActivationLockout_IsOneSecondPerPlayer()
        {
            var player = new PlayerPadManager(pad);
            Assert.IsTrue(player.CanActivate);
            player.NotifyActivated();
            player.Tick(0.99f);
            Assert.IsFalse(player.CanActivate);
            player.Tick(0.02f);
            Assert.IsTrue(player.CanActivate);

            var otherPlayer = new PlayerPadManager(pad);
            Assert.IsTrue(otherPlayer.CanActivate, "The lockout is per player, not per pad.");
        }

        [Test]
        public void EitherTeam_CanUseAnArmedPad()
        {
            var p = new LaunchPad(pad, ownerId: 1, teamId: 0, Vector3.zero, Vector3.forward);
            p.Tick(1f);
            Assert.IsTrue(p.CanBeUsed); // CanBeUsed has no team parameter: usability never depends on team.
        }

        [Test]
        public void Launch_ReplacesVelocity_NeverStacks()
        {
            var sim = new PlayerMovementSimulator(movement);
            var p = new LaunchPad(pad, 1, 0, Vector3.zero, Vector3.forward);
            sim.Restore(new Vector3(6f, -15f, 6f), false, false); // running and falling fast
            sim.ApplyLaunchVelocity(p.GetLaunchVelocity());
            Assert.AreEqual(new Vector3(0f, 12f, 14f), sim.Velocity);
            sim.ApplyLaunchVelocity(p.GetLaunchVelocity());
            Assert.AreEqual(new Vector3(0f, 12f, 14f), sim.Velocity, "A second launch replaces, not adds.");
        }

        [Test]
        public void AirSteer_IsThirtyDegreesPerSecond_PreservingSpeed()
        {
            var sim = new PlayerMovementSimulator(movement);
            sim.ApplyLaunchVelocity(new Vector3(0f, 12f, 14f));
            for (int i = 0; i < 30; i++) sim.ApplyAirSteer(Vector3.right, pad.airSteerDegreesPerSecond, 1f / 30f);
            Vector3 flat = new Vector3(sim.Velocity.x, 0f, sim.Velocity.z);
            Assert.AreEqual(14f, flat.magnitude, 1e-3f);
            Assert.AreEqual(30f, Vector3.Angle(Vector3.forward, flat), 0.05f);
        }
    }
}
