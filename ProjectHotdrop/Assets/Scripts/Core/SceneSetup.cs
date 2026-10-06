using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Player;
using Hotdrop.Input;
using Hotdrop.Camera;
using Hotdrop.UI;
using Hotdrop.Map;
using Hotdrop.Core;

namespace Hotdrop.Core
{
    /// <summary>
    /// Scene setup helper - creates all necessary objects for the offline vertical slice.
    /// Run this once in the Unity Editor to set up the scene hierarchy.
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Config Assets (drag from Assets/Resources/Config)")]
        public MovementConfig movementConfig;
        public WeaponDefinition[] weaponDefinitions;
        public PadConfig padConfig;
        public MatchRulesConfig matchRules;
        public MapConfig mapConfig;
        public BotConfig botConfig;
        public GraphicsConfig graphicsConfig;

        [Header("Prefabs (drag from Assets/Resources/Prefabs)")]
        public GameObject playerPrefab;
        public GameObject botPrefab;

        [ContextMenu("Setup Complete Offline Slice Scene")]
        public void SetupScene()
        {
            // 1. Create GameBootstrap
            GameObject bootstrapObj = new GameObject("GameBootstrap");
            var bootstrap = bootstrapObj.AddComponent<GameBootstrap>();

            // Assign configs
            bootstrap.movementConfig = movementConfig;
            bootstrap.weaponDefinitions = weaponDefinitions;
            bootstrap.padConfig = padConfig;
            bootstrap.matchRules = matchRules;
            bootstrap.mapConfig = mapConfig;
            bootstrap.botConfig = botConfig;
            bootstrap.graphicsConfig = graphicsConfig;
            bootstrap.playerPrefab = playerPrefab;
            bootstrap.botPrefab = botPrefab;

            // 2. Create Map Generator
            GameObject mapObj = new GameObject("Switchyard");
            var mapGen = mapObj.AddComponent<SwitchyardGenerator>();
            mapGen.mapConfig = mapConfig;
            // Materials would be assigned here
            bootstrap.mapGenerator = mapGen;

            // 3. Create Camera
            GameObject cameraObj = new GameObject("MainCamera");
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 55f;
            var tpc = cameraObj.AddComponent<ThirdPersonCamera>();
            tpc.graphicsConfig = graphicsConfig;
            bootstrap.mainCamera = tpc;

            // 4. Create Input Manager
            GameObject inputObj = new GameObject("TouchInputManager");
            var touchInput = inputObj.AddComponent<TouchInputManager>();
            touchInput.graphicsConfig = graphicsConfig;
            bootstrap.touchInput = touchInput;

            // 5. Create Match Controller
            GameObject matchObj = new GameObject("MatchController");
            var matchController = matchObj.AddComponent<MatchController>();
            matchController.matchRules = matchRules;
            matchController.mapConfig = mapConfig;
            matchController.movementConfig = movementConfig;
            matchController.botConfig = botConfig;
            matchController.weaponDefinitions = weaponDefinitions;
            matchController.playerPrefab = playerPrefab;
            matchController.botPrefab = botPrefab;
            matchController.mainCamera = tpc;
            matchController.touchInput = touchInput;
            bootstrap.matchController = matchController;

            // 6. Create Match HUD (Canvas)
            CreateHUD(bootstrap, matchController);

            // 7. Create spawn point markers for MatchController
            CreateSpawnMarkers(mapConfig, matchController);

            // 8. Create tutorial positions if needed
            CreateTutorialPositions(mapConfig);

            Debug.Log("Scene setup complete! Save the scene and press Play.");
        }

        void CreateHUD(GameBootstrap bootstrap, MatchController matchController)
        {
            // Canvas
            GameObject canvasObj = new GameObject("HUDCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.AddComponent<DensityCanvasScaler>(); // QA-019/020: physical sizing at runtime

            // EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // MatchHUD
            var matchHUD = canvasObj.AddComponent<MatchHUD>();
            matchHUD.graphicsConfig = graphicsConfig;
            matchHUD.matchRules = matchRules;
            bootstrap.matchController.matchHUD = matchHUD;
            bootstrap.matchHUD = matchHUD;

            // Create HUD UI elements programmatically
            CreateHUDElements(canvasObj.transform, matchHUD);
            foreach (var graphic in canvasObj.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = graphic.GetComponent<Button>() != null;
        }

        void CreateHUDElements(Transform canvasTransform, MatchHUD hud)
        {
            // Top Center - Scores & Timer
            GameObject topCenter = CreateUIPanel("TopCenter", canvasTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(400, 100));
            
            GameObject scoreAObj = CreateText("ScoreA", topCenter.transform, "0", 36, TextAlignmentOptions.Center, new Vector2(-150, 0));
            GameObject scoreBObj = CreateText("ScoreB", topCenter.transform, "0", 36, TextAlignmentOptions.Center, new Vector2(150, 0));
            GameObject timerObj = CreateText("Timer", topCenter.transform, "00:00", 28, TextAlignmentOptions.Center, new Vector2(0, 30));
            GameObject phaseObj = CreateText("Phase", topCenter.transform, "", 20, TextAlignmentOptions.Center, new Vector2(0, -15));

            hud.scoreAText = scoreAObj.GetComponent<TextMeshProUGUI>();
            hud.scoreBText = scoreBObj.GetComponent<TextMeshProUGUI>();
            hud.scoreAText.color = new Color(0.188f, 0.835f, 0.91f);
            hud.scoreBText.color = new Color(1f, 0.388f, 0.463f);
            hud.timerText = timerObj.GetComponent<TextMeshProUGUI>();
            hud.phaseText = phaseObj.GetComponent<TextMeshProUGUI>();

            // Active Zone & Next Zone
            GameObject zoneInfo = CreateUIPanel("ZoneInfo", canvasTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(300, 60));
            GameObject activeZoneText = CreateText("ActiveZoneText", zoneInfo.transform, "CENTER - UNCONTROLLED", 20, TextAlignmentOptions.Center, new Vector2(0, 15));
            GameObject nextZoneText = CreateText("NextZoneText", zoneInfo.transform, "NEXT: WEST", 18, TextAlignmentOptions.Center, new Vector2(0, -15));
            
            hud.activeZoneText = activeZoneText.GetComponent<TextMeshProUGUI>();
            hud.nextZoneText = nextZoneText.GetComponent<TextMeshProUGUI>();

            // HP Bar (Low Center)
            GameObject hpPanel = CreateUIPanel("HPPanel", canvasTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(300, 30));
            var hpSlider = hpPanel.AddComponent<Slider>();
            GameObject hpFill = CreateUIElement("Fill", hpPanel.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(0, 0));
            var hpFillImage = hpFill.AddComponent<Image>();
            hpFillImage.type = Image.Type.Filled;
            hpFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpSlider.fillRect = hpFill.GetComponent<RectTransform>();
            hpSlider.targetGraphic = hpFillImage;
            hpSlider.interactable = false;
            hpSlider.transition = Selectable.Transition.None;
            hpFillImage.color = new Color(0.188f, 0.835f, 0.91f);
            hpFill.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 12);
            
            hud.healthSlider = hpSlider;
            hud.healthFill = hpFillImage;
            hud.healthGradient = new Gradient();
            hud.healthGradient.SetKeys(new[] {
                new GradientColorKey(new Color(1f, 0.388f, 0.463f), 0),
                new GradientColorKey(new Color(0.188f, 0.835f, 0.91f), 1)
            }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });

            // Ammo (near fire button - bottom right)
            GameObject ammoPanel = CreateUIPanel("AmmoPanel", canvasTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-200, 200), new Vector2(150, 60));
            GameObject ammoText = CreateText("AmmoText", ammoPanel.transform, "24/24", 24, TextAlignmentOptions.Center, Vector2.zero);
            hud.ammoText = ammoText.GetComponent<TextMeshProUGUI>();

            // Pad State (left of fire button)
            GameObject padPanel = CreateUIPanel("PadPanel", canvasTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-400, 200), new Vector2(80, 80));
            GameObject padFill = CreateUIElement("PadFill", padPanel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var padFillImg = padFill.AddComponent<Image>();
            padFillImg.type = Image.Type.Filled;
            padFillImg.fillMethod = Image.FillMethod.Radial360;
            hud.padButtonFill = padFillImg;

            GameObject padReady = CreateText("PadReady", padPanel.transform, "✓", 24, TextAlignmentOptions.Center, Vector2.zero);
            padReady.SetActive(false);
            hud.padReadyIndicator = padReady;
            GameObject padCharge = CreateText("PadStatus", padPanel.transform, "", 16, TextAlignmentOptions.Center, new Vector2(0, -56));
            padCharge.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 38);
            hud.padChargeText = padCharge.GetComponent<TextMeshProUGUI>();
            padPanel.SetActive(false); // State is drawn on the actual adjustable pad control.

            // Weapon Select Panel (Preparation)
            GameObject weaponSelectPanel = CreateUIPanel("WeaponSelect", canvasTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 300));
            weaponSelectPanel.SetActive(false);
            hud.weaponSelectPanel = weaponSelectPanel;

            for (int i = 0; i < 3; i++)
            {
                GameObject btnObj = CreateButton($"WeaponBtn_{i}", weaponSelectPanel.transform, $"Weapon {i}", new Vector2(-180 + i * 180, 0), new Vector2(160, 200));
                var btn = btnObj.GetComponent<Button>();
                var btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                hud.weaponButtons[i] = new MatchHUD.WeaponSelectButton { button = btn, nameText = btnText };
            }

            // Countdown Panel
            GameObject countdownPanel = CreateUIPanel("Countdown", canvasTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 200));
            countdownPanel.SetActive(false);
            GameObject countdownText = CreateText("CountdownText", countdownPanel.transform, "3", 100, TextAlignmentOptions.Center, Vector2.zero);
            hud.countdownPanel = countdownPanel;
            hud.countdownText = countdownText.GetComponent<TextMeshProUGUI>();

            // Results Panel
            GameObject resultsPanel = CreateUIPanel("Results", canvasTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 400));
            resultsPanel.SetActive(false);
            GameObject resultTitle = CreateText("ResultTitle", resultsPanel.transform, "VICTORY", 48, TextAlignmentOptions.Center, new Vector2(0, 100));
            GameObject resultStats = CreateText("ResultStats", resultsPanel.transform, "Final Score\n120 - 95", 24, TextAlignmentOptions.Center, Vector2.zero);
            GameObject playAgainBtn = CreateButton("PlayAgainBtn", resultsPanel.transform, "PLAY AGAIN", new Vector2(-120, -100), new Vector2(200, 60));
            GameObject homeBtn = CreateButton("HomeBtn", resultsPanel.transform, "HOME", new Vector2(120, -100), new Vector2(200, 60));
            
            hud.resultsPanel = resultsPanel;
            hud.resultTitleText = resultTitle.GetComponent<TextMeshProUGUI>();
            hud.resultStatsText = resultStats.GetComponent<TextMeshProUGUI>();
            hud.playAgainButton = playAgainBtn.GetComponent<Button>();
            hud.homeButton = homeBtn.GetComponent<Button>();

            // Preparation Panel
            GameObject prepPanel = CreateUIPanel("Preparation", canvasTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(400, 100));
            prepPanel.SetActive(false);
            GameObject prepText = CreateText("PrepText", prepPanel.transform, "PREPARATION\n8s", 28, TextAlignmentOptions.Center, Vector2.zero);
            hud.preparationPanel = prepPanel;
            hud.preparationText = prepText.GetComponent<TextMeshProUGUI>();

            GameObject feedback = CreateText("CombatFeedback", canvasTransform, "", 30, TextAlignmentOptions.Center, Vector2.zero);
            feedback.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 140);
            feedback.SetActive(false);
            hud.combatFeedbackText = feedback.GetComponent<TextMeshProUGUI>();

            // Overtime Panel
            GameObject otPanel = CreateUIPanel("Overtime", canvasTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(300, 100));
            otPanel.SetActive(false);
            GameObject otText = CreateText("OTText", otPanel.transform, "OVERTIME", 36, TextAlignmentOptions.Center, new Vector2(0, 20));
            GameObject otProgress = CreateUIElement("OTProgress", otPanel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -30), new Vector2(250, 20));
            var otFill = otProgress.AddComponent<Image>();
            otFill.type = Image.Type.Filled;
            otFill.fillMethod = Image.FillMethod.Horizontal;
            hud.overtimePanel = otPanel;
            hud.zoneProgressFill = otFill;

            // Teammate Status (Upper Left)
            GameObject teamPanel = CreateUIPanel("Teammates", canvasTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -260), new Vector2(220, 160));
            hud.teammatePanel = teamPanel;
            
            for (int i = 0; i < 2; i++)
            {
                GameObject slot = CreateUIPanel($"Teammate_{i}", teamPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30 - i * 70), new Vector2(180, 60));
                GameObject teammateHpFill = CreateUIElement("HPFill", slot.transform, new Vector2(.05f, .1f), new Vector2(.95f, .3f), Vector2.zero, Vector2.zero);
                var fillImg = teammateHpFill.AddComponent<Image>();
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.color = new Color(.188f, .835f, .91f);
                GameObject nameText = CreateText("Name", slot.transform, $"TEAMMATE {i+1}", 16, TextAlignmentOptions.Left, new Vector2(0, 12));
                nameText.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 24);
                GameObject deadOverlay = CreateUIElement("DeadOverlay", slot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var deadImg = deadOverlay.AddComponent<Image>();
                deadImg.color = new Color(1, 0, 0, 0.5f);
                deadOverlay.SetActive(false);
                
                hud.teammateSlots[i] = new MatchHUD.TeammateStatusUI
                {
                    healthFill = fillImg,
                    nameText = nameText.GetComponent<TextMeshProUGUI>(),
                    deadOverlay = deadOverlay
                };
            }
        }

        GameObject CreateUIPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var background = obj.AddComponent<Image>();
            // Appearance has one owner (HudAppearance), so a regenerated scene cannot bake a
            // colour the live HUD would immediately disagree with. MatchHUD re-applies the same
            // rule at runtime, which is what makes the baked value authoritative-but-not-load-bearing.
            background.color = HudAppearance.FloatingPanelColor();
            background.raycastTarget = false;
            return obj;
        }

        GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return obj;
        }

        GameObject CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Vector2 anchoredPos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.one * 0.5f;
            rt.anchorMax = Vector2.one * 0.5f;
            rt.anchoredPosition = anchoredPos;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = UIDesignTokens.TextPrimary;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            tmp.fontSizeMax = fontSize;
            return obj;
        }

        GameObject CreateButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = CreateUIPanel(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, anchoredPos, size);
            var btn = obj.AddComponent<Button>();
            var img = obj.GetComponent<Image>();
            img.color = new Color(0.106f, 0.161f, 0.251f, 0.96f);
            btn.targetGraphic = img;
            GameObject textObj = CreateText("Label", obj.transform, label, 20, TextAlignmentOptions.Center, Vector2.zero);
            return obj;
        }

        void CreateSpawnMarkers(MapConfig mapConfig, MatchController matchController)
        {
            // Create 3 exit markers for each spawn
            Transform[] teamAExits = new Transform[3];
            Transform[] teamBExits = new Transform[3];

            // North spawn exits (Team A) - distributed along Z edge of spawn room
            float spawnHalfWidth = mapConfig.spawnRoomSize.x * 0.5f;
            float spawnHalfDepth = mapConfig.spawnRoomSize.y * 0.5f;
            Vector3 northCenter = mapConfig.northSpawnCenter;

            for (int i = 0; i < 3; i++)
            {
                float xOffset = -spawnHalfWidth + (spawnHalfWidth * 2f / 2f) * i; // -10, 0, 10 for 20m wide
                if (i == 1) xOffset = 0;
                else if (i == 0) xOffset = -spawnHalfWidth * 0.5f;
                else xOffset = spawnHalfWidth * 0.5f;

                GameObject exit = new GameObject($"TeamA_Exit_{i}");
                exit.transform.parent = matchController.transform;
                exit.transform.position = northCenter + new Vector3((i - 1) * 6f, 0.55f, -spawnHalfDepth + 2f);
                exit.transform.rotation = Quaternion.Euler(0, 180f, 0); // facing south
                teamAExits[i] = exit.transform;
            }

            // South spawn exits (Team B)
            Vector3 southCenter = mapConfig.southSpawnCenter;
            for (int i = 0; i < 3; i++)
            {
                float xOffset = -spawnHalfWidth + (spawnHalfWidth * 2f / 2f) * i;
                if (i == 1) xOffset = 0;
                else if (i == 0) xOffset = -spawnHalfWidth * 0.5f;
                else xOffset = spawnHalfWidth * 0.5f;

                GameObject exit = new GameObject($"TeamB_Exit_{i}");
                exit.transform.parent = matchController.transform;
                exit.transform.position = southCenter + new Vector3((i - 1) * 6f, 0.55f, spawnHalfDepth - 2f);
                exit.transform.rotation = Quaternion.Euler(0, 0f, 0); // facing north
                teamBExits[i] = exit.transform;
            }

            // Assign via reflection since fields are private
            var teamASpawnsField = typeof(MatchController).GetField("teamASpawns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var teamBSpawnsField = typeof(MatchController).GetField("teamBSpawns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (teamASpawnsField != null) teamASpawnsField.SetValue(matchController, teamAExits);
            if (teamBSpawnsField != null) teamBSpawnsField.SetValue(matchController, teamBExits);
        }

        void CreateTutorialPositions(MapConfig mapConfig)
        {
            // Create empty GameObjects for tutorial positions
            string[] names = {
                "TutorialStart",
                "TutorialMoveTarget",
                "TutorialShootTarget",
                "TutorialPadPlace",
                "TutorialPadLaunchTarget",
                "TutorialEnemyPad",
                "TutorialCaptureZone",
                "TutorialBotFight"
            };

            Vector3[] positions = {
                mapConfig.northSpawnCenter + new Vector3(0, 0.05f, -10),  // Start near north spawn
                mapConfig.northSpawnCenter + new Vector3(12, 0.05f, -15), // Move target, clear of the 18 m sightline blocker
                mapConfig.northSpawnCenter + new Vector3(0, 0.05f, -25),  // Shoot target
                mapConfig.centerZone + new Vector3(-10, 0.05f, 5),   // Clear legal launch lane
                mapConfig.platformCenters[1],                       // North-west platform
                mapConfig.centerZone + new Vector3(12, 0.05f, -10), // Clear of cover and zones
                mapConfig.centerZone,                                  // Capture zone
                mapConfig.westZone                                     // Bot fight
            };

            // Not parented under "Switchyard": SwitchyardGenerator.GenerateMap()
            // destroys all of its own children on Awake() to regenerate the
            // graybox, which would wipe these markers out before Start() ever
            // looks for them by name.
            GameObject markersRoot = new GameObject("TutorialMarkers");

            for (int i = 0; i < names.Length; i++)
            {
                GameObject obj = new GameObject(names[i]);
                obj.transform.position = positions[i];
                if (i == 0) obj.transform.rotation = Quaternion.Euler(0, 180f, 0);
                if (i == 3)
                {
                    Vector3 launchDirection = positions[4] - positions[3]; launchDirection.y = 0;
                    obj.transform.rotation = Quaternion.LookRotation(launchDirection);
                }
                obj.transform.parent = markersRoot.transform;
            }
        }
    }
}


