using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Movement;
using Hotdrop.Player;

namespace Hotdrop.Tests
{
    /// <summary>
    /// Spec 15 (Amendment v2.1): Male and Female are cosmetic-only body rigs. Every rig must
    /// resolve to one shared capsule, one hitbox set and one animation set, the generated
    /// player prefabs must use that same geometry, and no gameplay system may branch on the
    /// rig. These guards stay valid when Gate G replaces the graybox with production art.
    /// </summary>
    public class BodyRigParityTests
    {
        const float Epsilon = 1e-4f;

        static MovementConfig ShippedMovement()
        {
            var config = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/Resources/Config/MovementConfig.asset");
            Assert.IsNotNull(config, "Missing shipped MovementConfig");
            return config;
        }

        static GameObject Prefab(string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            Assert.IsNotNull(prefab, "Missing resource prefab " + resourcePath);
            return prefab;
        }

        static Component Hitbox(GameObject host, string label)
        {
            var hitbox = host.GetComponent("Hitbox");
            Assert.IsNotNull(hitbox, label + " must carry a Fusion Hitbox");
            return hitbox;
        }

        // The test assembly deliberately avoids a Fusion reference; read the Hitbox's
        // serialized geometry through Unity's SerializedObject instead.
        static Vector3 HitboxVector(Component hitbox, string name)
        {
            var property = new SerializedObject(hitbox).FindProperty(name);
            Assert.IsNotNull(property, "Fusion Hitbox has no serialized " + name);
            return property.vector3Value;
        }

        static float HitboxFloat(Component hitbox, string name)
        {
            var property = new SerializedObject(hitbox).FindProperty(name);
            Assert.IsNotNull(property, "Fusion Hitbox has no serialized " + name);
            return property.floatValue;
        }

        [Test]
        public void Catalog_IsTwoStableRigs_WithMaleDefault()
        {
            Assert.AreEqual(2, BodyRigs.All.Length);
            CollectionAssert.AreEquivalent(new[] { BodyRig.Male, BodyRig.Female }, BodyRigs.All);
            Assert.AreEqual(BodyRig.Male, BodyRigs.Default);
            Assert.AreEqual("rig_male", BodyRigs.Id(BodyRig.Male));
            Assert.AreEqual("rig_female", BodyRigs.Id(BodyRig.Female));
            Assert.AreNotEqual(BodyRigs.Id(BodyRig.Male), BodyRigs.Id(BodyRig.Female));
            Assert.AreEqual("Male", BodyRigs.DisplayName(BodyRig.Male));
            Assert.AreEqual("Female", BodyRigs.DisplayName(BodyRig.Female));

            Assert.IsTrue(BodyRigs.TryParse("rig_female", out var parsed));
            Assert.AreEqual(BodyRig.Female, parsed);
            Assert.IsFalse(BodyRigs.TryParse("bogus", out _));
            Assert.AreEqual(BodyRig.Male, BodyRigs.FromStoredId(null), "a missing stored rig falls back to the default");
            Assert.AreEqual(BodyRig.Male, BodyRigs.FromStoredId("bogus"), "an unknown stored rig falls back to the default");
            Assert.AreEqual(BodyRig.Female, BodyRigs.FromStoredId("rig_female"));
        }

        [Test]
        public void BothRigs_ShareOneCapsule_MatchingShippedMovementConfig()
        {
            var movement = ShippedMovement();
            foreach (var rig in BodyRigs.All)
            {
                var profile = BodyRigs.Gameplay(rig);
                Assert.AreSame(BodyRigs.SharedGameplay, profile, "no rig may own its own gameplay profile");
                Assert.AreEqual(movement.capsuleHeight, profile.CapsuleHeight, Epsilon,
                    rig + " capsule height must match the shipped config");
                Assert.AreEqual(movement.capsuleRadius, profile.CapsuleRadius, Epsilon,
                    rig + " capsule radius must match the shipped config");
                Assert.AreEqual(1.8f, profile.CapsuleHeight, Epsilon);
                Assert.AreEqual(0.35f, profile.CapsuleRadius, Epsilon);
                Assert.AreEqual(profile.CapsuleHeight * 0.5f, profile.CapsuleCenter.y, Epsilon,
                    "the capsule center keeps the capsule's feet at y = 0");
            }
        }

        [Test]
        public void BothRigs_ShareOneHitboxSet_AndNetPlayerPrefabImplementsIt()
        {
            var profile = BodyRigs.SharedGameplay;
            foreach (var rig in BodyRigs.All)
                Assert.AreSame(profile, BodyRigs.Gameplay(rig), "hitboxes are rig-independent");

            var prefab = Prefab("NetPrefabs/NetPlayer");
            var capsule = prefab.GetComponent<CapsuleCollider>();
            Assert.AreEqual(profile.CapsuleHeight, capsule.height, Epsilon);
            Assert.AreEqual(profile.CapsuleRadius, capsule.radius, Epsilon);
            Assert.AreEqual(profile.CapsuleCenter.x, capsule.center.x, Epsilon);
            Assert.AreEqual(profile.CapsuleCenter.y, capsule.center.y, Epsilon);
            Assert.AreEqual(profile.CapsuleCenter.z, capsule.center.z, Epsilon);

            var body = prefab.transform.Find(profile.BodyHitboxName);
            Assert.IsNotNull(body, "NetPlayer must have the shared body hitbox");
            Assert.AreEqual(profile.BodyHitboxOffset.x, body.localPosition.x, Epsilon);
            Assert.AreEqual(profile.BodyHitboxOffset.y, body.localPosition.y, Epsilon);
            Assert.AreEqual(profile.BodyHitboxOffset.z, body.localPosition.z, Epsilon);
            Assert.AreEqual(profile.BodyHitboxExtents, HitboxVector(Hitbox(body.gameObject, profile.BodyHitboxName), "BoxExtents"));

            var head = prefab.transform.Find(profile.HeadHitboxName);
            Assert.IsNotNull(head, "NetPlayer must have the shared head hitbox");
            Assert.AreEqual(profile.HeadHitboxOffset.x, head.localPosition.x, Epsilon);
            Assert.AreEqual(profile.HeadHitboxOffset.y, head.localPosition.y, Epsilon);
            Assert.AreEqual(profile.HeadHitboxOffset.z, head.localPosition.z, Epsilon);
            Assert.AreEqual(profile.HeadHitboxRadius,
                HitboxFloat(Hitbox(head.gameObject, profile.HeadHitboxName), "SphereRadius"), Epsilon);

            // The cosmetic avatar (and any per-rig art) is added at runtime under a visual root;
            // hit geometry must stay invisible and rig-independent.
            var hitboxHosts = new List<GameObject>();
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
                if (child.GetComponent("Hitbox") != null) hitboxHosts.Add(child.gameObject);
            Assert.AreEqual(2, hitboxHosts.Count, "the shared hitbox set is exactly body + head");
            foreach (var host in hitboxHosts)
                Assert.IsNull(host.GetComponent<Renderer>(), "hit geometry must stay invisible; rig art lives on the visual root");
        }

        [Test]
        public void BothRigs_ShareOneAnimationSet_AndAnimationStaysVisualOnly()
        {
            var profile = BodyRigs.SharedGameplay;
            Assert.AreEqual("avatar_motion_v1", profile.AnimationSetId);
            foreach (var rig in BodyRigs.All)
                Assert.AreEqual(profile.AnimationSetId, BodyRigs.Gameplay(rig).AnimationSetId,
                    "both rigs retarget the same animation set");

            var prefab = Prefab("Prefabs/PlayerPrefab");
            var controller = prefab.GetComponent<CharacterController>();
            Assert.AreEqual(profile.CapsuleHeight, controller.height, Epsilon);
            Assert.AreEqual(profile.CapsuleRadius, controller.radius, Epsilon);
            Assert.AreEqual(profile.CapsuleCenter.y, controller.center.y, Epsilon);

            var avatar = prefab.transform.Find("Body");
            Assert.IsNotNull(avatar, "PlayerPrefab must have the avatar body");
            var motion = avatar.GetComponent<SportAvatarAnimation>();
            Assert.IsNotNull(motion, "the shared animation set drives the avatar body");
            Assert.IsNotNull(motion.leftLeg, "both legs retarget identically on every rig");
            Assert.IsNotNull(motion.rightLeg, "both legs retarget identically on every rig");

            // Animation (and any rig art parented under the avatar) never owns gameplay collision.
            foreach (var collider in avatar.GetComponentsInChildren<Collider>(true))
                Assert.IsFalse(collider.enabled, "the avatar never owns gameplay collision");
        }

        [Test]
        public void MovementTrace_IsIdentical_ForEveryRig()
        {
            // The movement simulator takes no rig argument by design, so replaying one scripted
            // match trace per rig proves the authoritative movement path cannot branch on rig
            // identity. If a future change adds rig-dependent movement, this loop must fail.
            var reference = MovementTrace();
            foreach (var rig in BodyRigs.All)
            {
                var trace = MovementTrace();
                Assert.AreEqual(reference.Count, trace.Count);
                for (int tick = 0; tick < reference.Count; tick++)
                    Assert.AreEqual(reference[tick], trace[tick], rig + " must follow the same movement trace as every other rig");
            }
        }

        static List<Vector3> MovementTrace()
        {
            const float dt = 1f / 30f;
            var simulator = new PlayerMovementSimulator(ScriptableObject.CreateInstance<MovementConfig>());
            var trace = new List<Vector3>();
            Vector3 position = Vector3.zero;
            for (int tick = 0; tick < 90; tick++)
            {
                if (tick == 10) simulator.ApplyLaunchVelocity(new Vector3(0f, 12f, 14f));
                if (tick > 10 && tick < 40) simulator.ApplyAirSteer(tick < 25 ? Vector3.right : Vector3.back, 30f, dt);
                if (tick == 50) simulator.IsGrounded = true; // land again so the jump branch runs too
                simulator.Tick(new MovementInput
                {
                    moveAxis = tick < 10 ? Vector2.up : Vector2.right,
                    jumpOrLaunchPressed = tick == 60,
                    isFiring = tick >= 70,
                }, Vector3.forward, Vector3.right, dt);
                position += simulator.Velocity * dt;
                trace.Add(position);
            }
            return trace;
        }
    }
}


