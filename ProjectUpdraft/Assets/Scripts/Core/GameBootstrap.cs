using UnityEngine;
using UnityEngine.SceneManagement;
using Updraft.Config;
using Updraft.Match;
using Updraft.Player;
using Updraft.Input;
using Updraft.Camera;
using Updraft.UI;
using Updraft.Map;
using Updraft.Tutorial;

namespace Updraft.Core
{
    /// <summary>
    /// Bootstrap for the offline vertical slice (Milestones A & B).
    /// Sets up the complete match loop: graybox arena, movement, camera, three weapons,
    /// launch pads, bots, scoring, respawns.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // Gate A budget evidence: real wall-clock device timings, logged once each.
        // Static init runs when the class is first touched by the engine, as close to
        // process start as C# can observe; the editable-Unity warmup that deviceTime
        // excludes is covered by adb logcat timestamps.
        static readonly long processStartDeviceMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        static float firstAwakeRealSeconds = -1f;
        static long firstAwakeDeviceMs = -1;
        static bool startupLogged;

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
            DontDestroyOnLoad(gameObject);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (firstAwakeRealSeconds < 0f)
            {
                firstAwakeRealSeconds = Time.realtimeSinceStartup;
                firstAwakeDeviceMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    - (long)(Time.realtimeSinceStartup * 1000f);
            }

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
                int startupMs = (int)(firstAwakeDeviceMs - processStartDeviceMs);
                Debug.Log($"UPDRAFT_BUDGET startup={startupMs}ms (first script Awake minus static init; engine boot excluded)");
                Telemetry.Track("startup_budget", ("ms", startupMs));
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

            // Spec section 11: the tutorial runs on first launch; afterwards it is
            // replayable from Home rather than forced.
            if (startInTutorial && PlayerPrefs.GetInt(TutorialDoneKey, 0) == 0)
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

        float nextErrorReport;
        int errorsThisSession;

        // client_error feeds the crash-free KPI; capped so a looping error cannot flood the queue.
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
            if (graphicsConfig == null) return;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = graphicsConfig.lowPresetTargetFps;

            // Set URP quality settings for Low preset
            if (graphicsConfig.bakedLightingOnLow)
            {
                // Lightmapping would be set up in the scene
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

        public void StartTutorial()
        {
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
            tutorial.tutorialUI = TutorialUI.CreateView(matchHUD.transform, tutorial);
        }

        public void StartOfflinePractice()
        {
            GetComponent<OfflineHomeScreen>()?.Hide();
            touchInput.enabled = true;
            if (matchController != null)
            {
                matchController.StartOfflinePractice(5, 0); // 5 bots, human on Team A
            }
        }

        public const string TutorialDoneKey = "TutorialDone";

        void OnTutorialComplete()
        {
            PlayerPrefs.SetInt(TutorialDoneKey, 1);
            PlayerPrefs.Save();
            ReturnToHome();
        }

        public void ReturnToHome()
        {
            FindFirstObjectByType<TutorialController>()?.Shutdown();
            matchController?.StopOfflinePractice();
            matchHUD?.HideAllPanels();
            touchInput.enabled = false;
            var home = GetComponent<OfflineHomeScreen>();
            if (home == null) home = gameObject.AddComponent<OfflineHomeScreen>();
            home.Show(this);
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
