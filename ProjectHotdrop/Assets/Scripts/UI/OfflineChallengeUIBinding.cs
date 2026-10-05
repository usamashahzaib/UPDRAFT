using System;
using System.Collections.Generic;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Core;

namespace Hotdrop.UI
{
    /// <summary>
    /// UI binding interface for the Offline Travel Challenge system.
    /// Decouples the installed library/detail/tracker/results views from local progress.
    /// </summary>
    public interface IOfflineChallengeView
    {
        void ShowLibraryView(IReadOnlyList<OfflineChallengeEntry> entries, Func<string, ChallengeRecord> recordLookup);
        void ShowDetailView(OfflineChallengeEntry entry, ChallengeRecord record, bool hasResumeSnapshot);
        void UpdateGameplayHUDView(OfflineChallengeEntry entry, float elapsedTime, int targetsCompleted, int totalTargets);
        void ShowPauseMenuView(OfflineChallengeEntry entry, bool canResume);
        void ShowResultView(ChallengeResultData resultData);
    }

    /// <summary>
    /// Presenter bridging the OfflineChallengeController and any IOfflineChallengeView implementation.
    /// </summary>
    public class OfflineChallengePresenter : MonoBehaviour
    {
        [SerializeField] public OfflineChallengeController controller;
        IOfflineChallengeView activeView;

        public OfflineChallengeController Controller => controller;
        public IOfflineChallengeView ActiveView => activeView;

        public void BindView(IOfflineChallengeView view, OfflineChallengeController challengeController)
        {
            UnbindCurrentView();

            activeView = view;
            controller = challengeController;

            if (controller != null)
            {
                controller.OnStateChanged += HandleStateChanged;
                controller.OnGameplayUpdated += HandleGameplayUpdated;

                // Sync current state
                HandleStateChanged(controller.CurrentState);
            }
        }

        public void UnbindCurrentView()
        {
            if (controller != null)
            {
                controller.OnStateChanged -= HandleStateChanged;
                controller.OnDetailOpened -= HandleDetailOpened;
                controller.OnGameplayUpdated -= HandleGameplayUpdated;
                controller.OnResultCompleted -= HandleResultCompleted;
                controller.OnPauseStateChanged -= HandlePauseStateChanged;
            }
            activeView = null;
        }

        void OnDestroy()
        {
            UnbindCurrentView();
        }

        // View Actions Triggered by UI Buttons
        public void OnUserSelectChallenge(string challengeId)
        {
            controller?.SelectChallenge(challengeId);
        }

        public void OnUserStartChallenge(string challengeId)
        {
            controller?.StartChallenge(challengeId);
        }

        public void OnUserResumeChallenge()
        {
            controller?.ResumeFromSnapshot();
        }

        public void OnUserRestartChallenge()
        {
            controller?.CleanRestartChallenge();
        }

        public void OnUserPauseChallenge()
        {
            controller?.PauseChallenge();
        }

        public void OnUserUnpauseChallenge()
        {
            controller?.UnpauseChallenge();
        }

        public void OnUserRetryChallenge()
        {
            controller?.RetryCurrentChallenge();
        }

        public void OnUserNextChallenge()
        {
            controller?.NextChallenge();
        }

        public void OnUserReturnToLibrary()
        {
            controller?.OpenLibrary();
        }

        // Event Handlers
        void HandleStateChanged(ChallengeFlowState state)
        {
            if (activeView == null || controller == null) return;

            switch (state)
            {
                case ChallengeFlowState.Library:
                    activeView.ShowLibraryView(controller.config.entries, controller.RecordForCurrentContext);
                    break;
                case ChallengeFlowState.Detail:
                    bool hasSnapshot = controller.CanResumeSelected;
                    activeView.ShowDetailView(controller.SelectedEntry, controller.SelectedRecord, hasSnapshot);
                    break;
                case ChallengeFlowState.Gameplay:
                    if (controller.SelectedEntry != null)
                    {
                        activeView.UpdateGameplayHUDView(
                            controller.SelectedEntry,
                            controller.CurrentElapsedTime,
                            controller.CurrentTargetsCompleted,
                            controller.SelectedEntry.targetCount);
                    }
                    break;
                case ChallengeFlowState.Paused:
                    if (controller.SelectedEntry != null)
                    {
                        activeView.ShowPauseMenuView(controller.SelectedEntry, canResume: true);
                    }
                    break;
                case ChallengeFlowState.Result:
                    activeView.ShowResultView(controller.LastResult);
                    break;
            }
        }

        void HandleDetailOpened(OfflineChallengeEntry entry, ChallengeRecord record)
        {
            if (activeView == null || controller == null) return;
            bool hasSnapshot = controller.SaveSystem.GetSnapshot(entry?.id) != null;
            activeView.ShowDetailView(entry, record, hasSnapshot);
        }

        void HandleGameplayUpdated(OfflineChallengeEntry entry, float elapsedTime, int targetsCompleted, int totalTargets)
        {
            activeView?.UpdateGameplayHUDView(entry, elapsedTime, targetsCompleted, totalTargets);
        }

        void HandleResultCompleted(ChallengeResultData resultData)
        {
            activeView?.ShowResultView(resultData);
        }

        void HandlePauseStateChanged(bool isPaused)
        {
            if (isPaused && controller?.SelectedEntry != null)
            {
                activeView?.ShowPauseMenuView(controller.SelectedEntry, canResume: true);
            }
        }
    }
}
