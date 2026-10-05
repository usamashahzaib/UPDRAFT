using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Core;

namespace Hotdrop.UI
{
    /// <summary>Real installed offline route, not a remote concept/mock catalog.</summary>
    public sealed class OfflineChallengeScreen : MonoBehaviour, IOfflineChallengeView
    {
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
            if (menu != null) { menu.Root.SetActive(false); Destroy(menu.Root); menu = null; }
            if (tracker != null) tracker.SetActive(false);
        }

        void NewMenu(string name, string title, UnityEngine.Events.UnityAction back)
        {
            Hide();
            menu = MenuLayout.Create(game.matchHUD.transform, name, title, back);
            menu.Root.transform.SetAsLastSibling();
            menu.Status.text = runtime.Controller.SaveSystem.LastSaveSucceeded
                ? "INSTALLED OFFLINE · LOCAL RECORDS ONLY · NO ONLINE REWARDS"
                : "NOT SAVED — storage unavailable. Your previous saved record is safe.";
        }

        static string ChapterName(ChallengeChapter chapter) => chapter == ChallengeChapter.FirstFlight ? "FIRST FLIGHT"
            : chapter == ChallengeChapter.RooftopOperator ? "ROOFTOP OPERATOR" : "RELAY CAPTAIN";

        public void ShowLibraryView(IReadOnlyList<OfflineChallengeEntry> entries, Func<string, ChallengeRecord> recordLookup)
        {
            NewMenu("ChallengeLibrary", "PLAY OFFLINE", game.ReturnToHome);
            RenderLibrary(null, false);
        }

        public void ShowDetailView(OfflineChallengeEntry entry, ChallengeRecord record, bool hasResumeSnapshot)
        {
            if (entry == null) return;
            NewMenu("ChallengeDetail", entry.title.ToUpperInvariant(), presenter.OnUserReturnToLibrary);
            RenderLibrary(entry, hasResumeSnapshot);
        }

        void RenderLibrary(OfflineChallengeEntry selected, bool canResume)
        {
            var controller = runtime.Controller;
            OfflineChallengeLibraryView.Show(menu.Root, controller.config.entries,
                controller.RecordForCurrentContext, selected, canResume,
                controller.SaveSystem.LastSaveSucceeded
                    ? controller.SaveSystem.RecoveryMessage
                    : "NOT SAVED — storage unavailable. Previous record is safe.",
                game.ReturnToHome, presenter.OnUserSelectChallenge, presenter.OnUserStartChallenge,
                presenter.OnUserResumeChallenge, game.StartOfflinePractice);
        }

        public void UpdateGameplayHUDView(OfflineChallengeEntry entry, float elapsedTime, int targetsCompleted, int totalTargets)
        {
            if (entry == null) return;
            if (menu != null) { menu.Root.SetActive(false); Destroy(menu.Root); menu = null; }
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
            trackerText.text = $"{entry.title.ToUpperInvariant()}\n{targetsCompleted} / {totalTargets}   ·   {elapsedTime:0.0} s";
            if (trackedChallenge != entry.id) { trackedChallenge = entry.id; hintText.text = "Follow the objective marker"; }
        }

        public void SetObjectiveHint(string value) { if (hintText != null) hintText.text = value; }

        public void ShowPauseMenuView(OfflineChallengeEntry entry, bool canResume)
        {
            NewMenu("ChallengePause", "OFFLINE CHALLENGE PAUSED", presenter.OnUserUnpauseChallenge);
            MenuLayout.AddText(menu.Content, entry.title + "\nThis local game is paused. Online matches cannot pause.", 28, MatchHUD.MainText);
            MenuLayout.AddButton(menu.Content, "RESUME", presenter.OnUserUnpauseChallenge, "ChallengePauseResume", variant: ButtonVariant.Primary);
            MenuLayout.AddButton(menu.Content, "RESTART", presenter.OnUserRestartChallenge, "ChallengePauseRestart");
            MenuLayout.AddButton(menu.Content, "SETTINGS", () => game.GetComponent<OfflinePauseMenu>().OpenSettings(), "ChallengePauseSettings");
            MenuLayout.AddButton(menu.Content, "LIBRARY", presenter.OnUserReturnToLibrary, "ChallengePauseLibrary");
        }

        public void ShowResultView(ChallengeResultData result)
        {
            if (result.entry == null) return;
            NewMenu("ChallengeResult", "LOCAL RECORD", presenter.OnUserReturnToLibrary);
            var card = MenuLayout.Card(menu.Content);
            MenuLayout.AddText(card, result.entry.title.ToUpperInvariant(), 30, MatchHUD.MainText);
            MenuLayout.AddText(card, result.isSuccess ? $"COMPLETED · {result.medal}" : "NOT COMPLETED · TRY AGAIN", 32, MatchHUD.MainText, "ChallengeOutcome");
            MenuLayout.AddText(card, $"Time: {result.completionTimeSeconds:0.0} s · Score: {result.score:0}", 27, MatchHUD.MainText);
            if (!result.isSuccess)
                MenuLayout.AddText(card, runtime.Controller.CurrentElapsedTime >= result.entry.timeLimitSeconds && result.entry.timeLimitSeconds > 0
                    ? "TIME UP · " + runtime.Controller.CurrentTargetsCompleted + " / " + result.entry.targetCount + " objectives"
                    : runtime.Player != null && !runtime.Player.IsAlive ? "ELIMINATED · Try using cover before pushing the objective"
                    : "Objective requirements not met · Check the drill instructions", 23, MatchHUD.Objective);
            else if (result.oldRecord != null && result.oldRecord.isCompleted)
            {
                float delta = result.completionTimeSeconds - result.oldRecord.personalBestTimeSeconds;
                MenuLayout.AddText(card, Mathf.Abs(delta) < .05f ? "MATCHED YOUR BEST" : $"{Mathf.Abs(delta):0.0} s {(delta < 0 ? "FASTER" : "BEHIND YOUR BEST")}", 23, MatchHUD.Objective);
            }
            if (result.entry.chapter == ChallengeChapter.RooftopOperator)
                MenuLayout.AddText(card, $"Shot accuracy: {result.accuracyPercent:0}%", 25, MatchHUD.SecondaryText);
            MenuLayout.AddText(card, result.isNewPersonalBest && result.saveSucceeded ? "NEW LOCAL PERSONAL BEST"
                : !result.saveSucceeded ? "NOT SAVED — storage unavailable. Retry when storage is available." : "Records stay on this device. No online rewards.", 24, MatchHUD.SecondaryText);
            var entries = runtime.Controller.config.entries;
            bool last = entries.Count > 0 && entries[entries.Count - 1].id == result.entry.id;
            if (result.isSuccess)
                MenuLayout.AddButton(menu.Content, last ? "BACK TO CHALLENGES" : "NEXT CHALLENGE", presenter.OnUserNextChallenge, "ChallengeNext", variant: ButtonVariant.Primary);
            MenuLayout.AddButton(menu.Content, "RETRY", presenter.OnUserRetryChallenge, "ChallengeRetry", variant: result.isSuccess ? ButtonVariant.Secondary : ButtonVariant.Primary);
            if (!result.isSuccess)
                MenuLayout.AddButton(menu.Content, last ? "BACK TO CHALLENGES" : "NEXT CHALLENGE", presenter.OnUserNextChallenge, "ChallengeNext");
            MenuLayout.AddButton(menu.Content, "LIBRARY", presenter.OnUserReturnToLibrary, "ChallengeResultLibrary");
        }

        void OnDestroy() { if (presenter != null) presenter.UnbindCurrentView(); Hide(); }
    }
}
