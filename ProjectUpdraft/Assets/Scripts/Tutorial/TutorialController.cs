using UnityEngine;
using System.Collections;
using Updraft.Config;
using Updraft.Match;
using Updraft.Player;
using Updraft.Bots;
using Updraft.UI;
using Updraft.Camera;
using Updraft.Input;
using Updraft.Pads;

namespace Updraft.Tutorial
{
    /// <summary>
    /// Spec section 12: ~90s tutorial, no forced timer, skippable/replayable.
    /// Steps: Move → Kill target → Reload → Place pad → Ride pad → Use enemy pad → Destroy enemy pad → Capture zone → Bot fight.
    /// One instruction at a time; advance on completion. Explicitly teach: both teams can use every pad.
    /// After tutorial: offer practice or public play. Default weapon ARC.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        public enum TutorialStep
        {
            MoveToMarker,
            KillStationaryTarget,
            ReloadWeapon,
            PlacePad,
            RideOwnPad,
            UseEnemyPad,
            DestroyEnemyPad,
            CaptureZone,
            BotFight,
            Complete
        }

        [Header("Config")]
        [SerializeField] public MovementConfig movementConfig;
        [SerializeField] public WeaponDefinition[] weaponDefinitions;
        [SerializeField] public PadConfig padConfig;
        [SerializeField] public MapConfig mapConfig;
        [SerializeField] public MatchRulesConfig matchRules;
        [SerializeField] public BotConfig botConfig;

        [Header("Prefabs")]
        [SerializeField] public GameObject playerPrefab;
        [SerializeField] public GameObject botPrefab;
        [SerializeField] public GameObject targetPrefab;
        [SerializeField] public GameObject markerPrefab;

        [Header("References")]
        [SerializeField] public ThirdPersonCamera mainCamera;
        [SerializeField] public TouchInputManager touchInput;
        [SerializeField] public MatchHUD matchHUD;
        [SerializeField] public TutorialUI tutorialUI;

        [Header("Tutorial Positions")]
        [SerializeField] public Transform startPosition;
        [SerializeField] public Transform moveTargetPosition;
        [SerializeField] public Transform shootingTargetPosition;
        [SerializeField] public Transform padPlacementPosition;
        [SerializeField] public Transform padLaunchTargetPosition; // elevated platform
        [SerializeField] public Transform enemyPadPosition;
        [SerializeField] public Transform captureZonePosition;
        [SerializeField] public Transform botFightPosition;

        // State
        TutorialStep currentStep = TutorialStep.MoveToMarker;
        PlayerController localPlayer;
        BotController tutorialBot;
        GameObject currentMarker;
        GameObject shootingTarget;
        LaunchPadBehaviour enemyPad;
        bool stepCompleted;
        bool tutorialSkipped;

        // Events
        public System.Action OnTutorialComplete;

        void Start()
        {
            InitializeTutorial();
        }

        void InitializeTutorial()
        {
            // Create local player
            GameObject playerObj = Instantiate(playerPrefab, startPosition.position, startPosition.rotation);
            localPlayer = playerObj.GetComponent<PlayerController>();

            localPlayer.playerCamera = mainCamera;
            localPlayer.touchInput = touchInput;

            mainCamera.SetTarget(localPlayer.transform);

            // Default weapon ARC per spec
            localPlayer.Initialize(0, 0, weaponDefinitions[0]); // ARC = index 0

            // Subscribe to events
            localPlayer.OnHealthChanged += (hp) => matchHUD?.UpdateHealth(hp);
            localPlayer.OnAmmoChanged += (cur, max) => matchHUD?.UpdateAmmo(cur, max);
            localPlayer.OnPadPlaced += (pad) => CheckStepCompletion(TutorialStep.PlacePad);
            localPlayer.OnPadActivated += (pad) => CheckPadActivationStep(pad);

            // Initialize HUD
            matchHUD?.Initialize(null, localPlayer);
            matchHUD?.EnableWeaponSelect(false);
            matchHUD?.HideAllPanels();

            // Initialize tutorial UI
            tutorialUI?.Initialize(this);
            tutorialUI?.ShowStep(currentStep);

            // Start first step
            StartStep(TutorialStep.MoveToMarker);
        }

        void Update()
        {
            if (tutorialSkipped || currentStep == TutorialStep.Complete) return;

            // Check step completion conditions
            CheckStepConditions();
        }

        void StartStep(TutorialStep step)
        {
            currentStep = step;
            stepCompleted = false;
            tutorialUI?.ShowStep(step);

            // Clean up previous step objects
            CleanupStepObjects();

            switch (step)
            {
                case TutorialStep.MoveToMarker:
                    SetupMoveStep();
                    break;
                case TutorialStep.KillStationaryTarget:
                    SetupKillTargetStep();
                    break;
                case TutorialStep.ReloadWeapon:
                    SetupReloadStep();
                    break;
                case TutorialStep.PlacePad:
                    SetupPlacePadStep();
                    break;
                case TutorialStep.RideOwnPad:
                    SetupRidePadStep();
                    break;
                case TutorialStep.UseEnemyPad:
                    SetupUseEnemyPadStep();
                    break;
                case TutorialStep.DestroyEnemyPad:
                    SetupDestroyEnemyPadStep();
                    break;
                case TutorialStep.CaptureZone:
                    SetupCaptureZoneStep();
                    break;
                case TutorialStep.BotFight:
                    SetupBotFightStep();
                    break;
            }
        }

        void SetupMoveStep()
        {
            // Place marker at move target
            if (markerPrefab != null && moveTargetPosition != null)
            {
                currentMarker = Instantiate(markerPrefab, moveTargetPosition.position, Quaternion.identity);
            }
        }

        void SetupKillTargetStep()
        {
            // Spawn stationary target
            if (targetPrefab != null && shootingTargetPosition != null)
            {
                shootingTarget = Instantiate(targetPrefab, shootingTargetPosition.position, Quaternion.identity);
                var targetScript = shootingTarget.GetComponent<TutorialTarget>();
                if (targetScript != null)
                {
                    targetScript.OnDestroyed += () => CheckStepCompletion(TutorialStep.KillStationaryTarget);
                }
            }
        }

        void SetupReloadStep()
        {
            // Force player to have low ammo
            localPlayer.SetAmmo(2);
            matchHUD?.UpdateAmmo(2, localPlayer.CurrentWeapon.magazineSize);
        }

        void SetupPlacePadStep()
        {
            // Move player to pad placement position
            if (padPlacementPosition != null)
            {
                localPlayer.transform.position = padPlacementPosition.position;
                localPlayer.transform.rotation = padPlacementPosition.rotation;
            }

            // Ensure player has pad charge
            localPlayer.PadManager.GetType().GetField("HasCharge", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.SetValue(localPlayer.PadManager, true);
        }

        void SetupRidePadStep()
        {
            // Player places pad, then we check if they launch
            if (padLaunchTargetPosition != null)
            {
                currentMarker = Instantiate(markerPrefab, padLaunchTargetPosition.position, Quaternion.identity);
            }
        }

        void SetupUseEnemyPadStep()
        {
            // Spawn enemy-colored pad
            if (enemyPadPosition != null)
            {
                GameObject padObj = new GameObject("EnemyPad");
                padObj.transform.position = enemyPadPosition.position;
                padObj.transform.rotation = enemyPadPosition.rotation;
                padObj.layer = LayerMask.NameToLayer("Pad");

                var padBehaviour = padObj.AddComponent<LaunchPadBehaviour>();
                var padData = new LaunchPad(padConfig, 999, 1, enemyPadPosition.position, enemyPadPosition.forward); // Team 1 = enemy
                padBehaviour.Initialize(padData);
                enemyPad = padBehaviour;

                // Force pad to armed state
                padData.GetType().GetField("State", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(padData, PadState.Armed);
            }

            // Move player near enemy pad
            if (enemyPadPosition != null)
            {
                localPlayer.transform.position = enemyPadPosition.position + Vector3.forward * 2f;
            }
        }

        void SetupDestroyEnemyPadStep()
        {
            // Spawn another enemy pad
            SetupUseEnemyPadStep(); // Reuse but for destruction
            tutorialUI?.SetInstruction("Shoot the enemy pad to destroy it");
        }

        void SetupCaptureZoneStep()
        {
            // Move player to capture zone
            if (captureZonePosition != null)
            {
                localPlayer.transform.position = captureZonePosition.position;
                localPlayer.transform.rotation = captureZonePosition.rotation;
            }

            currentMarker = Instantiate(markerPrefab, captureZonePosition.position, Quaternion.identity);
        }

        void SetupBotFightStep()
        {
            // Spawn a bot to fight
            if (botPrefab != null && botFightPosition != null)
            {
                GameObject botObj = Instantiate(botPrefab, botFightPosition.position, botFightPosition.rotation);
                tutorialBot = botObj.GetComponent<BotController>();
                var botPlayer = botObj.GetComponent<PlayerController>();

                tutorialBot.Initialize(1, 1, weaponDefinitions[0], true); // easy difficulty
                botPlayer.Initialize(1, 1, weaponDefinitions[0]);

                localPlayer.OnDeath += _ => { /* respawn handled */ };
                botPlayer.OnDeath += _ => CheckStepCompletion(TutorialStep.BotFight);
            }
        }

        void CheckStepConditions()
        {
            switch (currentStep)
            {
                case TutorialStep.MoveToMarker:
                    if (currentMarker != null && Vector3.Distance(localPlayer.transform.position, currentMarker.transform.position) < 2f)
                    {
                        CompleteStep();
                    }
                    break;

                case TutorialStep.ReloadWeapon:
                    if (localPlayer.IsReloading || localPlayer.CurrentAmmo == localPlayer.CurrentWeapon.magazineSize)
                    {
                        CompleteStep();
                    }
                    break;

                case TutorialStep.RideOwnPad:
                    // Check if player launched and reached target height
                    if (!localPlayer.IsGrounded && localPlayer.transform.position.y > padLaunchTargetPosition.position.y - 1f)
                    {
                        CompleteStep();
                    }
                    break;

                case TutorialStep.UseEnemyPad:
                    // Check if player used enemy pad
                    // Handled in CheckPadActivationStep
                    break;

                case TutorialStep.DestroyEnemyPad:
                    if (enemyPad != null && enemyPad.PadData.State == PadState.Destroyed)
                    {
                        CompleteStep();
                    }
                    break;

                case TutorialStep.CaptureZone:
                    // Check if player is in zone
                    Vector3 zoneCenter = captureZonePosition.position;
                    bool inZone = ZoneOccupancy.IsValid(localPlayer.transform.position, zoneCenter, mapConfig.groundLevelY, matchRules, localPlayer.IsAlive, false);
                    if (inZone)
                    {
                        CompleteStep();
                    }
                    break;
            }
        }

        void CheckStepCompletion(TutorialStep step)
        {
            if (step == currentStep && !stepCompleted)
            {
                CompleteStep();
            }
        }

        void CheckPadActivationStep(LaunchPad pad)
        {
            if (currentStep == TutorialStep.RideOwnPad)
            {
                // Check if this was player's own pad
                if (pad.OwnerId == localPlayer.PlayerId)
                {
                    CompleteStep();
                }
            }
            else if (currentStep == TutorialStep.UseEnemyPad)
            {
                // Check if this was enemy pad
                if (pad.TeamId != localPlayer.TeamId)
                {
                    CompleteStep();
                }
            }
        }

        void CompleteStep()
        {
            stepCompleted = true;

            TutorialStep nextStep = GetNextStep(currentStep);
            if (nextStep == TutorialStep.Complete)
            {
                FinishTutorial();
            }
            else
            {
                StartCoroutine(AdvanceToNextStep(nextStep));
            }
        }

        IEnumerator AdvanceToNextStep(TutorialStep nextStep)
        {
            yield return new WaitForSeconds(1f); // Brief pause
            StartStep(nextStep);
        }

        TutorialStep GetNextStep(TutorialStep current)
        {
            switch (current)
            {
                case TutorialStep.MoveToMarker: return TutorialStep.KillStationaryTarget;
                case TutorialStep.KillStationaryTarget: return TutorialStep.ReloadWeapon;
                case TutorialStep.ReloadWeapon: return TutorialStep.PlacePad;
                case TutorialStep.PlacePad: return TutorialStep.RideOwnPad;
                case TutorialStep.RideOwnPad: return TutorialStep.UseEnemyPad;
                case TutorialStep.UseEnemyPad: return TutorialStep.DestroyEnemyPad;
                case TutorialStep.DestroyEnemyPad: return TutorialStep.CaptureZone;
                case TutorialStep.CaptureZone: return TutorialStep.BotFight;
                case TutorialStep.BotFight: return TutorialStep.Complete;
                default: return TutorialStep.Complete;
            }
        }

        void FinishTutorial()
        {
            currentStep = TutorialStep.Complete;
            tutorialUI?.ShowComplete();
            OnTutorialComplete?.Invoke();
        }

        public void SkipTutorial()
        {
            tutorialSkipped = true;
            FinishTutorial();
        }

        public void RestartTutorial()
        {
            // Clean up
            if (localPlayer != null) Destroy(localPlayer.gameObject);
            if (tutorialBot != null) Destroy(tutorialBot.gameObject);
            CleanupStepObjects();

            // Restart
            tutorialSkipped = false;
            InitializeTutorial();
        }

        void CleanupStepObjects()
        {
            if (currentMarker != null) Destroy(currentMarker);
            if (shootingTarget != null) Destroy(shootingTarget);
            if (enemyPad != null) Destroy(enemyPad.gameObject);
        }
    }
}