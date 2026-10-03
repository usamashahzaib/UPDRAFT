using System.Reflection;
using NUnit.Framework;
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

            // CREATE PARTY, JOIN CODE and REFRESH — and nothing left over from the Support page.
            int rowButtons = 0, inputs = 0;
            var content = Panel.Find("Nav_Scroll/Viewport/Content");
            Assert.IsNotNull(content, "page rows live in the scroll content");
            foreach (var child in content.GetComponentsInChildren<Transform>())
            {
                if (child.name == "Row_Button" && child.GetComponent<Button>() != null) rowButtons++;
                if (child.name == "Row_Input") inputs++;
            }
            Assert.AreEqual(3, rowButtons, "after leaving, create/join/refresh must be available again");
            Assert.AreEqual(1, inputs, "party entry shows exactly the invite-code input");
        }

        [Test]
        public void Close_ReturnsHome()
        {
            PlayerProductScreens.Open(root.transform, home, "Support");
            Invoke("Close");
            Assert.IsNull(Panel, "panel must be removed on BACK");
            Assert.IsTrue(home.activeSelf, "Home must be shown again on BACK");
        }
    }
}


