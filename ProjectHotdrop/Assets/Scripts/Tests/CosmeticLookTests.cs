using NUnit.Framework;
using UnityEngine;
using Hotdrop.UI;

namespace Hotdrop.Tests
{
    /// <summary>QA-024 regression: an equipped cosmetic produces a visible change on the avatar.</summary>
    public class CosmeticLookTests
    {
        GameObject stage;
        Transform mannequin;

        [SetUp]
        public void SetUp()
        {
            stage = new GameObject("Stage");
            mannequin = CosmeticLook.BuildMannequin(stage.transform);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(stage);

        [Test]
        public void BodyStyle_AddsVisiblePattern_WithoutColliders()
        {
            CosmeticLook.Apply(mannequin, "body_teal_stripe");
            var cosmetic = mannequin.Find("Cosmetic");
            Assert.IsNotNull(cosmetic, "an equipped body style must add visible geometry");
            Assert.Greater(cosmetic.childCount, 0);
            foreach (var c in cosmetic.GetComponentsInChildren<Collider>()) Assert.IsFalse(c.enabled, "cosmetics never affect hit detection");
        }

        [Test]
        public void WeaponWrap_RecoloursGun_AndStandardRestoresIt()
        {
            // The shared humanoid owns presentation under Rig; use the named weapon rather
            // than an immediate child so this regression stays about real weapon-wrap output.
            var gunTransform = mannequin.Find("Rig/Gun");
            Assert.IsNotNull(gunTransform, "the mannequin must expose its real weapon mesh");
            var gun = gunTransform.GetComponent<Renderer>();
            Assert.IsNotNull(gun, "the real weapon mesh must be renderable");
            var original = gun.sharedMaterial;
            CosmeticLook.Apply(mannequin, "wrap_hazard");
            Assert.AreNotSame(original, gun.sharedMaterial, "a wrap must change the weapon's look");
            CosmeticLook.Apply(mannequin, "");
            Assert.AreSame(original, gun.sharedMaterial, "unequipping restores the default weapon");
            Assert.IsNull(mannequin.Find("Cosmetic"));
        }

        [Test]
        public void ReApplying_ReplacesInsteadOfStacking()
        {
            CosmeticLook.Apply(mannequin, "body_rooftop");
            CosmeticLook.Apply(mannequin, "body_teal_stripe");
            int count = 0;
            foreach (Transform child in mannequin) if (child.name == "Cosmetic") count++;
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Parse_Composite_KeepsEverySlotIndependent()
        {
            var look = CosmeticLook.Parse("body_teal_stripe|wrap_hazard|badge_summit|rig_female");
            Assert.AreEqual("body_teal_stripe", look.BodyStyle);
            Assert.AreEqual("wrap_hazard", look.WeaponWrap);
            Assert.AreEqual("badge_summit", look.Badge);
            Assert.AreEqual("rig_female", look.BodyRig);

            // A legacy single value still parses as the body style.
            var legacy = CosmeticLook.Parse("body_rooftop");
            Assert.AreEqual("body_rooftop", legacy.BodyStyle);
            Assert.AreEqual("", legacy.WeaponWrap);

            // A weapon wrap alone must never be mistaken for a body style.
            var wrapOnly = CosmeticLook.Parse("wrap_hazard");
            Assert.AreEqual("", wrapOnly.BodyStyle);
            Assert.AreEqual("wrap_hazard", wrapOnly.WeaponWrap);
        }

        [Test]
        public void Apply_Composite_WearsBodyAndWrapAtTheSameTime()
        {
            var gun = mannequin.Find("Rig/Gun").GetComponent<Renderer>();
            var original = gun.sharedMaterial;

            CosmeticLook.Apply(mannequin, "body_teal_stripe|wrap_hazard");
            Assert.IsNotNull(mannequin.Find("Cosmetic"),
                "the body style must still render when a wrap is equipped");
            Assert.AreNotSame(original, gun.sharedMaterial, "the wrap must also render");

            // Clearing the look restores both slots at once.
            CosmeticLook.Apply(mannequin, "");
            Assert.IsNull(mannequin.Find("Cosmetic"));
            Assert.AreSame(original, gun.sharedMaterial);
        }

        [Test]
        public void DisplayNames_AreReadable()
        {
            Assert.AreEqual("Body style: Teal stripe", CosmeticLook.DisplayName("body_teal_stripe"));
            Assert.AreEqual("Weapon wrap: Hazard", CosmeticLook.DisplayName("wrap_hazard"));
            Assert.AreEqual("Badge: Summit", CosmeticLook.DisplayName("badge_summit"));
        }

        [Test]
        public void FirstFlightBadge_IsReadableAndTagged()
        {
            // C02/Amendment 1: the first eligible public match grants badge_first_flight, which
            // must read as a real badge on the results receipt and on name plates.
            Assert.AreEqual("Badge: First flight", CosmeticLook.DisplayName("badge_first_flight"));
            Assert.AreEqual("\u25C6FIRST_FLIGHT", CosmeticLook.BadgeTag("badge_first_flight"));
        }
    }
}


