using NUnit.Framework;
using UnityEngine;
using Updraft.UI;

namespace Updraft.Tests
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
            var gun = mannequin.Find("Gun").GetComponent<Renderer>();
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
        public void DisplayNames_AreReadable()
        {
            Assert.AreEqual("Body style: Teal stripe", CosmeticLook.DisplayName("body_teal_stripe"));
            Assert.AreEqual("Weapon wrap: Hazard", CosmeticLook.DisplayName("wrap_hazard"));
            Assert.AreEqual("Badge: Summit", CosmeticLook.DisplayName("badge_summit"));
        }
    }
}
