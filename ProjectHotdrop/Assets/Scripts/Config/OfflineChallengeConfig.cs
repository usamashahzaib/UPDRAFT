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
        public int version = 2;

        [Header("Objective Requirements")]
        public float timeLimitSeconds;           // Max allowed time (0 = no limit)
        public int targetCount;                  // Number of targets / checkpoints / zones / bots
        public int defaultWeaponIndex = 0;       // Weapon index (0 = ARC, 1 = Pulser, 2 = Scatter)
        public int botCount = 0;                 // Number of enemy bots spawned
        public float requiredAccuracy = 0f;      // Required minimum accuracy percentage (0..100)
        public string difficulty = "Standard";
        public Vector3 startPosition = new Vector3(0, .05f, 14);
        public float startYaw = 180;
        public Vector3[] routePoints = new[] { new Vector3(12, .05f, 20), new Vector3(-10, .05f, 5), Vector3.zero };
        public Vector3 padPosition = new Vector3(-10, .05f, 5);
        public Vector3 landingPosition = new Vector3(-20, 3, 14);
        public float checkpointRadius = 2f;
        public float landingRadius = 4f;
        public float holdSeconds = 15f;
        public Vector3[] targetPositions = new[]
        {
            new Vector3(-6, 1, 8), new Vector3(-2, 1, 8), new Vector3(2, 1, 8), new Vector3(6, 1, 8)
        };
        public float movingTargetDistance = 2f;
        public float movingTargetSpeed = 1.5f;

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

            if (Qualifies(medalThresholds.goldTimeSeconds, medalThresholds.goldScore,
                medalThresholds.goldAccuracy, completionTime, score, accuracyPercent)) return ChallengeMedal.Gold;
            if (Qualifies(medalThresholds.silverTimeSeconds, medalThresholds.silverScore,
                medalThresholds.silverAccuracy, completionTime, score, accuracyPercent)) return ChallengeMedal.Silver;

            return ChallengeMedal.Bronze;
        }

        static bool Qualifies(float time, float score, float accuracy, float elapsed, float actualScore, float actualAccuracy)
        {
            return (time > 0 || score > 0 || accuracy > 0) &&
                (time <= 0 || elapsed <= time) && (score <= 0 || actualScore >= score) &&
                (accuracy <= 0 || actualAccuracy >= accuracy);
        }
    }

    [CreateAssetMenu(fileName = "OfflineChallengeConfig", menuName = "Hotdrop/Config/Offline Challenge Config")]
    public class OfflineChallengeConfig : ScriptableObject
    {
        public int configVersion = 2;
        public List<OfflineChallengeEntry> entries = new List<OfflineChallengeEntry>();

        public OfflineChallengeEntry GetChallengeById(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            return entries.Find(e => e.id == id);
        }

        public List<OfflineChallengeEntry> GetChallengesForChapter(ChallengeChapter chapter)
        {
            if (entries == null) return new List<OfflineChallengeEntry>();
            var result = entries.FindAll(e => e.chapter == chapter);
            result.Sort((a, b) => a.indexInChapter.CompareTo(b.indexInChapter));
            return result;
        }

        /// <summary>
        /// Creates a default authored 12-entry config instance for runtime or test use.
        /// </summary>
        public static OfflineChallengeConfig CreateDefaultAuthoredConfig()
        {
            var config = CreateInstance<OfflineChallengeConfig>();
            config.configVersion = 2;
            config.entries = GetAuthoredEntries();
            return config;
        }

        public static List<OfflineChallengeEntry> GetAuthoredEntries()
        {
            var entries = new List<OfflineChallengeEntry>
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
                    landingRadius = 2f,
                    // Authored touchdown area on the near half of the north-west deck;
                    // not the point reached after sliding along the deck after first contact.
                    landingPosition = new Vector3(-18, 3, 12),
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
                    description = "Eliminate 3 targets and complete a magazine reload during the drill.",
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
                    description = "Launch from a pad and eliminate 2 targets while airborne. Grounded hits do not count.",
                    chapter = ChallengeChapter.RooftopOperator,
                    indexInChapter = 3,
                    objectiveType = ChallengeObjectiveType.LaunchAndHit,
                    targetPositions = new[] { new Vector3(-24, 3, 18), new Vector3(-16, 3, 18) },
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
                    difficulty = "Easy",
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
                    // Real Relay decides completion, including preparation, countdown and overtime.
                    timeLimitSeconds = 0f,
                    targetCount = 1,
                    botCount = 5,
                    defaultWeaponIndex = 0,
                    isTuned = true,
                    medalThresholds = new ChallengeMedalThresholds { thresholdsValidated = false }
                }
            };
            ApplyAuthoredMedalThresholds(entries);
            return entries;
        }

        /// <summary>
        /// Amendment 4C: each challenge has an authored Silver/Gold path so a completed
        /// challenge still gives the player something to chase. These are initial authored
        /// values (route-feasible, not arbitrarily large) and are versioned with the entry;
        /// tune them from real completion distributions before treating them as final.
        /// Completion always earns Bronze; a player must beat the same challenge version,
        /// control preset and difficulty to improve a medal.
        /// </summary>
        static void ApplyAuthoredMedalThresholds(List<OfflineChallengeEntry> entries)
        {
            foreach (var e in entries)
            {
                // time-based movement drills
                e.medalThresholds = e.id switch
                {
                    "ch_basic_route" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 45f, goldTimeSeconds = 30f },
                    "ch_own_pad_route" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 30f, goldTimeSeconds = 18f },
                    "ch_enemy_pad_route" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 30f, goldTimeSeconds = 18f },
                    "ch_safe_landing" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 28f, goldTimeSeconds = 16f },
                    // accuracy/time gunnery drills
                    "ch_stationary_targets" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 35f, goldTimeSeconds = 22f, silverAccuracy = 55f, goldAccuracy = 80f },
                    "ch_moving_targets" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 40f, goldTimeSeconds = 25f, silverAccuracy = 45f, goldAccuracy = 70f },
                    "ch_reload_discipline" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 40f, goldTimeSeconds = 26f, silverAccuracy = 50f, goldAccuracy = 75f },
                    "ch_launch_and_hit" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 45f, goldTimeSeconds = 28f, silverAccuracy = 40f, goldAccuracy = 65f },
                    // objective drills
                    "ch_capture_one_zone" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 40f, goldTimeSeconds = 24f },
                    "ch_rotate_between_zones" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 75f, goldTimeSeconds = 50f },
                    "ch_defend_against_bots" => new ChallengeMedalThresholds { thresholdsValidated = true, silverTimeSeconds = 70f, goldTimeSeconds = 45f, silverAccuracy = 40f, goldAccuracy = 65f },
                    "ch_complete_offline_relay" => new ChallengeMedalThresholds { thresholdsValidated = true, silverScore = 60f, goldScore = 90f },
                    _ => e.medalThresholds,
                };
                e.isTuned = true;
            }
        }
    }
}
