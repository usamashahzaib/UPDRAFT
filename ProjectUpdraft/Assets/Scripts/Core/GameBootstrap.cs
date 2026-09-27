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

            // Apply graphics settings
            ApplyGraphicsConfig();

            // Generate map
            if (mapGenerator != null)
            {
                mapGenerator.GenerateMap();
            }
        }

        void Start()
        {
            // Initialize systems
            InitializeSystems();

            // Start appropriate mode
            if (startInTutorial)
            {
                StartTutorial();
            }
            else if (startInOfflinePractice)
            {
                StartOfflinePractice();
            }
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
            tutorial.targetPrefab = Resources.Load<GameObject>("TutorialTarget");
            tutorial.markerPrefab = Resources.Load<GameObject>("TutorialMarker");
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
        }

        public void StartOfflinePractice()
        {
            if (matchController != null)
            {
                matchController.StartOfflinePractice(5, 0); // 5 bots, human on Team A
            }
        }

        void OnTutorialComplete()
        {
            // Show menu: Practice or Public Play
            Debug.Log("Tutorial complete - offering practice or public play");
            var tutorial = FindObjectOfType<TutorialController>();
            tutorial?.Shutdown();
            StartOfflinePractice();
        }

        public void ReturnToHome()
        {
            SceneManager.LoadScene("Home");
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
