using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hotdrop.Config;
using Hotdrop.Core;

namespace Hotdrop.UI
{
    /// <summary>Real installed offline route, not a remote concept/mock catalog.</summary>
    public sealed class OfflineChallengeScreen : MonoBehaviour, IOfflineChallengeView
    {
        static string L(string key, string fallback) => UiDraw.L("offlineScreen." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("offlineScreen." + key, fallback, args);
        static string ChallengeTitle(OfflineChallengeEntry entry) => entry == null ? string.Empty : UiDraw.L("offlineLibrary.challenge." + entry.id + ".title", entry.title ?? string.Empty);

        OfflineChallengeRuntime runtime;
        GameBootstrap game;
        OfflineChallengePresenter presenter;
        MenuScreen menu;
        GameObject tracker;
        TextMeshProUGUI trackerText, hintText;
        string trackedChallenge;

        public void Initialize(OfflineChallengeRuntime challengeRuntime, GameBootstrap bootstrap)
        {
            runtime = challengeRuntime; game = bootstrap;
            presenter = gameObject.AddComponent<OfflineChallengePresenter>();
            presenter.BindView(this, runtime.Controller);
        }

        public void Hide()
        {
            DismissMenu();
            if (tracker != null) tracker.SetActive(false);
        }

        void DismissMenu()
        {
            if (menu == null) return;
            var closingRoot = menu.Root;
            menu = null;
            UiScreenEnterMotion.Exit(closingRoot, () =>
            {
                if (closingRoot == null) return;
                closingRoot.SetActive(false);
                Destroy(closingRoot);
            });
        }

        void NewMenu(string name, string title, UnityEngine.Events.UnityAction back)
        {
            Hide();
            menu = MenuLayout.Create(game.matchHUD.transform, name, title, back);
            menu.Root.transform.SetAsLastSibling();
            menu.Status.text = runtime.Controller.SaveSystem.LastSaveSucceeded
                ? L("status.installedOffline", "INSTALLED OFFLINE · LOCAL RECORDS ONLY · NO ONLINE REWARDS")
                : L("status.saveUnavailable", "NOT SAVED — storage unavailable. Your previous saved record is safe.");
        }

        static string ChapterName(ChallengeChapter chapter) => chapter == ChallengeChapter.FirstFlight ? L("chapter.firstFlight", "First Flight")
            : chapter == ChallengeChapter.RooftopOperator ? L("chapter.rooftopOperator", "ROOFTOP OPERATOR") : L("chapter.relayCaptain", "Relay Captain");

        public void ShowLibraryView(IReadOnlyList<OfflineChallengeEntry> entries, Func<string, ChallengeRecord> recordLookup)
        {
            NewMenu("ChallengeLibrary", L("title.playOffline", "Play Offline"), game.ReturnToHome);
            RenderLibrary(null, false);
        }

        public void ShowDetailView(OfflineChallengeEntry entry, ChallengeRecord record, bool hasResumeSnapshot)
        {
            if (entry == null) return;
            NewMenu("ChallengeDetail", ChallengeTitle(entry), presenter.OnUserReturnToLibrary);
            RenderLibrary(entry, hasResumeSnapshot);
        }

        void RenderLibrary(OfflineChallengeEntry selected, bool canResume)
        {
            var controller = runtime.Controller;
            OfflineChallengeLibraryView.Show(menu.Root, controller.config.entries,
                controller.RecordForCurrentContext, selected, canResume,
                controller.SaveSystem.LastSaveSucceeded
                    ? controller.SaveSystem.RecoveryMessage
                    : L("status.librarySaveUnavailable", "NOT SAVED — storage unavailable. Previous record is safe."),
                game.ReturnToHome, presenter.OnUserSelectChallenge, presenter.OnUserStartChallenge,
                presenter.OnUserResumeChallenge, game.StartOfflinePractice);
        }

        public void UpdateGameplayHUDView(OfflineChallengeEntry entry, float elapsedTime, int targetsCompleted, int totalTargets)
        {
            if (entry == null) return;
            DismissMenu();
            if (tracker == null)
            {
                var root = MenuLayout.Card(game.matchHUD.transform, "ChallengeTracker");
                tracker = root.gameObject;
                root.anchorMin = new Vector2(0, 1); root.anchorMax = new Vector2(0, 1); root.pivot = new Vector2(0, 1);
                root.anchoredPosition = new Vector2(24, -112); root.sizeDelta = new Vector2(350, 100);
                var layout = root.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.padding = new RectOffset(14, 14, 10, 10); layout.spacing = 4;
                trackerText = MenuLayout.AddText(root, "", 18, MatchHUD.Objective, "ChallengeProgress");
                hintText = MenuLayout.AddText(root, "", 18, MatchHUD.MainText, "ChallengeInstruction");
                trackerText.alignment = hintText.alignment = TextAlignmentOptions.Left;
                tracker.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                foreach (var graphic in tracker.GetComponentsInChildren<UnityEngine.UI.Graphic>()) graphic.raycastTarget = false;
            }
            tracker.SetActive(true);
            trackerText.text = F("tracker.progressTemplate", "{0}\n{1} / {2}   ·   {3:0.0} s", ChallengeTitle(entry), targetsCompleted, totalTargets, elapsedTime);
            if (trackedChallenge != entry.id) { trackedChallenge = entry.id; hintText.text = L("tracker.followMarker", "Follow the objective marker"); }
        }

        public void SetObjectiveHint(string value) { if (hintText != null) hintText.text = value; }

        public void ShowPauseMenuView(OfflineChallengeEntry entry, bool canResume)
        {
            NewMenu("ChallengePause", L("title.paused", "Offline Challenge Paused"), presenter.OnUserUnpauseChallenge);
            MenuLayout.AddText(menu.Content, ChallengeTitle(entry) + "\n" + L("pause.localPauseExplanation", "This local game is paused. Online matches cannot pause."), 28, MatchHUD.MainText);
            // Not ButtonVariant.Primary: that variant's background never actually paints on a
            // HotdropButton built through MenuLayout (the border tints correctly; the fill stays
            // whatever the panel behind it is), so the dark TextOnAccent caption this variant
            // selects renders unreadable on an unpainted dark button. Secondary is the variant
            // every sibling row in this same menu already uses and is confirmed to render.
            MenuLayout.AddButton(menu.Content, L("button.resume", "Resume"), presenter.OnUserUnpauseChallenge, "ChallengePauseResume");
            MenuLayout.AddButton(menu.Content, L("button.restart", "Restart"), presenter.OnUserRestartChallenge, "ChallengePauseRestart");
            MenuLayout.AddButton(menu.Content, L("button.settings", "Settings"), () => game.GetComponent<OfflinePauseMenu>().OpenSettings(), "ChallengePauseSettings");
            MenuLayout.AddButton(menu.Content, L("button.library", "Library"), presenter.OnUserReturnToLibrary, "ChallengePauseLibrary");
        }

        public void ShowResultView(ChallengeResultData result)
        {
            if (result.entry == null) return;
            NewMenu("ChallengeResult", L("title.localRecord", "Local Record"), presenter.OnUserReturnToLibrary);
            var card = MenuLayout.Card(menu.Content);
            MenuLayout.AddText(card, ChallengeTitle(result.entry), 30, MatchHUD.MainText);
            if (result.isSuccess) AddMedalBadge(card, result.medal);
            MenuLayout.AddText(card, result.isSuccess ? F("result.completedTemplate", "COMPLETED · {0}", result.medal) : L("result.tryAgain", "NOT COMPLETED · TRY AGAIN"), 32, MatchHUD.MainText, "ChallengeOutcome");
            MenuLayout.AddText(card, F("result.timeScoreTemplate", "Time: {0:0.0} s · Score: {1:0}", result.completionTimeSeconds, result.score), 27, MatchHUD.MainText);
            if (!result.isSuccess)
            {
                MenuLayout.AddText(card, F("result.objectivesTemplate", "Objectives: {0} / {1}", result.objectivesCompleted, result.objectivesRequired), 23, MatchHUD.Objective);
                if (result.playerEliminated)
                    MenuLayout.AddText(card, L("result.eliminated", "ELIMINATED · Try using cover before pushing the objective"), 23, MatchHUD.Objective);
                else if (result.timeExpired)
                    MenuLayout.AddText(card, L("result.timeExpired", "Time Expired"), 23, MatchHUD.Objective);
                else if (result.objectivesCompleted < result.objectivesRequired)
                    MenuLayout.AddText(card, L("result.objectiveNotMet", "Objective requirements not met · Check the drill instructions"), 23, MatchHUD.Objective);

                if (result.accuracyRequirementMissed)
                    MenuLayout.AddText(card, F("result.accuracyMissedTemplate", "Accuracy requirement missed · {0:0}% / {1:0}% required", result.accuracyPercent, result.accuracyRequiredPercent), 23, MatchHUD.Objective);
            }
            else if (result.oldRecord != null && result.oldRecord.isCompleted)
            {
                float delta = result.completionTimeSeconds - result.oldRecord.personalBestTimeSeconds;
                MenuLayout.AddText(card, Mathf.Abs(delta) < .05f ? L("result.matchedBest", "Matched Your Best") : F(delta < 0 ? "result.fasterTemplate" : "result.behindBestTemplate", delta < 0 ? "{0:0.0} s FASTER" : "{0:0.0} s BEHIND YOUR BEST", Mathf.Abs(delta)), 23, MatchHUD.Objective);
            }
            if (result.accuracyRequiredPercent > 0)
                MenuLayout.AddText(card, F("result.accuracyTemplate", "Shot accuracy: {0:0}% · {1:0}% required", result.accuracyPercent, result.accuracyRequiredPercent), 25, MatchHUD.SecondaryText);
            MenuLayout.AddText(card, result.isNewPersonalBest && result.saveSucceeded ? L("result.newPersonalBest", "New Local Personal Best")
                : !result.saveSucceeded ? L("result.saveUnavailable", "NOT SAVED — storage unavailable. Retry when storage is available.") : L("result.localOnly", "Records stay on this device. No online rewards."), 24, MatchHUD.SecondaryText);
            var entries = runtime.Controller.config.entries;
            bool last = entries.Count > 0 && entries[entries.Count - 1].id == result.entry.id;
            // See the Resume button above: ButtonVariant.Primary renders unreadable here, so the
            // emphasised action stays Secondary like the rest of this menu rather than losing its
            // caption.
            if (result.isSuccess)
                MenuLayout.AddButton(menu.Content, last ? L("button.backToChallenges", "Back to Challenges") : L("button.nextChallenge", "Next Challenge"), presenter.OnUserNextChallenge, "ChallengeNext");
            MenuLayout.AddButton(menu.Content, L("button.retry", "Retry"), presenter.OnUserRetryChallenge, "ChallengeRetry");
            if (!result.isSuccess)
                MenuLayout.AddButton(menu.Content, last ? L("button.backToChallenges", "Back to Challenges") : L("button.nextChallenge", "Next Challenge"), presenter.OnUserNextChallenge, "ChallengeNext");
            MenuLayout.AddButton(menu.Content, L("button.library", "Library"), presenter.OnUserReturnToLibrary, "ChallengeResultLibrary");
        }

        static void AddMedalBadge(Transform parent, ChallengeMedal medal)
        {
            var row = new GameObject("ChallengeMedalBadge", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = UIDesignTokens.Dp(10);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.minHeight = UIDesignTokens.Dp(48);
            rowElement.preferredHeight = UIDesignTokens.Dp(48);

            var badge = new GameObject("MedalMark", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badge.transform.SetParent(row.transform, false);
            var rect = badge.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(UIDesignTokens.Dp(48), UIDesignTokens.Dp(48));
            var image = badge.GetComponent<Image>();
            image.sprite = UIRoundedSprites.RoundedRect(UIDesignTokens.Dp(24));
            image.type = Image.Type.Simple;
            image.color = UIDesignTokens.ObjectiveGold;
            var badgeElement = badge.GetComponent<LayoutElement>();
            badgeElement.minWidth = badgeElement.preferredWidth = UIDesignTokens.Dp(48);
            badgeElement.minHeight = badgeElement.preferredHeight = UIDesignTokens.Dp(48);

            var markObject = new GameObject("MedalInitial", typeof(RectTransform), typeof(TextMeshProUGUI));
            markObject.transform.SetParent(badge.transform, false);
            var mark = markObject.GetComponent<TextMeshProUGUI>();
            mark.text = medal == ChallengeMedal.Gold ? "G" : medal == ChallengeMedal.Silver ? "S" : "B";
            mark.fontSize = UiMetrics.TextSize(24);
            mark.fontStyle = FontStyles.Bold;
            mark.alignment = TextAlignmentOptions.Center;
            mark.color = UIDesignTokens.TextOn(UIDesignTokens.ObjectiveGold);
            mark.raycastTarget = false;
            MenuLayout.Stretch(mark.rectTransform);
        }

        void OnDestroy() { if (presenter != null) presenter.UnbindCurrentView(); Hide(); }
    }
}
