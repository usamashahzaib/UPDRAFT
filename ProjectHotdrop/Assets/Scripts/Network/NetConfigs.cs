using UnityEngine;
using Hotdrop.Config;
using System;

namespace Hotdrop.Network
{
    /// <summary>The same authoritative config assets the offline game uses (spec section 32).</summary>
    public static class NetConfigs
    {
        static MovementConfig movement;
        static PadConfig pad;
        static MapConfig map;
        static MatchRulesConfig rules;
        static ProgressionConfig progression;
        static WeaponDefinition[] weapons;

        public static MovementConfig Movement => movement ??= LoadMovementConfig();
        public static PadConfig Pad => pad ??= LoadPadConfig();
        public static MapConfig Map => map ??= LoadMapConfig();
        public static MatchRulesConfig Rules => rules ??= LoadMatchRulesConfig();
        // Spec 18: AFK windows live in config, never hardcoded on the server.
        public static ProgressionConfig Progression => progression ??= LoadProgressionConfig();

        public static WeaponDefinition[] Weapons => weapons ??= LoadWeapons();

        static WeaponDefinition[] LoadWeapons()
        {
            var configs = new[]
            {
                Resources.Load<WeaponDefinition>("Config/Weapon_ARC"),
                Resources.Load<WeaponDefinition>("Config/Weapon_ZIP"),
                Resources.Load<WeaponDefinition>("Config/Weapon_BRICK"),
            };
            var expectedIds = new[] { WeaponId.Arc, WeaponId.Zip, WeaponId.Brick };
            for (int i = 0; i < configs.Length; i++)
            {
                var weapon = configs[i];
                if (weapon == null) throw new InvalidOperationException($"Required weapon config at index {i} is missing.");
                if (weapon.id != expectedIds[i])
                    throw new InvalidOperationException($"Weapon config at index {i} must identify as {expectedIds[i]}.");
                ValidateWeaponConfig(weapon, i);
            }
            return configs;
        }

        static void ValidateWeaponConfig(WeaponDefinition weapon, int index)
        {
            string prefix = $"Weapon config {index} ({weapon.id})";
            ValidateNonNegativeFinite(prefix + ".bodyDamage", weapon.bodyDamage);
            ValidateNonNegativeFinite(prefix + ".damageAtFalloffEndpoint", weapon.damageAtFalloffEndpoint);
            ValidateNonNegativeFinite(prefix + ".headMultiplier", weapon.headMultiplier);
            ValidateNonNegativeFinite(prefix + ".fullDamageDistance", weapon.fullDamageDistance);
            ValidateNonNegativeFinite(prefix + ".falloffEndpointDistance", weapon.falloffEndpointDistance);
            ValidatePositiveFinite(prefix + ".maxRange", weapon.maxRange);
            if (weapon.fullDamageDistance >= weapon.falloffEndpointDistance || weapon.falloffEndpointDistance > weapon.maxRange)
                throw new InvalidOperationException($"{prefix} damage distances must satisfy fullDamageDistance < falloffEndpointDistance <= maxRange.");
            if (weapon.pelletsPerShot <= 0 || weapon.magazineSize <= 0)
                throw new InvalidOperationException($"{prefix} pelletsPerShot and magazineSize must be positive integers.");
            ValidatePositiveFinite(prefix + ".roundsPerMinute", weapon.roundsPerMinute);
            ValidatePositiveFinite(prefix + ".secondsPerShot", weapon.SecondsPerShot);
            ValidateNonNegativeFinite(prefix + ".reloadSeconds", weapon.reloadSeconds);
            ValidateNonNegativeFinite(prefix + ".spreadStationaryGrounded", weapon.spreadStationaryGrounded);
            ValidateNonNegativeFinite(prefix + ".spreadMovingGrounded", weapon.spreadMovingGrounded);
            ValidateNonNegativeFinite(prefix + ".spreadAirborne", weapon.spreadAirborne);
            ValidateNonNegativeFinite(prefix + ".verticalRecoilDegreesPerShot", weapon.verticalRecoilDegreesPerShot);

            if (weapon.recoilPattern == null) return;
            var keys = weapon.recoilPattern.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                ValidateNonNegativeFinite(prefix + $".recoilPattern[{i}].value", keys[i].value);
                if (!IsFinite(keys[i].time))
                    throw new InvalidOperationException($"{prefix}.recoilPattern[{i}].time must be finite.");
            }
        }

        static PadConfig LoadPadConfig()
        {
            var config = Resources.Load<PadConfig>("Config/PadConfig");
            if (config == null) throw new InvalidOperationException("Required PadConfig asset is missing.");

            if (config.chargeCapacity <= 0 || config.maxActivePadsPerOwner <= 0 ||
                config.maxActivePadsPerMatch <= 0 || config.maxPlacementsPerPlayerPerMatch <= 0)
                throw new InvalidOperationException("PadConfig charge capacity and placement caps must be positive integers.");
            ValidatePositiveFinite("PadConfig.lifetimeSeconds", config.lifetimeSeconds);
            ValidateNonNegativeFinite("PadConfig.armingSeconds", config.armingSeconds);
            ValidatePositiveFinite("PadConfig.health", config.health);
            ValidatePositiveFinite("PadConfig.diameter", config.diameter);
            ValidateNonNegativeFinite("PadConfig.launchHorizontalVelocity", config.launchHorizontalVelocity);
            ValidatePositiveFinite("PadConfig.launchUpwardVelocity", config.launchUpwardVelocity);
            ValidateCooldown("rechargeSeconds", config.rechargeSeconds);
            ValidateCooldown("launchWeaponLockoutSeconds", config.launchWeaponLockoutSeconds);
            ValidateCooldown("perPlayerActivationLockoutSeconds", config.perPlayerActivationLockoutSeconds);
            ValidateNonNegativeFinite("PadConfig.placementForwardOffset", config.placementForwardOffset);
            ValidateNonNegativeFinite("PadConfig.previewHoldThresholdSeconds", config.previewHoldThresholdSeconds);
            ValidateNonNegativeFinite("PadConfig.maxSurfaceTiltDegrees", config.maxSurfaceTiltDegrees);
            ValidateNonNegativeFinite("PadConfig.noPlacementNearSpawnExitRadius", config.noPlacementNearSpawnExitRadius);
            ValidateNonNegativeFinite("PadConfig.noPlacementInsideCaptureRadius", config.noPlacementInsideCaptureRadius);
            ValidateNonNegativeFinite("PadConfig.airSteerDegreesPerSecond", config.airSteerDegreesPerSecond);
            return config;
        }

        static MapConfig LoadMapConfig()
        {
            var config = Resources.Load<MapConfig>("Config/MapConfig");
            if (config == null) throw new InvalidOperationException("Required MapConfig asset is missing.");
            if (string.IsNullOrWhiteSpace(config.mapName))
                throw new InvalidOperationException("MapConfig.mapName is required.");

            ValidateFinite("MapConfig.boundsMin", config.boundsMin);
            ValidateFinite("MapConfig.boundsMax", config.boundsMax);
            if (config.boundsMin.x >= config.boundsMax.x || config.boundsMin.y >= config.boundsMax.y)
                throw new InvalidOperationException("MapConfig bounds must be finite and have positive width and depth.");
            ValidateFinite("MapConfig.groundLevelY", config.groundLevelY);
            ValidateFinite("MapConfig.centerZone", config.centerZone);
            ValidateFinite("MapConfig.westZone", config.westZone);
            ValidateFinite("MapConfig.eastZone", config.eastZone);
            ValidatePositiveFinite("MapConfig.zoneRadius", config.zoneRadius);
            ValidateFinite("MapConfig.northSpawnCenter", config.northSpawnCenter);
            ValidateFinite("MapConfig.southSpawnCenter", config.southSpawnCenter);
            if (!IsFinite(config.spawnRoomSize) || config.spawnRoomSize.x <= 0f || config.spawnRoomSize.y <= 0f)
                throw new InvalidOperationException("MapConfig.spawnRoomSize must have finite, positive dimensions.");
            if (config.exitsPerSpawn <= 0)
                throw new InvalidOperationException("MapConfig.exitsPerSpawn must be positive.");
            ValidatePositiveFinite("MapConfig.platformHeight", config.platformHeight);
            if (!IsFinite(config.platformSize) || config.platformSize.x <= 0f || config.platformSize.y <= 0f)
                throw new InvalidOperationException("MapConfig.platformSize must have finite, positive dimensions.");
            if (config.platformCenters == null)
                throw new InvalidOperationException("MapConfig.platformCenters cannot be missing.");
            for (int i = 0; i < config.platformCenters.Length; i++)
                ValidateFinite($"MapConfig.platformCenters[{i}]", config.platformCenters[i]);
            ValidatePositiveFinite("MapConfig.rampRunMeters", config.rampRunMeters);
            ValidatePositiveFinite("MapConfig.rampWidthMeters", config.rampWidthMeters);
            string boundsError = config.ValidateBounds();
            if (boundsError != null)
                throw new InvalidOperationException("MapConfig bounds: " + boundsError);
            string objectiveZoneError = config.ValidateObjectiveZones();
            if (objectiveZoneError != null)
                throw new InvalidOperationException("MapConfig objective zones: " + objectiveZoneError);
            float spawnWallMargin = string.Equals(config.mapName, MapConfig.SwitchyardName, StringComparison.Ordinal)
                ? MapConfig.SpawnRoomWallThicknessMeters
                : 0f;
            string spawnGeometryError = config.ValidateSpawnGeometry(spawnWallMargin);
            if (spawnGeometryError != null)
                throw new InvalidOperationException("MapConfig spawn geometry: " + spawnGeometryError);
            string spawnLayoutError = config.ValidateSwitchyardSpawnLayout();
            if (spawnLayoutError != null)
                throw new InvalidOperationException("MapConfig Switchyard spawn layout: " + spawnLayoutError);
            string spawnCapsuleError = config.ValidateSwitchyardSpawnCapsuleClearance(Movement);
            if (spawnCapsuleError != null)
                throw new InvalidOperationException("MapConfig Switchyard spawn capsule clearance: " + spawnCapsuleError);
            string roofGeometryError = config.ValidateSwitchyardRoofGeometry(Movement);
            if (roofGeometryError != null)
                throw new InvalidOperationException("MapConfig Switchyard roof geometry: " + roofGeometryError);
            string sightlineBlockerError = config.ValidateSwitchyardSightlineBlockers();
            if (sightlineBlockerError != null)
                throw new InvalidOperationException("MapConfig Switchyard sightline blockers: " + sightlineBlockerError);
            string spawnRoomClearanceError = config.ValidateSwitchyardSpawnRoomClearance();
            if (spawnRoomClearanceError != null)
                throw new InvalidOperationException("MapConfig Switchyard spawn-room clearance: " + spawnRoomClearanceError);
            ValidatePositiveFinite("MapConfig.minimumMainRouteWidth", config.minimumMainRouteWidth);
            ValidateFinite("MapConfig.sightlineBlockerZ", config.sightlineBlockerZ);
            return config;
        }

        static ProgressionConfig LoadProgressionConfig()
        {
            var config = Resources.Load<ProgressionConfig>("Config/ProgressionConfig");
            if (config == null) throw new InvalidOperationException("Required ProgressionConfig asset is missing.");

            ValidateNonNegativeFinite("ProgressionConfig.afkWarningSeconds", config.afkWarningSeconds);
            ValidateNonNegativeFinite("ProgressionConfig.afkRemovalAfterWarningSeconds", config.afkRemovalAfterWarningSeconds);
            ValidateNonNegativeFinite("ProgressionConfig.disconnectBotSubstitutionSeconds", config.disconnectBotSubstitutionSeconds);
            if (!IsFinite(config.afkWarningSeconds + config.afkRemovalAfterWarningSeconds))
                throw new InvalidOperationException("ProgressionConfig AFK warning and removal durations must have a finite sum.");
            return config;
        }

        static MovementConfig LoadMovementConfig()
        {
            var config = Resources.Load<MovementConfig>("Config/MovementConfig");
            if (config == null) throw new InvalidOperationException("Required MovementConfig asset is missing.");

            ValidatePositiveFinite("MovementConfig.maxHealth", config.maxHealth);
            ValidateNonNegativeFinite("MovementConfig.groundSpeed", config.groundSpeed);
            ValidateNonNegativeFinite("MovementConfig.speedWhileFiring", config.speedWhileFiring);
            ValidateNonNegativeFinite("MovementConfig.groundAcceleration", config.groundAcceleration);
            ValidateNonNegativeFinite("MovementConfig.gravity", config.gravity);
            ValidateNonNegativeFinite("MovementConfig.weaponMovingGroundSpeedThreshold", config.weaponMovingGroundSpeedThreshold);
            ValidateNonNegativeFinite("MovementConfig.jumpUpwardVelocity", config.jumpUpwardVelocity);
            ValidatePositiveFinite("MovementConfig.capsuleHeight", config.capsuleHeight);
            ValidatePositiveFinite("MovementConfig.capsuleRadius", config.capsuleRadius);
            ValidateNonNegativeFinite("MovementConfig.stepHeight", config.stepHeight);
            ValidateNonNegativeFinite("MovementConfig.respawnDelaySeconds", config.respawnDelaySeconds);
            ValidateNonNegativeFinite("MovementConfig.recoveryDelayAfterDamage", config.recoveryDelayAfterDamage);
            ValidateNonNegativeFinite("MovementConfig.recoveryPerSecond", config.recoveryPerSecond);
            ValidateNonNegativeFinite("MovementConfig.respawnProtectionSeconds", config.respawnProtectionSeconds);
            return config;
        }

        static MatchRulesConfig LoadMatchRulesConfig()
        {
            var config = Resources.Load<MatchRulesConfig>("Config/MatchRulesConfig");
            if (config == null) throw new InvalidOperationException("Required MatchRulesConfig asset is missing.");

            ValidatePositiveFinite("regulationDurationSeconds", config.regulationDurationSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.maxOvertimeSeconds", config.maxOvertimeSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.overtimeHoldSeconds", config.overtimeHoldSeconds);
            ValidatePositiveFinite("MatchRulesConfig.scoreToWin", config.scoreToWin);
            ValidatePositiveFinite("MatchRulesConfig.zoneCaptureRadius", config.zoneCaptureRadius);
            ValidateNonNegativeFinite("MatchRulesConfig.zoneMaxFeetHeight", config.zoneMaxFeetHeight);
            ValidateNonNegativeFinite("MatchRulesConfig.nextZoneWarningSeconds", config.nextZoneWarningSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.preparationSeconds", config.preparationSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.countdownSeconds", config.countdownSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.loadTimeoutSeconds", config.loadTimeoutSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.respawnDelaySeconds", config.respawnDelaySeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.musicPreUnlockSilenceSeconds", config.musicPreUnlockSilenceSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.musicRhythmStartSeconds", config.musicRhythmStartSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.musicLeadStartSeconds", config.musicLeadStartSeconds);
            ValidatePositiveFinite("MatchRulesConfig.musicBaseTempoBpm", config.musicBaseTempoBpm);
            ValidateFinite("MatchRulesConfig.musicEndgameTempoBonusBpm", config.musicEndgameTempoBonusBpm);
            ValidateNonNegativeFinite("MatchRulesConfig.endgameHudTrimSeconds", config.endgameHudTrimSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.botSubstitutionDelaySeconds", config.botSubstitutionDelaySeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.pingLifetimeSeconds", config.pingLifetimeSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.pingCooldownSeconds", config.pingCooldownSeconds);
            config.ValidateSpatialWeaponAudio();
            ValidateNonNegativeFinite("MatchRulesConfig.cockpitCombatRangeMeters", config.cockpitCombatRangeMeters);
            ValidateNonNegativeFinite("MatchRulesConfig.cockpitFadedOpacity", config.cockpitFadedOpacity);
            if (config.cockpitFadedOpacity > 1f)
                throw new InvalidOperationException("MatchRulesConfig.cockpitFadedOpacity must be between 0 and 1.");
            ValidatePositiveFinite("MatchRulesConfig.cockpitFadeDurationSeconds", config.cockpitFadeDurationSeconds);
            ValidateNonNegativeFinite("MatchRulesConfig.rosterIdleFadeSeconds", config.rosterIdleFadeSeconds);
            ValidatePositiveFinite("MatchRulesConfig.hitArcDurationSeconds", config.hitArcDurationSeconds);
            if (config.hitArcDurationSeconds < 0.05f)
                throw new InvalidOperationException("MatchRulesConfig.hitArcDurationSeconds must be at least 0.05 seconds.");
            ValidateRange("hitArcMaxOpacity", config.hitArcMaxOpacity, 0f, 1f);
            ValidateRange("hitArcWidthDegrees", config.hitArcWidthDegrees, 10f, 180f);
            ValidateRange("lowHealthThresholdFraction", config.lowHealthThresholdFraction, 0.05f, 1f);
            ValidateRange("lowHealthVignetteMaxOpacity", config.lowHealthVignetteMaxOpacity, 0f, 1f);
            ValidatePositiveFinite("MatchRulesConfig.lowHealthCueDurationSeconds", config.lowHealthCueDurationSeconds);
            if (config.lowHealthCueDurationSeconds < 0.05f)
                throw new InvalidOperationException("MatchRulesConfig.lowHealthCueDurationSeconds must be at least 0.05 seconds.");
            ValidatePositiveFinite("MatchRulesConfig.lowHealthAudioRepeatIntervalSeconds", config.lowHealthAudioRepeatIntervalSeconds);
            if (config.lowHealthAudioRepeatIntervalSeconds < 0.05f)
                throw new InvalidOperationException("MatchRulesConfig.lowHealthAudioRepeatIntervalSeconds must be at least 0.05 seconds.");
            ValidateRange("lowHealthDesaturationAmount", config.lowHealthDesaturationAmount, 0f, 100f);
            ValidateRange("flightSpeedLinesMaxOpacity", config.flightSpeedLinesMaxOpacity, 0f, 1f);
            if (config.shakeMaxEventsPerSecond < 1)
                throw new InvalidOperationException("MatchRulesConfig.shakeMaxEventsPerSecond must be at least 1.");
            ValidateNonNegativeFinite("MatchRulesConfig.padLaunchFovKickDegrees", config.padLaunchFovKickDegrees);
            if (config.padLaunchFovKickDegrees > 10f)
                throw new InvalidOperationException("MatchRulesConfig.padLaunchFovKickDegrees must be between 0 and 10.");
            ValidateNonNegativeFinite("MatchRulesConfig.padLaunchChromaticIntensity", config.padLaunchChromaticIntensity);
            if (config.padLaunchChromaticIntensity > 1f)
                throw new InvalidOperationException("MatchRulesConfig.padLaunchChromaticIntensity must be between 0 and 1.");
            ValidatePositiveFinite("MatchRulesConfig.padLaunchChromaticDurationSeconds", config.padLaunchChromaticDurationSeconds);
            if (config.padLaunchChromaticDurationSeconds < 0.01f || config.padLaunchChromaticDurationSeconds > 1f)
                throw new InvalidOperationException("MatchRulesConfig.padLaunchChromaticDurationSeconds must be between 0.01 and 1 second.");
            ValidateZoneSchedule(config);
            ValidateAimDistance("minimumAimPointDistanceMeters", config.minimumAimPointDistanceMeters);
            ValidateAimDistance("cameraAimRayDistanceMeters", config.cameraAimRayDistanceMeters);
            if (config.minimumAimPointDistanceMeters > config.cameraAimRayDistanceMeters)
                throw new InvalidOperationException("MatchRulesConfig.minimumAimPointDistanceMeters cannot exceed cameraAimRayDistanceMeters.");
            return config;
        }

        static void ValidateZoneSchedule(MatchRulesConfig config)
        {
            if (config.schedule == null || config.schedule.Length == 0)
                throw new InvalidOperationException("MatchRulesConfig.schedule must cover the regulation period.");

            float expectedStart = 0f;
            for (int i = 0; i < config.schedule.Length; i++)
            {
                var entry = config.schedule[i];
                if (!IsFinite(entry.startTime) || !IsFinite(entry.endTime) || entry.startTime != expectedStart || entry.endTime <= entry.startTime)
                    throw new InvalidOperationException($"MatchRulesConfig.schedule[{i}] must have finite, increasing, contiguous boundaries starting at zero.");
                if (!Enum.IsDefined(typeof(ZoneId), entry.zone))
                    throw new InvalidOperationException($"MatchRulesConfig.schedule[{i}].zone is not a defined Relay zone.");
                expectedStart = entry.endTime;
            }

            if (expectedStart != config.regulationDurationSeconds)
                throw new InvalidOperationException("MatchRulesConfig.schedule must end at regulationDurationSeconds.");
        }

        static void ValidateCooldown(string field, float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new InvalidOperationException($"PadConfig.{field} must be a finite, non-negative duration.");
        }

        static void ValidateAimDistance(string field, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.5f)
                throw new InvalidOperationException($"MatchRulesConfig.{field} must be a finite distance of at least 0.5 m.");
        }

        static void ValidateRange(string field, float value, float minimum, float maximum)
        {
            if (!IsFinite(value) || value < minimum || value > maximum)
                throw new InvalidOperationException($"MatchRulesConfig.{field} must be finite and between {minimum} and {maximum}.");
        }

        static void ValidateNonNegativeFinite(string field, float value)
        {
            if (!IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"{field} must be a finite, non-negative value.");
        }

        static void ValidateFinite(string field, float value)
        {
            if (!IsFinite(value))
                throw new InvalidOperationException($"{field} must be finite.");
        }

        static void ValidateFinite(string field, Vector2 value)
        {
            if (!IsFinite(value)) throw new InvalidOperationException($"{field} must be finite.");
        }

        static void ValidateFinite(string field, Vector3 value)
        {
            if (!IsFinite(value)) throw new InvalidOperationException($"{field} must be finite.");
        }

        static bool IsFinite(Vector2 value) => IsFinite(value.x) && IsFinite(value.y);
        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        static void ValidatePositiveFinite(string field, float value)
        {
            if (!IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"{field} must be a finite, positive value.");
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
