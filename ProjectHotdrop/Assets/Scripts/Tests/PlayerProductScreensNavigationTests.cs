using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.Core;

namespace Hotdrop.Tests
{
    /// <summary>
    /// QA-002 regression: product screens must never destroy their own exit. Page content
    /// is rebuilt through ClearRows (party render/leave, delete confirm/success, retry),
    /// and every one of those paths previously removed BACK along with the rows.
    /// </summary>
    public class PlayerProductScreensNavigationTests
    {
        GameObject root;
        GameObject home;

        [SetUp]
        public void SetUp()
        {
            RecentMatch.Forget();
            root = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            home = new GameObject("Home");
            home.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            Invoke("Close");
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(home);
        }

        static void Invoke(string method)
        {
            var info = typeof(PlayerProductScreens).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(info, "PlayerProductScreens." + method + " not found");
            info.Invoke(null, null);
        }

        Transform Panel => root.transform.Find("PlayerProductScreen");

        void AssertBackUsable()
        {
            Assert.IsNotNull(Panel, "product screen panel missing");
            var back = Panel.Find("Nav_Bar/Nav_Back");
            Assert.IsNotNull(back, "BACK was destroyed — the player has no exit");
            Assert.IsNotNull(back.GetComponent<Button>(), "BACK must remain a button");
            Assert.IsTrue(back.gameObject.activeInHierarchy, "BACK must stay visible");
            Assert.IsNull(back.GetComponentInParent<UnityEngine.UI.ScrollRect>(), "BACK must live outside the page rows, not in the scroll content");
        }

        [Test]
        public void Support_DeleteConfirm_And_Success_KeepBack()
        {
            PlayerProductScreens.Open(root.transform, home, "Support");
            AssertBackUsable();

            Invoke("ConfirmDelete");
            AssertBackUsable();

            Invoke("ClearRows"); // what a successful deletion does to the page
            AssertBackUsable();
        }

        [Test]
        public void PartyEntry_RebuiltAfterLeave_KeepsBackAndCreateControls()
        {
            PlayerProductScreens.Open(root.transform, home, "Support"); // any page; party needs a backend
            Invoke("ShowPartyEntry"); // what a successful leave now does
            AssertBackUsable();

            // Screen 05 entry state: CREATE PARTY, the code field with JOIN, and the room's own exit.
            var entry = Panel.Find("PartyRoomVisual");
            Assert.IsNotNull(entry, "after leaving, the party entry room is rebuilt");
            Assert.IsNotNull(entry.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "CreateParty"), "CREATE PARTY must be available again");
            Assert.IsNotNull(entry.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "JoinCode"), "JOIN must be available again");
            Assert.AreEqual(1, entry.GetComponentsInChildren<TMPro.TMP_InputField>(true).Length, "party entry shows exactly the invite-code input");
        }

        [Test]
        public void LocalAppearance_HasRealPreviewAndFreeRunnerAction_WithoutInventoryResponse()
        {
            PlayerProductScreens.Open(root.transform, home, "Support");
            Invoke("ClearRows");
            Invoke("BuildLocalAppearance");
            AssertBackUsable();
            var preview = Panel.GetComponentInChildren<RawImage>();
            Assert.IsNotNull(preview);
            Assert.IsNotNull(preview.texture, "Appearance must render the actual model before a backend reply.");
            Assert.IsNotNull(Panel.Find("Nav_Scroll/Viewport/Content/Row_AppearanceRunnerChoice"));
            Invoke("ClearRows");
            Assert.IsNull(Panel.Find("Nav_Scroll/Viewport/Content/Row_AppearanceRunnerChoice"), "Retry rebuilds must not leave duplicate runner actions.");
        }

        [Test]
        public void Appearance_CategoryTabsShowOnlyOwnedItemsInSelectedCategory()
        {
            PlayerProductScreens.Open(root.transform, home, "Support");
            Invoke("ClearRows");
            Invoke("BuildLocalAppearance");
            var items = new[]
            {
                new InventoryItemDto { id = 1, cosmeticId = "body_standard", acquiredVia = "starter" },
                new InventoryItemDto { id = 2, cosmeticId = "wrap_hazard", acquiredVia = "unlock" }
            };
            var render = typeof(PlayerProductScreens).GetMethod("RenderAppearanceInventory", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(render);
            render.Invoke(null, new object[] { items, "body_standard", "wrap_hazard", "" });
            var content = Panel.Find("Nav_Scroll/Viewport/Content");
            var inventory = content.Find("Row_AppearanceHero/AppearanceInformation");
            Assert.IsNotNull(inventory.Find("Row_InventoryItem_1"));
            Assert.IsNull(inventory.Find("Row_InventoryItem_2"));

            Assert.IsNotNull(inventory.Find("Row_InventoryTabs/InventoryTab_WRAPS").GetComponent<Button>());
            var selected = typeof(PlayerProductScreens).GetField("appearanceCategory", BindingFlags.NonPublic | BindingFlags.Static);
            selected.SetValue(null, "Weapon Wrap");
            render.Invoke(null, new object[] { items, "body_standard", "wrap_hazard", "" });
            Assert.IsNull(inventory.Find("Row_InventoryItem_1"));
            Assert.IsNotNull(inventory.Find("Row_InventoryItem_2"));
            AssertBackUsable();
            selected.SetValue(null, "Body Presentation");
        }

        [Test]
        public void Close_ReturnsHome()
        {
            PlayerProductScreens.Open(root.transform, home, "Support");
            Invoke("Close");
            // BACK slides the page out over one motion; EditMode has no frame loop, so settle it here.
            foreach (var driver in root.GetComponentsInChildren<Hotdrop.UI.UiMotionDriver>(true)) driver.Tick(1f);
            Assert.IsNull(Panel, "panel must be removed on BACK");
            Assert.IsTrue(home.activeSelf, "Home must be shown again on BACK");
        }

        [Test]
        public void Stale_Response_IsRejectedAfterScreenCloses()
        {
            PlayerProductScreens.Open(root.transform, home, "Support");
            var version = (int)typeof(PlayerProductScreens).GetField("screenVersion", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Invoke("Close");
            var stale = (bool)typeof(PlayerProductScreens).GetMethod("Stale", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { version });
            Assert.IsTrue(stale, "a reply from a closed page must not paint UI");
        }

        [TestCase("body_standard", "Body Presentation")]
        [TestCase("body_teal_stripe", "Body Style")]
        [TestCase("wrap_hazard", "Weapon Wrap")]
        [TestCase("badge_rookie", "Badge")]
        public void Appearance_MapsItemsToTruthfulCategories(string cosmeticId, string expected)
        {
            var category = (string)typeof(PlayerProductScreens).GetMethod("CosmeticCategory", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { cosmeticId });
            Assert.AreEqual(expected, category);
        }

        [TestCase(true, "GUEST ACCOUNT")]
        [TestCase(false, "LINKED ACCOUNT")]
        public void Account_SeparatesGuestAndLinkedStates(bool isGuest, string expected)
        {
            var label = (string)typeof(PlayerProductScreens).GetMethod("AccountStateLabel", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { isGuest });
            Assert.AreEqual(expected, label);
        }

        [Test]
        public void ReportTarget_UsesVisibleAliasAndSlot()
        {
            var selected = new RecentMatch.Entry { alias = "Skyline", slot = 2 };
            var message = (string)typeof(PlayerProductScreens).GetMethod("ReportTargetMessage", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { selected });
            StringAssert.Contains("Skyline", message);
            StringAssert.Contains("slot 3", message);
            StringAssert.DoesNotContain("ID", message);
        }

        [Test]
        public void DeleteAccount_UsesTwoDeliberateSteps_AndKeepsBack()
        {
            PlayerProductScreens.Open(root.transform, home, "Support");
            var initial = Panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.IsTrue(System.Array.Exists(initial, t => t.text == "PRESS & HOLD TO UNLOCK"), "deletion starts behind the 3-second hold gate");

            Invoke("ConfirmDelete");
            AssertBackUsable();
            var confirm = Panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.IsTrue(System.Array.Exists(confirm, t => t.text.Contains("This deletes your profile")));
            Assert.IsTrue(System.Array.Exists(confirm, t => t.text == "CANCEL"));
        }
    }
}


