using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Config;
using Hotdrop.Match;
using Hotdrop.Player;
using Hotdrop.Pads;

namespace Hotdrop.UI
{
    /// <summary>
    /// Spec section 17 gameplay HUD: scores + clock top-center; active zone and
    /// rotation countdown under it; teammate status upper-left; HP low-center;
    /// ammo by fire (drawn on the touch control); pad state on its button;
    /// objective edge marker. Colour is always paired with a text label.
    /// </summary>
    public class MatchHUD : MonoBehaviour
    {
        // UI foundation (Prompt 01): the HUD reads the central semantic tokens instead of
        // carrying its own literals. Field names are unchanged, so every existing caller -
        // scene wiring, HUD code and tests alike - keeps working.
        public static readonly Color Friendly = UIDesignTokens.ActionCyan;
        public static readonly Color Enemy = UIDesignTokens.HazardCoral;
        public static readonly Color Objective = UIDesignTokens.ObjectiveGold;
        public static readonly Color MainText = UIDesignTokens.TextPrimary;
        public static readonly Color SecondaryText = UIDesignTokens.TextSecondary;
        public static readonly Color PanelBg = UIDesignTokens.PanelBg;

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
        [SerializeField] public Image zoneProgressFill;
        [SerializeField] public GameObject overtimePanel;

        [Header("Upper Left - Teammate Status")]
        [SerializeField] public GameObject teammatePanel;
        [SerializeField] public TeammateStatusUI[] teammateSlots = new TeammateStatusUI[2];

        [Header("Low Center - HP")]
        [SerializeField] public Slider healthSlider;
        [SerializeField] public Image healthFill;
        [SerializeField] public Gradient healthGradient;

        [Header("By Fire Button - Ammo")]
        [SerializeField] public TextMeshProUGUI ammoText;
        [SerializeField] public Image ammoProgressFill;
        [SerializeField] public GameObject reloadIndicator;

        [Header("Pad Button - Pad State")]
        [SerializeField] public Image padButtonFill;
        [SerializeField] public GameObject padReadyIndicator;
        [SerializeField] public TextMeshProUGUI padChargeText;

        [Header("Objective Edge Marker")]
        [SerializeField] public GameObject objectiveMarkerPrefab;
        [SerializeField] public Transform markersContainer;
        [SerializeField] public UnityEngine.Camera mainCamera;
        [SerializeField] public float markerScreenEdgePadding = 60f;

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

        [Header("Combat Feedback")]
        [SerializeField] public TextMeshProUGUI combatFeedbackText;

        IMatchParticipant localPlayer;
        Hotdrop.Input.TouchInputManager touch;
        System.Action playAgainAction, homeAction;
        System.Action<int> selectWeaponAction;
        bool hasMatch;
        float combatFeedbackUntil;
        WeaponDefinition[] weapons;

        // Runtime-built elements.
        RectTransform canvasRect;
        RectTransform objectiveMarker;
        TextMeshProUGUI objectiveMarkerText;
        GameObject deathPanel;
        TextMeshProUGUI deathTitle, deathCountdown, deathSpectate;
        string lastAttackDirection = "";
        TextMeshProUGUI healthValueText;
        TextMeshProUGUI resultTableText;
        TextMeshProUGUI hitMarkerText;
        float hitMarkerUntil;
        string lastKillerName = "";
        string lastWeaponName = "";

        public bool IsDeathPanelVisible => deathPanel != null && deathPanel.activeSelf;

        void Awake()
        {
            canvasRect = GetComponent<RectTransform>();
            if (playAgainButton != null) playAgainButton.onClick.AddListener(() => playAgainAction?.Invoke());
            if (homeButton != null) homeButton.onClick.AddListener(() => homeAction?.Invoke());

            for (int i = 0; i < weaponButtons.Length; i++)
            {
                int idx = i;
                if (weaponButtons[i]?.button != null)
                    weaponButtons[i].button.onClick.AddListener(() => { selectWeaponAction?.Invoke(idx); HighlightWeapon(idx); });
            }

            BuildRuntimeElements();
            UiFloor.Apply(transform); // QA-019/020: >= 48 dp controls and ~12 dp text on every HUD element
            HideAllPanels();
        }

        void BuildRuntimeElements()
        {
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (GetComponent<PingSystem>() == null) gameObject.AddComponent<PingSystem>();

            // Objective edge marker: a diamond plus zone name and distance.
            var marker = Ui.Rect("ObjectiveMarker", transform, new Vector2(170, 90));
            objectiveMarker = marker;
            var diamond = Ui.Rect("Diamond", marker, new Vector2(34, 34));
            diamond.anchoredPosition = new Vector2(0, 18);
            diamond.localRotation = Quaternion.Euler(0, 0, 45);
            var diamondImage = diamond.gameObject.AddComponent<Image>();
            diamondImage.color = Objective; diamondImage.raycastTarget = false;
            objectiveMarkerText = Ui.Text("Label", marker, "", 22, Objective);
            objectiveMarkerText.rectTransform.anchoredPosition = new Vector2(0, -24);
            objectiveMarkerText.rectTransform.sizeDelta = new Vector2(170, 30);
            marker.gameObject.SetActive(false);

            // Death / respawn panel.
            var death = Ui.Panel("DeathPanel", transform, new Vector2(760, 150), PanelBg);
            death.anchorMin = death.anchorMax = new Vector2(0.5f, 0.5f);
            death.anchoredPosition = new Vector2(0, 150); // stays on screen on the density-scaled canvas
            deathPanel = death.gameObject;
            deathTitle = Ui.Text("Title", death, "", 30, Enemy); deathTitle.rectTransform.anchoredPosition = new Vector2(0, 40);
            deathCountdown = Ui.Text("Countdown", death, "", 26, MainText); deathCountdown.rectTransform.anchoredPosition = new Vector2(0, 0);
            deathSpectate = Ui.Text("Spectate", death, "", 20, SecondaryText); deathSpectate.rectTransform.anchoredPosition = new Vector2(0, -40);
            foreach (var t in new[] { deathTitle, deathCountdown, deathSpectate }) t.rectTransform.sizeDelta = new Vector2(740, 36);
            deathPanel.SetActive(false);

            // Numeric HP next to the bar.
            if (healthSlider != null)
            {
                healthValueText = Ui.Text("HPValue", healthSlider.transform, "100", 22, MainText);
                // Sit clear above the bar (it overlapped the bar edge on 720p phones) and read over bright floors.
                healthValueText.rectTransform.anchoredPosition = new Vector2(0, 36);
                healthValueText.fontStyle = FontStyles.Bold;
                healthValueText.outlineWidth = 0.25f;
                healthValueText.outlineColor = new Color32(10, 16, 28, 255);
                healthValueText.rectTransform.sizeDelta = new Vector2(200, 28);
            }

            // Hit marker at the crosshair.
            hitMarkerText = Ui.Text("HitMarker", transform, "X", 34, MainText);
            hitMarkerText.rectTransform.anchorMin = hitMarkerText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hitMarkerText.rectTransform.sizeDelta = new Vector2(60, 60);
            hitMarkerText.gameObject.SetActive(false);

            if (resultsPanel != null)
            {
                // QA-019/020: fill the screen (whatever its size) instead of a fixed 1100x760 box
                // that overflowed a phone; table text shrinks to fit but never below ~12 dp.
                var rt = resultsPanel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(24, 16); rt.offsetMax = new Vector2(-24, -16);
                if (resultTitleText != null) PinTop(resultTitleText.rectTransform, 8, 64);
                if (resultStatsText != null) PinTop(resultStatsText.rectTransform, 72, 96);
                resultTableText = Ui.Text("Table", resultsPanel.transform, "", 24, MainText);
                resultTableText.alignment = TextAlignmentOptions.Top;
                resultTableText.enableAutoSizing = true;
                resultTableText.fontSizeMin = UiMetrics.MinTextUnits; resultTableText.fontSizeMax = 24;
                var table = resultTableText.rectTransform;
                table.anchorMin = Vector2.zero; table.anchorMax = Vector2.one;
                table.offsetMin = new Vector2(16, UiMetrics.MinTouchUnits + 28); table.offsetMax = new Vector2(-16, -176);
                if (playAgainButton != null) PinBottom(playAgainButton, -1);
                if (homeButton != null) PinBottom(homeButton, 1);
            }

            if (weaponSelectPanel != null)
            {
                var rt = weaponSelectPanel.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(900, 260);
                rt.anchoredPosition = new Vector2(0, -60);
                for (int i = 0; i < weaponButtons.Length; i++)
                {
                    var b = weaponButtons[i];
                    if (b?.button == null) continue;
                    var brt = b.button.GetComponent<RectTransform>();
                    brt.sizeDelta = new Vector2(270, 200);
                    brt.anchoredPosition = new Vector2(-290 + i * 290, 0);
                    if (b.nameText != null)
                    {
                        b.nameText.rectTransform.anchoredPosition = new Vector2(0, 40);
                        b.nameText.rectTransform.sizeDelta = new Vector2(250, 50);
                        b.nameText.fontSize = 34; b.nameText.fontSizeMax = 34;
                    }
                    if (b.roleText == null)
                    {
                        b.roleText = Ui.Text("Role", b.button.transform, "", 21, SecondaryText);
                        b.roleText.rectTransform.anchoredPosition = new Vector2(0, -38);
                        b.roleText.rectTransform.sizeDelta = new Vector2(250, 100);
                        b.roleText.textWrappingMode = TextWrappingModes.Normal;
                        b.roleText.fontSizeMin = 17;
                    }
                }
            }
            if (preparationPanel != null)
            {
                var rt = preparationPanel.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 150);
                rt.sizeDelta = new Vector2(620, 80);
                if (preparationText != null)
                {
                    preparationText.rectTransform.sizeDelta = new Vector2(600, 70);
                    preparationText.fontSizeMax = 32; preparationText.fontSizeMin = 20;
                }
            }
            if (countdownText != null) countdownText.rectTransform.sizeDelta = new Vector2(200, 180);
            if (scoreAText != null && scoreBText != null)
            {
                // "ALLY 120" / "120 ENEMY" need room: widen the scoreboard and pin labels to its edges.
                ((RectTransform)scoreAText.transform.parent).sizeDelta = new Vector2(560, 100);
                scoreAText.rectTransform.sizeDelta = scoreBText.rectTransform.sizeDelta = new Vector2(190, 50);
                scoreAText.rectTransform.anchoredPosition = new Vector2(-170, -8);
                scoreBText.rectTransform.anchoredPosition = new Vector2(170, -8);
                scoreAText.alignment = TextAlignmentOptions.Left;
                scoreBText.alignment = TextAlignmentOptions.Right;
            }
            if (activeZoneText != null) { activeZoneText.rectTransform.sizeDelta = new Vector2(290, 30); activeZoneText.fontSizeMin = 16; }
            if (nextZoneText != null) { nextZoneText.rectTransform.sizeDelta = new Vector2(290, 26); nextZoneText.fontSizeMin = 14; }
            if (combatFeedbackText != null) combatFeedbackText.rectTransform.anchoredPosition = new Vector2(0, 150);
        }

        static void PlaceButton(Button button, Vector2 position)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(300, 90);
        }

        /// <summary>Anchors a results element to the panel top, <paramref name="top"/> units down.</summary>
        static void PinTop(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(16, -top - height); rt.offsetMax = new Vector2(-16, -top);
        }

        /// <summary>Results actions: a 48 dp row pinned to the panel bottom (side -1 left, +1 right).</summary>
        static void PinBottom(Button button, int side)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0); rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(320, UiMetrics.MinTouchUnits + 9);
            rt.anchoredPosition = new Vector2(side * 175f, 12);
        }

        /// <summary>Offline practice / tutorial (controller null = tutorial).</summary>
        public void Initialize(MatchController controller, PlayerController player, WeaponDefinition[] weaponDefinitions = null)
        {
            Bind(player, player.touchInput, weaponDefinitions ?? controller?.weaponDefinitions, controller != null,
                controller != null ? controller.OnPlayAgain : null,
                controller != null ? controller.OnReturnHome : null,
                controller != null ? controller.SetLocalWeapon : null);
            UpdateHealth(player.CurrentHealth / player.MaxHealth);
            UpdateAmmo(player.CurrentAmmo, player.MaxAmmo);
            HighlightWeapon(System.Array.IndexOf(weapons ?? new WeaponDefinition[0], player.CurrentWeapon));
        }

        /// <summary>Networked match: same HUD, actions routed to the network session.</summary>
        public void InitializeNetwork(IMatchParticipant local, Hotdrop.Input.TouchInputManager touchInput, WeaponDefinition[] weaponDefinitions,
            System.Action playAgain, System.Action home, System.Action<int> selectWeapon)
        {
            Bind(local, touchInput, weaponDefinitions, true, playAgain, home, selectWeapon);
            UpdateHealth(local.HealthFraction);
        }

        void Bind(IMatchParticipant local, Hotdrop.Input.TouchInputManager touchInput, WeaponDefinition[] weaponDefinitions, bool match,
            System.Action playAgain, System.Action home, System.Action<int> selectWeapon)
        {
            localPlayer = local;
            touch = touchInput;
            weapons = weaponDefinitions;
            hasMatch = match;
            playAgainAction = playAgain;
            homeAction = home;
            selectWeaponAction = selectWeapon;
            if (teammatePanel != null) teammatePanel.SetActive(match);
            SetZoneInfoVisible(true);
            if (!match)
            {
                // Training has no score, clock or rotation.
                if (activeZoneText != null) { activeZoneText.text = "TRAINING"; activeZoneText.color = Objective; }
                if (nextZoneText != null) nextZoneText.text = "";
                if (timerText != null) timerText.text = "";
                if (scoreAText != null) scoreAText.text = "";
                if (scoreBText != null) scoreBText.text = "";
            }
            if (weapons != null)
                for (int i = 0; i < weaponButtons.Length && i < weapons.Length; i++)
                {
                    if (weaponButtons[i]?.nameText != null) weaponButtons[i].nameText.text = weapons[i].displayName.ToUpperInvariant();
                    if (weaponButtons[i]?.roleText != null) weaponButtons[i].roleText.text = weapons[i].roleDescription;
                }
        }

        public void HighlightWeapon(int index)
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                var image = weaponButtons[i]?.button != null ? weaponButtons[i].button.GetComponent<Image>() : null;
                if (image == null) continue;
                bool selected = i == index;
                image.color = selected ? UIDesignTokens.WithAlpha(UIDesignTokens.ActionCyan, 0.35f) : PanelBg;
                if (weaponButtons[i].nameText != null)
                    weaponButtons[i].nameText.text = weapons != null && i < weapons.Length
                        ? (selected ? "> " : "") + weapons[i].displayName.ToUpperInvariant() + (selected ? " <" : "")
                        : weaponButtons[i].nameText.text;
            }
        }

        void Update()
        {
            if (combatFeedbackText != null && combatFeedbackText.gameObject.activeSelf && Time.unscaledTime >= combatFeedbackUntil)
                combatFeedbackText.gameObject.SetActive(false);
            if (hitMarkerText != null && hitMarkerText.gameObject.activeSelf && Time.unscaledTime >= hitMarkerUntil)
                hitMarkerText.gameObject.SetActive(false);
            if (localPlayer == null) return;
            // QA-016: an online avatar is destroyed when the server closes the session while the
            // results screen is still up; interface equality does not see Unity's destroyed state.
            if (localPlayer is Object unityPlayer && unityPlayer == null) return;

            UpdateObjectiveMarker();
            UpdateTeammates();
            UpdateDeathPanel();
            if (localPlayer is PlayerController offline)
                UpdateAmmo(offline.CurrentAmmo, offline.MaxAmmo, offline.IsReloading, offline.ReloadProgress);
        }

        void UpdateObjectiveMarker()
        {
            var match = MatchView.Current;
            // Real matches only: the tutorial points at its own markers.
            bool show = match != null && match.IsLiveMatch && mainCamera != null && localPlayer.IsAlive &&
                        (match.CurrentPhase == MatchPhase.InProgress || match.CurrentPhase == MatchPhase.Overtime ||
                         match.CurrentPhase == MatchPhase.Preparation || match.CurrentPhase == MatchPhase.Countdown);
            if (objectiveMarker == null) return;
            if (!show) { objectiveMarker.gameObject.SetActive(false); return; }

            Vector3 world = match.ActiveObjectivePosition + Vector3.up * 1.5f;
            Vector3 screen = mainCamera.WorldToScreenPoint(world);
            bool behind = screen.z < 0f;
            if (behind) screen = new Vector3(Screen.width - screen.x, Screen.height - screen.y, 0f);

            Rect safe = Screen.safeArea;
            float pad = markerScreenEdgePadding * (Screen.height / 1080f);
            bool onScreen = !behind && safe.Contains(new Vector2(screen.x, screen.y));
            if (!onScreen)
            {
                // Push to the nearest safe-area edge along the direction from screen centre.
                Vector2 centre = safe.center;
                Vector2 dir = new Vector2(screen.x, screen.y) - centre;
                if (behind && dir.sqrMagnitude < 1f) dir = Vector2.down;
                float sx = (safe.width * 0.5f - pad) / Mathf.Max(0.001f, Mathf.Abs(dir.x));
                float sy = (safe.height * 0.5f - pad) / Mathf.Max(0.001f, Mathf.Abs(dir.y));
                Vector2 edge = centre + dir * Mathf.Min(sx, sy);
                // Keep clear of the HP bar / controls band at the bottom and the scoreboard at the top.
                edge.y = Mathf.Clamp(edge.y, safe.yMin + safe.height * 0.3f, safe.yMax - safe.height * 0.25f);
                screen = new Vector3(edge.x, edge.y, 0f);
            }

            objectiveMarker.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            objectiveMarker.localPosition = local;
            float distance = Vector3.Distance(localPlayer.Position, match.ActiveObjectivePosition);
            objectiveMarkerText.text = $"{match.ScoringZone.ToString().ToUpperInvariant()} {distance:0}m";
        }

        void UpdateTeammates()
        {
            var view = MatchView.Current;
            if (!hasMatch || view == null || teammateSlots == null) return;
            int slot = 0;
            var players = view.Participants;
            for (int i = 0; i < players.Count && slot < teammateSlots.Length; i++)
            {
                var p = players[i];
                if (p == null || ReferenceEquals(p, localPlayer) || p.TeamId != localPlayer.TeamId) continue;
                var ui = teammateSlots[slot++];
                if (ui == null) continue;
                if (ui.nameText != null) ui.nameText.text = p.IsAlive ? p.DisplayName : $"{p.DisplayName}  DOWN {p.RespawnRemaining:0}s";
                ui.UpdateStatus(p.IsAlive, p.HealthFraction);
            }
        }

        void UpdateDeathPanel()
        {
            if (deathPanel == null) return;
            var phase = hasMatch ? MatchView.Current?.CurrentPhase : null;
            // QA-013: without a match (the tutorial) a death still gets the explanation panel.
            bool dead = !localPlayer.IsAlive && (!hasMatch || phase == MatchPhase.InProgress || phase == MatchPhase.Overtime);
            if (deathPanel.activeSelf != dead)
            {
                deathPanel.SetActive(dead);
                if (hasMatch) EnableWeaponSelect(dead || phase == MatchPhase.Preparation);
            }
            if (!dead) return;
            deathTitle.text = string.IsNullOrEmpty(lastKillerName)
                ? "ELIMINATED — LEFT THE ARENA"
                : $"ELIMINATED BY {lastKillerName}  ·  {lastWeaponName}" + (string.IsNullOrEmpty(lastAttackDirection) ? "" : $"  ·  FROM {lastAttackDirection}");
            int seconds = Mathf.CeilToInt(localPlayer.RespawnRemaining);
            deathCountdown.text = hasMatch ? $"RESPAWN IN {seconds}   ·   CHANGE WEAPON BELOW" : $"RESPAWN IN {seconds}";
            var spectated = MatchView.Current?.SpectatedTeammate;
            deathSpectate.text = !hasMatch ? "TIP: STRAFE WHILE FIRING AND FIGHT FROM COVER — THE BOT AIMS WHERE YOU WERE"
                : spectated != null ? $"WATCHING {spectated.DisplayName}" : "";
        }

        public void UpdateScores(int scoreA, int scoreB)
        {
            // Local team always on the left in friendly colour.
            bool localIsA = localPlayer == null || localPlayer.TeamId == 0;
            if (scoreAText != null) { scoreAText.text = $"ALLY {(localIsA ? scoreA : scoreB)}"; scoreAText.color = Friendly; }
            if (scoreBText != null) { scoreBText.text = $"{(localIsA ? scoreB : scoreA)} ENEMY"; scoreBText.color = Enemy; }
        }

        public void UpdateTimer(float matchTime)
        {
            if (timerText == null || matchRules == null) return;
            float remaining;
            string prefix = "";
            // Only a real (tied) overtime shows the OT clock; a decided match just ends at 0:00.
            if (matchTime >= matchRules.regulationDurationSeconds && MatchView.Current?.CurrentPhase == MatchPhase.Overtime)
            {
                remaining = matchRules.regulationDurationSeconds + matchRules.maxOvertimeSeconds - matchTime;
                prefix = "OT ";
            }
            else remaining = matchRules.regulationDurationSeconds - matchTime;
            remaining = Mathf.Max(0f, remaining);
            int total = Mathf.CeilToInt(remaining);
            timerText.text = $"{prefix}{total / 60}:{total % 60:00}";
        }

        public void UpdateHealth(float normalizedHealth)
        {
            if (healthSlider != null) healthSlider.value = normalizedHealth;
            if (healthFill != null && healthGradient != null) healthFill.color = healthGradient.Evaluate(normalizedHealth);
            if (healthValueText != null)
            {
                int hp = Mathf.CeilToInt(normalizedHealth * 100f); // spec: 100 HP for everyone
                healthValueText.text = normalizedHealth <= 0.3f ? $"LOW HP {hp}" : hp.ToString();
                healthValueText.color = normalizedHealth <= 0.3f ? Enemy : MainText;
            }
        }

        public void UpdateAmmo(int current, int max)
            => UpdateAmmo(current, max, false, 0f);

        public void UpdateAmmo(int current, int max, bool reloading, float reloadProgress)
        {
            string label = reloading ? $"RELOADING {Mathf.RoundToInt(Mathf.Clamp01(reloadProgress) * 100f)}%" : $"{current}/{max}";
            if (ammoText != null)
            {
                ammoText.text = label;
                // Ammo is drawn on the fire control itself so it never overlaps the button.
                var panel = ammoText.transform.parent;
                if (panel != null && panel != transform) panel.gameObject.SetActive(false);
            }
            if (touch != null) touch.AmmoLabel = label;
            if (ammoProgressFill != null) ammoProgressFill.fillAmount = max > 0 ? (float)current / max : 0f;
            if (reloadIndicator != null) reloadIndicator.SetActive(reloading);
        }

        public void UpdatePadState(LaunchPad pad) { }

        public void ShowPadReady() => ShowCombatFeedback("PAD READY", Objective, 0.8f);

        public void ShowPadActivated() => ShowCombatFeedback("LAUNCH", Objective, 0.5f);

        public void ShowPadRejected(string reason) => ShowCombatFeedback($"CAN'T PLACE PAD — {reason}", SecondaryText, 1.2f);

        public void ShowBlockedShot() => ShowCombatFeedback("SHOT BLOCKED", new Color(1f, 0.65f, 0.2f), 0.7f);

        public void ShowHitConfirmed()
        {
            if (hitMarkerText == null) return;
            hitMarkerText.gameObject.SetActive(true);
            hitMarkerUntil = Time.unscaledTime + 0.15f;
        }

        /// <summary>killerName empty = left the arena / no killer.</summary>
        public void ShowEliminated(string killerName, WeaponId weapon, string attackDirection = "")
        {
            lastKillerName = killerName ?? "";
            lastWeaponName = weapon.ToString().ToUpperInvariant();
            lastAttackDirection = attackDirection ?? "";
            HideCombatFeedback();
        }

        public void ShowEliminationConfirmed(string victimName) => ShowCombatFeedback($"ELIMINATED {victimName}", Friendly, 1.2f);

        public void HideCombatFeedback()
        {
            if (combatFeedbackText != null) combatFeedbackText.gameObject.SetActive(false);
        }

        public void ShowCombatFeedback(string message, Color color, float seconds)
        {
            if (combatFeedbackText == null) return;
            combatFeedbackText.text = message;
            combatFeedbackText.color = color;
            combatFeedbackText.gameObject.SetActive(true);
            combatFeedbackUntil = Time.unscaledTime + seconds;
        }

        public void UpdateZoneStatus(Vector3 zoneCenter, bool teamAPresent, bool teamBPresent)
        {
            if (activeZoneText == null) return;
            var match = MatchView.Current;
            bool localIsA = localPlayer == null || localPlayer.TeamId == 0;
            bool ally = localIsA ? teamAPresent : teamBPresent;
            bool enemy = localIsA ? teamBPresent : teamAPresent;
            string zone = match != null ? match.ScoringZone.ToString().ToUpperInvariant() : "ZONE";
            string status = ally && enemy ? "CONTESTED" : ally ? "ALLY SCORING" : enemy ? "ENEMY SCORING" : "OPEN";
            activeZoneText.text = $"{zone}  ·  {status}";
            activeZoneText.color = ally && enemy ? Objective : ally ? Friendly : enemy ? Enemy : MainText;
        }

        public void ShowRotation(float matchTime)
        {
            if (nextZoneText == null || matchRules == null) return;
            var upcoming = matchRules.GetUpcomingZone(matchTime);
            if (upcoming.HasValue)
            {
                float seconds = matchRules.SecondsUntilRotation(matchTime);
                nextZoneText.text = $"NEXT: {upcoming.Value.ToString().ToUpperInvariant()} IN {Mathf.CeilToInt(seconds)}s";
                nextZoneText.color = Objective;
            }
            else
            {
                nextZoneText.text = matchTime >= matchRules.regulationDurationSeconds ? "OVERTIME — HOLD 3s TO WIN" : "";
                nextZoneText.color = SecondaryText;
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
                        if (preparationText != null) preparationText.text = $"CHOOSE YOUR WEAPON  ·  {duration:0}s";
                    }
                    break;
                case MatchPhase.Countdown:
                    if (countdownPanel != null) countdownPanel.SetActive(true);
                    break;
            }
        }

        public void ShowPreparationCountdown(float elapsed)
        {
            if (preparationText != null && matchRules != null)
                preparationText.text = $"CHOOSE YOUR WEAPON  ·  {Mathf.CeilToInt(matchRules.preparationSeconds - elapsed)}s";
        }

        public void ShowCountdown(float elapsed)
        {
            if (countdownPanel == null || !countdownPanel.activeSelf) return;
            int count = Mathf.CeilToInt((matchRules != null ? matchRules.countdownSeconds : 3f) - elapsed);
            if (countdownText != null) countdownText.text = count > 0 ? count.ToString() : "GO";
        }

        public void EnableWeaponSelect(bool enabled)
        {
            if (weaponSelectPanel != null && weaponSelectPanel.activeSelf != enabled) weaponSelectPanel.SetActive(enabled);
        }

        public void OnMatchStart()
        {
            HideAllPanels();
            ShowCombatFeedback("GO — CAPTURE THE ZONE", Objective, 1.2f);
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
            if (nextZoneText != null && zone.HasValue && MatchView.Current == null)
                nextZoneText.text = $"NEXT: {zone.Value.ToString().ToUpperInvariant()}";
        }

        string resultsHeader = "";

        /// <summary>
        /// QA-017: the authoritative reward receipt (or a truthful pending state) under the score
        /// line of the results screen. Pass null to clear it.
        /// </summary>
        public void SetResultsReceipt(string receipt, Color color)
        {
            if (resultStatsText == null) return;
            resultStatsText.text = string.IsNullOrEmpty(receipt) ? resultsHeader
                : $"{resultsHeader}\n<size=85%><color=#{ColorUtility.ToHtmlStringRGB(color)}>{receipt}</color></size>";
        }

        public void ShowResults(bool isWin, bool isDraw, int scoreA, int scoreB) =>
            ShowResults(isWin, isDraw, scoreA, scoreB, null, "OFFLINE PRACTICE  ·  NO RATING OR CURRENCY");

        public void ShowResults(bool isWin, bool isDraw, int scoreA, int scoreB, System.Collections.Generic.IReadOnlyList<IMatchParticipant> players, string footer)
        {
            HideAllPanels();
            if (deathPanel != null) deathPanel.SetActive(false);
            if (objectiveMarker != null) objectiveMarker.gameObject.SetActive(false);
            // The match is over: combat controls and the zone banner would only clutter the results.
            SetZoneInfoVisible(false);
            if (touch != null) touch.enabled = false;
            if (resultsPanel != null) resultsPanel.SetActive(true);
            if (resultTitleText != null)
            {
                resultTitleText.text = isDraw ? "DRAW" : isWin ? "VICTORY" : "DEFEAT";
                resultTitleText.color = isDraw ? Objective : isWin ? Friendly : Enemy;
            }

            bool localIsA = localPlayer == null || localPlayer.TeamId == 0;
            int ally = localIsA ? scoreA : scoreB, enemy = localIsA ? scoreB : scoreA;
            resultsHeader = $"ALLY {ally}  —  {enemy} ENEMY\n<size=85%>{footer}</size>";
            if (resultStatsText != null) resultStatsText.text = resultsHeader;

            if (resultTableText == null || players == null) return;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>PLAYER                           ELIMS   DEATHS   ZONE TIME   LAUNCHES</b>\n");
            for (int team = 0; team < 2; team++)
            {
                int wanted = team == 0 ? (localIsA ? 0 : 1) : (localIsA ? 1 : 0);
                sb.Append(team == 0 ? "<color=#30D5E8>ALLY TEAM</color>\n" : "\n<color=#FF6376>ENEMY TEAM</color>\n");
                foreach (var p in players)
                {
                    if (p == null || p.TeamId != wanted) continue;
                    string name = ReferenceEquals(p, localPlayer) ? $"<b>{p.DisplayName} (YOU)</b>" : p.DisplayName;
                    sb.Append($"{name,-32}  {p.Eliminations,5}   {p.Deaths,6}   {Mathf.RoundToInt(p.ObjectiveSeconds),7}s   {p.PadLaunches,8}\n");
                }
            }
            if (localPlayer != null)
            {
                var bests = PersonalBests.Record(localPlayer);
                if (bests.Length > 0) sb.Append($"\n<color=#FFD166>NEW PERSONAL BEST: {bests}</color>");
            }
            resultTableText.text = sb.ToString();
        }

        public void UpdateTeammateStatus(int index, bool alive, float health)
        {
            if (index >= 0 && index < teammateSlots.Length && teammateSlots[index] != null)
                teammateSlots[index].UpdateStatus(alive, health);
        }

        void SetZoneInfoVisible(bool visible)
        {
            // Active/next zone texts share the ZoneInfo panel built by SceneSetup.
            var panel = activeZoneText != null ? activeZoneText.transform.parent : null;
            if (panel != null && panel != transform && panel.GetComponent<Canvas>() == null) panel.gameObject.SetActive(visible);
        }

        public void HideAllPanels()
        {
            if (preparationPanel != null) preparationPanel.SetActive(false);
            if (countdownPanel != null) countdownPanel.SetActive(false);
            if (weaponSelectPanel != null) weaponSelectPanel.SetActive(false);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (overtimePanel != null) overtimePanel.SetActive(false);
            if (deathPanel != null) deathPanel.SetActive(false);
        }

        [System.Serializable]
        public class TeammateStatusUI
        {
            public Image healthFill;
            public TextMeshProUGUI nameText;
            public GameObject deadOverlay;

            public void UpdateStatus(bool alive, float health)
            {
                if (healthFill != null) healthFill.fillAmount = alive ? health : 0f;
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

    /// <summary>Tiny runtime UI helpers shared by HUD pieces.</summary>
    public static class Ui
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Panel(string name, Transform parent, Vector2 size, Color color)
        {
            var rt = Rect(name, parent, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return rt;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, int size, Color color)
        {
            var rt = Rect(name, parent, new Vector2(400, size + 12));
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 12; tmp.fontSizeMax = size;
            return tmp;
        }
    }

    /// <summary>Spec section 18: personal bests (objective time, pad-assisted plays). Local only, no XP.</summary>
    public static class PersonalBests
    {
        public static string Record(IMatchParticipant player)
        {
            var sb = new System.Text.StringBuilder();
            Check("Best.ObjectiveSeconds", Mathf.RoundToInt(player.ObjectiveSeconds), "ZONE TIME", sb);
            Check("Best.PadLaunches", player.PadLaunches, "PAD LAUNCHES", sb);
            Check("Best.Eliminations", player.Eliminations, "ELIMINATIONS", sb);
            PlayerPrefs.Save();
            return sb.ToString();
        }

        static void Check(string key, int value, string label, System.Text.StringBuilder sb)
        {
            if (value <= 0 || value <= PlayerPrefs.GetInt(key, 0)) return;
            PlayerPrefs.SetInt(key, value);
            if (sb.Length > 0) sb.Append("  ·  ");
            sb.Append($"{label} {value}");
        }
    }
}



