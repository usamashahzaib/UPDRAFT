using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Updraft.Config;
using Updraft.Match;
using Updraft.Player;
using Updraft.Pads;

namespace Updraft.UI
{
    /// <summary>
    /// Spec section 18: Gameplay HUD.
    /// Scores+clock top-center; active zone + rotation countdown under;
    /// teammate status upper-left; HP low-center; ammo by fire;
    /// pad state on button; objective edge marker; network warning not over crosshair.
    /// </summary>
    public class MatchHUD : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] public GraphicsConfig graphicsConfig;
        [SerializeField] public MatchRulesConfig matchRules;

        [Header("Top Center - Scores & Clock")]
        [SerializeField] public TextMeshProUGUI scoreAText;
        [SerializeField] public TextMeshProUGUI scoreBText;
        [SerializeField] public TextMeshProUGUI timerText;
        [SerializeField] public TextMeshProUGUI phaseText;

        [Header("Active Zone & Rotation")]
        [SerializeField] public TextMeshProUGUI activeZoneText;
        [SerializeField] public TextMeshProUGUI nextZoneText;
        [SerializeField] public Image zoneProgressFill; // for overtime hold
        [SerializeField] public GameObject overtimePanel;

        [Header("Upper Left - Teammate Status")]
        [SerializeField] public GameObject teammatePanel;
        [SerializeField] public TeammateStatusUI[] teammateSlots = new TeammateStatusUI[2]; // 2 teammates max

        [Header("Low Center - HP")]
        [SerializeField] public Slider healthSlider;
        [SerializeField] public Image healthFill;
        [SerializeField] public Gradient healthGradient; // green to red

        [Header("By Fire Button - Ammo")]
        [SerializeField] public TextMeshProUGUI ammoText;
        [SerializeField] public Image ammoProgressFill;
        [SerializeField] public GameObject reloadIndicator;

        [Header("Pad Button - Pad State")]
        [SerializeField] public Image padButtonFill; // radial fill for recharge
        [SerializeField] public GameObject padReadyIndicator;
        [SerializeField] public TextMeshProUGUI padChargeText;

        [Header("Objective Edge Marker")]
        [SerializeField] public GameObject objectiveMarkerPrefab;
        [SerializeField] public Transform markersContainer;
        [SerializeField] public UnityEngine.Camera mainCamera; // Updraft.Camera (sibling namespace) shadows the bare name
        [SerializeField] public float markerScreenEdgePadding = 50f;

        [Header("Weapon Select (Preparation Phase)")]
        [SerializeField] public GameObject weaponSelectPanel;
        [SerializeField] public WeaponSelectButton[] weaponButtons = new WeaponSelectButton[3];

        [Header("Countdown")]
        [SerializeField] public GameObject countdownPanel;
        [SerializeField] public TextMeshProUGUI countdownText;
        [SerializeField] public Animation countdownAnimation;

        [Header("Results")]
        [SerializeField] public GameObject resultsPanel;
        [SerializeField] public TextMeshProUGUI resultTitleText;
        [SerializeField] public TextMeshProUGUI resultStatsText;
        [SerializeField] public Button playAgainButton;
        [SerializeField] public Button homeButton;

        [Header("Preparation")]
        [SerializeField] public GameObject preparationPanel;
        [SerializeField] public TextMeshProUGUI preparationText;

        // Runtime
        MatchController matchController;
        PlayerController localPlayer;
        GameObject[] objectiveMarkers = new GameObject[3];

        void Awake()
        {
            // Setup buttons
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(() => matchController?.OnPlayAgain());
            if (homeButton != null)
                homeButton.onClick.AddListener(() => matchController?.OnReturnHome());

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                int idx = i;
                if (weaponButtons[i] != null && weaponButtons[i].button != null)
                {
                    weaponButtons[i].button.onClick.AddListener(() => matchController?.SetLocalWeapon(idx));
                }
            }

            // Create objective markers
            for (int i = 0; i < 3; i++)
            {
                if (objectiveMarkerPrefab != null && markersContainer != null)
                {
                    objectiveMarkers[i] = Instantiate(objectiveMarkerPrefab, markersContainer);
                    objectiveMarkers[i].SetActive(false);
                }
            }

            HideAllPanels();
        }

        public void Initialize(MatchController controller, PlayerController player)
        {
            matchController = controller;
            localPlayer = player;
        }

        void Update()
        {
            if (localPlayer == null) return;

            // Update objective markers (edge markers for off-screen objectives)
            UpdateObjectiveMarkers();
        }

        void UpdateObjectiveMarkers()
        {
            if (mainCamera == null) return;

            Vector3[] zones = { /* Would get from MapConfig */ };
            // Simplified - would use actual zone positions
        }

        public void UpdateScores(int scoreA, int scoreB)
        {
            if (scoreAText != null) scoreAText.text = scoreA.ToString();
            if (scoreBText != null) scoreBText.text = scoreB.ToString();
        }

        public void UpdateTimer(float matchTime)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(matchTime / 60f);
                int seconds = Mathf.FloorToInt(matchTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        public void UpdateHealth(float normalizedHealth)
        {
            if (healthSlider != null) healthSlider.value = normalizedHealth;
            if (healthFill != null && healthGradient != null)
                healthFill.color = healthGradient.Evaluate(normalizedHealth);
        }

        public void UpdateAmmo(int current, int max)
        {
            if (ammoText != null) ammoText.text = $"{current}/{max}";
            if (ammoProgressFill != null) ammoProgressFill.fillAmount = (float)current / max;
            if (reloadIndicator != null) reloadIndicator.SetActive(current == 0);
        }

        public void UpdatePadState(LaunchPad pad)
        {
            // Update pad button visual
        }

        public void ShowPadReady()
        {
            if (padReadyIndicator != null) padReadyIndicator.SetActive(true);
        }

        public void ShowPadActivated()
        {
            // Brief flash
        }

        public void UpdateZoneStatus(Vector3 zoneCenter, bool teamAPresent, bool teamBPresent)
        {
            if (activeZoneText != null)
            {
                string status = teamAPresent && teamBPresent ? "CONTESTED"
                            : teamAPresent ? "TEAM A CONTROLS"
                            : teamBPresent ? "TEAM B CONTROLS"
                            : "UNCONTROLLED";
                activeZoneText.text = status;
            }
        }

        public void ShowPhase(MatchPhase phase, float duration)
        {
            HideAllPanels();

            switch (phase)
            {
                case MatchPhase.Preparation:
                    if (preparationPanel != null)
                    {
                        preparationPanel.SetActive(true);
                        if (preparationText != null)
                            preparationText.text = $"PREPARATION\n{duration - 0f:0}s";
                    }
                    break;
                case MatchPhase.Countdown:
                    if (countdownPanel != null)
                    {
                        countdownPanel.SetActive(true);
                    }
                    break;
            }
        }

        public void ShowPreparationCountdown(float elapsed)
        {
            if (preparationText != null)
            {
                float remaining = matchRules.preparationSeconds - elapsed;
                preparationText.text = $"PREPARATION\n{remaining:0}s";
            }
        }

        public void ShowCountdown(float elapsed)
        {
            if (countdownPanel != null && countdownPanel.activeSelf)
            {
                int count = 3 - Mathf.FloorToInt(elapsed);
                if (countdownText != null)
                    countdownText.text = count > 0 ? count.ToString() : "GO!";
                if (countdownAnimation != null && count > 0)
                    countdownAnimation.Play();
            }
        }

        public void EnableWeaponSelect(bool enabled)
        {
            if (weaponSelectPanel != null)
                weaponSelectPanel.SetActive(enabled);
        }

        public void OnMatchStart()
        {
            HideAllPanels();
        }

        public void ShowOvertime()
        {
            if (overtimePanel != null) overtimePanel.SetActive(true);
        }

        public void UpdateZoneProgress(float progress)
        {
            if (zoneProgressFill != null) zoneProgressFill.fillAmount = progress;
        }

        public void ShowNextZone(ZoneId? zone)
        {
            if (nextZoneText == null) return;

            nextZoneText.text = zone.HasValue ? $"NEXT: {zone.Value.ToString().ToUpper()}" : string.Empty;
        }

        public void ShowResults(bool isWin, bool isDraw, int scoreA, int scoreB)
        {
            if (resultsPanel != null) resultsPanel.SetActive(true);

            if (resultTitleText != null)
            {
                if (isDraw) resultTitleText.text = "DRAW";
                else resultTitleText.text = isWin ? "VICTORY" : "DEFEAT";
            }

            if (resultStatsText != null)
            {
                resultStatsText.text = $"FINAL SCORE\n{scoreA} - {scoreB}";
            }
        }

        public void UpdateTeammateStatus(int index, bool alive, float health)
        {
            if (index >= 0 && index < teammateSlots.Length && teammateSlots[index] != null)
            {
                teammateSlots[index].UpdateStatus(alive, health);
            }
        }

        public void HideAllPanels()
        {
            if (preparationPanel != null) preparationPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(false);
            if (weaponSelectPanel != null) weaponSelectPanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (overtimePanel != null) overtimePanel.SetActive(false);
        }

        [System.Serializable]
        public class TeammateStatusUI
        {
            public Image healthFill;
            public TextMeshProUGUI nameText;
            public GameObject deadOverlay;

            public void UpdateStatus(bool alive, float health)
            {
                if (healthFill != null) healthFill.fillAmount = health;
                if (deadOverlay != null) deadOverlay.SetActive(!alive);
            }
        }

        [System.Serializable]
        public class WeaponSelectButton
        {
            public Button button;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI roleText;
            public Image icon;
        }
    }
}