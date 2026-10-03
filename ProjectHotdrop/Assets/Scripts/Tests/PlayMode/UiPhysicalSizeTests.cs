using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Hotdrop.Core;
using Hotdrop.Player;
using Hotdrop.UI;
using Hotdrop.Match;

namespace Hotdrop.Tests.PlayMode
{
    /// <summary>
    /// QA-019 / QA-020 gate, measured on the audit's reference phone (Galaxy A06: 1600x720 px,
    /// 300 dpi). Every visible control must be at least 48 dp, every visible text at least
    /// ~12 dp, and nothing may sit off-screen (scrolling lists excepted) on each screen a
    /// player can reach offline.
    /// </summary>
    public class UiPhysicalSizeTests
    {
        const float Width = 1600f, Height = 720f, Dpi = 300f;
        const float MinTouchDp = 47.5f, MinTextDp = 11.8f;

        float scale;
        RectTransform canvasRect;
        readonly List<string> failures = new List<string>();

        [UnitySetUp]
        public IEnumerator LoadHome()
        {
            Time.timeScale = 1f;
            if (GameBootstrap.Instance != null) Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
            PlayerPrefs.SetInt(GameBootstrap.TutorialDoneKey, 1);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            PinCanvasToReferencePhone();
            yield return Settle();
        }

        [TearDown]
        public void Restore() { Time.timeScale = 1f; }

        void PinCanvasToReferencePhone()
        {
            var canvas = GameBootstrap.Instance.matchHUD.GetComponent<Canvas>();
            foreach (var d in canvas.GetComponents<DensityCanvasScaler>()) d.enabled = false;
            foreach (var c in canvas.GetComponents<CanvasScaler>()) c.enabled = false;
            scale = UiMetrics.ScaleFor(Width, Height, Dpi);
            canvas.renderMode = RenderMode.WorldSpace;
            canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.localScale = Vector3.one;
            canvasRect.sizeDelta = new Vector2(Width / scale, Height / scale);
        }

        static IEnumerator Settle()
        {
            for (int i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                yield return null;
            }
        }

        float ToDp(float units) => units * scale / (Dpi / 160f);

        Rect CanvasSpaceRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 min = canvasRect.InverseTransformPoint(corners[0]), max = canvasRect.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        void Check(string screen)
        {
            Canvas.ForceUpdateCanvases();
            var bounds = canvasRect.rect;
            foreach (var selectable in canvasRect.GetComponentsInChildren<Selectable>(false))
            {
                if (!selectable.isActiveAndEnabled || !selectable.interactable) continue;
                var graphic = selectable.targetGraphic;
                if (graphic != null && !graphic.raycastTarget) continue;
                var r = CanvasSpaceRect(selectable.GetComponent<RectTransform>());
                if (ToDp(r.height) < MinTouchDp || ToDp(r.width) < MinTouchDp)
                    failures.Add($"[{screen}] control '{Path(selectable.transform)}' is {ToDp(r.width):0}x{ToDp(r.height):0} dp (< 48 dp)");
                if (selectable.GetComponentInParent<ScrollRect>() == null && !Inside(bounds, r))
                    failures.Add($"[{screen}] control '{Path(selectable.transform)}' is off-screen");
            }
            foreach (var text in canvasRect.GetComponentsInChildren<TMP_Text>(false))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || text.color.a < 0.05f) continue;
                text.ForceMeshUpdate();
                if (ToDp(text.fontSize) < MinTextDp)
                    failures.Add($"[{screen}] text '{Path(text.transform)}' (\"{Short(text.text)}\") is {ToDp(text.fontSize):0.0} dp (< 12 dp)");
                if (text.GetComponentInParent<ScrollRect>() == null && !Inside(bounds, CanvasSpaceRect(text.rectTransform)))
                    failures.Add($"[{screen}] text '{Path(text.transform)}' (\"{Short(text.text)}\") is off-screen");
            }
        }

        static bool Inside(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - 1f && inner.xMax <= outer.xMax + 1f && inner.yMin >= outer.yMin - 1f && inner.yMax <= outer.yMax + 1f;

        static string Short(string s) => s.Length > 30 ? s.Substring(0, 30) + "…" : s.Replace("\n", " ");

        static string Path(Transform t)
        {
            string p = t.name;
            for (var x = t.parent; x != null && x.GetComponent<Canvas>() == null; x = x.parent) p = x.name + "/" + p;
            return p;
        }

        void AssertClean()
        {
            if (failures.Count > 0) Assert.Fail($"{failures.Count} physical-size problems on the A06:\n" + string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator Menus_MeetPhysicalMinimums()
        {
            var game = GameBootstrap.Instance;
            Check("Home");

            game.GetComponent<OfflinePauseMenu>().OpenSettings();
            yield return Settle();
            Check("Settings");
            game.GetComponent<OfflinePauseMenu>().Close();
            yield return Settle();

            var home = GameObject.Find("Home");
            PlayerProductScreens.Open(game, home, "Support");
            yield return Settle();
            Check("Support");
            typeof(PlayerProductScreens).GetMethod("ConfirmDelete", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            yield return Settle();
            Check("Support/ConfirmDelete");
            typeof(PlayerProductScreens).GetMethod("ShowPartyEntry", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            yield return Settle();
            Check("Party/Entry");
            typeof(PlayerProductScreens).GetMethod("Close", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            yield return Settle();

            AssertClean();
        }

        [UnityTest]
        public IEnumerator TutorialAndMatchHud_MeetPhysicalMinimums()
        {
            var game = GameBootstrap.Instance;
            game.touchInput.enabled = true;
            game.StartTutorial();
            yield return Settle();
            Check("Tutorial");

            game.StartOfflinePractice();
            yield return Settle();
            Check("Practice/Preparation");
            float deadline = Time.realtimeSinceStartup + 20f;
            var player = Object.FindFirstObjectByType<PlayerController>();
            while (Time.realtimeSinceStartup < deadline && (MatchView.Current == null || MatchView.Current.CurrentPhase != MatchPhase.InProgress))
                yield return null;
            yield return Settle();
            Check("Practice/Live");

            player = null;
            foreach (var p in PlayerController.All) if (!p.IsBot) player = p;
            player.ApplyDamage(1000, 999, Hotdrop.Config.WeaponId.Arc);
            yield return Settle();
            Check("Practice/Dead");

            game.GetComponent<OfflinePauseMenu>().Open();
            yield return Settle();
            Check("Practice/Paused");
            game.GetComponent<OfflinePauseMenu>().Close();
            yield return Settle();

            game.matchHUD.ShowResults(true, false, 120, 87, MatchView.Current?.Participants, "OFFLINE PRACTICE  ·  NO RATING OR CURRENCY");
            yield return Settle();
            Check("Results");

            AssertClean();
        }
    }
}



