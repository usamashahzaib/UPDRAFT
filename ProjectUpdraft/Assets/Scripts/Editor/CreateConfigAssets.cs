using UnityEngine;
using UnityEditor;
using Updraft.Config;

namespace Updraft.Editor
{
    /// <summary>
    /// Editor script to create all config ScriptableObject assets with spec-correct default values.
    /// Run via Tools/Updraft/Create Config Assets
    /// </summary>
    public class CreateConfigAssets
    {
        const string ConfigPath = "Assets/Resources/Config";

        [MenuItem("Tools/Updraft/Create Config Assets")]
        public static void CreateAllConfigs()
        {
            CreateFolderStructure();
            CreateMovementConfig();
            CreateWeaponDefinitions();
            CreatePadConfig();
            CreateMatchRulesConfig();
            CreateMapConfig();
            CreateBotConfig();
            CreateMatchmakingConfig();
            CreateProgressionConfig();
            CreateGraphicsConfig();
            CreateCatalogConfig();
            CreateAdConfig();
            CreateKpiGateConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("All Updraft config assets created at " + ConfigPath);
        }

        static void CreateFolderStructure()
        {
            string[] folders = {
                "Assets/Resources",
                ConfigPath,
                "Assets/Resources/Prefabs",
                "Assets/Resources/UI",
                "Assets/Resources/Audio",
                "Assets/Resources/VFX"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = System.IO.Path.GetDirectoryName(folder);
                    string name = System.IO.Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        static void CreateMovementConfig()
        {
            var config = ScriptableObject.CreateInstance<MovementConfig>();
            config.maxHealth = 100f;
            config.groundSpeed = 6f;
            config.speedWhileFiring = 5.1f;
            config.groundAcceleration = 35f;
            config.gravity = 20f;
            config.jumpUpwardVelocity = 6f;
            config.capsuleHeight = 1.8f;
            config.capsuleRadius = 0.35f;
            config.stepHeight = 0.35f;
            config.respawnDelaySeconds = 4f;
            config.recoveryDelayAfterDamage = 5f;
            config.recoveryPerSecond = 25f;
            config.respawnProtectionSeconds = 1.5f;

            AssetDatabase.CreateAsset(config, ConfigPath + "/MovementConfig.asset");
        }

        static void CreateWeaponDefinitions()
        {
            // ARC Rifle
            var arc = ScriptableObject.CreateInstance<WeaponDefinition>();
            arc.id = WeaponId.Arc;
            arc.displayName = "ARC Rifle";
            arc.roleDescription = "Mid-range precision. Steady damage, low spread, moderate fire rate.";
            arc.bodyDamage = 18f;
            arc.pelletsPerShot = 1;
            arc.fullDamageDistance = 20f;
            arc.falloffEndpointDistance = 45f;
            arc.damageAtFalloffEndpoint = 10f;
            arc.maxRange = 55f;
            arc.headMultiplier = 1.25f;
            arc.roundsPerMinute = 480f;
            arc.magazineSize = 24;
            arc.reloadSeconds = 1.6f;
            arc.spreadStationaryGrounded = 0.45f;
            arc.spreadMovingGrounded = 0.75f;
            arc.spreadAirborne = 1.2f;
            arc.verticalRecoilDegreesPerShot = 0.55f;
            AssetDatabase.CreateAsset(arc, ConfigPath + "/Weapon_ARC.asset");

            // ZIP SMG
            var zip = ScriptableObject.CreateInstance<WeaponDefinition>();
            zip.id = WeaponId.Zip;
            zip.displayName = "ZIP SMG";
            zip.roleDescription = "Close-range volume. High fire rate, wider spread, fast reload.";
            zip.bodyDamage = 13f;
            zip.pelletsPerShot = 1;
            zip.fullDamageDistance = 10f;
            zip.falloffEndpointDistance = 28f;
            zip.damageAtFalloffEndpoint = 6f;
            zip.maxRange = 35f;
            zip.headMultiplier = 1.25f;
            zip.roundsPerMinute = 780f;
            zip.magazineSize = 30;
            zip.reloadSeconds = 1.5f;
            zip.spreadStationaryGrounded = 1.0f;
            zip.spreadMovingGrounded = 1.3f;
            zip.spreadAirborne = 2.0f;
            zip.verticalRecoilDegreesPerShot = 0.30f;
            AssetDatabase.CreateAsset(zip, ConfigPath + "/Weapon_ZIP.asset");

            // BRICK Scattergun
            var brick = ScriptableObject.CreateInstance<WeaponDefinition>();
            brick.id = WeaponId.Brick;
            brick.displayName = "BRICK Scattergun";
            brick.roleDescription = "Point-blank burst. 8 pellets, extreme close range, slow fire rate.";
            brick.bodyDamage = 9f;
            brick.pelletsPerShot = 8;
            brick.fullDamageDistance = 7f;
            brick.falloffEndpointDistance = 20f;
            brick.damageAtFalloffEndpoint = 3f;
            brick.maxRange = 25f;
            brick.headMultiplier = 1.25f;
            brick.roundsPerMinute = 75f;
            brick.magazineSize = 6;
            brick.reloadSeconds = 2.0f;
            brick.spreadStationaryGrounded = 4.0f;
            brick.spreadMovingGrounded = 4.0f;
            brick.spreadAirborne = 4.5f;
            brick.verticalRecoilDegreesPerShot = 2.0f;
            AssetDatabase.CreateAsset(brick, ConfigPath + "/Weapon_BRICK.asset");
        }

        static void CreatePadConfig()
        {
            var config = ScriptableObject.CreateInstance<PadConfig>();
            config.chargeCapacity = 1;
            config.rechargeSeconds = 16f;
            config.lifetimeSeconds = 18f;
            config.armingSeconds = 0.75f;
            config.health = 60f;
            config.diameter = 1.4f;
            config.launchHorizontalVelocity = 14f;
            config.launchUpwardVelocity = 12f;
            config.maxActivePadsPerOwner = 1;
            config.maxPlacementsPerMatch = 6;
            config.perPlayerActivationLockoutSeconds = 1f;
            config.placementForwardOffset = 1.6f;
            config.previewHoldThresholdSeconds = 0.18f;
            config.maxSurfaceTiltDegrees = 5f;
            config.noPlacementNearSpawnExitRadius = 5f;
            config.noPlacementInsideCaptureRadius = 6f;
            config.airSteerDegreesPerSecond = 30f;

            AssetDatabase.CreateAsset(config, ConfigPath + "/PadConfig.asset");
        }

        static void CreateMatchRulesConfig()
        {
            var config = ScriptableObject.CreateInstance<MatchRulesConfig>();
            config.regulationDurationSeconds = 180f;
            config.maxOvertimeSeconds = 30f;
            config.overtimeHoldSeconds = 3f;
            config.scoreToWin = 120;
            config.zoneCaptureRadius = 6f;
            config.zoneMaxFeetHeight = 1.5f;
            config.preparationSeconds = 8f;
            config.countdownSeconds = 3f;
            config.loadTimeoutSeconds = 30f;
            config.respawnDelaySeconds = 4f;
            config.botSubstitutionDelaySeconds = 5f;

            config.schedule = new ZoneScheduleEntry[]
            {
                new ZoneScheduleEntry { startTime = 0f,   endTime = 30f,  zone = ZoneId.Center },
                new ZoneScheduleEntry { startTime = 30f,  endTime = 60f,  zone = ZoneId.West },
                new ZoneScheduleEntry { startTime = 60f,  endTime = 90f,  zone = ZoneId.East },
                new ZoneScheduleEntry { startTime = 90f,  endTime = 120f, zone = ZoneId.Center },
                new ZoneScheduleEntry { startTime = 120f, endTime = 150f, zone = ZoneId.East },
                new ZoneScheduleEntry { startTime = 150f, endTime = 180f, zone = ZoneId.West },
            };
            config.nextZoneWarningSeconds = 10f;

            AssetDatabase.CreateAsset(config, ConfigPath + "/MatchRulesConfig.asset");
        }

        static void CreateMapConfig()
        {
            var config = ScriptableObject.CreateInstance<MapConfig>();
            config.mapName = "Switchyard";
            config.boundsMin = new Vector2(-48f, -40f);
            config.boundsMax = new Vector2(48f, 40f);
            config.groundLevelY = 0f;
            config.centerZone = new Vector3(0f, 0f, 0f);
            config.westZone = new Vector3(-22f, 0f, 0f);
            config.eastZone = new Vector3(22f, 0f, 0f);
            config.zoneRadius = 6f;
            config.northSpawnCenter = new Vector3(0f, 0f, 35f);
            config.spawnRoomSize = new Vector2(20f, 8f);
            config.southSpawnCenter = new Vector3(0f, 0f, -35f);
            config.exitsPerSpawn = 3;
            config.platformHeight = 3f;
            config.platformSize = new Vector2(10f, 8f);
            config.platformCenters = new Vector3[]
            {
                new Vector3(20f, 3f, 14f),
                new Vector3(-20f, 3f, 14f),
                new Vector3(20f, 3f, -14f),
                new Vector3(-20f, 3f, -14f),
            };
            config.minimumMainRouteWidth = 4f;
            config.sightlineBlockerZ = 20f;

            AssetDatabase.CreateAsset(config, ConfigPath + "/MapConfig.asset");
        }

        static void CreateBotConfig()
        {
            var config = ScriptableObject.CreateInstance<BotConfig>();
            config.easyPracticeReactionSeconds = 0.6f;
            config.standardReactionSeconds = 0.35f;
            config.retreatHealthFraction = 0.3f;
            config.focusFireDesyncChance = 0.35f;
            config.useLineOfSightOnly = true;

            AssetDatabase.CreateAsset(config, ConfigPath + "/BotConfig.asset");
        }

        static void CreateMatchmakingConfig()
        {
            var config = ScriptableObject.CreateInstance<MatchmakingConfig>();
            config.maxPartySize = 3;
            config.inviteCodeLength = 8;
            config.startingRating = 1000;
            config.eloK = 24;
            config.evaluationIntervalSeconds = 2f;
            config.initialRatingWindow = 150;
            config.widenAfterSeconds = 20f;
            config.widenStepEverySeconds = 10;
            config.widenStepAmount = 50;
            config.maxRatingWindow = 350;
            config.preferredMaxLatencyMs = 120;
            config.showStillSearchingAfterSeconds = 60f;
            config.botFillQueueThresholdSeconds = 45f;

            AssetDatabase.CreateAsset(config, ConfigPath + "/MatchmakingConfig.asset");
        }

        static void CreateProgressionConfig()
        {
            var config = ScriptableObject.CreateInstance<ProgressionConfig>();
            config.xpPerEligibleMatch = 120;
            config.xpWinBonus = 30;
            config.xpDrawBonus = 15;
            config.xpPerLevel = 400;
            config.maxLevel = 20;
            config.minPresenceFraction = 0.75f;
            config.afkWarningSeconds = 30f;
            config.afkRemovalAfterWarningSeconds = 15f;
            config.creditsPerEligibleMatch = 20;
            config.creditsWinBonus = 5;
            config.creditsPerLevelUp = 20;
            config.priceBodyColorStyle = 300;
            config.priceWeaponWrap = 200;
            config.priceProfileBadge = 100;
            config.appearanceUnlockLevels = new int[] { 3, 6, 10, 15, 20 };
            config.appearanceUnlockAfterMatches = 3;

            AssetDatabase.CreateAsset(config, ConfigPath + "/ProgressionConfig.asset");
        }

        static void CreateGraphicsConfig()
        {
            var config = ScriptableObject.CreateInstance<GraphicsConfig>();
            config.lowPresetTargetFps = 30;
            config.mediumHighPresetTargetFps = 60;
            config.lowPresetMaxDrawCalls = 150;
            config.lowPresetMaxVisibleTriangles = 250000;
            config.targetDownloadMb = 250;
            config.targetInstalledMb = 600;
            config.targetClientMemoryMbLow = 750;
            config.targetReturningStartupSeconds = 8f;
            config.targetArenaLoadSeconds = 10f;
            config.targetKbPerSecondPerClient = 100f;
            config.dynamicResolution = true;
            config.bakedLightingOnLow = true;
            config.realtimeShadowsOnLow = false;
            config.bloomOnLow = false;
            config.motionBlurOnLow = false;
            config.depthOfFieldOnLow = false;
            config.ssaoOnLow = false;

            AssetDatabase.CreateAsset(config, ConfigPath + "/GraphicsConfig.asset");
        }

        static void CreateCatalogConfig()
        {
            var config = ScriptableObject.CreateInstance<CatalogConfig>();
            config.shopEnabled = false; // Gated behind Milestone D

            config.bundles = new CosmeticBundle[]
            {
                new CosmeticBundle
                {
                    id = "signal",
                    displayName = "Signal",
                    referenceUsdPrice = 1.99f,
                    contents = new[] { "Body style: Signal", "Weapon wrap: Signal (ARC/ZIP/BRICK)" }
                },
                new CosmeticBundle
                {
                    id = "foundry",
                    displayName = "Foundry",
                    referenceUsdPrice = 3.99f,
                    contents = new[] { "Body style: Foundry", "Profile badge: Foundry" }
                },
                new CosmeticBundle
                {
                    id = "skyline",
                    displayName = "Skyline",
                    referenceUsdPrice = 5.99f,
                    contents = new[] { "Body style: Skyline", "Profile badge: Skyline" }
                },
            };

            AssetDatabase.CreateAsset(config, ConfigPath + "/CatalogConfig.asset");
        }

        static void CreateAdConfig()
        {
            var config = ScriptableObject.CreateInstance<AdConfig>();
            config.rewardedAdsEnabled = false; // Gated behind Milestone D
            config.extraCrateRollMaxPerDay = 1;
            config.creditBonusAmount = 20;
            config.creditBonusMaxPerDay = 3;
            config.cosmeticTrialHours = 24f;
            config.cosmeticTrialMaxPerDay = 1;

            AssetDatabase.CreateAsset(config, ConfigPath + "/AdConfig.asset");
        }

        static void CreateKpiGateConfig()
        {
            var config = ScriptableObject.CreateInstance<KpiGateConfig>();
            config.minTutorialCompletionRate = 0.80f;
            config.minD1Retention = 0.35f;
            config.minD7Retention = 0.15f;
            config.minD30Retention = 0.05f;
            config.minCrashFreeSessionRate = 0.995f;
            config.minVoluntaryInstantRematchRate = 0.50f;
            config.maxMedianPublicQueueSeconds = 45f;
            config.failedPassesBeforePivotDecision = 2;

            AssetDatabase.CreateAsset(config, ConfigPath + "/KpiGateConfig.asset");
        }
    }
}
