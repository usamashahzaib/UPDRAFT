using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hotdrop.Config
{
    public enum ChallengeChapter
    {
        FirstFlight = 0,
        RooftopOperator = 1,
        RelayCaptain = 2
    }

    public enum ChallengeObjectiveType
    {
        BasicRoute,
        OwnPadRoute,
        EnemyPadRoute,
        SafeLanding,
        StationaryTargets,
        MovingTargets,
        ReloadDiscipline,
        LaunchAndHit,
        CaptureOneZone,
        RotateBetweenZones,
        DefendAgainstBots,
        CompleteOfflineRelay
    }

    public enum ChallengeMedal
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3
    }

    [Serializable]
    public struct ChallengeMedalThresholds
    {
        public bool thresholdsValidated; // If false, Silver/Gold thresholds are untuned/disabled
        public float silverTimeSeconds;  // Completion time <= silverTimeSeconds for Silver
        public float goldTimeSeconds;    // Completion time <= goldTimeSeconds for Gold
        public float silverScore;        // Score >= silverScore for Silver
        public float goldScore;          // Score >= goldScore for Gold
        public float silverAccuracy;     // Accuracy >= silverAccuracy for Silver
        public float goldAccuracy;       // Accuracy >= goldAccuracy for Gold
    }

    [Serializable]
    public class OfflineChallengeEntry
    {
        public string id;                        // Unique ID, e.g., "ch_basic_route"
        public string title;                     // Display title, e.g., "Basic Route"
        public string description;               // Detail description
        public ChallengeChapter chapter;         // FirstFlight, RooftopOperator, RelayCaptain
        public int indexInChapter;               // 0..3
        public ChallengeObjectiveType objectiveType;
        public int version = 1;

        [Header("Objective Requirements")]
        public float timeLimitSeconds;           // Max allowed time (0 = no limit)
        public int targetCount;                  // Number of targets / checkpoints / zones / bots
        public int defaultWeaponIndex = 0;       // Weapon index (0 = ARC, 1 = Pulser, 2 = Scatter)
        public int botCount = 0;                 // Number of enemy bots spawned
        public float requiredAccuracy = 0f;      // Required minimum accuracy percentage (0..100)

        [Header("Tuning & Medals")]
        public bool isTuned = false;             // Untuned entries marked for dev tuning only
        public ChallengeMedalThresholds medalThresholds;

        /// <summary>
        /// Evaluates medal based on completion time, accuracy, and score.
        /// Unvalidated/untuned thresholds safely return Bronze upon completion.
        /// Player builds must never display developer placeholders.
        /// </summary>
        public ChallengeMedal CalculateMedal(float completionTime, float accuracyPercent, float score)
        {
            // Base completion earns Bronze minimum
            if (!medalThresholds.thresholdsValidated || !isTuned)
            {
                return ChallengeMedal.Bronze;
            }

            // Check Gold conditions
            bool qualifiesGold = false;
            if (medalThresholds.goldTimeSeconds > 0f && completionTime <= medalThresholds.goldTimeSeconds)
            {
                qualifiesGold = true;
            }
            if (medalThresholds.goldScore > 0f && score >= medalThresholds.goldScore)
            {
                qualifiesGold = true;
            }

            if (qualifiesGold) return ChallengeMedal.Gold;

            // Check Silver conditions
            bool qualifiesSilver = false;
            if (medalThresholds.silverTimeSeconds > 0f && completionTime <= medalThresholds.silverTimeSeconds)
            {
                qualifiesSilver = true;
            }
            if (medalThresholds.silverScore > 0f && score >= medalThresholds.silverScore)
            {
                qualifiesSilver = true;
            }

            if (qualifiesSilver) return ChallengeMedal.Silver;

            return ChallengeMedal.Bronze;
        }
    }

    [CreateAssetMenu(fileName = "OfflineChallengeConfig", menuName = "Hotdrop/Config/Offline Challenge Config")]
    public class OfflineChallengeConfig : ScriptableObject
    {
        public int configVersion = 1;
        public List<OfflineChallengeEntry> entries = new List<OfflineChallengeEntry>();

        public OfflineChallengeEntry GetChallengeById(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            return entries.Find(e => e.id == id);
        }

        public List<OfflineChallengeEntry> GetChallengesForChapter(ChallengeChapter chapter)
        {
            if (entries == null) return new List<OfflineChallengeEntry>();
            return entries.FindAll(e => e.chapter == chapter);
        }

        /// <summary>
        /// Creates a default authored 12-entry config instance for runtime or test use.
        /// </summary>
        public static OfflineChallengeConfig CreateDefaultAuthoredConfig()
        {
            var config = CreateInstance<OfflineChallengeConfig>();
            config.configVersion = 1;
            config.entries = GetAuthoredEntries();
            return config;
        }

        public static List<OfflineChallengeEntry> GetAuthoredEntries()
        {
            return new List<OfflineChallengeEntry>
            {
                // Chapter 1: First Flight
                new OfflineChallengeEntry
                {
                    id = "ch_basic_route",
                    title = "Basic Route",
                    description = "Navigate through the industrial rooftop checkpoints using ground movement.",
                    chapter = ChallengeChapter.FirstFlight,
                    indexInChapter = 0,
                    objectiveType = ChallengeObjectiveType.BasicRoute,
                    timeLimitSeconds = 90f,
                    targetCount = 3,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_own_pad_route",
                    title = "Own-Pad Route",
                    description = "Deploy your Launch Pad and jump to reach the elevated relay platform.",
                    chapter = ChallengeChapter.FirstFlight,
                    indexInChapter = 1,
                    objectiveType = ChallengeObjectiveType.OwnPadRoute,
                    timeLimitSeconds = 90f,
                    targetCount = 1,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_enemy_pad_route",
                    title = "Enemy-Pad Route",
                    description = "Locate and use an opponent pad to gain altitude and reach the objective.",
                    chapter = ChallengeChapter.FirstFlight,
                    indexInChapter = 2,
                    objectiveType = ChallengeObjectiveType.EnemyPadRoute,
                    timeLimitSeconds = 90f,
                    targetCount = 1,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_safe_landing",
                    title = "Safe Landing",
                    description = "Launch off a pad and land accurately within the designated drop target.",
                    chapter = ChallengeChapter.FirstFlight,
                    indexInChapter = 3,
                    objectiveType = ChallengeObjectiveType.SafeLanding,
                    timeLimitSeconds = 90f,
                    targetCount = 1,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },

                // Chapter 2: Rooftop Operator
                new OfflineChallengeEntry
                {
                    id = "ch_stationary_targets",
                    title = "Stationary Targets",
                    description = "Eliminate 4 stationary target range dummies across the rooftop.",
                    chapter = ChallengeChapter.RooftopOperator,
                    indexInChapter = 0,
                    objectiveType = ChallengeObjectiveType.StationaryTargets,
                    timeLimitSeconds = 60f,
                    targetCount = 4,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_moving_targets",
                    title = "Moving Targets",
                    description = "Track and hit 3 moving targets patrolling the gantry.",
                    chapter = ChallengeChapter.RooftopOperator,
                    indexInChapter = 1,
                    objectiveType = ChallengeObjectiveType.MovingTargets,
                    timeLimitSeconds = 60f,
                    targetCount = 3,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_reload_discipline",
                    title = "Reload Discipline",
                    description = "Eliminate targets while maintaining clean magazine reload timing.",
                    chapter = ChallengeChapter.RooftopOperator,
                    indexInChapter = 2,
                    objectiveType = ChallengeObjectiveType.ReloadDiscipline,
                    timeLimitSeconds = 60f,
                    targetCount = 3,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_launch_and_hit",
                    title = "Launch and Hit",
                    description = "Launch from a pad and strike a target while airborne or immediately on landing.",
                    chapter = ChallengeChapter.RooftopOperator,
                    indexInChapter = 3,
                    objectiveType = ChallengeObjectiveType.LaunchAndHit,
                    timeLimitSeconds = 60f,
                    targetCount = 2,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },

                // Chapter 3: Relay Captain
                new OfflineChallengeEntry
                {
                    id = "ch_capture_one_zone",
                    title = "Capture One Zone",
                    description = "Occupy and secure the active relay zone for 15 seconds.",
                    chapter = ChallengeChapter.RelayCaptain,
                    indexInChapter = 0,
                    objectiveType = ChallengeObjectiveType.CaptureOneZone,
                    timeLimitSeconds = 90f,
                    targetCount = 1,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_rotate_between_zones",
                    title = "Rotate Between Zones",
                    description = "Follow active zone rotations from Center to West to East.",
                    chapter = ChallengeChapter.RelayCaptain,
                    indexInChapter = 1,
                    objectiveType = ChallengeObjectiveType.RotateBetweenZones,
                    timeLimitSeconds = 120f,
                    targetCount = 3,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_defend_against_bots",
                    title = "Defend Against Bots",
                    description = "Defend the active zone against 2 incoming enemy bots.",
                    chapter = ChallengeChapter.RelayCaptain,
                    indexInChapter = 2,
                    objectiveType = ChallengeObjectiveType.DefendAgainstBots,
                    timeLimitSeconds = 120f,
                    targetCount = 2,
                    botCount = 2,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                },
                new OfflineChallengeEntry
                {
                    id = "ch_complete_offline_relay",
                    title = "Complete Offline Relay",
                    description = "Complete a full 3-minute offline Relay match against bots with LOCAL RECORD tracking.",
                    chapter = ChallengeChapter.RelayCaptain,
                    indexInChapter = 3,
                    objectiveType = ChallengeObjectiveType.CompleteOfflineRelay,
                    timeLimitSeconds = 180f,
                    targetCount = 1,
                    botCount = 5,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                }
            };
        }
    }
}
