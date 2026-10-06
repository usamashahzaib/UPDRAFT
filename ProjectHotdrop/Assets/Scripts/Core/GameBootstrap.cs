using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Player;
using Hotdrop.Input;
using Hotdrop.Camera;
using Hotdrop.UI;
using Hotdrop.Map;
using Hotdrop.Tutorial;

namespace Hotdrop.Core
{
    /// <summary>
    /// Bootstrap for the offline vertical slice (Milestones A & B).
    /// Sets up the complete match loop: graybox arena, movement, camera, three weapons,
    /// launch pads, bots, scoring, respawns.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // Gate A budget evidence. In-game scripts cannot observe the Android process
        // boot, so the process-level startup budget is measured externally with
        // `adb shell am start -W` (TotalTime). This instrument logs the engine-relative
        // time of the first script Awake, which is comparable across builds.
        static float firstAwakeRealSeconds = -1f;
        static bool startupLogged;
        static readonly Dictionary<VolumeComponent, bool> volumeComponentDefaults = new Dictionary<VolumeComponent, bool>();
        static bool qualityDefaultsCaptured;
        static int defaultTextureMipmapLimit, defaultMaximumLodLevel;
        static float defaultLodBias;
        static UnityEngine.ShadowQuality defaultShadowQuality;

        [Header("Config Assets")]
        [SerializeField] public MovementConfig movementConfig;
        [SerializeField] public WeaponDefinition[] weaponDefinitions;
        [SerializeField] public PadConfig padConfig;
        [SerializeField] public MatchRulesConfig matchRules;
        [SerializeField] public MapConfig mapConfig;
        [SerializeField] public BotConfig botConfig;
        [SerializeField] public GraphicsConfig graphicsConfig;

        [Header("Prefabs")]
        [SerializeField] public GameObject playerPrefab;
        [SerializeField] public GameObject botPrefab;

        [Header("Scene References")]
        [SerializeField] public SwitchyardGenerator mapGenerator;
        [SerializeField] public MatchController matchController;
        [SerializeField] public ThirdPersonCamera mainCamera;
        [SerializeField] public TouchInputManager touchInput;
        [SerializeField] public MatchHUD matchHUD;

        [Header("Game Mode")]
        [SerializeField] bool startInTutorial = true;
        [SerializeField] bool startInOfflinePractice = false;

        static GameBootstrap instance;
        public static GameBootstrap Instance => instance;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            // QA-019/020: size every UI element from the device's real density (48 dp targets, ~12 dp text).
            if (matchHUD != null) Hotdrop.UI.DensityCanvasScaler.Install(matchHUD.GetComponent<Canvas>());
            // Amendment 2 comfort option: honour the stored larger-text choice before the first screen is built.
            if (PlayerPrefs.GetInt(OfflinePauseMenu.LargerTextKey, 0) == 1) OfflinePauseMenu.ApplyLargerText(true);
            DontDestroyOnLoad(gameObject);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (firstAwakeRealSeconds < 0f) firstAwakeRealSeconds = Time.realtimeSinceStartup;

            // Apply graphics settings
            ApplyGraphicsConfig();
            OriginalGameAudio.Warmup();
            _ = GameAudio.Instance; // applies saved volumes before any sound plays
            if (PlayerPrefs.GetInt("Telemetry.FirstOpenSent", 0) == 0)
            {
                Telemetry.Track("first_open");
                PlayerPrefs.SetInt("Telemetry.FirstOpenSent", 1);
                PlayerPrefs.Save();
            }
            Application.logMessageReceived += ReportClientError;
            gameObject.AddComponent<DevicePerformanceProbe>();
            gameObject.AddComponent<OfflinePauseMenu>();
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Player"));
            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Pad"));

            // Generate map
            if (mapGenerator != null)
            {
                mapGenerator.GenerateMap();
            }
        }

        void Start()
        {
            if (!startupLogged)
            {
                startupLogged = true;
                int engineMs = (int)(firstAwakeRealSeconds * 1000f);
                Debug.Log($"HOTDROP_BUDGET engine_to_first_script={engineMs}ms (process boot measured externally via am start -W)");
                Telemetry.Track("startup_budget", ("ms", engineMs));
                TelemetryUploader.Kick(force: true); // QA-027: deliver queued events from earlier sessions
            }

            // Initialize systems
            InitializeSystems();

            if (OnlineHooks.IsDedicatedServer)
            {
                // Headless match server: the map is generated; everything else is networked.
                if (touchInput != null) touchInput.enabled = false;
                if (matchHUD != null) matchHUD.gameObject.SetActive(false);
                return;
            }

            ShowStartup();
        }

        /// <summary>Routes from the startup connection presentation into the locally valid
        /// player journey. Kept public for retry/resume and focused PlayMode coverage.</summary>
        public void ContinueFromStartup()
        {
            // Spec section 11: the tutorial runs on first launch; afterwards it is replayable
            // from Home. A fresh player gets the cosmetic runner choice first.
            if (startInTutorial && PlayerPrefs.GetInt(TutorialDoneKey, 0) == 0 && !BodyRigStore.Chosen)
            {
                ShowFirstRunRigChoice();
            }
            else if (startInTutorial && PlayerPrefs.GetInt(TutorialDoneKey, 0) == 0)
            {
                StartTutorial();
            }
            else if (startInTutorial)
            {
                ReturnToHome();
            }
            else if (startInOfflinePractice)
            {
                StartOfflinePractice();
            }
        }

        void ShowStartup()
        {
            var startup = GetComponent<StartupConnectionScreen>();
            if (startup == null) startup = gameObject.AddComponent<StartupConnectionScreen>();
            startup.Show(this);
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            // PlayerPrefs is the only state this first-run shell owns. Persist it before a
            // mobile OS can reclaim the process during selection or training.
            PlayerPrefs.Save();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused || OnlineHooks.IsDedicatedServer) return;
            // A finished tutorial and Home resume directly; an unfinished choice/tutorial keeps
            // its active view. Startup is only restored when no player-facing route exists.
            if (FindFirstObjectByType<TutorialController>() == null && GetComponent<OfflineHomeScreen>() == null)
                ShowStartup();
        }

        float nextErrorReport;
        int errorsThisSession;

        // C13: client_error is an ERROR event, not a crash. A caught exception or a logged error
        // is not a process death, so it must never be counted toward a crash-free KPI. Crash and
        // ANR rates come from Google Play vitals (users-crashed / users-ANR), which measure real
        // process terminations. This is an error-rate signal only; capped so a looping error
        // cannot flood the queue.
        void ReportClientError(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (Time.realtimeSinceStartup < nextErrorReport || errorsThisSession >= 20) return;
            nextErrorReport = Time.realtimeSinceStartup + 5f;
            errorsThisSession++;
            string firstFrame = string.IsNullOrEmpty(stackTrace) ? "" : stackTrace.Split('\n')[0];
            Telemetry.Track("client_error", ("type", type.ToString()),
                ("message", condition.Length > 200 ? condition.Substring(0, 200) : condition), ("frame", firstFrame));
        }

        void OnDestroy()
        {
            if (instance == this) Application.logMessageReceived -= ReportClientError;
        }

        void ApplyGraphicsConfig()
        {
            // Amendment 2: the saved graphics preset (Settings > Graphics) wins; LOW is the
            // default so a first launch matches the low-end budget in the spec.
            ApplyGraphicsQuality(PlayerPrefs.GetInt(OfflinePauseMenu.GraphicsQualityKey, 0));
        }

        /// <summary>
        /// Applies Performance (60 FPS target, 80% render scale), Balanced (60 FPS target,
        /// full render scale), or Cinematic (30 FPS target, full render scale).
        /// The render-scale change is only made inside a player: in the editor the URP asset is
        /// the checked-in project file, and a runtime preset must never rewrite the check-out.
        /// Device measurement of the result is the job of DevicePerformanceProbe.
        /// </summary>
        public static void ApplyGraphicsQuality(int quality)
        {
            var config = instance != null ? instance.graphicsConfig : null;
            quality = Mathf.Clamp(quality, 0, 2);
            CaptureQualityDefaults();
            if (quality == 0)
            {
                QualitySettings.globalTextureMipmapLimit = Mathf.Clamp(config != null ? config.performanceTextureMipmapLimit : 1, 0, 3);
                QualitySettings.lodBias = Mathf.Clamp(config != null ? config.performanceLodBias : 0.7f, 0.1f, 2f);
                QualitySettings.maximumLODLevel = Mathf.Clamp(config != null ? config.performanceMaximumLodLevel : 1, 0, 3);
            }
            else
            {
                QualitySettings.globalTextureMipmapLimit = defaultTextureMipmapLimit;
                QualitySettings.lodBias = defaultLodBias;
                QualitySettings.maximumLODLevel = defaultMaximumLodLevel;
            }
            QualitySettings.shadows = quality == 0 && !(config != null && config.realtimeShadowsOnLow)
                ? UnityEngine.ShadowQuality.Disable
                : defaultShadowQuality;
            int performanceFps = config != null ? config.performanceTargetFps : 60;
            int balancedFps = config != null ? config.balancedTargetFps : 60;
            int cinematicFps = config != null ? config.cinematicPresetTargetFps : 30;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = quality == 0 ? performanceFps : quality == 1 ? balancedFps : cinematicFps;
            float performanceScale = config != null ? config.performanceRenderScale : 0.8f;
            float balancedScale = config != null ? config.balancedRenderScale : 1f;
            float cinematicScale = config != null ? config.cinematicRenderScale : 1f;
            float renderScale = quality == 0 ? performanceScale : quality == 1 ? balancedScale : cinematicScale;
            if (Application.isPlaying && !Application.isEditor
                && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                urp.renderScale = renderScale;
            ApplyVolumeQualityOverrides(config, quality == 0);
            string preset = quality == 0 ? "performance" : quality == 1 ? "balanced" : "cinematic";
            Debug.Log($"HOTDROP_GRAPHICS preset={preset} targetFps={Application.targetFrameRate} renderScale={renderScale} textureMipLimit={QualitySettings.globalTextureMipmapLimit} lodBias={QualitySettings.lodBias:0.##} maxLod={QualitySettings.maximumLODLevel}");
        }

        static void CaptureQualityDefaults()
        {
            if (qualityDefaultsCaptured) return;
            qualityDefaultsCaptured = true;
            defaultTextureMipmapLimit = QualitySettings.globalTextureMipmapLimit;
            defaultLodBias = QualitySettings.lodBias;
            defaultMaximumLodLevel = QualitySettings.maximumLODLevel;
            defaultShadowQuality = QualitySettings.shadows;
        }

        static void ApplyVolumeQualityOverrides(GraphicsConfig config, bool performancePreset)
        {
            if (!Application.isPlaying) return;
            var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
            foreach (var volume in volumes)
            {
                if (volume == null || volume.sharedProfile == null) continue;
                // Volume.profile gives this scene volume a runtime copy. Never change the
                // shared project asset when a player changes a graphics preset.
                var profile = volume.profile;
                foreach (var component in profile.components)
                {
                    if (component == null) continue;
                    string type = component.GetType().Name;
                    bool? lowTierValue = type switch
                    {
                        "Bloom" => config != null ? config.bloomOnLow : false,
                        "MotionBlur" => config != null ? config.motionBlurOnLow : false,
                        "DepthOfField" => config != null ? config.depthOfFieldOnLow : false,
                        _ => null,
                    };
                    if (!lowTierValue.HasValue) continue;
                    if (!volumeComponentDefaults.ContainsKey(component))
                        volumeComponentDefaults.Add(component, component.active);
                    component.active = performancePreset ? lowTierValue.Value : volumeComponentDefaults[component];
                }
            }
        }

        void InitializeSystems()
        {
            if (matchHUD == null && matchController != null) matchHUD = matchController.matchHUD;
            // Link references
            if (matchController != null)
            {
                matchController.matchRules = matchRules;
                matchController.mapConfig = mapConfig;
                matchController.movementConfig = movementConfig;
                matchController.botConfig = botConfig;
                matchController.weaponDefinitions = weaponDefinitions;
                matchController.playerPrefab = playerPrefab;
                matchController.botPrefab = botPrefab;
                matchController.mainCamera = mainCamera;
                matchController.touchInput = touchInput;
                matchController.matchHUD = matchHUD;
            }

            if (matchHUD != null)
            {
                matchHUD.graphicsConfig = graphicsConfig;
                matchHUD.matchRules = matchRules;
            }
        }

        /// <summary>Amendment 1: first-launch runner choice before the guided lesson.</summary>
        public void ShowFirstRunRigChoice()
        {
            // The chooser owns the first interaction. Disabling TouchInputManager also
            // suppresses its OnGUI combat controls until StartTutorial hands them back.
            if (touchInput != null) touchInput.enabled = false;
            var home = GetComponent<OfflineHomeScreen>();
            if (home == null) home = gameObject.AddComponent<OfflineHomeScreen>();
            home.ShowRigChoice(firstRun: true);
        }

        public void StartTutorial()
        {
            GetComponent<OfflineChallengeRuntime>()?.Leave();
            matchController?.StopOfflinePractice();
            GetComponent<OfflineHomeScreen>()?.Hide();
            touchInput.enabled = true;
            // Find or create TutorialController
            var tutorial = FindObjectOfType<TutorialController>();
            if (tutorial == null)
            {
                GameObject tutorialObj = new GameObject("TutorialController");
                tutorial = tutorialObj.AddComponent<TutorialController>();
            }

            tutorial.movementConfig = movementConfig;
            tutorial.weaponDefinitions = weaponDefinitions;
            tutorial.padConfig = padConfig;
            tutorial.mapConfig = mapConfig;
            tutorial.matchRules = matchRules;
            tutorial.botConfig = botConfig;
            tutorial.playerPrefab = playerPrefab;
            tutorial.botPrefab = botPrefab;
            tutorial.targetPrefab = Resources.Load<GameObject>("Prefabs/TutorialTarget");
            tutorial.markerPrefab = Resources.Load<GameObject>("Prefabs/TutorialMarker");
            tutorial.mainCamera = mainCamera;
            tutorial.touchInput = touchInput;
            tutorial.matchHUD = matchHUD;
            tutorial.startPosition = GameObject.Find("TutorialStart")?.transform;
            tutorial.moveTargetPosition = GameObject.Find("TutorialMoveTarget")?.transform;
            tutorial.shootingTargetPosition = GameObject.Find("TutorialShootTarget")?.transform;
            tutorial.padPlacementPosition = GameObject.Find("TutorialPadPlace")?.transform;
            tutorial.padLaunchTargetPosition = GameObject.Find("TutorialPadLaunchTarget")?.transform;
            tutorial.enemyPadPosition = GameObject.Find("TutorialEnemyPad")?.transform;
            tutorial.captureZonePosition = GameObject.Find("TutorialCaptureZone")?.transform;
            tutorial.botFightPosition = GameObject.Find("TutorialBotFight")?.transform;

            tutorial.OnTutorialComplete += OnTutorialComplete;
            // CreateView builds a brand-new card every call and nothing removed the previous one,
            // so a second entry stacked a second card directly on top of this one. Proved in the
            // 04_training parity capture: two "Training instruction" Graphics at the same rect
            // (Unity draw depths -11 and -16), and the upper card's 93%-opaque plate composited
            // over the lower card's caption, leaving "Move to the marker using the left joystick"
            // at luma 39 against a plate of luma 20 - unreadable, while SKIP on the same card
            // rendered pure white. Reachable by a player: first run auto-starts the tutorial, and
            // Home > More > TRAINING starts it again. Reuse the view the controller already owns.
            if (tutorial.tutorialUI == null)
                tutorial.tutorialUI = TutorialUI.CreateView(matchHUD.transform, tutorial);
        }

        public void StartOfflinePractice()
        {
            GetComponent<OfflineChallengeRuntime>()?.Leave();
            FindFirstObjectByType<TutorialController>()?.Shutdown();
            GetComponent<OfflineHomeScreen>()?.Hide();
            touchInput.enabled = true;
            if (matchController != null)
            {
                matchController.StartOfflinePractice(5, 0); // 5 bots, human on Team A
            }
        }

        public const string TutorialDoneKey = "TutorialDone";
        public const string TrainingMedalKey = "TrainingMedal";

        void OnTutorialComplete()
        {
            PlayerPrefs.SetInt(TutorialDoneKey, 1);
            // Amendment 1: the guided lesson earns a local training medal - never online
            // currency, inventory or an online entitlement.
            PlayerPrefs.SetInt(TrainingMedalKey, 1);
            PlayerPrefs.Save();
            ReturnToHome();
        }

        public void ReturnToHome()
        {
            GetComponent<OfflineChallengeRuntime>()?.Leave();
            TelemetryUploader.Kick(); // QA-027
            FindFirstObjectByType<TutorialController>()?.Shutdown();
            matchController?.StopOfflinePractice();
            matchHUD?.HideAllPanels();
            touchInput.enabled = false;
            var home = GetComponent<OfflineHomeScreen>();
            if (home == null) home = gameObject.AddComponent<OfflineHomeScreen>();
            home.Show(this);
        }

        public void OpenOfflineLibrary()
        {
            FindFirstObjectByType<TutorialController>()?.Shutdown();
            matchController?.StopOfflinePractice();
            matchHUD?.HideAllPanels();
            var challenges = GetComponent<OfflineChallengeRuntime>();
            if (challenges == null) challenges = gameObject.AddComponent<OfflineChallengeRuntime>();
            challenges.OpenLibrary();
        }

        public void QuitApplication()
        {
            Application.Quit();
        }

        // Called from UI buttons
        public void OnPlayAgainClicked()
        {
            matchController?.OnPlayAgain();
        }

        public void OnHomeClicked()
        {
            ReturnToHome();
        }
    }
}



