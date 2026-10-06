using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.UI;

namespace Hotdrop.Tests
{
    /// <summary>
    /// Gate for the reusable UI foundation (Prompt 01): semantic token consistency, the 48 dp
    /// interactive floor, safe-area arithmetic across every supported landscape ratio, and the
    /// behaviour of the shared components.
    ///
    /// These are the rules a later screen can silently break, so they are asserted on the shared
    /// code rather than on any one screen.
    /// </summary>
    public class UIDesignFoundationTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();
        bool hadPresentationProfile;
        int oldPresentationProfile;

        [SetUp]
        public void SetUp()
        {
            const string key = "Hotdrop.UiPresentationProfile";
            hadPresentationProfile = PlayerPrefs.HasKey(key);
            oldPresentationProfile = PlayerPrefs.GetInt(key);
            UiPresentationProfile.Select(UiPresentationProfileId.Hybrid);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            spawned.Clear();
            UIMotionSettings.OverrideForTests = null;
            SafeAreaRoot.SimulatedScreen = null;
            SafeAreaRoot.SimulatedInsets = null;
            UiMetrics.TextScale = 1f;
            const string key = "Hotdrop.UiPresentationProfile";
            if (hadPresentationProfile) PlayerPrefs.SetInt(key, oldPresentationProfile);
            else PlayerPrefs.DeleteKey(key);
        }

        GameObject NewHost(string name = "Host")
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            spawned.Add(host);
            return host;
        }

        // ============================================================ progress + status strip

        [Test]
        public void ProgressBar_FillTracksTheFractionAndStaysClamped()
        {
            var host = NewHost();
            var bar = UIKit.ProgressBar(host.transform, 0.5f, UIDesignTokens.ObjectiveGold);
            var fill = (RectTransform)bar.Find("Fill");
            Assert.AreEqual(0.5f, fill.anchorMax.x, 0.001f, "half-filled bar");

            UIKit.SetProgress(bar, 1f);
            Assert.AreEqual(1f, fill.anchorMax.x, 0.001f, "full bar must reach the right edge");

            // Out-of-range input clamps instead of drawing outside the track.
            UIKit.SetProgress(bar, 4f);
            Assert.AreEqual(1f, fill.anchorMax.x, 0.001f, "clamped high");
            UIKit.SetProgress(bar, -2f);
            Assert.AreEqual(0f, fill.anchorMax.x, 0.001f, "clamped low");
            Assert.AreEqual(0f, fill.anchorMin.x, 0.001f, "fill always starts at the left edge");
        }

        [Test]
        public void ProgressBar_HasATrackAndItsFillIsNeverTheWholeBar()
        {
            var host = NewHost();
            var bar = UIKit.ProgressBar(host.transform, 1f);
            Assert.IsNotNull(bar.Find("Track"), "a visible track behind the fill");
            var fill = (RectTransform)bar.Find("Fill");
            Assert.AreEqual(Vector2.zero, fill.anchorMin, "fill is anchored to the left edge");
            Assert.AreEqual(0f, fill.anchorMin.y, 0.001f, "fill is anchored to the bottom");
            Assert.AreEqual(1f, fill.anchorMax.y, 0.001f, "fill spans the full track height");
        }

        [Test]
        public void ProgressBar_FillIsADriverSoCallersCanUpdateIt()
        {
            var host = NewHost();
            var bar = UIKit.ProgressBar(host.transform, 0f);
            var driver = bar.GetComponent<UiProgressBar>();
            Assert.IsNotNull(driver, "a bar can be updated without rebuilding the rect");
            driver.Fraction = 0.25f;
            Assert.AreEqual(0.25f, ((RectTransform)bar.Find("Fill")).anchorMax.x, 0.001f);
            Assert.AreEqual(0.25f, driver.Fraction, 0.001f, "the reported value matches the pixels");
        }

        [Test]
        public void StatusStrip_ShowsIdentityAndOmitsEmptySections()
        {
            var host = NewHost();
            var strip = UIKit.StatusStrip(host.transform, "VividSwift", "LV 14", "7 SEALS", "EU · 24 ms",
                () => { }, "Strip");

            // The identity role is TitleMedium, which DESIGN.md renders in caps.
            Assert.AreEqual("VIVIDSWIFT", strip.Find("Row/Identity").GetComponent<TMPro.TextMeshProUGUI>().text);
            Assert.AreEqual("LV 14", strip.Find("Row/Level").GetComponent<TMPro.TextMeshProUGUI>().text);
            Assert.AreEqual("7 SEALS", strip.Find("Row/Seals").GetComponent<TMPro.TextMeshProUGUI>().text);
            Assert.IsNotNull(strip.Find("Row/Settings"), "settings stays reachable from Home");
        }

        [Test]
        public void StatusStrip_WithoutSealsOrRegion_BuildsWithoutThoseRows()
        {
            // An offline guest build must not show an empty "0 SEALS" or a fabricated region.
            var host = NewHost();
            var strip = UIKit.StatusStrip(host.transform, "GUEST", "", "", "");
            Assert.AreEqual("GUEST", strip.Find("Row/Identity").GetComponent<TMPro.TextMeshProUGUI>().text);
            Assert.IsNull(strip.Find("Row/Seals"), "no empty seals row");
            Assert.IsNull(strip.Find("Row/Region"), "no fabricated region row");
            Assert.IsNull(strip.Find("Row/Settings"), "no settings control when none was supplied");
        }

        [Test]
        public void StatusStrip_KeepsIdentityLeftAndSettingsRight()
        {
            var host = NewHost();
            var strip = UIKit.StatusStrip(host.transform, "VividSwift", "LV 14", "7 SEALS", "EU · 24 ms", () => { });
            var group = strip.Find("Row").GetComponent<HorizontalLayoutGroup>();
            Assert.AreEqual(TextAnchor.MiddleLeft, group.childAlignment, "identity reads from the left");
            Assert.IsTrue(group.spacing > 0f, "sections are separated");
        }

        // ============================================================ tokens

        [Test]
        public void StructuralBorder_StretchesToHostAndUsesFourDistinctEdges()
        {
            var host = NewHost();
            var border = UIKit.Border(host.transform, "Frame", UIDesignTokens.ActionCyan, 2, 3);
            Assert.AreEqual(Vector2.zero, border.anchorMin);
            Assert.AreEqual(Vector2.one, border.anchorMax);
            var top = (RectTransform)border.Find("Top");
            var bottom = (RectTransform)border.Find("Bottom");
            var left = (RectTransform)border.Find("Left");
            var right = (RectTransform)border.Find("Right");
            Assert.AreEqual(1, top.anchorMin.y);
            Assert.AreEqual(0, bottom.anchorMin.y);
            Assert.AreEqual(0, left.anchorMin.x);
            Assert.AreEqual(1, right.anchorMin.x);
            UIKit.FrameOf(border).SetWeight(4);
            Assert.Greater(left.anchoredPosition.x, 0);
            Assert.Less(right.anchoredPosition.x, 0);
            Assert.Less(top.anchoredPosition.y, 0);
            Assert.Greater(bottom.anchoredPosition.y, 0);
        }

        [Test]
        public void Palette_MatchesTheShippingDesignLanguage()
        {
            // Hybrid profile: corrected surfaces and separate primary-action/team roles.
            Assert.AreEqual("101827", UIDesignTokens.Hex(UIDesignTokens.Background), "background");
            Assert.AreEqual("1B2940", UIDesignTokens.Hex(UIDesignTokens.SurfacePanel), "panel");
            Assert.AreEqual("263A59", UIDesignTokens.Hex(UIDesignTokens.SurfaceSelected), "selected surface");
            Assert.AreEqual("F4F7FB", UIDesignTokens.Hex(UIDesignTokens.TextPrimary), "primary text");
            Assert.AreEqual("BBC6D8", UIDesignTokens.Hex(UIDesignTokens.TextSecondary), "secondary text");
            Assert.AreEqual("3DE0C0", UIDesignTokens.Hex(UIDesignTokens.PrimaryAction), "primary action");
            Assert.AreEqual("4DA3FF", UIDesignTokens.Hex(UIDesignTokens.Ally), "ally");
            Assert.AreEqual("FF5A66", UIDesignTokens.Hex(UIDesignTokens.HazardCoral), "enemy");
            Assert.AreEqual("FFB84D", UIDesignTokens.Hex(UIDesignTokens.ObjectiveGold), "objective/reward");
        }

        [Test]
        public void Palette_HasExactlyOneSourceOfTruthForMatchHudAndMenuLayout()
        {
            // The HUD and the menus must not carry a competing palette beside the tokens.
            Assert.IsTrue(UIDesignTokens.SameColor(MatchHUD.Friendly, UIDesignTokens.Ally), "MatchHUD.Friendly");
            Assert.IsTrue(UIDesignTokens.SameColor(MatchHUD.Enemy, UIDesignTokens.HazardCoral), "MatchHUD.Enemy");
            Assert.IsTrue(UIDesignTokens.SameColor(MatchHUD.Objective, UIDesignTokens.ObjectiveGold), "MatchHUD.Objective");
            Assert.IsTrue(UIDesignTokens.SameColor(MatchHUD.MainText, UIDesignTokens.TextPrimary), "MatchHUD.MainText");
            Assert.IsTrue(UIDesignTokens.SameColor(MatchHUD.SecondaryText, UIDesignTokens.TextSecondary), "MatchHUD.SecondaryText");
            Assert.IsTrue(UIDesignTokens.SameColor(MatchHUD.PanelBg, UIDesignTokens.PanelBg), "MatchHUD.PanelBg");
            Assert.IsTrue(UIDesignTokens.SameColor(MenuLayout.Background, UIDesignTokens.Background), "MenuLayout.Background");
            Assert.IsTrue(UIDesignTokens.SameColor(MenuLayout.ButtonFill, UIDesignTokens.SurfacePanel), "MenuLayout.ButtonFill");
        }

        [Test]
        public void Tones_AreDistinctPerSignalSoColourIsNotTheOnlyCue()
        {
            // Friendly, enemy and objective must be three different hues, or "colour is never the
            // only signal" has nothing to stand on.
            Assert.AreNotEqual(UIDesignTokens.Hex(UIDesignTokens.ForTone(UiTone.Friendly)),
                UIDesignTokens.Hex(UIDesignTokens.ForTone(UiTone.Enemy)));
            Assert.AreNotEqual(UIDesignTokens.Hex(UIDesignTokens.ForTone(UiTone.Enemy)),
                UIDesignTokens.Hex(UIDesignTokens.ForTone(UiTone.Objective)));
            Assert.AreNotEqual(UIDesignTokens.Hex(UIDesignTokens.ForTone(UiTone.Friendly)),
                UIDesignTokens.Hex(UIDesignTokens.ForTone(UiTone.Objective)));
        }

        [Test]
        public void TextOnSurface_StaysReadableOnBothDarkAndFilledAccents()
        {
            // Filled accents take the dark canvas colour; dark surfaces take light text.
            Assert.IsTrue(UIDesignTokens.SameColor(UIDesignTokens.TextOn(UIDesignTokens.ActionCyan), UIDesignTokens.TextOnAccent));
            Assert.IsTrue(UIDesignTokens.SameColor(UIDesignTokens.TextOn(UIDesignTokens.HazardCoral), UIDesignTokens.TextOnAccent));
            Assert.IsTrue(UIDesignTokens.SameColor(UIDesignTokens.TextOn(UIDesignTokens.SurfacePanel), UIDesignTokens.TextPrimary));
            Assert.IsTrue(UIDesignTokens.SameColor(UIDesignTokens.TextOn(UIDesignTokens.Background), UIDesignTokens.TextPrimary));
        }

        [Test]
        public void TypographyRoles_NeverFallBelowTheTextFloor()
        {
            foreach (var role in UITypography.AllRoles)
            {
                var style = UITypography.Style(role);
                Assert.GreaterOrEqual(style.designDp, UiMetrics.MinTextDp,
                    $"{role} is {style.designDp} dp, below the {UiMetrics.MinTextDp} dp floor");
            }
        }

        [Test]
        public void TypographyRoles_NeverRenderBelowTheTextFloorAtAnyComfortScale()
        {
            try
            {
                foreach (var scale in new[] { 1f, UiMetrics.LargerTextScale, 0.75f })
                {
                    UiMetrics.TextScale = scale;
                    foreach (var role in UITypography.AllRoles)
                        Assert.GreaterOrEqual(UITypography.Style(role).SizeUnits, UiMetrics.MinTextUnits - 0.001f,
                            $"{role} at comfort scale {scale} renders below the floor");
                }
            }
            finally { UiMetrics.TextScale = 1f; }
        }

        [Test]
        public void DataRoles_AreTabularAndProseRolesAreNot()
        {
            foreach (var role in UITypography.AllRoles)
            {
                bool isData = role == UiTextRole.DataLarge || role == UiTextRole.DataMedium || role == UiTextRole.DataSmall;
                Assert.AreEqual(isData, UITypography.IsTabular(role), $"{role} tabular flag");
            }
        }

        [Test]
        public void ClockAndAmmo_AreFixedWidthSoValuesDoNotJitter()
        {
            Assert.AreEqual("0:05", UITypography.Clock(4.1f));
            Assert.AreEqual("1:00", UITypography.Clock(59.9f));
            Assert.AreEqual("12:34", UITypography.Clock(754f));
            Assert.AreEqual("24/30", UITypography.Ammo(24, 30));
            Assert.AreEqual("0/0", UITypography.Ammo(-1, 0), "negative values are clamped, not rendered");
        }

        [Test]
        public void TextExpansionTolerance_ShrinksTextToFitThirtyPercentWider()
        {
            // 100 dp of natural text in 110 dp of space fits at full size once expanded to 130.
            Assert.AreEqual(100f, UITypography.SizeThatFits(100f, 100f, 130f), 0.01f);

            // In only 110 dp it must shrink to 110 / 1.3 of the natural width.
            Assert.AreEqual(110f / 1.3f, UITypography.SizeThatFits(100f, 100f, 110f), 0.01f);

            // It never shrinks below the ~12 dp floor no matter how narrow the box gets.
            Assert.AreEqual(UiMetrics.MinTextDp, UITypography.SizeThatFits(100f, 100f, 1f), 0.01f);
        }

        [Test]
        public void MotionDurations_AllSitInsideThe180To240MillisecondBand()
        {
            foreach (MotionRole role in System.Enum.GetValues(typeof(MotionRole)))
            {
                float seconds = UIMotion.Duration(role);
                Assert.GreaterOrEqual(seconds, UIDesignTokens.MotionMinSeconds - 0.0001f, $"{role} too fast");
                Assert.LessOrEqual(seconds, UIDesignTokens.MotionMaxSeconds + 0.0001f, $"{role} too slow");
            }
        }

        // ============================================================ motion

        [Test]
        public void EaseOut_IsZeroAtStartAndOneAtEnd()
        {
            Assert.AreEqual(0f, UIMotion.EaseOut(0f), 0.0001f);
            Assert.AreEqual(1f, UIMotion.EaseOut(1f), 0.0001f);
            Assert.Greater(UIMotion.EaseOut(0.25f), 0.25f, "ease-out must lead, not trail");
        }

        [Test]
        public void ReduceMotion_ReplacesTranslationAndScaleWithFades()
        {
            UIMotionSettings.OverrideForTests = true;
            Assert.IsFalse(UIMotion.Animates(MotionChannel.Translation));
            Assert.IsFalse(UIMotion.Animates(MotionChannel.Scale));
            Assert.IsTrue(UIMotion.Animates(MotionChannel.Opacity), "opacity is the channel Reduce Motion keeps");

            // Translation and scale hold their start value: nothing moves.
            Assert.AreEqual(0f, UIMotion.Value(0f, 100f, 0.5f, MotionChannel.Translation), 0.0001f);
            Assert.AreEqual(1f, UIMotion.Value(1f, 1.4f, 0.5f, MotionChannel.Scale), 0.0001f);
            // Opacity still animates.
            Assert.AreEqual(0.5f, UIMotion.Value(0f, 1f, 0.5f, MotionChannel.Opacity), 0.0001f);
        }

        [Test]
        public void ReduceMotion_CollapsesTravelDistanceToZero()
        {
            var host = NewHost();
            var rect = UIKit.Rect(host.transform, "Panel", new Vector2(200f, 100f));

            UIMotionSettings.OverrideForTests = false;
            var moving = UIMotion.Play(rect, null, 0f, 1f, MotionRole.Standard, MotionChannel.Translation,
                new Vector2(0f, -20f), Vector2.zero, 1f, 1f);
            Assert.AreNotEqual(Vector2.zero, moving.fromOffset, "normal motion should travel");

            UIMotionSettings.OverrideForTests = true;
            var still = UIMotion.Play(rect, null, 0f, 1f, MotionRole.Standard, MotionChannel.Translation,
                new Vector2(0f, -20f), Vector2.zero, 1f, 1f);
            Assert.AreEqual(Vector2.zero, still.fromOffset, "Reduce Motion must not travel");
            Assert.AreEqual(1f, still.fromScale, "Reduce Motion must not scale");
        }

        [Test]
        public void MotionDriver_RunsATransitionToCompletionAndFiresItsCallback()
        {
            var host = NewHost();
            var group = host.AddComponent<CanvasGroup>();
            var driver = host.AddComponent<UiMotionDriver>();
            bool completed = false;

            driver.Play(UIMotion.Fade(group, null, 0f, 1f, MotionRole.Standard, () => completed = true));

            driver.Tick(UIDesignTokens.MotionStandardSeconds * 0.5f);
            Assert.AreEqual(0.5f, group.alpha, 0.05f, "half a fade duration is roughly half way");
            Assert.IsFalse(completed);

            driver.Tick(UIDesignTokens.MotionStandardSeconds);
            Assert.AreEqual(1f, group.alpha, 0.001f);
            Assert.IsTrue(completed, "the end-state callback must fire");
            Assert.AreEqual(0, driver.Count, "a finished transition is dropped from the driver");
        }

        // ============================================================ 48 dp floor

        [Test]
        public void MinTouchUnits_IsExactly48Dp()
        {
            Assert.AreEqual(48f, UiMetrics.MinTouchDp, 0.0001f);
            // In the authoring space (UiMetrics.DpPerUnit dp per canvas unit) the floor really is
            // 48 dp, which is the invariant every screen relies on.
            Assert.AreEqual(48f, UiMetrics.DpForUnits(UiMetrics.MinTouchUnits), 0.001f,
                "the floor constant must really be 48 dp, not merely named as such");
            Assert.AreEqual(12f, UiMetrics.DpForUnits(UiMetrics.MinTextUnits), 0.001f);
        }

        [Test]
        public void Button_GrowsASmallTransformUpToTheInteractiveFloor()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "PLAY", ButtonVariant.Primary);
            var rt = button.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10f, 10f);

            button.EnforceMinimumSize();

            Assert.GreaterOrEqual(rt.sizeDelta.x, UiMetrics.MinTouchUnits - 0.001f, "width floor");
            Assert.GreaterOrEqual(rt.sizeDelta.y, UiMetrics.MinTouchUnits - 0.001f, "height floor");
        }

        [Test]
        public void Button_UnderALayoutGroup_FloorsThroughALayoutElementInstead()
        {
            var host = NewHost();
            var column = UIKit.Column(host.transform, "Column");
            var button = UIKit.Button(column, "PLAY", ButtonVariant.Primary);
            var rt = button.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(10f, 10f);

            button.EnforceMinimumSize();

            var element = button.GetComponent<LayoutElement>();
            Assert.IsNotNull(element, "a layout-driven button must carry a LayoutElement");
            Assert.GreaterOrEqual(element.minHeight, UiMetrics.MinTouchUnits - 0.001f);
            Assert.GreaterOrEqual(element.minWidth, UiMetrics.MinTouchUnits - 0.001f);
        }

        [Test]
        public void EveryInteractiveComponentTheKitBuilds_ClearsTheFloor()
        {
            var host = NewHost();
            var column = UIKit.Column(host.transform, "Column");

            // Only tap targets. A display-only control such as a progress bar is exempt, exactly
            // as UiFloor already treats a non-interactive Selectable.
            var components = new List<RectTransform>
            {
                UIKit.Button(column, "PRIMARY", ButtonVariant.Primary).transform as RectTransform,
                UIKit.IconButton(column, "BACK", UiGlyph.Back).transform as RectTransform,
                UIKit.NavItem(column, "PLAY").transform as RectTransform,
                SliderView.Create(column, "VOLUME").transform as RectTransform,
                ToggleView.Create(column, "REDUCE MOTION").transform as RectTransform,
                InputFieldView.Create(column, "CALLSIGN").transform as RectTransform,
            };

            foreach (var rt in components)
            {
                // A LayoutElement is enough: the layout group turns it into a real rect.
                var element = rt.GetComponent<LayoutElement>();
                Assert.IsNotNull(element, $"{rt.name} has no LayoutElement to carry its floor");
                Assert.GreaterOrEqual(element.minHeight, UiMetrics.MinTouchUnits - 0.001f,
                    $"{rt.name} is below the 48 dp floor");
            }
        }

        [Test]
        public void DisplayOnlyComponents_AreNotTapTargets()
        {
            var host = NewHost();
            var column = UIKit.Column(host.transform, "Column");

            var progress = ProgressBarView.Create(column);
            var badge = UIKit.StatusBadge(column, "[ZONE A // CAPTURED]", UiTone.Objective);

            foreach (var rt in new[] { progress.transform as RectTransform, badge })
            {
                Assert.IsNull(rt.GetComponent<Selectable>(), $"{rt.name} must not be selectable");
                foreach (var image in rt.GetComponentsInChildren<Image>())
                    Assert.IsFalse(image.raycastTarget, $"{rt.name} must not intercept a tap");
            }
        }

        // ============================================================ safe area

        [Test]
        public void SupportedAspects_CoverSixteenNineThroughTwentyNine()
        {
            Assert.AreEqual(16f / 9f, SafeAreaMath.Ratio(LandscapeAspect.SixteenByNine), 0.001f);
            Assert.AreEqual(18f / 9f, SafeAreaMath.Ratio(LandscapeAspect.EighteenByNine), 0.001f);
            Assert.AreEqual(19.5f / 9f, SafeAreaMath.Ratio(LandscapeAspect.NineteenPointFiveByNine), 0.001f);
            Assert.AreEqual(20f / 9f, SafeAreaMath.Ratio(LandscapeAspect.TwentyByNine), 0.001f);
        }

        [Test]
        public void SafeArea_HonoursTheOsCutoutAndTheFixedLateralMargin()
        {
            // A 20:9 2400x1080 phone at 400 dpi with a 40 dp notch on the left edge.
            float dpi = 400f;
            var notch = SafeAreaMath.Notch(1080f, 40f, dpi, -1);
            var insets = SafeAreaMath.ForAspect(LandscapeAspect.TwentyByNine, 1080f, dpi, notch);

            float scale = SafeAreaMath.ScaleFor(2400f, 1080f, dpi);
            // The notch alone would be this many units; the fixed 48 dp margin is larger, so the
            // margin must win on the cut edge.
            float notchUnits = notch.left / scale;
            float marginUnits = UiMetrics.UnitsForDp(UIDesignTokens.EdgeMarginDp);
            Assert.GreaterOrEqual(insets.left, marginUnits - 0.001f, "left edge must clear the 48 dp margin");
            Assert.Greater(insets.left, notchUnits - 0.001f, "left edge must clear the notch");
            Assert.GreaterOrEqual(insets.right, marginUnits - 0.001f, "right edge must clear the 48 dp margin");
        }

        [Test]
        public void SafeArea_AlwaysReservesTheBottomGestureBand()
        {
            foreach (var aspect in new[] { LandscapeAspect.SixteenByNine, LandscapeAspect.EighteenByNine, LandscapeAspect.NineteenPointFiveByNine, LandscapeAspect.TwentyByNine })
            {
                // A device that reports no insets at all still gets the gesture reserve.
                var insets = SafeAreaMath.ForAspect(aspect, 1080f, 400f, ScreenInsets.None);
                Assert.GreaterOrEqual(insets.bottom, UiMetrics.UnitsForDp(UIDesignTokens.GestureInsetDp) - 0.001f,
                    $"{aspect} must reserve the gesture area");
            }
        }

        [Test]
        public void SafeArea_IsNeverNegativeAndNeverEatsTheScreen()
        {
            foreach (var aspect in new[] { LandscapeAspect.SixteenByNine, LandscapeAspect.EighteenByNine, LandscapeAspect.NineteenPointFiveByNine, LandscapeAspect.TwentyByNine })
            {
                float ratio = SafeAreaMath.Ratio(aspect);
                float widthPx = ratio * 1080f;
                float scale = SafeAreaMath.ScaleFor(widthPx, 1080f, 400f);
                var insets = SafeAreaMath.ForAspect(aspect, 1080f, 400f, ScreenInsets.None);
                var content = SafeAreaMath.ContentRect(widthPx / scale, 1080f / scale, insets);

                Assert.GreaterOrEqual(insets.left, 0f, $"{aspect} left inset");
                Assert.GreaterOrEqual(insets.right, 0f, $"{aspect} right inset");
                Assert.GreaterOrEqual(insets.top, 0f, $"{aspect} top inset");
                Assert.GreaterOrEqual(insets.bottom, 0f, $"{aspect} bottom inset");
                Assert.Greater(content.width, 0f, $"{aspect} leaves no usable width");
                Assert.Greater(content.height, 0f, $"{aspect} leaves no usable height");
            }
        }

        [Test]
        public void SafeArea_ClampsAPathologicalCutoutSoContentSurvives()
        {
            // A device that reports an inset covering the whole screen must not produce an
            // empty or negative content rect.
            var everything = new ScreenInsets { left = 4000f, right = 4000f, top = 4000f, bottom = 4000f };
            var insets = SafeAreaMath.Compute(2400f, 1080f, 400f, everything);
            var content = SafeAreaMath.ContentRect(2400f, 1080f, insets);
            Assert.GreaterOrEqual(content.width, 0f);
            Assert.GreaterOrEqual(content.height, 0f);
            Assert.LessOrEqual(insets.Horizontal, 2400f, "insets cannot exceed the screen");
            Assert.LessOrEqual(insets.Vertical, 1080f, "insets cannot exceed the screen");
        }

        [Test]
        public void SafeArea_FixedMarginsMeanUltrawideKeepsMoreUsableWidth()
        {
            // Ultrawide is the same phone height with more width, and the margins are a fixed
            // dp value rather than a percentage - so an ultrawide device keeps more usable width.
            var narrowInsets = SafeAreaMath.ForAspect(LandscapeAspect.SixteenByNine, 1080f, 400f, ScreenInsets.None);
            var wideInsets = SafeAreaMath.ForAspect(LandscapeAspect.TwentyByNine, 1080f, 400f, ScreenInsets.None);

            Assert.AreEqual(narrowInsets.left, wideInsets.left, 0.001f, "the lateral margin is fixed in dp");
            Assert.AreEqual(narrowInsets.right, wideInsets.right, 0.001f, "the lateral margin is fixed in dp");

            float narrowWidth = SafeAreaMath.Ratio(LandscapeAspect.SixteenByNine) * 1080f;
            float wideWidth = SafeAreaMath.Ratio(LandscapeAspect.TwentyByNine) * 1080f;
            float scale = SafeAreaMath.ScaleFor(narrowWidth, 1080f, 400f);
            var narrowContent = SafeAreaMath.ContentRect(narrowWidth / scale, 1080f / scale, narrowInsets);
            var wideContent = SafeAreaMath.ContentRect(wideWidth / scale, 1080f / scale, wideInsets);

            Assert.Greater(wideContent.width, narrowContent.width, "20:9 must yield more usable width than 16:9");
        }

        [Test]
        public void SafeAreaRoot_AppliesItsInsetsToTheContentRect()
        {
            var host = NewHost();
            var rect = host.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1500f, 675f);

            var root = host.AddComponent<SafeAreaRoot>();
            root.Apply();

            var applied = root.Applied;
            Assert.GreaterOrEqual(applied.left, UiMetrics.UnitsForDp(UIDesignTokens.EdgeMarginDp) - 0.001f);
            Assert.GreaterOrEqual(applied.bottom, UiMetrics.UnitsForDp(UIDesignTokens.GestureInsetDp) - 0.001f);

            var content = root.Content;
            Assert.IsNotNull(content);
            Assert.Greater(content.offsetMin.x, 0f, "content is pushed clear of the left inset");
            Assert.Greater(content.offsetMin.y, 0f, "content is pushed clear of the gesture area");
            Assert.Less(content.offsetMax.x, 0f, "content is pulled in from the right inset");
            Assert.Less(content.offsetMax.y, 0f, "content is pulled in from the top inset");
        }

        [Test]
        public void SafeAreaRoot_ApplyIsIdempotentAndReentrancySafe()
        {
            // Regression: with no explicit content child, Apply() resizes the root's own rect,
            // which raised OnRectTransformDimensionsChange and re-entered Apply() forever.
            var host = NewHost();
            host.GetComponent<RectTransform>().sizeDelta = new Vector2(1500f, 675f);
            var root = host.AddComponent<SafeAreaRoot>();

            root.Apply();
            var first = root.Applied;
            root.Apply();
            root.Apply();

            Assert.AreEqual(first.left, root.Applied.left, 0.0001f, "repeated applies must be stable");
            Assert.AreEqual(first.bottom, root.Applied.bottom, 0.0001f);
            Assert.AreEqual(first.left, root.Content.offsetMin.x, 0.001f,
                "the offsets must match the computed insets, not drift");
        }

        [Test]
        public void SafeAreaMath_Contains_RejectsAControlUnderACutout()
        {
            var insets = SafeAreaMath.Compute(2400f, 1080f, 400f, ScreenInsets.None);
            var content = SafeAreaMath.ContentRect(1500f, 675f, insets);

            var inside = new Rect(content.xMin + 10f, content.yMin + 10f, 100f, 100f);
            var underCutout = new Rect(0f, 0f, 40f, 40f);
            var overflowRight = new Rect(content.xMax - 10f, content.yMin, 100f, 40f);

            Assert.IsTrue(SafeAreaMath.Contains(content, inside));
            Assert.IsFalse(SafeAreaMath.Contains(content, underCutout), "a control under the margin must be rejected");
            Assert.IsFalse(SafeAreaMath.Contains(content, overflowRight), "a control past the right margin must be rejected");
        }

        // ============================================================ component states

        [Test]
        public void Button_StartsNormalAndAcceptsAPress()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "PLAY", ButtonVariant.Primary);

            Assert.AreEqual(HotdropButtonState.Normal, button.State);

            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.AreEqual(HotdropButtonState.Pressed, button.State);
            Assert.IsFalse(button.IsBlocked, "a pressed button is still usable");
        }

        [Test]
        public void Button_HeldStateArrivesAfterTheHoldThreshold()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "FIRE", ButtonVariant.Primary);
            button.HoldThresholdSeconds = 0.2f;

            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            button.Tick(0.1f);
            Assert.AreEqual(HotdropButtonState.Pressed, button.State, "a short press is still Pressed");

            button.Tick(0.2f);
            Assert.AreEqual(HotdropButtonState.Held, button.State, "a sustained press becomes Held");
        }

        [Test]
        public void Button_ReleasingReturnsToNormalAndRaisesPressed()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "PLAY", ButtonVariant.Primary);
            int presses = 0;
            button.Pressed += () => presses++;

            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            button.OnPointerUp(new UnityEngine.EventSystems.PointerEventData(null));

            Assert.AreEqual(HotdropButtonState.Normal, button.State);
            Assert.AreEqual(1, presses);
        }

        [Test]
        public void Button_HeldPressDoesNotAlsoCountAsATap()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "FIRE", ButtonVariant.Primary);
            button.HoldThresholdSeconds = 0.1f;
            int presses = 0;
            button.Pressed += () => presses++;

            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            button.Tick(0.2f);
            button.OnPointerUp(new UnityEngine.EventSystems.PointerEventData(null));

            Assert.AreEqual(0, presses, "releasing a held press is not a tap");
            Assert.AreEqual(HotdropButtonState.Normal, button.State);
        }

        [Test]
        public void Button_DisabledStateBlocksPresses()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "PLAY", ButtonVariant.Primary);
            button.SetInteractable(false);

            Assert.AreEqual(HotdropButtonState.Disabled, button.State);
            Assert.IsTrue(button.IsBlocked);

            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.AreEqual(HotdropButtonState.Disabled, button.State, "a disabled button cannot be pressed");

            button.SetInteractable(true);
            Assert.AreEqual(HotdropButtonState.Normal, button.State);
        }

        [Test]
        public void Button_LoadingStateBlocksPressesAndClearsAfterwards()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "SUBMIT", ButtonVariant.Primary);
            button.SetLoading(true);

            Assert.AreEqual(HotdropButtonState.Loading, button.State);
            Assert.IsTrue(button.IsLoading);
            Assert.IsTrue(button.IsBlocked);
            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.AreEqual(HotdropButtonState.Loading, button.State, "a loading button cannot be pressed");

            button.SetLoading(false);
            Assert.AreEqual(HotdropButtonState.Normal, button.State);
        }

        [Test]
        public void Button_CooldownCountsDownAndThenReleasesTheButton()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "REDEEM", ButtonVariant.Primary);
            button.BeginCooldown(1f);

            Assert.AreEqual(HotdropButtonState.Cooldown, button.State);
            Assert.IsTrue(button.IsBlocked);
            Assert.AreEqual(1f, button.CooldownRemaining, 0.001f);
            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.AreEqual(HotdropButtonState.Cooldown, button.State, "a cooling button cannot be pressed");

            button.Tick(0.4f);
            Assert.AreEqual(HotdropButtonState.Cooldown, button.State);
            Assert.AreEqual(0.6f, button.CooldownRemaining, 0.05f);

            button.Tick(0.7f);
            Assert.AreEqual(HotdropButtonState.Normal, button.State);
            Assert.AreEqual(0f, button.CooldownRemaining, 0.001f);
            Assert.IsFalse(button.IsBlocked, "the button works again once the cooldown is over");
        }

        [Test]
        public void Button_CooldownWinsOverNormalButYieldsToLoading()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "REDEEM", ButtonVariant.Primary);

            button.BeginCooldown(0.5f);
            button.SetLoading(true);
            Assert.AreEqual(HotdropButtonState.Loading, button.State, "loading is the more specific block");

            button.SetLoading(false);
            Assert.AreEqual(HotdropButtonState.Cooldown, button.State, "the cooldown is still running underneath");

            button.CancelCooldown();
            Assert.AreEqual(HotdropButtonState.Normal, button.State);
        }

        [Test]
        public void Button_ZeroLengthCooldownIsANoOp()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "REDEEM", ButtonVariant.Primary);
            button.BeginCooldown(0f);
            Assert.AreEqual(HotdropButtonState.Normal, button.State);
            Assert.AreEqual(0f, button.CooldownRemaining);
        }

        [Test]
        public void Button_DraggingOffReleasesAHold()
        {
            var host = NewHost();
            var button = UIKit.Button(host.transform, "FIRE", ButtonVariant.Primary);
            button.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
            button.Tick(0.5f);
            Assert.AreEqual(HotdropButtonState.Held, button.State);

            button.OnPointerExit(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.AreEqual(HotdropButtonState.Normal, button.State, "leaving the control releases the hold");
        }

        [Test]
        public void Button_VariantsChangeTheirFillNotJustTheirAccent()
        {
            var host = NewHost();

            var primary = UIKit.Button(host.transform, "PRIMARY", ButtonVariant.Primary);
            var secondary = UIKit.Button(host.transform, "SECONDARY", ButtonVariant.Secondary);
            var destructive = UIKit.Button(host.transform, "DESTRUCTIVE", ButtonVariant.Destructive);

            var primaryFill = primary.transform.Find("Fill").GetComponent<Image>().color;
            var secondaryFill = secondary.transform.Find("Fill").GetComponent<Image>().color;
            var destructiveFill = destructive.transform.Find("Fill").GetComponent<Image>().color;

            Assert.AreNotEqual(primaryFill, secondaryFill, "primary and secondary must not look alike");
            Assert.AreNotEqual(primaryFill, destructiveFill, "a destructive action must not look like a safe one");
            Assert.AreNotEqual(secondaryFill, destructiveFill);
        }

        [Test]
        public void IconOnlyButton_KeepsItsAccessibleCaption()
        {
            var host = NewHost();
            var button = UIKit.IconButton(host.transform, "GO BACK", UiGlyph.Back);
            Assert.IsTrue(button.IconOnly);
            // The caption is hidden visually but must remain in the label for screen readers.
            button.SetCaption("GO BACK");
            var label = button.transform.Find("Content/Label").GetComponent<TMPro.TextMeshProUGUI>();
            Assert.AreEqual("GO BACK", label.text);
        }

        // ============================================================ cues

        [Test]
        public void Cues_GiveEachSignalADistinctShapeAndAWord()
        {
            var host = NewHost();

            var friendly = UICue.Create(host.transform, UiTone.Friendly);
            var enemy = UICue.Create(host.transform, UiTone.Enemy);
            var objective = UICue.Create(host.transform, UiTone.Objective);

            Assert.AreEqual(CueShape.Ring, friendly.Shape);
            Assert.AreEqual(CueShape.SplitChevron, enemy.Shape);
            Assert.AreEqual(CueShape.RingDiamond, objective.Shape);

            // Shape, colour and word are three redundant channels.
            Assert.AreNotEqual(friendly.Shape, enemy.Shape);
            Assert.AreNotEqual(enemy.Shape, objective.Shape);
            Assert.IsTrue(friendly.HasTextSignal, "friendly cue needs its word");
            Assert.IsTrue(enemy.HasTextSignal, "enemy cue needs its word");
            Assert.IsTrue(objective.HasTextSignal, "objective cue needs its word");
            Assert.AreEqual("ALLY", friendly.Label);
            Assert.AreEqual("ENEMY", enemy.Label);
            Assert.AreEqual("ZONE", objective.Label);
        }

        [Test]
        public void Cues_BuildGeometryRatherThanDependingOnAnIconAsset()
        {
            var host = NewHost();
            var cue = UICue.Create(host.transform, UiTone.Enemy);
            Assert.Greater(cue.transform.childCount, 0, "the cue draws real geometry");

            var images = cue.GetComponentsInChildren<Image>();
            Assert.Greater(images.Length, 0);
            foreach (var image in images)
                Assert.IsFalse(image.raycastTarget, "a cue is decoration, not a tap target");
        }

        // ============================================================ overlays

        [Test]
        public void NetworkIndicator_CarriesItsStateInBothBarsAndAWord()
        {
            var host = NewHost();
            var indicator = NetworkIndicatorView.Create(host.transform);

            indicator.SetQuality(4, false);
            Assert.AreEqual("EXCELLENT", indicator.Readout);
            Assert.IsFalse(indicator.IsOffline);

            indicator.SetQuality(1, false);
            Assert.AreEqual("POOR", indicator.Readout);

            indicator.SetQuality(3, true);
            Assert.AreEqual("OFFLINE", indicator.Readout);
            Assert.IsTrue(indicator.IsOffline);
        }

        [Test]
        public void OfflineBanner_TogglesVisibilityAndKeepsACaption()
        {
            var host = NewHost();
            var banner = OfflineBannerView.Create(host.transform, "OFFLINE — SAVED CONTENT STILL WORKS");
            Assert.IsFalse(banner.IsVisible);

            banner.SetVisible(true);
            Assert.IsTrue(banner.IsVisible);

            banner.SetVisible(false);
            Assert.IsFalse(banner.IsVisible);
        }

        [Test]
        public void Modal_ClosesItsOwnActionsAreNotTheExitPath()
        {
            var host = NewHost();
            var modal = ModalView.Create(host.transform, "CONFIRM", "LEAVE THE MATCH?");
            var cancel = modal.AddAction("CANCEL", ButtonVariant.Secondary, null);
            var confirm = modal.AddAction("LEAVE", ButtonVariant.Destructive, null);

            Assert.IsNotNull(cancel, "a modal needs a way out");
            Assert.IsNotNull(confirm);
            Assert.AreEqual(2, modal.transform.Find("Panel").GetComponentInChildren<HorizontalLayoutGroup>().transform.childCount);
        }

        [Test]
        public void ErrorRetry_OffersARetryThatCanBePutIntoItsLoadingState()
        {
            var host = NewHost();
            var error = ErrorRetryView.Create(host.transform, "COULD NOT REACH THE SERVER.", null);

            Assert.IsNotNull(error.Retry, "an error without a retry is a dead end");
            error.SetRetrying(true);
            Assert.IsTrue(error.Retry.IsLoading);
            error.SetRetrying(false);
            Assert.IsFalse(error.Retry.IsLoading);

            error.SetMessage("OFFLINE PRACTICE IS STILL AVAILABLE.");
        }

        // ============================================================ controls

        [Test]
        public void ProgressBar_ClampsAndReportsItsValueInText()
        {
            var host = NewHost();
            var bar = ProgressBarView.Create(host.transform, "ProgressBar", UiTone.Friendly, readout: true);

            bar.SetValue(0.5f);
            Assert.AreEqual(0.5f, bar.Value, 0.0001f);
            Assert.AreEqual("50%", bar.ValueLabel.text, "bar length is always paired with a number");

            bar.SetValue(2f);
            Assert.AreEqual(1f, bar.Value, 0.0001f, "an over-range value clamps rather than overflowing");
            bar.SetValue(-1f);
            Assert.AreEqual(0f, bar.Value, 0.0001f);
        }

        [Test]
        public void Toggle_MovesItsThumbAndKeepsItsValue()
        {
            var host = NewHost();
            var toggle = ToggleView.Create(host.transform, "REDUCE MOTION", false);
            Assert.IsFalse(toggle.Value);

            var thumb = toggle.transform.Find("Row/Track/Thumb").GetComponent<RectTransform>();
            toggle.SetValue(true);
            var engagedX = thumb.anchoredPosition.x;

            toggle.SetValue(false);
            var restX = thumb.anchoredPosition.x;

            Assert.IsFalse(toggle.Value);
            // The thumb travels right when on and comes back to the left inset when off. It never
            // leaves the track, so the assertion is relative movement, not an absolute position.
            Assert.Greater(engagedX, restX, "the thumb must move right when the toggle is on");
            Assert.LessOrEqual(restX, UIDesignTokens.Dp(4f) + thumb.sizeDelta.x * 0.5f + 0.01f,
                "the off position sits at the left inset");
        }

        [Test]
        public void Slider_ShowsItsValueAndNeverGoesBelowItsRange()
        {
            var host = NewHost();
            // A 0-100 range so the "0%" format reports the value the player actually set.
            var slider = SliderView.Create(host.transform, "VOLUME", 0f, 100f, 50f);
            slider.SetValue(75f, "0%");

            Assert.AreEqual(75f, slider.Slider.value, 0.001f);
            Assert.AreEqual("75%", slider.Readout.text, "the readout shows the live value, not the format");

            // A decimal format keeps its own precision.
            slider.SetValue(62.5f, "0.0");
            Assert.AreEqual("62.5", slider.Readout.text);

            slider.Slider.value = -3f;
            Assert.AreEqual(0f, slider.Slider.value, 0.001f, "the slider clamps to its own range");
            Assert.AreEqual("0.0", slider.Readout.text, "the readout follows a clamped drag");
        }

        [Test]
        public void InputField_HonoursItsCharacterLimit()
        {
            var host = NewHost();
            var input = InputFieldView.Create(host.transform, "CALLSIGN", "ENTER A NAME", 8);
            Assert.AreEqual(8, input.Input.characterLimit);
            Assert.AreEqual("", input.Value);
        }

        [Test]
        public void RoundedSprite_CacheIsBoundedWhenTheSameSizeIsRebuiltRepeatedly()
        {
            // Warm the cache, then confirm that rebuilding the same geometry adds nothing: a
            // screen that is torn down and rebuilt must not grow the texture count.
            UIRoundedSprites.RoundedRect(UIDesignTokens.Dp(UIDesignTokens.RadiusMdDp), 2f);
            int before = UIRoundedSprites.CachedCount;
            for (int i = 0; i < 50; i++) UIRoundedSprites.RoundedRect(UIDesignTokens.Dp(UIDesignTokens.RadiusMdDp), 2f);
            Assert.AreEqual(before, UIRoundedSprites.CachedCount,
                "rebuilding the same geometry must not keep allocating sprites");
        }

        // ============================================================ gallery

        [Test]
        public void Gallery_IsNeverAvailableInANonDevelopmentPlayer()
        {
            // The gate that keeps a review tool out of a shipped navigation path.
            bool available = Application.isEditor || Debug.isDebugBuild;
            Assert.AreEqual(available, UIComponentGallery.IsAvailable);
            if (!available)
            {
                Assert.IsNull(UIComponentGallery.Open(), "a release player must not open the gallery");
                Assert.IsNotEmpty(UIComponentGallery.UnavailableReason);
            }
        }

        [Test]
        public void Gallery_DemonstratesEveryButtonState()
        {
            var host = NewHost();
            var view = UIComponentGalleryView.Build(host.transform, LandscapeAspect.SixteenByNine);

            // Six variants plus six states, all reachable from the gallery.
            Assert.GreaterOrEqual(view.StateSpecimenCount, 10, "every variant and every state needs a specimen");

            var states = new HashSet<HotdropButtonState>();
            foreach (var button in view.GetComponentsInChildren<HotdropButton>(true))
                states.Add(button.State);

            foreach (HotdropButtonState state in System.Enum.GetValues(typeof(HotdropButtonState)))
                Assert.IsTrue(states.Contains(state), $"the gallery never shows the {state} state");

            Object.DestroyImmediate(view.gameObject);
        }

        [Test]
        public void Gallery_BuildsAndSwitchesAspectRatios()
        {
            var host = NewHost();
            var view = UIComponentGalleryView.Build(host.transform, LandscapeAspect.SixteenByNine);
            Assert.AreEqual(LandscapeAspect.SixteenByNine, view.Aspect);

            view.NextAspect();
            Assert.AreEqual(LandscapeAspect.EighteenByNine, view.Aspect);
            view.SetAspect(LandscapeAspect.TwentyByNine);
            Assert.AreEqual(LandscapeAspect.TwentyByNine, view.Aspect);

            Object.DestroyImmediate(view.gameObject);
        }
    }
}
