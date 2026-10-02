using System;
using System.Collections.Generic;
using UnityEngine;
using Hotdrop.Config;

namespace Hotdrop.Core
{
    public enum ChallengeFlowState
    {
        Library,
        Detail,
        Gameplay,
        Paused,
        Result
    }

    public struct ChallengeResultData
    {
        public OfflineChallengeEntry entry;
        public ChallengeRecord oldRecord;
        public ChallengeRecord newRecord;
        public bool isSuccess;
        public float completionTimeSeconds;
        public float score;
        public float accuracyPercent;
        public ChallengeMedal medal;
        public bool isNewPersonalBest;
        public string resultLabel; // Always "LOCAL RECORD" per Spec
    }

    /// <summary>
    /// Requirement 4 & 6: Manages Challenge Library, Detail view, gameplay objective tracking,
    /// interruption resume/restart snapshot, pause, result processing (labeled LOCAL RECORD),
    /// Retry, Next Challenge, and Return to Library.
    /// </summary>
    public class OfflineChallengeController : MonoBehaviour
    {
        [Header("Configuration & Save")]
        [SerializeField] public OfflineChallengeConfig config;
        [SerializeField] public string customSavePath;

        // Runtime Controllers & Data
        LocalChallengeSave saveSystem;
        ChallengeFlowState currentState = ChallengeFlowState.Library;
        OfflineChallengeEntry selectedEntry;
        ChallengeRecord selectedRecord;

        // Gameplay Active State
        bool isGameplayActive;
        float currentElapsedTime;
        int currentTargetsCompleted;
        int currentShotsFired;
        int currentShotsHit;
        float currentScore;

        // Public Properties
        public ChallengeFlowState CurrentState => currentState;
        public OfflineChallengeEntry SelectedEntry => selectedEntry;
        public ChallengeRecord SelectedRecord => selectedRecord;
        public LocalChallengeSave SaveSystem => saveSystem;
        public float CurrentElapsedTime => currentElapsedTime;
        public int CurrentTargetsCompleted => currentTargetsCompleted;

        // Events
        public event Action<ChallengeFlowState> OnStateChanged;
        public event Action<OfflineChallengeEntry, ChallengeRecord> OnDetailOpened;
        public event Action<OfflineChallengeEntry, float, int, int> OnGameplayUpdated;
        public event Action<ChallengeResultData> OnResultCompleted;
        public event Action<bool> OnPauseStateChanged;

        void Awake()
        {
            InitializeController();
        }

        public void InitializeController()
        {
            if (config == null)
            {
                config = Resources.Load<OfflineChallengeConfig>("Config/OfflineChallengeConfig");
                if (config == null)
                {
                    config = OfflineChallengeConfig.CreateDefaultAuthoredConfig();
                }
            }

            saveSystem = new LocalChallengeSave(customSavePath);
            SetState(ChallengeFlowState.Library);
        }

        public void OpenLibrary()
        {
            selectedEntry = null;
            selectedRecord = null;
            SetState(ChallengeFlowState.Library);
        }

        public void SelectChallenge(string challengeId)
        {
            if (config == null) return;
            var entry = config.GetChallengeById(challengeId);
            if (entry == null)
            {
                Debug.LogWarning($"[OfflineChallengeController] Unknown challenge ID: {challengeId}");
                return;
            }

            selectedEntry = entry;
            selectedRecord = saveSystem.GetRecord(challengeId);
            SetState(ChallengeFlowState.Detail);
            OnDetailOpened?.Invoke(selectedEntry, selectedRecord);
        }

        public bool StartChallenge(string challengeId)
        {
            SelectChallenge(challengeId);
            if (selectedEntry == null) return false;

            currentElapsedTime = 0f;
            currentTargetsCompleted = 0;
            currentShotsFired = 0;
            currentShotsHit = 0;
            currentScore = 0f;
            isGameplayActive = true;

            saveSystem.ClearSnapshot();
            SetState(ChallengeFlowState.Gameplay);
            SaveCurrentSnapshot();

            return true;
        }

        public bool ResumeFromSnapshot()
        {
            if (selectedEntry == null) return false;

            var snapshot = saveSystem.GetSnapshot(selectedEntry.id);
            if (snapshot == null)
            {
                Debug.LogWarning($"[OfflineChallengeController] No valid snapshot for {selectedEntry.id}; forcing clean restart.");
                return CleanRestartChallenge();
            }

            try
            {
                currentElapsedTime = snapshot.elapsedTime;
                currentTargetsCompleted = snapshot.targetsCompleted;
                currentScore = snapshot.score;
                isGameplayActive = true;
                SetState(ChallengeFlowState.Gameplay);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OfflineChallengeController] Snapshot recovery failed ({ex.Message}); clean restart.");
                return CleanRestartChallenge();
            }
        }

        public bool CleanRestartChallenge()
        {
            if (selectedEntry == null) return false;
            saveSystem.ClearSnapshot();
            return StartChallenge(selectedEntry.id);
        }

        public void UpdateGameplay(float deltaTime, bool shotFired = false, bool shotHit = false)
        {
            if (currentState != ChallengeFlowState.Gameplay || !isGameplayActive) return;

            currentElapsedTime += deltaTime;

            if (shotFired) currentShotsFired++;
            if (shotHit) currentShotsHit++;

            // Time limit check
            if (selectedEntry.timeLimitSeconds > 0f && currentElapsedTime >= selectedEntry.timeLimitSeconds)
            {
                CompleteChallenge(isSuccess: currentTargetsCompleted >= selectedEntry.targetCount);
                return;
            }

            OnGameplayUpdated?.Invoke(selectedEntry, currentElapsedTime, currentTargetsCompleted, selectedEntry.targetCount);
        }

        public void RegisterTargetCompleted(int count = 1)
        {
            if (currentState != ChallengeFlowState.Gameplay || !isGameplayActive) return;

            currentTargetsCompleted += count;
            currentScore += 100f * count;

            // Requirement 8: Save snapshot safely on step completion
            SaveCurrentSnapshot();

            OnGameplayUpdated?.Invoke(selectedEntry, currentElapsedTime, currentTargetsCompleted, selectedEntry.targetCount);

            if (currentTargetsCompleted >= selectedEntry.targetCount)
            {
                CompleteChallenge(isSuccess: true);
            }
        }

        public void PauseChallenge()
        {
            if (currentState != ChallengeFlowState.Gameplay) return;

            // Requirement 8: Save snapshot on pause
            SaveCurrentSnapshot();
            SetState(ChallengeFlowState.Paused);
            OnPauseStateChanged?.Invoke(true);
        }

        public void UnpauseChallenge()
        {
            if (currentState != ChallengeFlowState.Paused) return;

            SetState(ChallengeFlowState.Gameplay);
            OnPauseStateChanged?.Invoke(false);
        }

        public void CompleteChallenge(bool isSuccess)
        {
            if (!isGameplayActive) return;
            isGameplayActive = false;

            float accuracy = currentShotsFired > 0 ? ((float)currentShotsHit / currentShotsFired) * 100f : 100f;
            ChallengeMedal medal = isSuccess ? selectedEntry.CalculateMedal(currentElapsedTime, accuracy, currentScore) : ChallengeMedal.None;

            var oldRecord = saveSystem.GetRecord(selectedEntry.id);
            bool isNewPersonalBest = false;

            ChallengeRecord newRecord = new ChallengeRecord
            {
                challengeId = selectedEntry.id,
                isCompleted = isSuccess || (oldRecord != null && oldRecord.isCompleted),
                medal = (oldRecord != null && oldRecord.medal > medal) ? oldRecord.medal : medal,
                personalBestTimeSeconds = (oldRecord == null || oldRecord.personalBestTimeSeconds <= 0f || currentElapsedTime < oldRecord.personalBestTimeSeconds)
                    ? currentElapsedTime : oldRecord.personalBestTimeSeconds,
                personalBestScore = (oldRecord == null || currentScore > oldRecord.personalBestScore)
                    ? currentScore : oldRecord.personalBestScore,
                challengeVersion = selectedEntry.version,
                controlPreset = "Classic",
                difficulty = "Standard"
            };

            if (oldRecord == null || newRecord.personalBestTimeSeconds < oldRecord.personalBestTimeSeconds || newRecord.medal > oldRecord.medal)
            {
                isNewPersonalBest = true;
            }

            // Save record atomically
            saveSystem.SaveRecord(newRecord);
            saveSystem.ClearSnapshot();

            selectedRecord = newRecord;

            ChallengeResultData result = new ChallengeResultData
            {
                entry = selectedEntry,
                oldRecord = oldRecord,
                newRecord = newRecord,
                isSuccess = isSuccess,
                completionTimeSeconds = currentElapsedTime,
                score = currentScore,
                accuracyPercent = accuracy,
                medal = medal,
                isNewPersonalBest = isNewPersonalBest,
                resultLabel = "LOCAL RECORD" // Requirement 6: Label results LOCAL RECORD
            };

            SetState(ChallengeFlowState.Result);
            OnResultCompleted?.Invoke(result);
        }

        public void RetryCurrentChallenge()
        {
            if (selectedEntry == null) return;
            StartChallenge(selectedEntry.id);
        }

        public void NextChallenge()
        {
            if (selectedEntry == null || config == null) return;

            var chapterEntries = config.GetChallengesForChapter(selectedEntry.chapter);
            int idx = chapterEntries.FindIndex(e => e.id == selectedEntry.id);
            if (idx >= 0 && idx + 1 < chapterEntries.Count)
            {
                StartChallenge(chapterEntries[idx + 1].id);
            }
            else
            {
                // Advance to next chapter if available
                int nextChapterInt = (int)selectedEntry.chapter + 1;
                if (Enum.IsDefined(typeof(ChallengeChapter), nextChapterInt))
                {
                    var nextChapterEntries = config.GetChallengesForChapter((ChallengeChapter)nextChapterInt);
                    if (nextChapterEntries.Count > 0)
                    {
                        StartChallenge(nextChapterEntries[0].id);
                        return;
                    }
                }
                OpenLibrary();
            }
        }

        public void SaveCurrentSnapshot()
        {
            if (selectedEntry == null || !isGameplayActive) return;

            LocalChallengeSaveSnapshot snapshot = new LocalChallengeSaveSnapshot
            {
                challengeId = selectedEntry.id,
                elapsedTime = currentElapsedTime,
                targetsCompleted = currentTargetsCompleted,
                score = currentScore,
                stepIndex = currentTargetsCompleted
            };

            saveSystem.SaveSnapshot(snapshot);
        }

        void OnApplicationPause(bool pauseStatus)
        {
            // Requirement 8: Save safely on background / interruption
            if (pauseStatus && isGameplayActive)
            {
                PauseChallenge();
            }
        }

        void OnApplicationFocus(bool focusStatus)
        {
            if (!focusStatus && isGameplayActive)
            {
                PauseChallenge();
            }
        }

        void SetState(ChallengeFlowState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }
    }
}
