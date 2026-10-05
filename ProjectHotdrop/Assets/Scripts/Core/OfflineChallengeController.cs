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
        public bool saveSucceeded;
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
        bool ownsConfig;

        // Gameplay Active State
        bool isGameplayActive;
        float currentElapsedTime;
        int currentTargetsCompleted;
        int currentShotsFired;
        int currentShotsHit;
        float currentScore;
        int lastHitShot;
        string attemptPreset;
        public string ControlPreset => PlayerPrefs.GetInt("LeftHandedMode", 0) == 1 ? "Classic Left" : "Classic";
        public string Difficulty => selectedEntry?.difficulty ?? "Standard";
        public Func<OfflineChallengeEntry, bool> BeginWorld;
        public Func<LocalChallengeSaveSnapshot, bool> RestoreWorld;
        public Func<LocalChallengeSaveSnapshot, bool> CanRestoreWorld;
        public Func<string> CaptureWorld;
        public ChallengeResultData LastResult { get; private set; }

        // Public Properties
        public ChallengeFlowState CurrentState => currentState;
        public OfflineChallengeEntry SelectedEntry => selectedEntry;
        public ChallengeRecord SelectedRecord => selectedRecord;
        public LocalChallengeSave SaveSystem => saveSystem;
        public float CurrentElapsedTime => currentElapsedTime;
        public int CurrentTargetsCompleted => currentTargetsCompleted;
        public bool IsAttemptActive => isGameplayActive;
        public bool CanResumeSelected
        {
            get
            {
                var s = saveSystem?.GetSnapshot(selectedEntry?.id);
                return s != null && s.challengeVersion == selectedEntry.version &&
                    s.controlPreset == ControlPreset && s.difficulty == Difficulty &&
                    s.targetsCompleted < selectedEntry.targetCount && (CanRestoreWorld == null || CanRestoreWorld(s));
            }
        }

        public ChallengeRecord RecordForCurrentContext(string id)
        {
            var entry = config?.GetChallengeById(id);
            return entry == null ? null : saveSystem.GetRecord(id, entry.version, ControlPreset, entry.difficulty);
        }

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
                    ownsConfig = true;
                }
            }

            saveSystem = new LocalChallengeSave(customSavePath);
            SetState(ChallengeFlowState.Library);
        }

        public void OpenLibrary()
        {
            isGameplayActive = false;
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
                selectedEntry = null;
                selectedRecord = null;
                isGameplayActive = false;
                SetState(ChallengeFlowState.Library);
                Debug.LogWarning($"[OfflineChallengeController] Unknown challenge ID: {challengeId}");
                return;
            }

            selectedEntry = entry;
            isGameplayActive = false;
            selectedRecord = RecordForCurrentContext(challengeId);
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
            lastHitShot = 0;
            currentScore = 0f;
            attemptPreset = ControlPreset;

            if (BeginWorld != null && !BeginWorld(selectedEntry)) return false;
            isGameplayActive = true;

            saveSystem.ClearSnapshot();
            SetState(ChallengeFlowState.Gameplay);
            SaveCurrentSnapshot();
            Telemetry.Track("offline_challenge_started", ("challenge", selectedEntry.id), ("chapter", selectedEntry.chapter.ToString()));

            return true;
        }

        public bool ResumeFromSnapshot()
        {
            if (selectedEntry == null) return false;

            var snapshot = saveSystem.GetSnapshot(selectedEntry.id);
            if (snapshot == null || !CanResumeSelected)
            {
                Debug.LogWarning($"[OfflineChallengeController] No valid snapshot for {selectedEntry.id}; forcing clean restart.");
                return CleanRestartChallenge();
            }

            try
            {
                if (RestoreWorld != null && !RestoreWorld(snapshot)) return false;
                currentElapsedTime = snapshot.elapsedTime;
                currentTargetsCompleted = snapshot.targetsCompleted;
                currentScore = snapshot.score;
                currentShotsFired = snapshot.shotsFired;
                currentShotsHit = snapshot.shotsHit;
                lastHitShot = currentShotsFired;
                attemptPreset = snapshot.controlPreset;
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

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0) return;
            if (attemptPreset != ControlPreset) attemptPreset = "Mixed controls";
            currentElapsedTime += deltaTime;

            if (shotFired) currentShotsFired++;
            // Scatter can confirm several pellets from one shot; accuracy counts shots, not pellets.
            if (shotHit && currentShotsFired > lastHitShot)
            { currentShotsHit++; lastHitShot = currentShotsFired; }

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

            if (count <= 0) return;
            count = Mathf.Min(count, selectedEntry.targetCount - currentTargetsCompleted);

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

            float accuracy = currentShotsFired > 0 ? ((float)currentShotsHit / currentShotsFired) * 100f : 0f;
            isSuccess &= selectedEntry.requiredAccuracy <= 0 || accuracy >= selectedEntry.requiredAccuracy;
            ChallengeMedal medal = isSuccess ? selectedEntry.CalculateMedal(currentElapsedTime, accuracy, currentScore) : ChallengeMedal.None;

            var oldRecord = saveSystem.GetRecord(selectedEntry.id, selectedEntry.version, attemptPreset, Difficulty);
            bool isNewPersonalBest = false;

            ChallengeRecord newRecord = new ChallengeRecord
            {
                challengeId = selectedEntry.id,
                isCompleted = isSuccess || (oldRecord != null && oldRecord.isCompleted),
                medal = (oldRecord != null && oldRecord.medal > medal) ? oldRecord.medal : medal,
                personalBestTimeSeconds = isSuccess && (oldRecord == null || !oldRecord.isCompleted || currentElapsedTime < oldRecord.personalBestTimeSeconds)
                    ? currentElapsedTime : oldRecord?.personalBestTimeSeconds ?? 0,
                personalBestScore = isSuccess && (oldRecord == null || currentScore > oldRecord.personalBestScore)
                    ? currentScore : oldRecord?.personalBestScore ?? 0,
                challengeVersion = selectedEntry.version,
                controlPreset = attemptPreset,
                difficulty = Difficulty
            };

            if (isSuccess && (oldRecord == null || !oldRecord.isCompleted || newRecord.personalBestTimeSeconds < oldRecord.personalBestTimeSeconds || newRecord.medal > oldRecord.medal || newRecord.personalBestScore > oldRecord.personalBestScore))
            {
                isNewPersonalBest = true;
            }

            // Save record atomically
            if (isSuccess) saveSystem.SaveRecord(newRecord);
            bool recordSaved = saveSystem.LastSaveSucceeded;
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
                saveSucceeded = recordSaved && saveSystem.LastSaveSucceeded,
                resultLabel = "LOCAL RECORD" // Requirement 6: Label results LOCAL RECORD
            };
            LastResult = result;
            // Amendment 4C: challenge attempts/completions are client-owned UI facts; the
            // local save remains the only record of offline progress (never an online grant).
            Telemetry.Track("offline_challenge_completed", ("challenge", selectedEntry.id), ("success", isSuccess),
                ("medal", medal.ToString()), ("seconds", currentElapsedTime), ("accuracy", accuracy));

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
                challengeVersion = selectedEntry.version,
                controlPreset = attemptPreset,
                difficulty = Difficulty,
                elapsedTime = currentElapsedTime,
                targetsCompleted = currentTargetsCompleted,
                score = currentScore,
                stepIndex = currentTargetsCompleted,
                shotsFired = currentShotsFired,
                shotsHit = currentShotsHit,
                customStateJson = CaptureWorld?.Invoke()
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

        void OnApplicationQuit() => SaveCurrentSnapshot();

        void OnDestroy()
        {
            if (!ownsConfig || config == null) return;
            if (Application.isPlaying) Destroy(config); else DestroyImmediate(config);
        }

        void SetState(ChallengeFlowState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }
    }
}
