using System;
using System.Collections.Generic;
using System.Linq;
using Hotdrop.Config;
using Hotdrop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>
    /// Screen 14: the installed offline missions, local records and the selected mission, rebuilt
    /// from its UIUX HTML (CSS px) as a scrolling page over a sticky footer. Records, medals and
    /// thresholds are the local save's and the mission config's own.
    /// </summary>
    public static class OfflineChallengeLibraryView
    {
        static string L(string key, string fallback) => UiDraw.L("offlineLibrary." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("offlineLibrary." + key, fallback, args);
        static string ChallengeTitle(OfflineChallengeEntry entry) => entry == null ? string.Empty : UiDraw.L("offlineLibrary.challenge." + entry.id + ".title", entry.title ?? string.Empty);
        static string ChallengeDescription(OfflineChallengeEntry entry) => entry == null ? string.Empty : UiDraw.L("offlineLibrary.challenge." + entry.id + ".description", entry.description ?? string.Empty);
        static string DifficultyName(string difficulty) => UiDraw.L("offlineLibrary.difficulty." + (difficulty ?? string.Empty).ToLowerInvariant(), difficulty ?? string.Empty);
        static string MedalName(ChallengeMedal medal) => L("medal." + medal.ToString().ToLowerInvariant(), medal.ToString().ToUpperInvariant());

        static Color Lowest => UIDesignTokens.Background;
        static Color Low => UIDesignTokens.SurfacePanel;
        static Color Mid => Color.Lerp(UIDesignTokens.Background, UIDesignTokens.SurfacePanel, .55f);
        static Color High => UIDesignTokens.SurfaceSelected;
        static Color Selected => UIDesignTokens.SurfaceSelected;
        static Color Primary => UIDesignTokens.PrimaryAction;
        static Color PrimaryC => UIDesignTokens.PrimaryAction;
        static Color Gold => UIDesignTokens.ObjectiveGold;
        static Color Tertiary => UIDesignTokens.ObjectiveGold;
        static Color Bronze => UiDraw.Hex("#c8a050");
        static Color Silver => UiDraw.Hex("#c9d1d9");
        static Color OnSurface => UIDesignTokens.TextPrimary;
        static Color OnVariant => UIDesignTokens.TextSecondary;
        static Color Outline => UIDesignTokens.TextMuted;
        static Color Ink => UIDesignTokens.TextOnAccent;

        const float CaptionDp = UIDesignTokens.CaptionDp;
        const float UiDp = UIDesignTokens.BodyLargeDp;
        const float TitleDp = UIDesignTokens.TitleLargeDp;
        const float Pad = 24f, Gap = 24f;
        // 76 px design bar around 52 px actions; grows with the 48 dp floor on dense phones.
        static float FooterHeight => UiMetrics.BarHeight(76f, 52f);

        sealed class DensityReflow : MonoBehaviour
        {
            internal Vector3 lastScreen;
            internal float lastWidth;
            internal bool initialized;
            GameObject owner;
            IReadOnlyList<OfflineChallengeEntry> entries;
            Func<string, ChallengeRecord> records;
            OfflineChallengeEntry selected;
            bool hasSnapshot;
            string saveStatus;
            Action back, resume, practice;
            Action<string> select, start;

            internal void Initialize(GameObject viewOwner, IReadOnlyList<OfflineChallengeEntry> viewEntries,
                Func<string, ChallengeRecord> viewRecords, OfflineChallengeEntry viewSelected, bool viewHasSnapshot,
                string viewSaveStatus, Action viewBack, Action<string> viewSelect, Action<string> viewStart,
                Action viewResume, Action viewPractice)
            {
                owner = viewOwner; entries = viewEntries; records = viewRecords; selected = viewSelected;
                hasSnapshot = viewHasSnapshot; saveStatus = viewSaveStatus; back = viewBack;
                select = viewSelect; start = viewStart; resume = viewResume; practice = viewPractice;
            }

            void LateUpdate()
            {
                if (!initialized || owner == null || !owner.activeInHierarchy) return;
                var root = (RectTransform)transform;
                Vector3 screen = UiMetrics.CurrentScreen;
                float width = root.rect.width;
                if (screen == lastScreen && Mathf.Abs(width - lastWidth) <= 4f) return;

                float scrollPosition = 1f;
                var scroll = root.Find("Viewport")?.GetComponent<ScrollRect>();
                if (scroll != null) scrollPosition = scroll.verticalNormalizedPosition;
                Show(owner, entries, records, selected, hasSnapshot, saveStatus, back, select, start, resume, practice, scrollPosition);
            }
        }

        public static void Show(GameObject owner, IReadOnlyList<OfflineChallengeEntry> entries,
            Func<string, ChallengeRecord> records, OfflineChallengeEntry selected, bool hasSnapshot,
            string saveStatus, Action back, Action<string> select, Action<string> start,
            Action resume, Action practice, float scrollPosition = 1f)
        {
            if (owner == null) return;
            var previous = owner.transform.Find("OfflineChallengeVisual");
            if (previous != null)
            {
                previous.name = "OfflineChallengeVisual_Superseded";
                previous.gameObject.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(previous.gameObject);
                else UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }
            var root = UiDraw.Node(owner.transform, "OfflineChallengeVisual");
            UiDraw.Fill(root);
            UiDraw.Fill(UiDraw.Box(root, "Base", Lowest).rectTransform);
            float width = root.rect.width > 0f ? root.rect.width : owner.GetComponent<RectTransform>().rect.width;
            if (width <= 0f) width = 1280f;
            float w = width - Pad * 2f;

            var viewport = UiDraw.Node(root, "Viewport");
            UiDraw.Fill(viewport, 0f, 0f, 0f, FooterHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var page = UiDraw.Node(viewport, "Page");
            page.anchorMin = page.anchorMax = new Vector2(0f, 1f); page.pivot = new Vector2(0f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = page; scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30f;

            selected ??= entries?.FirstOrDefault(e => e != null && records(e.id)?.isCompleted != true)
                ?? entries?.FirstOrDefault(e => e != null);
            int total = entries?.Count ?? 0;
            int done = entries?.Count(e => e != null && records(e.id)?.isCompleted == true) ?? 0;

            float y = 20f;
            y = Intro(page, w, y, total, done, back) + 20f;
            y = Guarantee(page, w, y, saveStatus) + 24f;
            float unit = (w - 11f * Gap) / 12f;
            float leftW = unit * 7f + Gap * 6f, rightW = unit * 5f + Gap * 4f, rightX = Pad + leftW + Gap;
            float ly = y;
            for (int c = 0; c < 3; c++)
                ly = Chapter(page, Pad, ly, leftW, (ChallengeChapter)c, c, entries, records, selected, select) + 20f;
            float ry = Inspection(page, rightX, y, rightW, selected, selected != null ? records(selected.id) : null, hasSnapshot, start, resume) + 20f;
            ry = Practice(page, rightX, ry, rightW, practice) + 20f;
            y = Mathf.Max(ly, ry) + 4f;
            var meta = UiDraw.Text(page, "Meta", L("meta.noNetwork", "No Network Required"), UiFont.MonoMedium, CaptionDp, OnVariant, .1f);
            TL(meta.rectTransform, Pad + 14f, y, w, 16f);
            TL(UiDraw.Dot(page, "MetaDot", 8f, PrimaryC).rectTransform, Pad, y + 4f, 8f, 8f);
            page.sizeDelta = new Vector2(width, y + 40f);
            page.anchoredPosition = Vector2.zero;

            Footer(root, total, selected, hasSnapshot, back, start);
            var reflow = root.gameObject.AddComponent<DensityReflow>();
            reflow.Initialize(owner, entries, records, selected, hasSnapshot, saveStatus, back, select, start, resume, practice);
            Reflow(root, reflow);
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scrollPosition);
        }

        static void Reflow(RectTransform root, DensityReflow watcher)
        {
            if (root == null) return;
            if (root.Find("Footer") is RectTransform footer)
                footer.sizeDelta = new Vector2(footer.sizeDelta.x, FooterHeight);
            if (root.Find("Viewport") is RectTransform viewport)
                viewport.offsetMin = new Vector2(viewport.offsetMin.x, FooterHeight);
            UiFloor.Apply(root);
            watcher.lastScreen = UiMetrics.CurrentScreen;
            watcher.lastWidth = root.rect.width;
            watcher.initialized = true;
        }

        static float Intro(RectTransform page, float w, float y, int total, int done, Action back)
        {
            var card = Card(page, "Introduction", Low, Pad, y, w, 136f);
            float x = 24f;
            var chip = UiDraw.Text(card, "Kicker", L("intro.airplaneMode", "Works in Airplane Mode"), UiFont.Mono, CaptionDp, Primary, .1f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(chip) + 24f;
            var plate = UiDraw.Box(card, "KickerPlate", High, 4f);
            plate.transform.SetSiblingIndex(chip.transform.GetSiblingIndex());
            TL(plate.rectTransform, x, 22f, cw, 20f); TL(chip.rectTransform, x, 22f, cw, 20f);
            x += cw + 12f;
            foreach (var part in new[] { F("intro.challengeCountTemplate", "{0} Challenges", total), L("intro.noAccount", "No Account Needed") })
            {
                var t = UiDraw.Text(card, "Meta", part, UiFont.MonoMedium, CaptionDp, OnVariant, .1f);
                TL(t.rectTransform, x, 24f, UiDraw.Width(t) + 2f, 16f);
                x += UiDraw.Width(t) + 20f;
            }
            TL(UiDraw.Text(card, "Title", L("intro.title", "Offline Travel Challenges"), UiFont.Heading, TitleDp, OnSurface, -.03f).rectTransform, 24f, 46f, w - 400f, 48f);
            TL(UiDraw.Text(card, "Description", F("intro.descriptionTemplate", "{0} solo challenges  •  No data needed  •  Already on your phone", total), UiFont.BodySemiBold, UiDp, OnVariant).rectTransform,
                24f, 98f, w - 400f, 22f);
            var button = UiDraw.Tap(card, "ReturnHome", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var label = UiDraw.Text(button.transform, "Label", L("button.returnHome", "Return to Home"), UiFont.Heading, UiDp, OnSurface, .01f);
            float bw = 24f + 22f + 12f + UiDraw.Width(label) + 24f;
            UiDraw.Place((RectTransform)button.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f, 0f), new Vector2(bw, 60f));
            UiDraw.Place(UiDraw.Icon(button.transform, "Icon", "flight_land", 22f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(22f, 22f));
            UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(58f, 0f), new Vector2(UiDraw.Width(label) + 2f, 26f));
            var recLabel = UiDraw.Text(card, "RecordsLabel", L("records.title", "Local Records"), UiFont.MonoMedium, CaptionDp, OnVariant, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(recLabel.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f - bw - 20f, 10f), new Vector2(240f, 14f));
            var recValue = UiDraw.Text(card, "RecordsValue", F("records.progressTemplate", "{0} / {1}  Completed", done, total), UiFont.Mono, UiDp, Tertiary, .05f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(recValue.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-24f - bw - 20f, -9f), new Vector2(260f, 18f));
            return y + 136f;
        }

        static float Guarantee(RectTransform page, float w, float y, string saveStatus)
        {
            var card = Card(page, "LocalRecords", Low, Pad, y, w, 64f);
            var plate = UiDraw.Box(card, "IconPlate", High, 8f);
            UiDraw.Place(plate.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(40f, 40f));
            UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", "flight", 22f, Primary).rectTransform);
            var label = UiDraw.Text(card, "Label", L("records.savedOnDevice", "SAVED ON\nTHIS PHONE"), UiFont.Mono, CaptionDp, Primary, .1f);
            UiDraw.Place(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(64f, 0f), new Vector2(140f, 32f));
            var text = UiDraw.Text(card, "Assurance", string.IsNullOrWhiteSpace(saveStatus)
                ? L("records.localAssurance", "Offline medals and records stay on this device. No cloud sync, no microtransactions, no background battery drain.")
                : saveStatus, UiFont.Body, UiDp, OnSurface);
            text.textWrappingMode = TextWrappingModes.Normal;
            UiDraw.Place(text.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(214f, 0f), new Vector2(w - 214f - 200f, 40f));
            var sync = UiDraw.Text(card, "Sync", L("records.saveReady", "Save Ready"), UiFont.Mono, CaptionDp, Tertiary, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(sync.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-20f, 0f), new Vector2(180f, 14f));
            return y + 64f;
        }

        static float Chapter(RectTransform page, float x, float y, float w, ChallengeChapter chapter, int index,
            IReadOnlyList<OfflineChallengeEntry> entries, Func<string, ChallengeRecord> records, OfflineChallengeEntry selected, Action<string> select)
        {
            var chapterEntries = entries?.Where(e => e != null && e.chapter == chapter).OrderBy(e => e.indexInChapter).Take(4).ToArray()
                ?? Array.Empty<OfflineChallengeEntry>();
            int completed = chapterEntries.Count(e => records(e.id)?.isCompleted == true);
            bool allGold = chapterEntries.Length > 0 && chapterEntries.All(e => records(e.id)?.medal == ChallengeMedal.Gold);
            int rows = Mathf.CeilToInt(chapterEntries.Length / 2f);
            float h = 24f + 52f + 18f + rows * 128f + Mathf.Max(0, rows - 1) * 12f + 24f;
            var card = Card(page, "Chapter_" + chapter, Low, x, y, w, h);
            TL(UiDraw.Box(card, "Accent", completed > 0 ? PrimaryC : UIDesignTokens.SurfaceDisabled, 3f).rectTransform, 24f, 34f, 6f, 28f);
            TL(UiDraw.Text(card, "ChapterNumber", F("chapter.numberTemplate", "Chapter {0:00}", index + 1), UiFont.Heading, CaptionDp, Primary, .05f).rectTransform, 46f, 28f, 300f, 16f);
            TL(UiDraw.Text(card, "ChapterName", ChapterName(chapter), UiFont.Heading, UiDp, OnSurface, -.01f).rectTransform, 46f, 46f, 400f, 28f);
            string status = F("chapter.progressTemplate", "{0}/{1} Completed", completed, chapterEntries.Length) + (allGold ? L("chapter.goldMasterySuffix", "  •  GOLD MASTERY") : "");
            var count = UiDraw.Text(card, "Count", status, UiFont.Mono, CaptionDp, completed == chapterEntries.Length && completed > 0 ? Tertiary : OnVariant, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(count.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-24f, -52f), new Vector2(360f, 16f));
            float tileW = (w - 48f - 12f) / 2f;
            for (int i = 0; i < chapterEntries.Length; i++)
            {
                var entry = chapterEntries[i];
                var record = records(entry.id);
                bool chosen = selected != null && selected.id == entry.id;
                float tx = 24f + (i % 2) * (tileW + 12f), ty = 94f + (i / 2) * 140f;
                var tile = UiDraw.Tap(card, "Challenge_" + entry.id, chosen ? Selected : Mid, 8f, () => { GameAudio.Play2D(GameCue.UiTap); select?.Invoke(entry.id); });
                TL((RectTransform)tile.transform, tx, ty, tileW, 128f);
                var t = (RectTransform)tile.transform;
                TL(UiDraw.Text(t, "Number", F("stage.numberTemplate", "Stage {0:00}.{1}", index + 1, i + 1) + (chosen ? L("stage.selectedSuffix", "  •  SELECTED") : ""), UiFont.MonoMedium, CaptionDp, chosen ? Primary : OnVariant, .1f).rectTransform, 16f, 16f, tileW - 60f, 16f);
                TL(UiDraw.Text(t, "Title", ChallengeTitle(entry), UiFont.BodySemiBold, UiDp, chosen ? Primary : OnSurface).rectTransform, 16f, 34f, tileW - 60f, 28f);
                var medal = record?.isCompleted == true ? record.medal : ChallengeMedal.None;
                UiDraw.Place(UiDraw.Icon(t, "Medal", medal == ChallengeMedal.Gold ? "stars" : medal == ChallengeMedal.None ? "radio_button_unchecked" : "military_tech", 24f,
                        medal == ChallengeMedal.Gold ? Gold : medal == ChallengeMedal.Silver ? Silver : medal == ChallengeMedal.Bronze ? Bronze : Outline, medal == ChallengeMedal.Gold).rectTransform,
                    new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -28f), new Vector2(24f, 24f));
                string left = record?.isCompleted == true ? F("stage.personalRecordTemplate", "{0:0.0}s [LOCAL REC]", record.personalBestTimeSeconds) : entry.timeLimitSeconds > 0 ? F("stage.timeLimitTemplate", "LIMIT {0:0}s", entry.timeLimitSeconds) : F("stage.objectivesTemplate", "{0} Objectives", entry.targetCount);
                string right = medal == ChallengeMedal.Gold ? L("stage.goldMastered", "Gold Mastered") : medal != ChallengeMedal.None ? F("stage.currentMedalTemplate", "Current: {0}", MedalName(medal)) : L("stage.notAttempted", "Not Attempted");
                var best = UiDraw.Text(t, "Best", left, UiFont.Mono, CaptionDp, record?.isCompleted == true ? Tertiary : OnVariant, .05f);
                UiDraw.Place(best.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(16f, 24f), new Vector2(tileW * .6f, 16f));
                var state = UiDraw.Text(t, "State", right, UiFont.MonoMedium, CaptionDp, OnVariant, .1f, TextAlignmentOptions.MidlineRight);
                UiDraw.Place(state.rectTransform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-16f, 24f), new Vector2(tileW * .45f, 16f));
            }
            return y + h;
        }

        static float Inspection(RectTransform page, float x, float y, float w, OfflineChallengeEntry selected, ChallengeRecord record,
            bool hasSnapshot, Action<string> start, Action resume)
        {
            var card = Card(page, "Inspection", Low, x, y, w, 100f);
            UiDraw.Glow(card, UIDesignTokens.WithAlpha(PrimaryC, .08f), 12f, 20f);
            TL(UiDraw.Dot(card, "Dot", 8f, PrimaryC).rectTransform, 24f, 32f, 8f, 8f);
            TL(UiDraw.Text(card, "Kicker", L("detail.kicker", "Challenge Details"), UiFont.Heading, CaptionDp, Primary, .05f).rectTransform, 38f, 28f, w - 180f, 16f);
            var medal = record?.isCompleted == true ? record.medal : ChallengeMedal.None;
            var chipText = UiDraw.Text(card, "MedalChip", medal == ChallengeMedal.None ? L("detail.noMedal", "Medal: None") : F("detail.medalTemplate", "Medal: {0}", MedalName(medal)), UiFont.Mono, CaptionDp, OnSurface, .1f, TextAlignmentOptions.Midline);
            float cw = UiDraw.Width(chipText) + 18f;
            var chip = UiDraw.Box(card, "MedalPlate", High, 4f);
            chip.transform.SetSiblingIndex(chipText.transform.GetSiblingIndex());
            UiDraw.Place(chip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-24f, -36f), new Vector2(cw, 22f));
            UiDraw.Place(chipText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-24f, -36f), new Vector2(cw, 22f));
            TL(UiDraw.Text(card, "Title", selected != null ? ChallengeTitle(selected) : L("detail.chooseMission", "Choose a Mission"), UiFont.Heading, TitleDp, OnSurface, -.03f).rectTransform, 24f, 54f, w - 48f, 40f);
            var th = selected?.medalThresholds ?? default;
            string recordLine = record?.isCompleted == true
                ? F("detail.recordMedalTemplate", "{0} Medal {1:0.0}s", MedalName(medal), record.personalBestTimeSeconds) + (medal != ChallengeMedal.Gold && th.goldTimeSeconds > 0 ? F("detail.goldTargetSuffix", " - Target: {0:0.0}s", th.goldTimeSeconds) : "")
                : L("detail.noCompletedRecord", "No completed local record") + (th.goldTimeSeconds > 0 ? F("detail.goldTimeSuffix", " - Gold: {0:0.0}s", th.goldTimeSeconds) : "");
            TL(UiDraw.Text(card, "Record", recordLine, UiFont.Mono, CaptionDp, Tertiary, .05f).rectTransform, 24f, 98f, w - 48f, 16f);
            float cy = 126f;
            // Route diagram.
            var route = UiDraw.Box(card, "RouteVisual", Lowest, 8f);
            float rh = (w - 48f) * 160f / 360f;
            TL(route.rectTransform, 24f, cy, w - 48f, rh);
            var routeArt = UiDraw.Art(route.transform, "Route", "challenge_route", Vector2.zero);
            UiDraw.Fill(routeArt.rectTransform);
            var routeTag = UiDraw.Text(route.transform, "RouteLabel", L("detail.route", "West Gantry Route"), UiFont.Mono, CaptionDp, Primary, .1f);
            UiDraw.Place(routeTag.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(16f, 16f), new Vector2(240f, 14f));
            var simTag = UiDraw.Text(route.transform, "SimLabel", L("detail.offline", "Offline"), UiFont.Mono, CaptionDp, Tertiary, .1f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(simTag.rectTransform, new Vector2(1f, 0f), new Vector2(1f, .5f), new Vector2(-16f, 16f), new Vector2(160f, 14f));
            cy += rh + 16f;
            // Objective and mechanics.
            var objective = UiDraw.Box(card, "Objective", Mid, 8f);
            var desc = UiDraw.Text(objective.transform, "Description", selected != null ? ChallengeDescription(selected) : L("detail.selectChallenge", "Select an installed challenge to inspect it."), UiFont.Body, UiDp, OnSurface);
            desc.textWrappingMode = TextWrappingModes.Normal; desc.alignment = TextAlignmentOptions.TopLeft;
            float dh = Mathf.Max(20f, desc.GetPreferredValues(desc.text, w - 80f, 0f).y);
            TL(UiDraw.Text(objective.transform, "ObjectiveLabel", L("detail.primaryObjective", "Primary Objective"), UiFont.Heading, CaptionDp, OnSurface, .05f).rectTransform, 16f, 16f, w - 80f, 16f);
            TL(desc.rectTransform, 16f, 34f, w - 80f, dh);
            string requirements = selected == null ? "" : selected.timeLimitSeconds > 0
                ? F("detail.requirementsWithTimeTemplate", "Limit {0:0}s  •  {1} objectives  •  {2}", selected.timeLimitSeconds, selected.targetCount, DifficultyName(selected.difficulty))
                : F("detail.requirementsTemplate", "{0} objectives  •  {1}", selected.targetCount, DifficultyName(selected.difficulty));
            TL(UiDraw.Text(objective.transform, "MechanicsLabel", L("detail.requiredMechanics", "Required Mechanics"), UiFont.Heading, CaptionDp, OnSurface, .05f).rectTransform, 16f, 34f + dh + 14f, w - 80f, 16f);
            TL(UiDraw.Text(objective.transform, "Requirements", requirements, UiFont.BodySemiBold, UiDp, Primary).rectTransform, 16f, 52f + dh + 14f, w - 80f, 22f);
            float oh = 52f + dh + 14f + 20f + 16f;
            TL(objective.rectTransform, 24f, cy, w - 48f, oh);
            cy += oh + 16f;
            // Personal record with the medal bands.
            var rec = UiDraw.Box(card, "PersonalRecord", Mid, 8f);
            TL(rec.rectTransform, 24f, cy, w - 48f, 130f);
            TL(UiDraw.Text(rec.transform, "Label", L("record.title", "Personal Record"), UiFont.Heading, CaptionDp, OnSurface, .05f).rectTransform, 16f, 26f, 200f, 16f);
            var value = UiDraw.Text(rec.transform, "Value", record?.isCompleted == true ? F("record.valueTemplate", "{0:0.0}s ({1})", record.personalBestTimeSeconds, MedalName(medal)) : L("record.none", "No Record"), UiFont.Mono, UiDp, Tertiary, .02f, TextAlignmentOptions.MidlineRight);
            UiDraw.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-16f, -34f), new Vector2(260f, 28f));
            float barW = w - 48f - 32f, seg = barW / 3f;
            var bands = new[] { (Bronze, L("medal.bronze", "Bronze"), selected != null && selected.timeLimitSeconds > 0 ? F("medal.timeTemplate", "{0:0.0}s", selected.timeLimitSeconds) : L("medal.finish", "Finish")),
                (Silver, L("medal.silver", "Silver"), th.silverTimeSeconds > 0 ? F("medal.timeTemplate", "{0:0.0}s", th.silverTimeSeconds) : "—"), (Gold, L("medal.gold", "Gold"), th.goldTimeSeconds > 0 ? F("medal.timeTemplate", "{0:0.0}s", th.goldTimeSeconds) : "—") };
            for (int i = 0; i < 3; i++)
            {
                var b = UiDraw.Box(rec.transform, "Band" + i, new Color(bands[i].Item1.r, bands[i].Item1.g, bands[i].Item1.b, (int)medal > i ? 1f : .35f), i == 0 || i == 2 ? 5f : 0f);
                TL(b.rectTransform, 16f + i * seg, 60f, seg, 12f);
                var name = UiDraw.Text(rec.transform, "BandName" + i, bands[i].Item2, UiFont.Mono, CaptionDp, OnVariant, .1f, TextAlignmentOptions.Midline);
                TL(name.rectTransform, 16f + i * seg, 84f, seg, 14f);
                var time = UiDraw.Text(rec.transform, "BandTime" + i, bands[i].Item3, UiFont.Mono, CaptionDp, i == 0 ? OnSurface : i == 1 ? Primary : Tertiary, .05f, TextAlignmentOptions.Midline);
                TL(time.rectTransform, 16f + i * seg, 100f, seg, 16f);
            }
            cy += 130f + 16f;
            if (selected != null)
            {
                string id = selected.id;
                float bw = hasSnapshot ? (w - 48f - 12f) * .58f : w - 48f;
                var startButton = UiDraw.Tap(card, "StartChallenge", PrimaryC, 8f, () => { GameAudio.Play2D(GameCue.UiTap); start?.Invoke(id); });
                TL((RectTransform)startButton.transform, 24f, cy, bw, 68f);
                UiDraw.Glow((RectTransform)startButton.transform, UIDesignTokens.WithAlpha(PrimaryC, .3f), 8f, 16f);
                var sl = UiDraw.Text(startButton.transform, "Label", hasSnapshot ? L("button.restart", "Restart") : L("button.startChallenge", "Start Challenge"), UiFont.HeadingSemiBold, UiDp, Ink, .01f);
                float lw = UiDraw.Width(sl);
                UiDraw.Place(UiDraw.Icon(startButton.transform, "Icon", "play_arrow", 22f, Ink).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 12f) / 2f, 0f), new Vector2(22f, 22f));
                UiDraw.Place(sl.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(17f, 0f), new Vector2(lw + 2f, 30f));
                if (hasSnapshot)
                {
                    var resumeButton = UiDraw.Tap(card, "ResumeChallenge", Gold, 8f, () => { GameAudio.Play2D(GameCue.UiTap); resume?.Invoke(); });
                    TL((RectTransform)resumeButton.transform, 24f + bw + 12f, cy, w - 48f - bw - 12f, 68f);
                    UiDraw.Fill(UiDraw.Text(resumeButton.transform, "Label", L("button.resume", "Resume"), UiFont.HeadingSemiBold, UiDp, Ink, .01f, TextAlignmentOptions.Midline).rectTransform);
                }
                cy += 68f + 24f;
            }
            card.sizeDelta = new Vector2(w, cy);
            return y + cy;
        }

        static float Practice(RectTransform page, float x, float y, float w, Action practice)
        {
            var card = Card(page, "PracticeRelay", Low, x, y, w, 100f);
            var plate = UiDraw.Box(card, "IconPlate", High, 8f);
            TL(plate.rectTransform, 24f, 24f, 48f, 48f);
            UiDraw.Fill(UiDraw.Icon(plate.transform, "Icon", "smart_toy", 26f, Primary).rectTransform);
            TL(UiDraw.Text(card, "Kicker", L("practice.kicker", "Full 3v3 Local Simulation"), UiFont.Heading, CaptionDp, Primary, .05f).rectTransform, 84f, 28f, w - 200f, 16f);
            TL(UiDraw.Text(card, "Title", L("practice.title", "Practice Relay (3 Min)"), UiFont.Heading, UiDp, OnSurface, -.01f).rectTransform, 84f, 46f, w - 120f, 28f);
            var tag = UiDraw.Text(card, "NoInternet", L("practice.noInternet", "No Internet"), UiFont.Mono, CaptionDp, Tertiary, .1f, TextAlignmentOptions.Midline);
            float tw = UiDraw.Width(tag) + 18f;
            var tagPlate = UiDraw.Box(card, "NoInternetPlate", High, 4f);
            tagPlate.transform.SetSiblingIndex(tag.transform.GetSiblingIndex());
            UiDraw.Place(tagPlate.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-24f, -32f), new Vector2(tw, 22f));
            UiDraw.Place(tag.rectTransform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-24f, -32f), new Vector2(tw, 22f));
            float iw = w - 48f, ih = 140f;
            var shot = UiDraw.Box(card, "Shot", Lowest, 8f);
            TL(shot.rectTransform, 24f, 88f, iw, ih);
            shot.gameObject.AddComponent<RectMask2D>();
            var photo = UiDraw.Node(shot.transform, "Photo").gameObject.AddComponent<RawImage>();
            UiDraw.Fill(photo.rectTransform);
            photo.texture = UiPresentationProfile.LoadRooftopArtwork();
            float aspect = iw / ih;
            photo.uvRect = new Rect(0f, .5f - .5f * (16f / 9f) / aspect, 1f, (16f / 9f) / aspect);
            photo.color = new Color(.8f, .82f, .85f, 1f);
            var fade = UiDraw.VerticalGradient(shot.transform, "Fade", new Color(0f, 0f, 0f, .85f), new Color(0f, 0f, 0f, .2f), new Color(0f, 0f, 0f, 0f));
            fade.raycastTarget = false;
            var caption = UiDraw.Text(shot.transform, "Caption", L("practice.zoneCaption", "Dynamic 3-Zone Control: Center  •  West  •  East"), UiFont.MonoMedium, CaptionDp, OnSurface, .03f);
            UiDraw.Place(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0f, .5f), new Vector2(16f, 20f), new Vector2(iw - 32f, 16f));
            var body = UiDraw.Text(card, "Description", L("practice.description", "3-minute full bot practice match with dynamic zone rotation, full AI teammates and local performance logging. No online rewards."), UiFont.Body, UiDp, OnSurface);
            body.textWrappingMode = TextWrappingModes.Normal; body.alignment = TextAlignmentOptions.TopLeft;
            float bh = body.GetPreferredValues(body.text, iw, 0f).y;
            TL(body.rectTransform, 24f, 88f + ih + 16f, iw, bh);
            float cy = 88f + ih + 16f + bh + 16f;
            float chipW = (iw - 12f) / 2f;
            Chip(card, "Opponents", "groups", L("practice.opponents", "3v3 Opponent Squad"), 24f, cy, chipW);
            Chip(card, "Clock", "timer", L("practice.clock", "180 Sec Relay"), 24f + chipW + 12f, cy, chipW);
            cy += 40f + 16f;
            var button = UiDraw.Tap(card, "StartPractice", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); practice?.Invoke(); });
            TL((RectTransform)button.transform, 24f, cy, iw, 60f);
            var label = UiDraw.Text(button.transform, "Label", L("practice.enterRelay", "Enter 3v3 Relay"), UiFont.HeadingSemiBold, UiDp, OnSurface, .01f);
            float lw = UiDraw.Width(label);
            UiDraw.Place(UiDraw.Icon(button.transform, "Icon", "sports_esports", 20f, OnSurface).rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-(lw + 10f) / 2f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(15f, 0f), new Vector2(lw + 2f, 28f));
            cy += 60f + 24f;
            card.sizeDelta = new Vector2(w, cy);
            return y + cy;
        }

        static void Chip(RectTransform card, string name, string icon, string text, float x, float y, float w)
        {
            var chip = UiDraw.Box(card, name, Mid, 8f);
            TL(chip.rectTransform, x, y, w, 40f);
            UiDraw.Place(UiDraw.Icon(chip.transform, "Icon", icon, 18f, Primary).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(12f, 0f), new Vector2(18f, 18f));
            UiDraw.Place(UiDraw.Text(chip.transform, "Text", text, UiFont.MonoMedium, CaptionDp, OnSurface, .1f).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(40f, 0f), new Vector2(w - 50f, 16f));
        }

        static void Footer(RectTransform root, int total, OfflineChallengeEntry selected, bool hasSnapshot, Action back, Action<string> start)
        {
            var bar = UiDraw.Box(root, "Footer", UIDesignTokens.WithAlpha(UIDesignTokens.Background, .95f)).rectTransform;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1f, 0f); bar.pivot = new Vector2(.5f, 0f);
            bar.sizeDelta = new Vector2(0f, FooterHeight); bar.anchoredPosition = Vector2.zero;
            UiDraw.HRule(bar, "Rule", UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceStructural, .5f), 0f);
            var backButton = UiDraw.Tap(bar, "Back", High, 8f, () => { GameAudio.Play2D(GameCue.UiTap); back?.Invoke(); });
            var backText = UiDraw.Text(backButton.transform, "Label", L("button.back", "Back"), UiFont.Heading, UiDp, OnSurface, .01f);
            float bw = 20f + 20f + 12f + UiDraw.Width(backText) + 24f;
            UiDraw.Place((RectTransform)backButton.transform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(bw, 52f));
            UiDraw.Place(UiDraw.Icon(backButton.transform, "Icon", "arrow_back", 20f, OnSurface).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(20f, 0f), new Vector2(20f, 20f));
            UiDraw.Place(backText.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(52f, 0f), new Vector2(UiDraw.Width(backText) + 2f, 28f));
            UiDraw.Place(UiDraw.Text(bar, "Status", F("footer.statusTemplate", "{0} CHALLENGES  •  PROGRESS SAVED ON THIS PHONE", total), UiFont.MonoMedium, CaptionDp, OnVariant, .1f).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(Pad + bw + 16f, 0f), new Vector2(480f, 16f));
            if (selected == null) return;
            string id = selected.id;
            var deploy = UiDraw.Tap(bar, "DeployChallenge", PrimaryC, 8f, () => { GameAudio.Play2D(GameCue.UiTap); start?.Invoke(id); });
            var dl = UiDraw.Text(deploy.transform, "Label", L("button.startChallenge", "Start Challenge"), UiFont.Heading, UiDp, Ink, .01f);
            float dw = 24f + 22f + 12f + UiDraw.Width(dl) + 28f;
            UiDraw.Place((RectTransform)deploy.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-Pad, 0f), new Vector2(dw, 52f));
            UiDraw.Place(UiDraw.Icon(deploy.transform, "Icon", "rocket_launch", 22f, Ink).rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, 0f), new Vector2(22f, 22f));
            UiDraw.Place(dl.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(58f, 0f), new Vector2(UiDraw.Width(dl) + 2f, 28f));
        }

        static RectTransform Card(RectTransform parent, string name, Color fill, float x, float y, float w, float h)
        {
            var box = UiDraw.Box(parent, name, fill, 12f);
            TL(box.rectTransform, x, y, w, h);
            return box.rectTransform;
        }

        static void TL(RectTransform rt, float x, float y, float w, float h) =>
            UiDraw.Place(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));

        static string ChapterName(ChallengeChapter chapter) => chapter == ChallengeChapter.FirstFlight
            ? L("chapter.firstFlight", "First Flight") : chapter == ChallengeChapter.RooftopOperator ? L("chapter.rooftopOperator", "ROOFTOP OPERATOR") : L("chapter.relayCaptain", "Relay Captain");
    }
}
