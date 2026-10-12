using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hotdrop.Core;
using Hotdrop.Match;
using Hotdrop.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    public enum ResultsTrustState
    {
        Practice,
        Cancelled,
        Pending,
        Verified,
        RecordedIneligible,
        Unavailable,
        UnverifiedDevelopment,
    }

    /// <summary>
    /// Screen 11 results, rebuilt from its UIUX HTML (CSS px). The page scrolls under a sticky
    /// action bar, as the reference does. Every figure is the match's own: where the reference
    /// shows data this game does not record (ping, telemetry, pad-use attribution) the slot carries
    /// a real equivalent instead of an invented number.
    /// </summary>
    public static class ResultsView
    {
        sealed class ReflowSnapshot
        {
            internal MatchHUD hud;
            internal bool win, draw;
            internal int ally, enemy;
            internal IReadOnlyList<IMatchParticipant> players;
            internal IMatchParticipant local;
            internal string footer, nextGoal;
            internal string personalBest;
            internal Action replay, home;
            internal ResultsTrustState trustState;
            internal Action<int> reportParticipant;
            internal Func<int, bool> reportParticipantEligible;
            internal string receipt, saveNote;
            internal Color receiptColor, rewardColor;
            internal bool hasReceipt;
            internal MatchReceiptDto reward;

            internal ReflowSnapshot(DensityReflow source)
            {
                hud = source.hud; win = source.win; draw = source.draw; ally = source.ally; enemy = source.enemy;
                players = source.players; local = source.local; footer = source.footer; nextGoal = source.nextGoal;
                personalBest = source.personalBest;
                replay = source.replay; home = source.home; trustState = source.trustState;
                reportParticipant = source.reportParticipant; reportParticipantEligible = source.reportParticipantEligible;
                receipt = source.receipt; saveNote = source.saveNote; receiptColor = source.receiptColor;
                hasReceipt = source.hasReceipt; reward = source.reward; rewardColor = source.rewardColor;
            }
        }

        static Color Lowest => UIDesignTokens.Background;
        static Color Low => UIDesignTokens.SurfacePanel;
        static Color Mid => Color.Lerp(UIDesignTokens.Background, UIDesignTokens.SurfacePanel, .55f);
        static Color High => UIDesignTokens.SurfaceSelected;
        static Color Highest => UIDesignTokens.SurfaceDisabled;
        static Color Primary => UIDesignTokens.PrimaryAction;
        static Color Secondary => UIDesignTokens.HazardCoral;
        static Color Tertiary => UIDesignTokens.ObjectiveGold;
        static Color OnSurface => UIDesignTokens.TextPrimary;
        static Color OnVariant => UIDesignTokens.TextSecondary;
        static Color Outline => UIDesignTokens.TextMuted;
        static Color OnPrimary => UIDesignTokens.TextOnAccent;

        public static string Localized(string key, string fallback) => UiDraw.L("results." + key, fallback);
        static string L(string key, string fallback) => Localized(key, fallback);

        const float ResultsCaptionDp = UIDesignTokens.CaptionDp;
        const float ResultsValueDp = UIDesignTokens.BodyLargeDp;
        const float ResultsDisplayDp = UIDesignTokens.TitleLargeDp;
        // Bundled Results fonts have line extents up to 1.32 em. Padding keeps the
        // glyph box clear while preserving Ellipsis for horizontal truncation.
        static float ResultsTextLineHeight(float designDp, float padding = 4f) =>
            UiMetrics.TextSize(designDp) * 1.32f + padding;

        // Size UiDraw.Text assigns, so a re-fit after the text changes can grow back from a
        // previous shrink instead of ratcheting down.
        static float AuthoredSize(float designDp) => UiMetrics.ScaleTextSize(designDp) / UiDraw.TextUnitScale;

        static float FitTextToWidth(TMP_Text text, float availableWidth)
        {
            if (text == null) return 0f;
            availableWidth = Mathf.Max(0f, availableWidth);
            float naturalWidth = UiDraw.Width(text);
            if (naturalWidth > availableWidth && naturalWidth > 0f)
                text.fontSize = Mathf.Max(UiMetrics.MinTextUnits, text.fontSize * availableWidth / naturalWidth);
            text.overflowMode = TextOverflowModes.Ellipsis;
            return Mathf.Min(UiDraw.Width(text), availableWidth);
        }
        const float Pad = 24f, Gap = 12f;
        // 80 px design bar around the 52 px PLAY AGAIN; grows with the 48 dp floor on dense phones.
        static float ActionsHeight => UiMetrics.BarHeight(80f, 52f);

        sealed class DensityReflow : MonoBehaviour
        {
            internal MatchHUD hud;
            internal Vector3 lastScreen;
            internal float lastWidth;
            internal float lastTextScale;
            internal bool initialized;
            internal bool win, draw;
            internal int ally, enemy;
            internal IReadOnlyList<IMatchParticipant> players;
            internal IMatchParticipant local;
            internal string footer, nextGoal;
            internal string personalBest;
            internal Action replay, home;
            internal ResultsTrustState trustState;
            internal Action<int> reportParticipant;
            internal Func<int, bool> reportParticipantEligible;
            internal string receipt, saveNote;
            internal Color receiptColor, rewardColor;
            internal bool hasReceipt;
            internal MatchReceiptDto reward;

            void OnEnable()
            {
                StreamSafePresentation.Changed += OnStreamSafeChanged;
                LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            }

            void OnDisable()
            {
                StreamSafePresentation.Changed -= OnStreamSafeChanged;
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            }

            void OnSelectedLocaleChanged(Locale locale)
            {
                if (!initialized || hud == null) return;
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
                float scrollPosition = 1f;
                var root = (RectTransform)transform;
                var scroll = root.Find("Viewport")?.GetComponent<ScrollRect>();
                if (scroll != null) scrollPosition = scroll.verticalNormalizedPosition;
                Rebuild(scrollPosition);
            }

            void OnStreamSafeChanged()
            {
                if (!initialized || hud == null) return;
                StreamSafePresentation.Changed -= OnStreamSafeChanged;
                float scrollPosition = 1f;
                var root = (RectTransform)transform;
                var scroll = root.Find("Viewport")?.GetComponent<ScrollRect>();
                if (scroll != null) scrollPosition = scroll.verticalNormalizedPosition;
                Rebuild(scrollPosition);
            }

            void Rebuild(float scrollPosition)
            {
                Show(hud, win, draw, ally, enemy, players, local, footer, nextGoal, replay, home,
                    trustState, reportParticipant, reportParticipantEligible, null,
                    new ReflowSnapshot(this), scrollPosition, true);
            }

            void LateUpdate()
            {
                Vector3 screen = UiMetrics.CurrentScreen;
                var root = (RectTransform)transform;
                float width = root.rect.width;
                if (!initialized)
                {
                    Reflow(root, this);
                    return;
                }
                bool widthChanged = Mathf.Abs(width - lastWidth) > 4f;
                bool textScaleChanged = !Mathf.Approximately(UiMetrics.TextScale, lastTextScale);
                if (screen == lastScreen && !widthChanged && !textScaleChanged) return;

                // The result cards use measured widths and explicit positions, so changing only
                // the viewport leaves their geometry at the old display width. Rebuild the page
                // from the match snapshot while retaining the user's reading position.
                float scrollPosition = 1f;
                var scroll = root.Find("Viewport")?.GetComponent<ScrollRect>();
                if (scroll != null) scrollPosition = scroll.verticalNormalizedPosition;
                Rebuild(scrollPosition);
            }

            internal void CopyStateFrom(ReflowSnapshot source)
            {
                hud = source.hud; win = source.win; draw = source.draw; ally = source.ally; enemy = source.enemy;
                players = source.players; local = source.local; footer = source.footer; nextGoal = source.nextGoal;
                personalBest = source.personalBest;
                replay = source.replay; home = source.home; trustState = source.trustState;
                reportParticipant = source.reportParticipant; reportParticipantEligible = source.reportParticipantEligible;
                receipt = source.receipt; saveNote = source.saveNote; receiptColor = source.receiptColor;
                hasReceipt = source.hasReceipt; reward = source.reward; rewardColor = source.rewardColor;
            }
        }

        /// <summary>
        /// Resolves a path under the Progression card regardless of whether it is still nested
        /// under the scrolling Viewport/Page (the real hierarchy Show() builds) or found at the
        /// bare "Progression/..." path some older lookups assumed. Without the first branch,
        /// RevealReward's XP meter/skip/burst/save-note lookups return null and it silently takes
        /// the "no animation" fallback on every real reward reveal.
        /// </summary>
        static Transform FindInProgression(Transform root, string relativePath) =>
            root.Find("Viewport/Page/Progression/" + relativePath) ?? root.Find("Progression/" + relativePath);

        static void FitOutcomeHeader(Transform root)
        {
            var outcome = root?.Find("Viewport/Page/Outcome") ?? root?.Find("Outcome");
            var eyebrow = outcome != null ? outcome.Find("Eyebrow")?.GetComponent<TextMeshProUGUI>() : null;
            var plate = outcome != null ? outcome.Find("EyebrowPlate") as RectTransform : null;
            if (eyebrow == null || plate == null) return;

            const float left = 12f, top = 12f;
            float lineHeight = eyebrow.rectTransform.sizeDelta.y;
            var playerCard = outcome.Find("PlayerCard") as RectTransform;
            float outcomeWidth = (outcome as RectTransform)?.rect.width ?? 0f;
            float rightLimit = playerCard != null
                ? Mathf.Max(left, outcomeWidth - 12f - playerCard.rect.width - 8f)
                : outcomeWidth - 12f;
            eyebrow.fontSize = AuthoredSize(ResultsCaptionDp);
            float width = FitTextToWidth(eyebrow, Mathf.Max(0f, rightLimit - left - 8f));
            TopLeft(plate, left, top, width + 8f, lineHeight);
            TopLeft(eyebrow.rectTransform, left + 4f, top, width + 2f, lineHeight);
            float x = left + width + 12f;
            foreach (var meta in outcome.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (meta.name != "Meta") continue;
                meta.fontSize = AuthoredSize(ResultsCaptionDp);
                float metaWidth = FitTextToWidth(meta, Mathf.Max(0f, rightLimit - x));
                TopLeft(meta.rectTransform, x, top, metaWidth, lineHeight);
                x += metaWidth + 4f;
            }
        }

        static DensityReflow FindReflow(MatchHUD hud) =>
            hud != null ? hud.transform.Find("ResultsVisual")?.GetComponent<DensityReflow>() : null;

        /// <summary>
        /// Re-flows the Progression card after its receipt line changes. The card is sized from the
        /// text it was built with; a later server message ("REWARD PENDING — …") can wrap to more
        /// lines, so the meter, save note, card and page move down instead of being drawn over.
        /// Mirrors the layout in <see cref="Progression"/>.
        /// </summary>
        static void FitProgression(TextMeshProUGUI receipt)
        {
            var box = receipt != null ? receipt.transform.parent as RectTransform : null;
            if (box == null || box.name != "Progression") return;
            var heading = box.Find("Heading")?.GetComponent<TextMeshProUGUI>();
            float headingHeight = UiMetrics.TextSize(ResultsCaptionDp) + 4f;
            if (heading != null)
            {
                headingHeight = Mathf.Max(18f, headingHeight);
                heading.rectTransform.sizeDelta = new Vector2(heading.rectTransform.sizeDelta.x, headingHeight);
                SetTop(heading.transform, 12f);
            }
            float receiptTop = 12f + headingHeight + 12f;
            var rt = receipt.rectTransform;
            float inner = rt.sizeDelta.x;
            float rh = Mathf.Max(UiMetrics.TextSize(ResultsCaptionDp) + 4f, receipt.GetPreferredValues(receipt.text, inner, 0f).y);
            rt.sizeDelta = new Vector2(inner, rh);
            SetTop(receipt.transform, receiptTop);
            SetTop(box.Find("XpMeter"), receiptTop + rh + 6f);
            SetTop(box.Find("RewardBurst"), receiptTop + rh + 1f);
            var noteBox = box.Find("SaveNote") as RectTransform;
            var noteText = noteBox != null ? noteBox.Find("Text")?.GetComponent<TextMeshProUGUI>() : null;
            float noteHeight = 28f;
            if (noteBox != null && noteText != null)
            {
                var noteTextRect = noteText.rectTransform;
                float noteTextHeight = Mathf.Max(UiMetrics.TextSize(ResultsCaptionDp) + 4f,
                    noteText.GetPreferredValues(noteText.text, noteTextRect.sizeDelta.x, 0f).y);
                noteHeight = Mathf.Max(noteHeight, noteTextHeight + 8f);
                noteTextRect.sizeDelta = new Vector2(noteTextRect.sizeDelta.x, noteTextHeight);
            }
            float noteTop = receiptTop + rh + 18f;
            SetTop(noteBox, noteTop);
            if (noteBox != null) noteBox.sizeDelta = new Vector2(noteBox.sizeDelta.x, noteHeight);
            float h = noteTop + noteHeight + 12f;
            box.sizeDelta = new Vector2(box.sizeDelta.x, h);
            // Progression ends the right column; the page only ever needs to grow to hold it.
            if (box.parent is RectTransform page && page.name == "Page")
            {
                float bottom = -box.anchoredPosition.y + h + 24f;
                if (bottom > page.sizeDelta.y) page.sizeDelta = new Vector2(page.sizeDelta.x, bottom);
            }
        }

        static void SetTop(Transform t, float top)
        {
            if (t is RectTransform r) r.anchoredPosition = new Vector2(r.anchoredPosition.x, -top);
        }

        public static void Hide(MatchHUD hud)
        {
            var root = hud != null ? hud.transform.Find("ResultsVisual") : null;
            if (root == null) return;
            var rootObject = root.gameObject;
            UiScreenEnterMotion.Exit(rootObject, () => DestroyRoot(rootObject));
        }

        static void DestroyRoot(GameObject root)
        {
            if (root == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(root);
            else UnityEngine.Object.DestroyImmediate(root);
        }

        public static void SetReceipt(MatchHUD hud, string receipt, Color color, string saveNote = null)
        {
            var state = FindReflow(hud);
            if (state != null && !string.IsNullOrWhiteSpace(receipt))
            {
                state.receipt = receipt;
                state.receiptColor = color;
                state.hasReceipt = true;
                state.reward = null;
                if (!string.IsNullOrEmpty(saveNote)) state.saveNote = saveNote;
            }
            var root = hud != null ? hud.transform.Find("ResultsVisual") : null;
            if (root == null || string.IsNullOrWhiteSpace(receipt)) return;
            bool streamSafe = StreamSafePresentation.Enabled;
            string shownReceipt = streamSafe ? L("profile.accreditation.xpHidden", "Progress Hidden") : receipt;
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == "Receipt") { t.text = shownReceipt; t.color = color; }
            // A final receipt state replaces "waiting" so the card never implies a pending save.
            var note = FindInProgression(root, "SaveNote/Text")?.GetComponent<TextMeshProUGUI>();
            if (note != null && !string.IsNullOrEmpty(saveNote)) note.text = streamSafe ? L("profile.accreditation.xpHidden", "Progress Hidden") : saveNote;
            var receiptText = FindInProgression(root, "Receipt")?.GetComponent<TextMeshProUGUI>();
            if (receiptText != null) FitProgression(receiptText);
        }

        public static void SetSaveNote(MatchHUD hud, string saveNote)
        {
            var state = FindReflow(hud);
            if (state != null && !string.IsNullOrEmpty(saveNote)) state.saveNote = saveNote;
            var root = hud != null ? hud.transform.Find("ResultsVisual") : null;
            if (root == null || string.IsNullOrEmpty(saveNote)) return;
            var note = root.Find("Viewport/Page/Progression/SaveNote/Text")?.GetComponent<TextMeshProUGUI>()
                ?? root.Find("Progression/SaveNote/Text")?.GetComponent<TextMeshProUGUI>();
            if (note != null) note.text = StreamSafePresentation.Enabled ? L("profile.accreditation.xpHidden", "Progress Hidden") : saveNote;
            var receipt = FindInProgression(root, "Receipt")?.GetComponent<TextMeshProUGUI>();
            if (receipt != null) FitProgression(receipt);
        }

        public static void SetTrustState(MatchHUD hud, ResultsTrustState state, string saveNote = null)
        {
            var reflow = FindReflow(hud);
            if (reflow != null)
            {
                reflow.trustState = state;
                if (!string.IsNullOrEmpty(saveNote)) reflow.saveNote = saveNote;
            }
            var root = hud != null ? hud.transform.Find("ResultsVisual") : null;
            if (root == null) return;
            var banner = root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Eyebrow");
            if (banner != null) banner.text = state switch
            {
                ResultsTrustState.Practice => L("trust.practiceComplete", "Practice Match Complete"),
                ResultsTrustState.Cancelled => L("trust.cancelled", "Match Cancelled"),
                ResultsTrustState.Verified => L("trust.serverVerified", "MATCH COMPLETE  ·  SERVER VERIFIED"),
                ResultsTrustState.RecordedIneligible => L("trust.serverRecorded", "MATCH COMPLETE  ·  SERVER RECORDED"),
                ResultsTrustState.Pending => L("trust.receiptPending", "MATCH COMPLETE  ·  RECEIPT PENDING"),
                ResultsTrustState.Unavailable => L("trust.receiptUnavailable", "MATCH COMPLETE  ·  RECEIPT UNAVAILABLE"),
                _ => L("trust.resultUnverified", "MATCH COMPLETE  ·  RESULT UNVERIFIED"),
            };
            FitOutcomeHeader(root);
            var heading = root.Find("Viewport/Page/Progression/Heading")?.GetComponent<TextMeshProUGUI>()
                ?? root.Find("Progression/Heading")?.GetComponent<TextMeshProUGUI>();
            if (heading != null) heading.text = StreamSafePresentation.Enabled ? L("profile.accreditation.xpHidden", "Progress Hidden") : state switch
            {
                ResultsTrustState.Practice => L("progression.practice", "Practice Progression"),
                ResultsTrustState.Cancelled => L("progression.unchanged", "Progression Unchanged"),
                ResultsTrustState.Verified => L("progression.earnedBreakdown", "Earned Progression Breakdown"),
                ResultsTrustState.RecordedIneligible => L("progression.noRewards", "No Rewards Awarded"),
                _ => L("progression.notConfirmed", "Progression Not Confirmed"),
            };
            var note = root.Find("Viewport/Page/Progression/SaveNote/Text")?.GetComponent<TextMeshProUGUI>()
                ?? root.Find("Progression/SaveNote/Text")?.GetComponent<TextMeshProUGUI>();
            if (note != null && !string.IsNullOrEmpty(saveNote)) note.text = StreamSafePresentation.Enabled ? L("profile.accreditation.xpHidden", "Progress Hidden") : saveNote;
            var receipt = FindInProgression(root, "Receipt")?.GetComponent<TextMeshProUGUI>();
            if (receipt != null) FitProgression(receipt);
        }

        /// <summary>Reveals only values persisted in the server receipt. The skip control stays
        /// local to this card; the sticky next-match action remains live throughout.</summary>
        public static void RevealReward(MatchHUD hud, MatchReceiptDto reward, Color color)
            => RevealReward(hud, reward, color, true);

        static void RevealReward(MatchHUD hud, MatchReceiptDto reward, Color color, bool animate)
        {
            var state = FindReflow(hud);
            if (state != null) { state.reward = reward; state.rewardColor = color; }
            var root = hud != null ? hud.transform.Find("ResultsVisual") : null;
            if (root == null || reward == null) return;
            string unlocked = reward.unlocked != null && reward.unlocked.Length > 0
                ? UiDraw.LFormat("results.receipt.unlockedTemplate", "  ·  UNLOCKED {0}", string.Join(", ", Array.ConvertAll(reward.unlocked, CosmeticLook.DisplayName))) : "";
            int finalLevel = reward.profileXpAfter.HasValue && reward.xpPerLevel > 0
                ? Mathf.Min(Mathf.Max(1, reward.maxLevel), 1 + Mathf.Max(0, reward.profileXpAfter.Value) / reward.xpPerLevel)
                : reward.level;
            string finalReceipt = UiDraw.LFormat("results.receipt.rewardTemplate", "+{0} XP  ·  +{1} CREDITS  ·  LEVEL {2}{3}",
                reward.xpGained.ToString("N0", CultureInfo.InvariantCulture), reward.creditsGained.ToString("N0", CultureInfo.InvariantCulture), finalLevel, unlocked);
            var receiptText = root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Receipt");
            if (receiptText == null) return;
            receiptText.color = color;
            if (StreamSafePresentation.Enabled)
            {
                receiptText.text = L("profile.accreditation.xpHidden", "Progress Hidden");
                var hiddenMeter = FindInProgression(root, "XpMeter");
                if (hiddenMeter != null) hiddenMeter.gameObject.SetActive(false);
                var hiddenSkip = FindInProgression(root, "SkipReward");
                if (hiddenSkip != null) hiddenSkip.gameObject.SetActive(false);
                var hiddenBurst = FindInProgression(root, "RewardBurst");
                if (hiddenBurst != null) hiddenBurst.gameObject.SetActive(false);
                var hiddenNote = FindInProgression(root, "SaveNote/Text")?.GetComponent<TextMeshProUGUI>();
                if (hiddenNote != null) hiddenNote.text = L("profile.accreditation.xpHidden", "Progress Hidden");
                FitProgression(receiptText);
                return;
            }
            // Size the card for the settled line up front; the count-up text is never longer.
            receiptText.text = finalReceipt;

            var track = FindInProgression(root, "XpMeter");
            var fill = track != null ? track.Find("XpFill") as RectTransform : null;
            var skip = FindInProgression(root, "SkipReward")?.GetComponent<Button>();
            var burst = FindInProgression(root, "RewardBurst") as RectTransform;
            var note = FindInProgression(root, "SaveNote/Text")?.GetComponent<TextMeshProUGUI>();
            if (note != null) note.text = L("save.progressSaved", "Progress saved to your account.");
            FitProgression(receiptText);
            if (!reward.profileXpBefore.HasValue || !reward.profileXpAfter.HasValue || reward.xpPerLevel <= 0 || fill == null || skip == null)
            {
                receiptText.text = finalReceipt;
                return;
            }

            if (!animate)
            {
                receiptText.text = finalReceipt;
                SetXpFill(fill, XpFraction(reward.profileXpAfter.Value, reward.xpPerLevel, reward.maxLevel));
                if (burst != null) burst.gameObject.SetActive(false);
                skip.gameObject.SetActive(false);
                return;
            }

            skip.gameObject.SetActive(true);
            if (burst != null) burst.gameObject.SetActive(true);
            hud.StartCoroutine(AnimateReward(receiptText, fill, skip, burst, reward, finalReceipt, unlocked));
        }

        static IEnumerator AnimateReward(TextMeshProUGUI receiptText, RectTransform fill, Button skip, RectTransform burst,
            MatchReceiptDto reward, string finalReceipt, string unlocked)
        {
            const float duration = 1.15f; // below the Bible's 2.5 s maximum
            float elapsed = 0f;
            bool skipped = false;
            skip.onClick.RemoveAllListeners();
            skip.onClick.AddListener(() => skipped = true);
            int before = Mathf.Max(0, reward.profileXpBefore.Value);
            int after = Mathf.Max(before, reward.profileXpAfter.Value);
            while (elapsed < duration && !skipped)
            {
                if (receiptText == null || fill == null || skip == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                int shownXp = Mathf.RoundToInt(reward.xpGained * eased);
                int shownCredits = Mathf.RoundToInt(reward.creditsGained * eased);
                int profileXp = Mathf.RoundToInt(Mathf.Lerp(before, after, eased));
                int shownLevel = reward.xpPerLevel > 0
                    ? Mathf.Min(Mathf.Max(1, reward.maxLevel), 1 + profileXp / reward.xpPerLevel)
                    : reward.level;
                receiptText.text = UiDraw.LFormat("results.receipt.rewardTemplate", "+{0} XP  ·  +{1} CREDITS  ·  LEVEL {2}{3}",
                    shownXp.ToString("N0", CultureInfo.InvariantCulture), shownCredits.ToString("N0", CultureInfo.InvariantCulture), shownLevel, t >= 1f ? unlocked : "");
                SetXpFill(fill, XpFraction(profileXp, reward.xpPerLevel, reward.maxLevel));
                AnimateBurst(burst, t);
                yield return null;
            }

            if (receiptText == null || fill == null || skip == null) yield break;
            receiptText.text = finalReceipt;
            SetXpFill(fill, XpFraction(after, reward.xpPerLevel, reward.maxLevel));
            if (burst != null) burst.gameObject.SetActive(false);
            if (skip != null) skip.gameObject.SetActive(false);
        }

        static float XpFraction(int totalXp, int xpPerLevel, int maxLevel)
        {
            if (xpPerLevel <= 0) return 0f;
            if (maxLevel > 0 && totalXp >= (maxLevel - 1) * xpPerLevel) return 1f;
            return Mathf.Clamp01((totalXp % xpPerLevel) / (float)xpPerLevel);
        }

        static void SetXpFill(RectTransform fill, float fraction)
        {
            var track = fill.parent as RectTransform;
            if (track == null) return;
            fill.sizeDelta = new Vector2(track.rect.width * Mathf.Clamp01(fraction), 0f);
        }

        static void AnimateBurst(RectTransform burst, float t)
        {
            if (burst == null) return;
            float phase = Mathf.Clamp01(t / .42f);
            burst.localScale = Vector3.one * Mathf.Lerp(.5f, 3f, phase);
            var image = burst.GetComponent<Image>();
            if (image != null) image.color = new Color(Tertiary.r, Tertiary.g, Tertiary.b, 1f - phase);
        }

        public static void Show(MatchHUD hud, bool win, bool draw, int ally, int enemy,
            IReadOnlyList<IMatchParticipant> players, IMatchParticipant local, string footer,
            string nextGoal, Action replay, Action home, ResultsTrustState trustState,
            Action<int> reportParticipant = null, Func<int, bool> reportParticipantEligible = null,
            string personalBest = null)
            => Show(hud, win, draw, ally, enemy, players, local, footer, nextGoal, replay, home,
                trustState, reportParticipant, reportParticipantEligible, personalBest, null, 1f, false);

        static void Show(MatchHUD hud, bool win, bool draw, int ally, int enemy,
            IReadOnlyList<IMatchParticipant> players, IMatchParticipant local, string footer,
            string nextGoal, Action replay, Action home, ResultsTrustState trustState,
            Action<int> reportParticipant, Func<int, bool> reportParticipantEligible, string suppliedPersonalBest,
            ReflowSnapshot preserved, float scrollPosition, bool reflowing)
        {
            // A fresh results receipt replaces any previous card immediately. Normal dismissals
            // use Hide() so the outgoing view can complete its authored transition.
            //
            // Object.Destroy() (used by DestroyRoot in play mode) defers the actual removal to
            // end of frame, so the dying card stays a live, named "ResultsVisual" child for the
            // rest of this call. Every setter below (SetTrustState, SetReceipt, RevealReward, ...)
            // re-resolves the card by that exact name via hud.transform.Find("ResultsVisual"),
            // which returns the FIRST matching sibling - the dying one, since it was created
            // first. On a second match in the same session this silently wrote the trust banner
            // and reward reveal onto the card about to be destroyed, leaving the real, new card
            // stuck on its placeholder text. Renaming the dying card first removes the ambiguity
            // so every later Find("ResultsVisual") can only resolve to the one just built.
            var previous = hud.transform.Find("ResultsVisual");
            if (previous != null)
            {
                previous.name = "ResultsVisual_Destroying";
                DestroyRoot(previous.gameObject);
            }
            var root = UiDraw.Node(hud.transform, "ResultsVisual");
            if (!reflowing) root.gameObject.AddComponent<UiScreenEnterMotion>();
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Lowest).rectTransform);
            var photo = UiDraw.Node(root, "RooftopBackdrop").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(photo.rectTransform);
            photo.texture = UiPresentationProfile.LoadRooftopArtwork();
            photo.color = new Color(.6f, .62f, .66f, .25f);
            photo.raycastTarget = false;
            var surface = Lowest;
            UiDraw.VerticalGradient(root, "Shade", surface, new Color(surface.r, surface.g, surface.b, .8f), new Color(surface.r, surface.g, surface.b, .9f));

            bool localPractice = trustState == ResultsTrustState.Practice;
            string personalBest = preserved != null ? preserved.personalBest
                : suppliedPersonalBest ?? (local != null ? PersonalBests.Record(local, localPractice) : "");
            float width = root.rect.width > 0f ? root.rect.width : hud.GetComponent<RectTransform>().rect.width;
            if (width <= 0f) width = 1280f;
            float contentW = width - Pad * 2f;

            // Scrolling page above the sticky action bar (80 px design; taller on dense phones).
            var viewport = UiDraw.Node(root, "Viewport");
            UiDraw.Fill(viewport, 0f, 0f, 0f, ActionsHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var page = UiDraw.Node(viewport, "Page");
            // Top-left anchored with an explicit width: everything inside is placed from the top-left.
            page.anchorMin = page.anchorMax = new Vector2(0f, 1f); page.pivot = new Vector2(0f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = page; scroll.horizontal = false; scroll.viewport = viewport;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            float y = 12f;
            y = Outcome(page, contentW, y, win, draw, ally, enemy, local, localPractice) + 20f;
            // Keep three contribution tiles readable on phone widths. Reserve enough
            // physical width for those tiles and a compact roster; below that combined
            // minimum, use the page's existing vertical scroll instead of squeezing both.
            const float contributionMinDp = 360f, rosterMinDp = 360f;
            float gapDp = UiMetrics.DpForUnits(Gap);
            bool stackCards = UiMetrics.DpForUnits(contentW) < contributionMinDp + rosterMinDp + gapDp;
            float leftW = stackCards ? contentW : UiMetrics.UnitsForDp(contributionMinDp);
            float rightW = stackCards ? contentW : contentW - leftW - Gap;
            float rightX = stackCards ? Pad : Pad + leftW + Gap;
            float leftH = Contribution(page, leftW, y, local, nextGoal, localPractice, personalBest);
            bool reportAvailable = reportParticipant != null && trustState != ResultsTrustState.Practice
                && trustState != ResultsTrustState.UnverifiedDevelopment;
            float rosterY = stackCards ? y + leftH + Gap : y;
            float rightH = Scoreboard(page, rightX, rightW, rosterY, ally, enemy, players, local,
                localPractice, reportAvailable ? reportParticipant : null,
                reportAvailable ? reportParticipantEligible : null);
            float progressionTop = rosterY + rightH + Gap;
            float progH = Progression(page, rightX, rightW, progressionTop, footer, trustState);
            y = (stackCards ? progressionTop + progH : Mathf.Max(y + leftH, progressionTop + progH)) + 24f;
            page.sizeDelta = new Vector2(width, y);
            page.anchoredPosition = Vector2.zero;

            Actions(root, replay, home);
            var reflow = root.gameObject.AddComponent<DensityReflow>();
            if (preserved != null) reflow.CopyStateFrom(preserved);
            else
            {
                reflow.hud = hud; reflow.win = win; reflow.draw = draw; reflow.ally = ally; reflow.enemy = enemy;
                reflow.players = players; reflow.local = local; reflow.footer = footer; reflow.nextGoal = nextGoal;
                reflow.personalBest = personalBest;
                reflow.replay = replay; reflow.home = home; reflow.trustState = trustState;
                reflow.reportParticipant = reportParticipant; reflow.reportParticipantEligible = reportParticipantEligible;
            }
            Reflow(root, reflow);
            SetTrustState(hud, reflow.trustState, reflow.saveNote);
            if (reflow.reward != null) RevealReward(hud, reflow.reward, reflow.rewardColor, false);
            else if (reflow.hasReceipt) SetReceipt(hud, reflow.receipt, reflow.receiptColor, reflow.saveNote);
            var rebuiltScroll = root.Find("Viewport")?.GetComponent<ScrollRect>();
            if (rebuiltScroll != null) rebuiltScroll.verticalNormalizedPosition = Mathf.Clamp01(scrollPosition);
        }

        static void Reflow(RectTransform root, DensityReflow watcher)
        {
            if (root == null) return;
            if (root.Find("Actions") is RectTransform actions)
                actions.sizeDelta = new Vector2(actions.sizeDelta.x, ActionsHeight);
            if (root.Find("Viewport") is RectTransform viewport)
                viewport.offsetMin = new Vector2(viewport.offsetMin.x, ActionsHeight);
            UiFloor.Apply(root);
            watcher.lastScreen = UiMetrics.CurrentScreen;
            watcher.lastWidth = root.rect.width;
            watcher.lastTextScale = UiMetrics.TextScale;
            watcher.initialized = true;
        }

        // ------------------------------------------------------------ outcome banner

        static float Outcome(RectTransform page, float w, float y, bool win, bool draw, int ally, int enemy, IMatchParticipant local, bool localPractice)
        {
            var box = Card(page, "Outcome", UIDesignTokens.WithAlpha(Low, .9f), Pad, y, w, 88f);
            float captionLineHeight = Mathf.Max(18f, ResultsTextLineHeight(ResultsCaptionDp));
            float titleHeight = Mathf.Max(44f, ResultsTextLineHeight(ResultsDisplayDp, 6f));
            float scoreLineHeight = Mathf.Max(30f, ResultsTextLineHeight(ResultsValueDp, 8f));
            float titleTop = 12f + captionLineHeight + 4f;
            float scoreTop = titleTop + (titleHeight - scoreLineHeight) * .5f;
            var tag = UiDraw.Text(box, "Eyebrow", localPractice ? L("trust.practiceComplete", "Practice Match Complete") : L("trust.resultUnverified", "MATCH COMPLETE  ·  RESULT UNVERIFIED"),
                UiFont.Heading, ResultsCaptionDp, Primary, .1f);
            var tagPlate = UiDraw.Box(box, "EyebrowPlate", High, 4f);
            tagPlate.transform.SetSiblingIndex(tag.transform.GetSiblingIndex());
            var meta = UiDraw.Text(box, "Meta",
                L("meta.mode", "Regulation 3v3 Relay") + "  •  " + L("meta.arena", "Switchyard Rooftop"),
                UiFont.MonoMedium, ResultsCaptionDp, OnVariant, .05f);
            var title = UiDraw.Text(box, "Title", draw ? L("outcome.draw", "Draw") : win ? L("outcome.victory", "Victory") : L("outcome.defeat", "Defeat"), UiFont.Heading, ResultsDisplayDp,
                draw ? Tertiary : win ? Primary : UIDesignTokens.SignalWarning, -.05f);
            // Score pill: ally TO enemy + how it ended.
            var pill = UiDraw.Box(box, "ScorePill", Mid, 8f);
            var a = UiDraw.Text(pill.transform, "Ally", ally.ToString(), UiFont.Mono, ResultsValueDp, Primary, .02f);
            var to = UiDraw.Text(pill.transform, "To", L("score.to", "To"), UiFont.MonoMedium, ResultsCaptionDp, Outline, .05f);
            var e = UiDraw.Text(pill.transform, "Enemy", enemy.ToString(), UiFont.Mono, ResultsValueDp, Secondary, .02f);
            var how = UiDraw.Text(pill.transform, "How", L("score.final", "Final Score"), UiFont.Heading, ResultsCaptionDp, OnVariant, .08f);
            float px = 8f;
            foreach (var (t, gap) in new[] { (a, 4f), (to, 4f), (e, 8f), (how, 0f) })
            {
                px += UiDraw.Width(t) + gap;
            }

            // Player card (right): name, the weapon actually carried, take-off glyph.
            var card = UiDraw.Box(box, "PlayerCard", High, 8f);
            string name = local?.DisplayName ?? L("common.player", "Player");
            string gun = local is PlayerController pc && pc.CurrentWeapon != null ? PreparationView.WeaponName(pc.CurrentWeapon) : "";
            var n = UiDraw.Text(card.transform, "Name", name, UiFont.BodyBold, ResultsValueDp, OnSurface, .01f, TextAlignmentOptions.MidlineRight);
            var sub = UiDraw.Text(card.transform, "Spec", gun, UiFont.MonoMedium, ResultsCaptionDp, Primary, .05f, TextAlignmentOptions.MidlineRight);
            n.overflowMode = TextOverflowModes.Ellipsis;
            sub.overflowMode = TextOverflowModes.Ellipsis;
            float playerValueHeight = Mathf.Max(22f, ResultsTextLineHeight(ResultsValueDp));
            float playerCaptionHeight = Mathf.Max(18f, ResultsTextLineHeight(ResultsCaptionDp));
            const float playerLineGap = 4f;
            float playerCardHeight = Mathf.Max(56f, playerValueHeight + playerCaptionHeight + playerLineGap + 20f);
            float playerCenterOffset = 1f;
            float naturalCardWidth = Mathf.Max(UiDraw.Width(n), UiDraw.Width(sub)) + 12f + 12f + 40f + 12f;
            float cw = Mathf.Min(naturalCardWidth, w * .42f);
            UiDraw.Place(card.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(cw, playerCardHeight));
            float nameCenterY = playerCenterOffset + (playerCaptionHeight + playerLineGap) * .5f;
            float subCenterY = playerCenterOffset - (playerValueHeight + playerLineGap) * .5f;
            float playerTextWidth = Mathf.Max(0f, cw - 76f);
            UiDraw.Place(n.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-64f, nameCenterY), new Vector2(playerTextWidth, playerValueHeight));
            UiDraw.Place(sub.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-64f, subCenterY), new Vector2(playerTextWidth, playerCaptionHeight));
            var glyph = UiDraw.Box(card.transform, "Glyph", Mid, 8f);
            UiDraw.Place(glyph.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(40f, 40f));
            UiDraw.Fill(UiDraw.Icon(glyph.transform, "Icon", "flight_takeoff", 24f, Primary).rectTransform);

            // Reserve the player-card column before placing the title and score. At narrow
            // widths the score moves below the title; if even the score line is too wide,
            // its caption gets a second line rather than drawing under the player card.
            float left = 12f;
            float leftRight = Mathf.Max(left, w - 12f - cw - 8f);
            float leftWidth = Mathf.Max(0f, leftRight - left);
            // The top status/meta line shares the banner with the player card just
            // like the title and score below it. Bound both labels to the same free
            // column so localized or larger-text copy cannot draw under that card.
            float headerWidth = Mathf.Max(0f, leftRight - left);
            float tagWidth = FitTextToWidth(tag, headerWidth);
            float plateWidth = tagWidth + 8f;
            TopLeft(tagPlate.rectTransform, left, 12f, plateWidth, captionLineHeight);
            TopLeft(tag.rectTransform, left + 4f, 12f, tagWidth, captionLineHeight);
            float metaX = left + plateWidth + 4f;
            float metaWidth = Mathf.Max(0f, leftRight - metaX);
            FitTextToWidth(meta, metaWidth);
            TopLeft(meta.rectTransform, metaX, 12f, metaWidth, captionLineHeight);
            float titleWidth = Mathf.Min(UiDraw.Width(title), leftWidth);
            title.overflowMode = TextOverflowModes.Ellipsis;
            TopLeft(title.rectTransform, left, titleTop, titleWidth, titleHeight);
            float naturalPillWidth = px + 16f;
            bool scoreBelowTitle = titleWidth + 16f + naturalPillWidth > leftWidth;
            float pillX = scoreBelowTitle ? left : left + titleWidth + 16f;
            float pillAvailableWidth = Mathf.Max(0f, leftRight - pillX);
            bool splitScoreRows = naturalPillWidth > pillAvailableWidth;
            float pillTop = scoreBelowTitle ? titleTop + titleHeight + 4f : scoreTop;
            float howHeight = Mathf.Max(18f, ResultsTextLineHeight(ResultsCaptionDp));
            float pillHeight = splitScoreRows
                ? 16f + scoreLineHeight + 4f + howHeight
                : scoreLineHeight;
            float pillWidth = Mathf.Min(naturalPillWidth, pillAvailableWidth);
            if (splitScoreRows)
            {
                float innerScoreWidth = Mathf.Max(0f, pillWidth - 16f);
                float scoreGap = 8f;
                float availableScores = Mathf.Max(0f, innerScoreWidth - scoreGap);
                float naturalA = UiDraw.Width(a), naturalTo = UiDraw.Width(to), naturalE = UiDraw.Width(e);
                float naturalScores = Mathf.Max(.001f, naturalA + naturalTo + naturalE);
                float aWidth = availableScores * naturalA / naturalScores;
                float toWidth = availableScores * naturalTo / naturalScores;
                float eWidth = Mathf.Max(0f, availableScores - aWidth - toWidth);
                a.overflowMode = to.overflowMode = e.overflowMode = TextOverflowModes.Ellipsis;
                float firstY = (howHeight + 4f) * .5f;
                float secondY = -(scoreLineHeight + 4f) * .5f;
                float rowX = 8f;
                UiDraw.Place(a.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(rowX, firstY), new Vector2(aWidth, scoreLineHeight));
                rowX += aWidth + 4f;
                UiDraw.Place(to.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(rowX, firstY), new Vector2(toWidth, scoreLineHeight));
                rowX += toWidth + 4f;
                UiDraw.Place(e.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(rowX, firstY), new Vector2(eWidth, scoreLineHeight));
                how.overflowMode = TextOverflowModes.Ellipsis;
                float howWidth = Mathf.Min(UiDraw.Width(how) + 2f, innerScoreWidth);
                UiDraw.Place(how.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, secondY), new Vector2(howWidth, howHeight));
            }
            else
            {
                float rowX = 8f;
                foreach (var (t, gap) in new[] { (a, 4f), (to, 4f), (e, 8f), (how, 0f) })
                {
                    float lineHeight = t == a || t == e ? ResultsTextLineHeight(ResultsValueDp) : ResultsTextLineHeight(ResultsCaptionDp);
                    float textWidth = Mathf.Min(UiDraw.Width(t) + 2f, Mathf.Max(0f, pillWidth - rowX - 8f));
                    t.overflowMode = TextOverflowModes.Ellipsis;
                    UiDraw.Place(t.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(rowX, 0f), new Vector2(textWidth, Mathf.Max(18f, lineHeight)));
                    rowX += textWidth + gap;
                }
            }
            TopLeft(pill.rectTransform, pillX, pillTop, pillWidth, pillHeight);
            float bannerHeight = Mathf.Max(88f, Mathf.Max(titleTop + titleHeight + 12f,
                Mathf.Max(playerCardHeight + 16f, pillTop + pillHeight + 12f)));
            box.sizeDelta = new Vector2(w, bannerHeight);
            FitOutcomeHeader(page);
            return y + bannerHeight;
        }

        // ------------------------------------------------------------ contribution (left column)

        static float Contribution(RectTransform page, float w, float y, IMatchParticipant local, string nextGoal, bool localPractice, string bests)
        {
            var box = Card(page, "Contribution", Low, Pad, y, w, 100f);
            float inner = w - 24f;
            float captionLineHeight = Mathf.Max(18f, ResultsTextLineHeight(ResultsCaptionDp));
            float headingIconSize = UiMetrics.TextSize(18f);
            float headerHeight = Mathf.Max(captionLineHeight, headingIconSize);
            var headingIcon = UiDraw.Icon(box, "Icon", "verified_user", 18f, Primary);
            TopLeft(headingIcon.rectTransform, 12f, 12f + (headerHeight - headingIconSize) * .5f, headingIconSize, headingIconSize);
            float headingX = 12f + headingIconSize + 8f;
            var heading = UiDraw.Text(box, "Heading", localPractice ? L("contribution.yours", "Your Contribution") : L("contribution.verified", "Verified Contribution"), UiFont.Heading, ResultsCaptionDp, OnSurface, .08f);
            var right = UiDraw.Text(box, "Impact", L("contribution.directImpact", "Direct Impact"), UiFont.MonoMedium, ResultsCaptionDp, Primary, .05f, TextAlignmentOptions.MidlineRight);
            float impactWidth = Mathf.Min(140f, inner * .4f);
            float impactX = 12f + inner - impactWidth;
            FitTextToWidth(right, impactWidth);
            FitTextToWidth(heading, Mathf.Max(0f, impactX - headingX - 8f));
            TopLeft(heading.rectTransform, headingX, 12f + (headerHeight - captionLineHeight) * .5f, Mathf.Max(0f, impactX - headingX - 8f), captionLineHeight);
            TopLeft(right.rectTransform, impactX, 12f + (headerHeight - captionLineHeight) * .5f, impactWidth, captionLineHeight);

            int zone = Mathf.RoundToInt(local?.ObjectiveSeconds ?? 0f);
            var tiles = new (string label, string value, Color color, string sub)[]
            {
                (L("contribution.objectiveHold", "Obj Hold"), $"{zone / 60:00}:{zone % 60:00}", OnSurface, L("contribution.zoneTime", "Zone time")),
                (L("contribution.combatKd", "Combat K/D"), $"{local?.Eliminations ?? 0} / {local?.Deaths ?? 0}", Primary, L("contribution.elimsDeaths", "Elims / deaths")),
                (L("contribution.padCatapults", "Pad Catapults"), (local?.PadLaunches ?? 0).ToString(), OnSurface, L("contribution.launches", "Launches")),
            };
            float tileW = (inner - 2f * 8f) / 3f;
            float tileLabelHeight = Mathf.Max(14f, ResultsTextLineHeight(ResultsCaptionDp));
            float tileValueHeight = Mathf.Max(22f, ResultsTextLineHeight(ResultsValueDp));
            float tileSubHeight = Mathf.Max(16f, ResultsTextLineHeight(ResultsCaptionDp));
            float tileTop = 12f + headerHeight + 12f;
            float tileHeight = 16f + tileLabelHeight + 4f + tileValueHeight + 4f + tileSubHeight;
            for (int i = 0; i < 3; i++)
            {
                var tile = UiDraw.Box(box, "Tile" + i, Mid, 8f);
                TopLeft(tile.rectTransform, 12f + i * (tileW + 8f), tileTop, tileW, tileHeight);
                var label = UiDraw.Text(tile.transform, "Label", tiles[i].label, UiFont.Heading, ResultsCaptionDp, OnVariant, .08f);
                var value = UiDraw.Text(tile.transform, "Value", tiles[i].value, UiFont.Mono, ResultsValueDp, tiles[i].color, .02f);
                var sub = UiDraw.Text(tile.transform, "Sub", tiles[i].sub, UiFont.Body, ResultsCaptionDp, Outline, .01f);
                float textWidth = tileW - 16f;
                FitTextToWidth(label, textWidth);
                FitTextToWidth(value, textWidth);
                FitTextToWidth(sub, textWidth);
                TopLeft(label.rectTransform, 8f, 8f, textWidth, tileLabelHeight);
                float valueTop = 8f + tileLabelHeight + 4f;
                TopLeft(value.rectTransform, 8f, valueTop, textWidth, tileValueHeight);
                TopLeft(sub.rectTransform, 8f, valueTop + tileValueHeight + 4f, textWidth, tileSubHeight);
            }
            float cy = tileTop + tileHeight + 12f + 4f;
            if (!StreamSafePresentation.Enabled && !string.IsNullOrEmpty(bests))
                cy = Callout(box, inner, cy, "timer", Tertiary, L("callout.personalBest", "Personal Best"), L("callout.record", "[Record]"), bests) + 4f;
            if (!StreamSafePresentation.Enabled && !string.IsNullOrWhiteSpace(nextGoal))
                cy = Callout(box, inner, cy, "flag", Primary, L("callout.nextGoal", "Next Goal"), "", nextGoal.Replace("NEXT: ", "")) + 4f;
            var quality = UiDraw.Box(box, "Quality", Mid, 8f);
            float qualityHeight = Mathf.Max(26f, captionLineHeight + 8f);
            TopLeft(quality.rectTransform, 12f, cy + 8f, inner, qualityHeight);
            float qualityLabelW = Mathf.Min(160f, (inner - 24f) * .5f);
            float qualityValueW = Mathf.Max(0f, Mathf.Min(180f, inner - 24f - qualityLabelW));
            var qualityLabel = UiDraw.Text(quality.transform, "Label", L("quality.matchMode", "Match Mode"), UiFont.MonoMedium, ResultsCaptionDp, Outline, .05f);
            FitTextToWidth(qualityLabel, qualityLabelW);
            UiDraw.Place(qualityLabel.rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(qualityLabelW, captionLineHeight));
            var qualityValue = UiDraw.Text(quality.transform, "Value", localPractice ? L("quality.practiceMatch", "Practice Match") : L("quality.regulation", "Regulation 3v3"), UiFont.Mono, ResultsCaptionDp, Primary, .05f, TextAlignmentOptions.MidlineRight);
            FitTextToWidth(qualityValue, qualityValueW);
            UiDraw.Place(qualityValue.rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-8f, 0f), new Vector2(qualityValueW, captionLineHeight));
            float h = cy + 8f + qualityHeight + 12f;
            box.sizeDelta = new Vector2(w, h);
            return h;
        }

        static float Callout(RectTransform box, float inner, float y, string icon, Color tone, string title, string tag, string body)
        {
            var c = UiDraw.Box(box, "Callout_" + title, UIDesignTokens.WithAlpha(High, .8f), 8f);
            float iconSize = UiMetrics.TextSize(20f);
            float titleHeight = Mathf.Max(16f, ResultsTextLineHeight(ResultsCaptionDp));
            float headerHeight = Mathf.Max(titleHeight, iconSize);
            TopLeft(UiDraw.Icon(c.transform, "Icon", icon, 20f, tone).rectTransform,
                8f, 8f + (headerHeight - iconSize) * .5f, iconSize, iconSize);
            float titleX = 16f + iconSize;
            float headerRight = inner - 8f;
            float headerWidth = Mathf.Max(0f, headerRight - titleX);
            var t = UiDraw.Text(c.transform, "Title", title, UiFont.Heading, ResultsCaptionDp, tone, .05f);
            TextMeshProUGUI tagText = null;
            float titleWidth = headerWidth;
            if (!string.IsNullOrEmpty(tag))
            {
                tagText = UiDraw.Text(c.transform, "Tag", tag, UiFont.MonoMedium, ResultsCaptionDp, Outline, .05f);
                float desiredTagWidth = Mathf.Min(UiDraw.Width(tagText), headerWidth);
                float desiredTitleWidth = Mathf.Min(UiDraw.Width(t), Mathf.Max(0f, headerWidth - desiredTagWidth - 4f));
                titleWidth = FitTextToWidth(t, desiredTitleWidth);
                float tagX = titleX + titleWidth + 4f;
                float tagWidth = FitTextToWidth(tagText, Mathf.Max(0f, headerRight - tagX));
                TopLeft(tagText.rectTransform, tagX, 8f + (headerHeight - titleHeight) * .5f, tagWidth, titleHeight);
            }
            else FitTextToWidth(t, titleWidth);
            TopLeft(t.rectTransform, titleX, 8f + (headerHeight - titleHeight) * .5f, titleWidth, titleHeight);
            var text = UiDraw.Text(c.transform, "Body", body, UiFont.Body, ResultsCaptionDp, OnVariant, .01f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            float bodyTop = 8f + headerHeight + 4f;
            float bodyWidth = Mathf.Max(0f, headerRight - titleX);
            float bh = Mathf.Max(ResultsTextLineHeight(ResultsCaptionDp), text.GetPreferredValues(body, bodyWidth, 0f).y);
            TopLeft(text.rectTransform, titleX, bodyTop, bodyWidth, bh);
            float h = bodyTop + bh + 8f;
            TopLeft(c.rectTransform, 12f, y, inner, h);
            return y + h;
        }

        // ------------------------------------------------------------ scoreboard (right column)

        static float Scoreboard(RectTransform page, float x, float w, float y, int ally, int enemy,
            IReadOnlyList<IMatchParticipant> players, IMatchParticipant local, bool localPractice,
            Action<int> reportParticipant, Func<int, bool> reportParticipantEligible)
        {
            var box = Card(page, "Roster", Low, x, y, w, 100f);
            float inner = w - 24f, col = inner / 12f;
            // Wide columns start at 600 dp, not the 560 dp they need, so a roster that lands just over
            // the old cutoff beside a 360 dp contribution card (564 dp at 2400x1080, 400 dpi) stays
            // compact instead of flipping with a few units of safe-area inset.
            bool compact = UiMetrics.DpForUnits(w) < 600f;
            float reportButtonWidth = UiMetrics.UnitsForDp(60f);
            float reportButtonHeight = UiMetrics.MinTouchUnits;
            float compactNameHeight = ResultsTextLineHeight(ResultsValueDp);
            float compactMetricLineHeight = ResultsTextLineHeight(ResultsCaptionDp);
            float compactTeamHeadingHeight = ResultsTextLineHeight(ResultsValueDp);
            float compactTeamPointsHeight = ResultsTextLineHeight(ResultsCaptionDp);
            float wideHeaderHeight = Mathf.Max(26f, ResultsTextLineHeight(ResultsValueDp));
            float wideChipHeight = ResultsTextLineHeight(ResultsCaptionDp);
            float wideLabelHeight = Mathf.Max(22f, ResultsTextLineHeight(ResultsCaptionDp));
            bool localA = local == null || local.TeamId == 0;
            float cy = 12f;
            for (int group = 0; group < 2; group++)
            {
                int team = group == 0 ? (localA ? 0 : 1) : (localA ? 1 : 0);
                Color tone = group == 0 ? Primary : Secondary;
                if (group == 1)
                {
                    TopLeft(UiDraw.Box(box, "Divider", UIDesignTokens.BorderColor(.4f)).rectTransform, 12f, cy + 4f, inner, 1f);
                    cy += 12f;
                }
                // Team header.
                TopLeft(UiDraw.Dot(box, "TeamDot_" + group, 10f, tone).rectTransform, 12f,
                    cy + ((compact ? compactTeamHeadingHeight : wideHeaderHeight) - 10f) * .5f, 10f, 10f);
                var teamName = UiDraw.Text(box, "TeamHeading_" + group, group == 0 ? L("team.ally", "Ally Team") : L("team.rival", "Rival Team"), UiFont.Heading, ResultsValueDp, OnSurface);
                if (compact) teamName.overflowMode = TextOverflowModes.Ellipsis;
                float tn = UiDraw.Width(teamName);
                TopLeft(teamName.rectTransform, 30f, cy, compact ? inner - 42f : tn + 2f,
                    compact ? compactTeamHeadingHeight : wideHeaderHeight);
                var chipText = UiDraw.Text(box, "TeamPoints_" + group, UiDraw.LFormat("results." + (group == 0 ? "team.allyPoints" : "team.opponentPoints"), group == 0 ? "ALLIES  ·  {0} PTS" : "OPPONENTS  ·  {0} PTS", group == 0 ? ally : enemy),
                    UiFont.Mono, ResultsCaptionDp, tone, .05f, TextAlignmentOptions.Midline);
                if (compact) chipText.overflowMode = TextOverflowModes.Ellipsis;
                float cw = UiDraw.Width(chipText) + 8f;
                var chip = UiDraw.Box(box, "TeamChip_" + group, Mid, 4f);
                chip.transform.SetSiblingIndex(chipText.transform.GetSiblingIndex());
                if (compact)
                {
                    float compactChipWidth = Mathf.Max(0f, inner - 42f);
                    chipText.alignment = TextAlignmentOptions.MidlineLeft;
                    float pointsY = cy + compactTeamHeadingHeight + 4f;
                    TopLeft(chip.rectTransform, 30f, pointsY, compactChipWidth, compactTeamPointsHeight);
                    TopLeft(chipText.rectTransform, 34f, pointsY, compactChipWidth - 8f, compactTeamPointsHeight);
                }
                else
                {
                    float chipTop = cy + (wideHeaderHeight - wideChipHeight) * .5f;
                    TopLeft(chip.rectTransform, 30f + tn + 8f, chipTop, cw, wideChipHeight);
                    TopLeft(chipText.rectTransform, 30f + tn + 8f, chipTop, cw, wideChipHeight);
                }
                if (!compact)
                {
                    var note = UiDraw.Text(box, "TeamNote_" + group, group == 0 ? L("team.matchRecord", "Match Record") : L("team.equalRoster", "Equal Competition Roster"),
                        UiFont.MonoMedium, ResultsCaptionDp, Outline, .05f, TextAlignmentOptions.MidlineRight);
                    // Share the header row with the points chip: take only the space it leaves.
                    note.overflowMode = TextOverflowModes.Ellipsis;
                    float noteLeft = 30f + tn + 8f + cw + 8f;
                    float noteWidth = Mathf.Clamp(12f + inner - noteLeft, 0f, 260f);
                    UiDraw.Place(note.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-12f, -(cy + wideHeaderHeight * .5f)), new Vector2(noteWidth, wideChipHeight));
                }
                cy += compact ? compactTeamHeadingHeight + 4f + compactTeamPointsHeight + 8f : wideHeaderHeight + 12f;
                if (group == 0 && !compact)
                {
                    var labels = new (string text, float center, TextAlignmentOptions align)[]
                    {
                        (L("roster.pilot", "Pilot"), 0f, TextAlignmentOptions.MidlineLeft), (L("roster.zoneHold", "Zone Hold"), 5f, TextAlignmentOptions.Midline),
                        (L("roster.kd", "K / D"), 7f, TextAlignmentOptions.Midline), (L("roster.pads", "Pads"), 9f, TextAlignmentOptions.Midline), (L("roster.roleOrReport", "Role / Report"), 12f, TextAlignmentOptions.MidlineRight),
                    };
                    foreach (var l in labels) Cell(box, "Col_" + l.text, l.text, UiFont.Heading, ResultsCaptionDp, Outline, .08f, l.align, col, l.center, cy + 4f, wideLabelHeight);
                    cy += wideLabelHeight + 4f;
                }
                int slot = 0;
                if (players != null)
                    foreach (var p in players)
                    {
                        if (p == null || p.TeamId != team) continue;
                        bool self = ReferenceEquals(p, local);
                        int participantSlot = p.ParticipantId;
                        bool canReport = reportParticipant != null && reportParticipantEligible != null
                            && !self && participantSlot >= 0 && reportParticipantEligible(participantSlot);
                        var row = UiDraw.Box(box, "Player_" + group + "_" + slot++, self ? High : Mid, 8f);
                        float compactRowHeight = Mathf.Max(84f, compactNameHeight + 2f * compactMetricLineHeight + 22f,
                            canReport ? reportButtonHeight : 0f);
                        float wideNameHeight = ResultsTextLineHeight(ResultsValueDp);
                        float wideSubHeight = ResultsTextLineHeight(ResultsCaptionDp);
                        const float wideNameSubGap = 2f;
                        float wideTextStackHeight = wideNameHeight + wideNameSubGap + wideSubHeight;
                        float wideRowHeight = Mathf.Max(52f, wideTextStackHeight + 12f,
                            canReport ? reportButtonHeight : 0f);
                        float rowHeight = compact ? compactRowHeight : wideRowHeight;
                        TopLeft(row.rectTransform, 12f, cy, inner, rowHeight);
                        var r = row.rectTransform;
                        float nameX = 8f;
                        if (self)
                        {
                            var badge = UiDraw.Text(r, "Badge", L("common.you", "You"), UiFont.Heading, ResultsCaptionDp, OnPrimary, .08f, TextAlignmentOptions.Midline);
                            float bw = UiDraw.Width(badge) + 12f;
                            float badgeHeight = compact ? compactMetricLineHeight : ResultsTextLineHeight(ResultsCaptionDp);
                            var plate = UiDraw.Box(r, "BadgePlate", self ? Primary : Highest, 4f);
                            plate.transform.SetSiblingIndex(badge.transform.GetSiblingIndex());
                            float nameY = compact ? rowHeight * .5f - compactNameHeight * .5f - 4f : (wideSubHeight + wideNameSubGap) * .5f;
                            UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, nameY), new Vector2(bw, badgeHeight));
                            UiDraw.Place(badge.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, nameY), new Vector2(bw, badgeHeight));
                            nameX += bw + 4f;
                        }
                        else
                        {
                            UiDraw.Place(UiDraw.Dot(r, "Dot", 8f, group == 0 ? UIDesignTokens.SurfaceStructural : UIDesignTokens.WithAlpha(Secondary, .5f)).rectTransform,
                                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, compact ? rowHeight * .5f - compactNameHeight * .5f - 4f : (wideSubHeight + wideNameSubGap) * .5f), new Vector2(8f, 8f));
                            nameX += 12f;
                        }
                        string display = p.DisplayName;
                        var nm = UiDraw.Text(r, "Name", !p.IsConnected ? UiDraw.LFormat("results.roster.leftPlayer", "(Left)  {0}", display) : display, self ? UiFont.BodyBold : UiFont.BodySemiBold, ResultsValueDp, OnSurface, .01f);
                        nm.overflowMode = TextOverflowModes.Ellipsis;
                        float nameRight = compact ? inner - 8f : col * 4f;
                        if (compact && canReport) nameRight -= reportButtonWidth + 8f;
                        float nameWidth = nameRight - nameX;
                        if (canReport)
                        {
                            var report = UiDraw.Tap(r, "Report", High, 8f, () => reportParticipant(participantSlot));
                            UiDraw.Place((RectTransform)report.transform, new Vector2(1f, .5f), new Vector2(1f, .5f),
                                new Vector2(-4f, 0f), new Vector2(reportButtonWidth, reportButtonHeight));
                            var label = UiDraw.Text(report.transform, "Label", UiDraw.L("support.report.action", "Report"), UiFont.Heading, ResultsCaptionDp, Primary, .03f, TextAlignmentOptions.Midline);
                            float labelInset = UiMetrics.UnitsForDp(2f);
                            FitTextToWidth(label, Mathf.Max(0f, reportButtonWidth - labelInset * 2f));
                            UiDraw.Fill(label.rectTransform, labelInset, 0f, labelInset, 0f);
                        }
                        UiDraw.Place(nm.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                            new Vector2(nameX, compact ? rowHeight * .5f - compactNameHeight * .5f - 4f : (wideSubHeight + wideNameSubGap) * .5f),
                            new Vector2(Mathf.Max(0f, nameWidth), compact ? compactNameHeight : wideNameHeight));
                        if (!compact)
                            UiDraw.Place(UiDraw.Text(r, "Sub", self ? L("roster.yourRun", "Your run") : group == 0 ? L("roster.squadAlly", "Squad Ally") : L("roster.rival", "Rival"), UiFont.MonoMedium, ResultsCaptionDp, self ? Primary : Outline, .05f).rectTransform,
                                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(nameX, -(wideNameHeight + wideNameSubGap) * .5f), new Vector2(col * 4f - nameX, wideSubHeight));
                        int held = Mathf.RoundToInt(p.ObjectiveSeconds);
                        string zoneTime = $"{held / 60:00}:{held % 60:00}";
                        if (compact)
                        {
                            var metrics = new (string label, string value, Color color)[]
                            {
                                (L("roster.zoneHold", "Zone Hold"), zoneTime, group == 0 || self ? OnSurface : OnVariant),
                                (L("roster.kd", "K / D"), $"{p.Eliminations} / {p.Deaths}", self ? Primary : group == 1 ? Secondary : OnVariant),
                                (L("roster.pads", "Pads"), p.PadLaunches.ToString(), OnSurface),
                            };
                            float metricAreaWidth = inner - (canReport ? reportButtonWidth + 8f : 0f);
                            float metricWidth = (metricAreaWidth - 16f) / metrics.Length;
                            float labelY = -rowHeight * .5f + 8f + compactMetricLineHeight * .5f;
                            float valueY = labelY + compactMetricLineHeight + 2f;
                            for (int i = 0; i < metrics.Length; i++)
                            {
                                float centerX = 8f + metricWidth * (i + .5f);
                                var metricLabel = UiDraw.Text(r, "CompactMetricLabel" + i, metrics[i].label, UiFont.Heading,
                                    ResultsCaptionDp, Outline, .03f, TextAlignmentOptions.Midline);
                                metricLabel.overflowMode = TextOverflowModes.Ellipsis;
                                UiDraw.Place(metricLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                                    new Vector2(centerX - metricWidth * .5f, labelY), new Vector2(metricWidth, compactMetricLineHeight));
                                var metricValue = UiDraw.Text(r, "CompactMetricValue" + i, metrics[i].value, UiFont.Mono,
                                    ResultsCaptionDp, metrics[i].color, .02f, TextAlignmentOptions.Midline);
                                metricValue.overflowMode = TextOverflowModes.Ellipsis;
                                UiDraw.Place(metricValue.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                                    new Vector2(centerX - metricWidth * .5f, valueY), new Vector2(metricWidth, compactMetricLineHeight));
                            }
                        }
                        else
                        {
                            RowCell(r, "Zone", zoneTime, UiFont.Mono, ResultsCaptionDp, group == 0 || self ? OnSurface : OnVariant, TextAlignmentOptions.Midline, col, 5f);
                            RowCell(r, "KD", $"{p.Eliminations} / {p.Deaths}", UiFont.Mono, ResultsCaptionDp, self ? Primary : group == 1 ? Secondary : OnVariant, TextAlignmentOptions.Midline, col, 7f);
                            RowCell(r, "Pads", p.PadLaunches.ToString(), UiFont.MonoMedium, ResultsCaptionDp, OnSurface, TextAlignmentOptions.Midline, col, 9f);
                            if (!canReport)
                                RowCell(r, "Role", self ? L("common.you", "You") : group == 0 ? L("roster.ally", "Ally") : L("roster.rivalUpper", "Rival"), UiFont.MonoMedium, ResultsCaptionDp, self ? Primary : OnVariant, TextAlignmentOptions.MidlineRight, col, 12f);
                        }
                        cy += rowHeight + 4f;
                    }
                cy += 8f;
            }
            float h = cy + 4f;
            box.sizeDelta = new Vector2(w, h);
            return h;
        }

        static void Cell(RectTransform box, string name, string text, UiFont font, float size, Color color, float tracking,
            TextAlignmentOptions align, float col, float center, float top, float height)
        {
            var t = UiDraw.Text(box, name, text, font, size, color, tracking, align);
            float x = 12f + 8f + (align == TextAlignmentOptions.MidlineLeft ? 0f : align == TextAlignmentOptions.MidlineRight ? center * col - 16f - 120f : center * col - 60f);
            TopLeft(t.rectTransform, x, top, 120f, height);
        }

        static void RowCell(RectTransform row, string name, string text, UiFont font, float size, Color color, TextAlignmentOptions align, float col, float center)
        {
            var t = UiDraw.Text(row, name, text, font, size, color, .03f, align);
            float x = align == TextAlignmentOptions.MidlineRight ? center * col - 8f - 120f : center * col - 60f;
            UiDraw.Place(t.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(x, 0f), new Vector2(120f, ResultsTextLineHeight(size)));
        }        // ------------------------------------------------------------ progression


        static float Progression(RectTransform page, float x, float w, float y, string footer, ResultsTrustState trustState)
        {
            bool localPractice = trustState == ResultsTrustState.Practice;
            var box = Card(page, "Progression", Low, x, y, w, 100f);
            // The footer string shown in Progression only carries informational match metadata
            // (for example the progression/next-goal note). Cancellation and receipt state are
            // represented explicitly from the authoritative caller via trustState; never by matching
            // footer text. That keeps cancelled, practice, pending-receipt, and verified-result
            // behavior independent of how a footer is worded, and preserves the sticky Play Again
            // action in every case the caller keeps it alive.
            float inner = w - 24f;
            float captionLineHeight = Mathf.Max(18f, UiMetrics.TextSize(ResultsCaptionDp) + 4f);
            UiDraw.Place(UiDraw.Icon(box, "Icon", "military_tech", 18f, Tertiary).rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(12f, -21f), new Vector2(18f, 18f));
            TopLeft(UiDraw.Text(box, "Heading", localPractice ? L("progression.practice", "Practice Progression") : trustState == ResultsTrustState.Cancelled ? L("progression.unchanged", "Progression Unchanged") : trustState == ResultsTrustState.Verified ? L("progression.earnedBreakdown", "Earned Progression Breakdown") : L("progression.notConfirmed", "Progression Not Confirmed"), UiFont.Heading, ResultsCaptionDp, OnSurface, .08f).rectTransform, 34f, 12f, 320f, captionLineHeight);
            float receiptTop = 12f + captionLineHeight + 12f;
            bool streamSafe = StreamSafePresentation.Enabled;
            var receipt = UiDraw.Text(box, "Receipt", streamSafe ? L("profile.accreditation.xpHidden", "Progress Hidden") : footer ?? "", UiFont.Mono, ResultsCaptionDp, Tertiary, .05f);
            receipt.textWrappingMode = TextWrappingModes.Normal;
            receipt.alignment = TextAlignmentOptions.TopLeft;
            float rh = Mathf.Max(UiMetrics.TextSize(ResultsCaptionDp) + 4f, receipt.GetPreferredValues(receipt.text, inner, 0f).y);
            TopLeft(receipt.rectTransform, 12f, receiptTop, inner, rh);
            var meter = UiDraw.Box(box, "XpMeter", Mid, 3f);
            meter.gameObject.SetActive(!streamSafe);
            TopLeft(meter.rectTransform, 12f, receiptTop + rh + 6f, inner, 6f);
            var fill = UiDraw.Box(meter.transform, "XpFill", Primary, 3f);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.pivot = new Vector2(0f, .5f);
            fill.rectTransform.anchoredPosition = Vector2.zero;
            fill.rectTransform.sizeDelta = Vector2.zero;
            var burst = UiDraw.Ring(box, "RewardBurst", 16f, 1.5f, Tertiary);
            TopLeft(burst.rectTransform, 8f, receiptTop + rh + 1f, 16f, 16f);
            burst.gameObject.SetActive(false);
            var skip = UiDraw.Tap(box, "SkipReward", Mid, 8f, null);
            TopLeft((RectTransform)skip.transform, inner - 72f, 4f, 64f, 48f);
            TopLeft(UiDraw.Text(skip.transform, "Label", L("action.skip", "Skip"), UiFont.Heading, ResultsCaptionDp, OnSurface, .04f, TextAlignmentOptions.Midline).rectTransform, 0f, 0f, 64f, 48f);
            skip.gameObject.SetActive(false);

            var note = UiDraw.Box(box, "SaveNote", Mid, 8f);
            UiDraw.Place(UiDraw.Icon(note.transform, "Lock", "lock", 14f, Outline).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(8f, 0f), new Vector2(14f, 14f));
            string initialNote = streamSafe ? L("profile.accreditation.xpHidden", "Progress Hidden") : trustState switch
            {
                ResultsTrustState.Practice => L("save.practiceNoRewards", "No online XP, credits, contracts, or mastery are awarded in local practice."),
                ResultsTrustState.Cancelled => L("save.cancelledNoRewards", "Cancelled matches change no rating, XP, or credits."),
                ResultsTrustState.Pending => L("save.waiting", "Waiting for the authoritative match receipt."),
                ResultsTrustState.Unavailable => L("save.unavailable", "This screen could not confirm the receipt. Check your profile later."),
                ResultsTrustState.UnverifiedDevelopment => L("save.development", "No server receipt is available for this development session."),
                _ => L("save.waiting", "Waiting for the authoritative match receipt."),
            };
            var noteText = UiDraw.Text(note.transform, "Text", initialNote, UiFont.MonoMedium, ResultsCaptionDp, OnVariant, .05f);
            noteText.textWrappingMode = TextWrappingModes.Normal;
            noteText.alignment = TextAlignmentOptions.MidlineLeft;
            float noteTextHeight = Mathf.Max(UiMetrics.TextSize(ResultsCaptionDp) + 4f,
                noteText.GetPreferredValues(initialNote, inner - 36f, 0f).y);
            float noteHeight = Mathf.Max(28f, noteTextHeight + 8f);
            float noteTop = receiptTop + rh + 18f;
            TopLeft(note.rectTransform, 12f, noteTop, inner, noteHeight);
            UiDraw.Place(noteText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                new Vector2(28f, 0f), new Vector2(inner - 36f, noteTextHeight));
            float h = noteTop + noteHeight + 12f;
            box.sizeDelta = new Vector2(w, h);
            return h;
        }

        // ------------------------------------------------------------ sticky actions

        static void Actions(RectTransform root, Action replay, Action home)
        {
            var bar = UiDraw.Box(root, "Actions", UIDesignTokens.WithAlpha(Low, .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, ActionsHeight); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UIDesignTokens.BorderColor(.5f), 0f);
            var back = UiDraw.Tap(bar, "Home", High, 8f, () => home?.Invoke());
            var backText = UiDraw.Text(back.transform, "Caption", L("action.returnHome", "Return Home"), UiFont.BodySemiBold, ResultsValueDp, OnSurface, .01f);
            float bw = 16f + 20f + 8f + UiDraw.Width(backText) + 16f;
            UiDraw.Place((RectTransform)back.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(bw, 48f));
            UiDraw.Place(UiDraw.Icon(back.transform, "Icon", "arrow_back", 20f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(20f, 20f));
            float backTextHeight = Mathf.Max(22f, UiMetrics.TextSize(ResultsValueDp) + 4f);
            UiDraw.Place(backText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(44f, 0f), new Vector2(UiDraw.Width(backText) + 2f, backTextHeight));

            var play = UiDraw.Tap(bar, "Replay", Primary, 12f, () => replay?.Invoke());
            var playText = UiDraw.Text(play.transform, "Caption", L("action.playAgain", "Play Again"), UiFont.Heading, ResultsValueDp, OnPrimary, .02f);
            float pw = 40f + UiDraw.Width(playText) + 8f + 20f + 40f;
            UiDraw.Place((RectTransform)play.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(pw, 52f));
            UiDraw.Glow((RectTransform)play.transform, UIDesignTokens.WithAlpha(Primary, .3f), 12f, 16f);
            float playTextHeight = Mathf.Max(26f, UiMetrics.TextSize(ResultsValueDp) + 4f);
            UiDraw.Place(playText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(UiDraw.Width(playText) + 2f, playTextHeight));
            UiDraw.Place(UiDraw.Icon(play.transform, "Icon", "play_arrow", 20f, OnPrimary).rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-40f, 0f), new Vector2(20f, 20f));
            var ready = UiDraw.Text(bar, "ReadyStatus", L("action.readyStatus", "Ready Status"), UiFont.Heading, ResultsCaptionDp, OnVariant, .08f, TextAlignmentOptions.MidlineRight);
            float readyWidth = UiDraw.Width(ready) + 2f;
            float readyHeight = Mathf.Max(16f, UiMetrics.TextSize(ResultsCaptionDp) + 4f);
            UiDraw.Place(ready.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad - pw - 16f, 0f), new Vector2(readyWidth, readyHeight));
        }

        // ------------------------------------------------------------ helpers

        static RectTransform Card(RectTransform parent, string name, Color fill, float x, float y, float w, float h)
        {
            var box = UiDraw.Box(parent, name, fill, 12f);
            TopLeft(box.rectTransform, x, y, w, h);
            return box.rectTransform;
        }

        static void TopLeft(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
    }
}
