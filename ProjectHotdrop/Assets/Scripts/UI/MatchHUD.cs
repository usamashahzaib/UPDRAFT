using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Config;
using Hotdrop.Core;
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
        static string L(string key, string fallback) => UiDraw.L("hud." + key, fallback);
        static string F(string key, string fallback, params object[] args) => UiDraw.LFormat("hud." + key, fallback, args);
        public static string Localized(string key, string fallback) => L(key, fallback);
        static string PadReasonKey(string value)
        {
            var key = new System.Text.StringBuilder();
            bool separator = false;
            foreach (char c in value ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c))
                {
                    if (separator && key.Length > 0) key.Append('.');
                    key.Append(char.ToLowerInvariant(c));
                    separator = false;
                }
                else separator = true;
            }
            return key.ToString();
        }
        public static string LocalizedPadReason(string reason) =>
            string.IsNullOrEmpty(reason) ? "" : L("pad.reject." + PadReasonKey(reason), reason);

        // UI foundation (Prompt 01): the HUD reads the central semantic tokens instead of
        // carrying its own literals. Field names are unchanged, so every existing caller -
        // scene wiring, HUD code and tests alike - keeps working.
        public static Color Friendly => UIDesignTokens.Ally;
        public static Color Enemy => UIDesignTokens.HazardCoral;
        public static Color Objective => UIDesignTokens.ObjectiveGold;
        public static Color MainText => UIDesignTokens.TextPrimary;
        public static Color SecondaryText => UIDesignTokens.TextSecondary;
        public static Color PanelBg => UIDesignTokens.PanelBg;
        const int HudCaptionFontSize = (int)UIDesignTokens.CaptionDp;
        const int HudValueFontSize = (int)UIDesignTokens.BodyLargeDp;
        const int HudEmphasisFontSize = (int)UIDesignTokens.TitleMediumDp;

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
        // This panel's rect overlaps weapon card 0's by 55 x 40 units. Measured and deliberately
        // left unguarded; the evidence and the one condition that would change that are recorded
        // in docs/audit/UIUX_PARITY_2026-10-02.md. Do not "fix" it here without reading that.
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
        System.Action<int> reportParticipantAction;
        System.Func<int, bool> reportParticipantEligible;
        System.Action<int> selectWeaponAction;
        bool hasMatch;
        float combatFeedbackUntil;
        WeaponDefinition[] weapons;
        int selectedPreparationWeapon;
        int lastKillerParticipantId = -1;
        int eliminationVictimParticipantId = -1;
        string eliminationVictimFallbackName = "";
        IReadOnlyList<IMatchParticipant> resultsParticipants;
        string resultsFooter = "", resultsNextGoal = "", resultsPersonalBest = "", resultsReceipt = "";
        Color resultsReceiptColor;
        int resultsAllyScore, resultsEnemyScore;

        // Runtime-built elements.
        RectTransform canvasRect;
        RectTransform objectiveMarker;
        TextMeshProUGUI objectiveMarkerText;
        Image objectiveMarkerBacking;
        PingSystem pingSystem;
        QuickChatWheelView quickChatWheel;

        /// <summary>Reused buffer for the panels currently on screen; see <see cref="ActivePanelRects"/>.</summary>
        readonly List<Rect> markerExclusions = new List<Rect>();

        /// <summary>
        /// Canvas-local Y that transient combat feedback is laid out at, above the marker band.
        ///
        /// <para><b>Why this is not a free choice.</b> The readable band runs to
        /// <c>canvas.yMax - height * 0.24</c>, which is 187.2 on the 720-unit canvas the 1920x1080
        /// capture renders, and the zone caption starts at 222. This value sits in the strip between
        /// them: its ink clears the band below by about 5.5 units and the zone caption above by about
        /// 4.7. Below the band, the toast's ink landed inside the only 40-unit gap a marker has on the
        /// preparation screen, and no placement existed that cleared both - the marker would have had
        /// to keep a position that overlapped the weapon panel. Laying the toast out here removes the
        /// contention instead of shrinking anything a player reads.</para>
        ///
        /// <para><c>TheToastBandClearsTheMarkerBand</c> pins the invariant.</para>
        /// </summary>
        public const float CombatFeedbackBandY = 205f;

        /// <summary>Half the measured ink height of the combat-feedback line at font size 22.</summary>
        public const float CombatFeedbackInkHalfHeight = 12.3f;

        void OnEnable() => StreamSafePresentation.Changed += RefreshLegacyResultsPrivacy;

        void OnDisable() => StreamSafePresentation.Changed -= RefreshLegacyResultsPrivacy;

        void RefreshLegacyResultsPrivacy()
        {
            if (resultsPanel == null || !resultsPanel.activeInHierarchy || resultsParticipants == null) return;
            RebuildLegacyResultsText();
        }

        /// <summary>
        /// The live footprint, in canvas-local units, of every HUD panel currently on screen.
        ///
        /// <para>Measured from the panels' own rects rather than assumed from constants, because
        /// the set of panels changes with the match phase. The fixed reserved bands in
        /// <see cref="HudMarkerLayer"/> describe the score bar and the touch controls; they know
        /// nothing about the weapon-select panel that occupies the lower middle during preparation,
        /// which is why an objective caption printed across its weapon names in the screen 07
        /// capture. Nothing here names a screen: a caller asks what is on screen now and
        /// <see cref="HudMarkerLayer.AvoidPanels"/> keeps markers off it.</para>
        /// </summary>
        public void ActivePanelRects(List<Rect> into)
        {
            if (into == null) return;
            into.Clear();
            if (canvasRect == null) return;
            foreach (var panel in new[] { weaponSelectPanel, preparationPanel, countdownPanel, overtimePanel, teammatePanel })
                AppendPanelRect(into, panel == null ? null : panel.transform);
            foreach (string name in HudAppearance.FloatingPanels)
                AppendPanelRect(into, canvasRect.Find(name));
            if (uiux != null)
            {
                overlayPanels.Clear();
                uiux.ReadablePanels(overlayPanels);
                foreach (var panel in overlayPanels) AppendPanelRect(into, panel);
            }
            // The transient combat-feedback channel is a real element a player reads, and it is not
            // a panel, so nothing else published its footprint. Measured at runtime y 97.71..122.29
            // in the screen 09 capture - inside the gap markers occupy - which is exactly the class of
            // overlap this list exists to prevent: something on screen the rule could not see.
            if (combatFeedbackText != null)
                AppendTextInkRect(into, combatFeedbackText);
        }

        /// <summary>
        /// Adds a text element's ink to <paramref name="into"/> rather than its container box.
        ///
        /// <para>Combat feedback is authored in a 700x140 box to hold a single 24.6-unit line. Taking
        /// the box would report 140 units of occupied screen for 25 units of readable text and leave
        /// markers nowhere to go; taking the ink measures what the player can actually read. Falls
        /// back to the container when the mesh has not been generated yet, so the measurement is
        /// never silently empty.</para>
        /// </summary>
        void AppendTextInkRect(List<Rect> into, TMPro.TMP_Text text)
        {
            if (text == null || !text.gameObject.activeInHierarchy) return;
            var rect = text.rectTransform;
            var bounds = text.textBounds;
            if (bounds.size.sqrMagnitude <= 0f) { AppendPanelRect(into, rect); return; }
            Vector3 min = rect.TransformPoint(bounds.min);
            Vector3 max = rect.TransformPoint(bounds.max);
            var a = canvasRect.InverseTransformPoint(min);
            var b = canvasRect.InverseTransformPoint(max);
            into.Add(Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
                Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)));
        }

        /// <summary>
        /// Adds one panel's live footprint to <paramref name="into"/>, skipping panels that are
        /// hidden or are not a RectTransform. Takes a <see cref="Transform"/> so both a direct
        /// GameObject field and a <c>RectTransform.Find</c> result can be measured the same way.
        /// </summary>
        readonly List<RectTransform> overlayPanels = new List<RectTransform>();

        void AppendPanelRect(List<Rect> into, Transform panel)
        {
            if (panel == null || !panel.gameObject.activeInHierarchy) return;
            var rect = panel as RectTransform;
            if (rect == null) return;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var local = canvasRect.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x); maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y); maxY = Mathf.Max(maxY, local.y);
            }
            into.Add(new Rect(minX, minY, maxX - minX, maxY - minY));
        }
        GameObject deathPanel;
        TextMeshProUGUI deathTitle, deathCountdown, deathSpectate;
        string lastAttackDirection = "";
        TextMeshProUGUI healthValueText;
        UiuxHud uiux;

        /// <summary>The UIUX screen-08 HUD overlay this class feeds.</summary>
        public UiuxHud Overlay => uiux;
        TextMeshProUGUI padStatusText;
        TextMeshProUGUI resultTableText;
        TextMeshProUGUI hitMarkerText;
        float hitMarkerUntil;
        string lastKillerName = "";
        string lastWeaponName = "";

        public bool IsDeathPanelVisible => deathPanel != null && deathPanel.activeSelf;

        void Awake()
        {
            if (matchRules == null) matchRules = Resources.Load<MatchRulesConfig>("Config/MatchRulesConfig");
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
            damageFeedback = DamageFeedbackView.Create(transform, canvasRect, matchRules);
            flightSpeedLines = FlightSpeedLinesView.Create(transform, canvasRect);
            BuildUiuxOverlay();
            // Appearance and marker projection each have exactly one owner; this class asks,
            // it does not decide. See HudAppearance / HudMarkerLayer.
            HudAppearance.ApplyFloatingPanels(canvasRect);
            UiFloor.Apply(transform); // QA-019/020: >= 48 dp controls and ~12 dp text on every HUD element
            HideAllPanels();
        }

        void BuildRuntimeElements()
        {
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (GetComponent<PingSystem>() == null) gameObject.AddComponent<PingSystem>();
            pingSystem = GetComponent<PingSystem>();// Objective edge marker: a diamond plus zone name and distance, laid out on one row
            quickChatWheel = GetComponent<QuickChatWheelView>();
            if (quickChatWheel == null) quickChatWheel = gameObject.AddComponent<QuickChatWheelView>();
            // inside HudMarkerLayer.MarkerFootprintSize. Icon-above-caption made the marker 74 units
            // tall, and the preparation screen's only free gap inside the readable band is 40, so no
            // placement rule could ever have cleared it. One row is 36 and fits; the panel geometry
            // is untouched and no rule names a screen.
            var marker = Ui.Rect("ObjectiveMarker", transform, HudMarkerLayer.MarkerFootprintSize);
            objectiveMarker = marker;
            // UIUX objective reticle inside the one-row footprint: a 34 px gold ring (gold/20 fill,
            // 2 px gold/90 rim, flag glyph) and a mono label chip on a #1b2940/90 plate with a
            // gold/50 rail. The chip is the world-anchored caption's legibility plate.
            // High-contrast option: an opaque profile-background disc under the ring, inside the
            // ring's own 34 px footprint, so the cue holds against a bright sky. Off by default.
            objectiveMarkerBacking = UiDraw.Dot(marker, "HighContrastBacking", 34f, UIDesignTokens.WithAlpha(UIDesignTokens.Background, 1f));
            objectiveMarkerBacking.rectTransform.anchoredPosition = new Vector2(HudMarkerLayer.MarkerIconCenterX, 0);
            objectiveMarkerBacking.gameObject.SetActive(false);
            var ring = UiDraw.Ring(marker, "Diamond", 34f, 2f, UiDraw.Hex("#ffd166", .9f));
            ring.rectTransform.anchoredPosition = new Vector2(HudMarkerLayer.MarkerIconCenterX, 0);
            var ringFill = UiDraw.Dot(ring.transform, "Fill", 32f, UiDraw.Hex("#ffd166", .2f));
            ringFill.rectTransform.anchoredPosition = Vector2.zero;
            ringFill.transform.SetAsFirstSibling();
            UiDraw.Icon(ring.transform, "Flag", "flag", 18f, UiDraw.Hex("#ffd166")).rectTransform.anchoredPosition = Vector2.zero;
            var objectivePlateImage = UiDraw.Box(marker, "Plate", UiDraw.Hex("#1b2940", .9f), 4f, UiDraw.Hex("#ffd166", .5f));
            var objectivePlate = objectivePlateImage.rectTransform;
            objectivePlate.anchoredPosition = new Vector2(HudMarkerLayer.MarkerCaptionCenterX, 0);
            objectivePlate.sizeDelta = new Vector2(HudMarkerLayer.MarkerCaptionSize.x, 20f);
            objectiveMarkerText = UiDraw.Text(marker, "Label", "", UiFont.Mono, HudCaptionFontSize, UiDraw.Hex("#ffd166"), .05f, TextAlignmentOptions.Midline);
            objectiveMarkerText.rectTransform.anchoredPosition = new Vector2(HudMarkerLayer.MarkerCaptionCenterX, 0);
            objectiveMarkerText.rectTransform.sizeDelta = HudMarkerLayer.MarkerCaptionSize;
            marker.gameObject.SetActive(false);

            // Death / respawn panel.
            var death = Ui.Panel("DeathPanel", transform, new Vector2(760, 150), PanelBg);
            death.anchorMin = death.anchorMax = new Vector2(0.5f, 0.5f);
            death.anchoredPosition = new Vector2(0, 150); // stays on screen on the density-scaled canvas
            deathPanel = death.gameObject;
            deathTitle = Ui.Text("Title", death, "", HudEmphasisFontSize, Enemy); deathTitle.rectTransform.anchoredPosition = new Vector2(0, 40);
            deathCountdown = Ui.Text("Countdown", death, "", HudEmphasisFontSize, MainText); deathCountdown.rectTransform.anchoredPosition = new Vector2(0, 0);
            deathSpectate = Ui.Text("Spectate", death, "", HudValueFontSize, SecondaryText); deathSpectate.rectTransform.anchoredPosition = new Vector2(0, -40);
            foreach (var t in new[] { deathTitle, deathCountdown, deathSpectate }) t.rectTransform.sizeDelta = new Vector2(740, 36);
            deathPanel.SetActive(false);

            // Numeric HP next to the bar.
            if (healthSlider != null)
            {
                healthValueText = Ui.Text("HPValue", healthSlider.transform, "100", HudValueFontSize, MainText);
                // Sit clear above the bar (it overlapped the bar edge on 720p phones) and read over bright floors.
                healthValueText.rectTransform.anchoredPosition = new Vector2(0, 36);
                healthValueText.fontStyle = FontStyles.Bold;
                healthValueText.outlineWidth = 0.25f;
                healthValueText.outlineColor = new Color32(10, 16, 28, 255);
                healthValueText.rectTransform.sizeDelta = new Vector2(200, 28);
            }

            // Hit marker at the crosshair.
            hitMarkerText = Ui.Text("HitMarker", transform, "X", HudEmphasisFontSize, MainText);
            UIFonts.Use(hitMarkerText, UiFont.IconFilled);
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
                    // 240 tall, not 200: the stat block needs three lines when a weapon adds a
                    // pellet count ("9 DMG - 8 PELLETS - 75 RPM" measured 279 wide against a 270
                    // card), and 200 could not hold title + role + stats with any margin. Still
                    // inside the 900x260 panel (-180..60), so the panel the marker rule measures
                    // is unchanged and the 40-unit marker gap is unaffected.
                    brt.sizeDelta = new Vector2(270, 240);
                    brt.anchoredPosition = new Vector2(-290 + i * 290, 0);
                    // Card stack, laid out from the card's own measured interior (y -160..+40 at
                    // 1920x1080). Before this the title sat high with ~35 units of dead space above
                    // it while the stat block hung 9.25 units below the card's bottom border - the
                    // numbers a player picks a weapon by rendered outside the card they belong to.
                    // Keep title, role and wrapped stats inside the card with separation between
                    // blocks. Typography uses the Preparation tokens below, so long configured
                    // values wrap without shrinking below the 12 dp caption floor.
                    if (b.nameText != null)
                    {
                        b.nameText.rectTransform.anchoredPosition = new Vector2(0, 87.5f);
                        b.nameText.rectTransform.sizeDelta = new Vector2(250, 50);
                        b.nameText.fontSize = UiMetrics.ScaleTextSize(HudEmphasisFontSize);
                        b.nameText.enableAutoSizing = true;
                        b.nameText.fontSizeMin = b.nameText.fontSizeMax = UiMetrics.ScaleTextSize(HudEmphasisFontSize);
                    }
                    if (b.roleText == null) b.roleText = Ui.Text("Role", b.button.transform, "", HudCaptionFontSize, SecondaryText);
                    // Applied whether or not the text was just created: a scene-authored Role child
                    // must get the same layout as a code-built one, or this file would only own half
                    // of the card it is responsible for.
                    b.roleText.rectTransform.anchoredPosition = new Vector2(0, 23);
                    b.roleText.rectTransform.sizeDelta = new Vector2(250, 100);
                    b.roleText.textWrappingMode = TextWrappingModes.Normal;
                    b.roleText.fontSize = UiMetrics.ScaleTextSize(HudCaptionFontSize);
                    b.roleText.enableAutoSizing = true;
                    b.roleText.fontSizeMin = b.roleText.fontSizeMax = UiMetrics.ScaleTextSize(HudCaptionFontSize);
                    // Values are filled directly from WeaponDefinition; never use a made-up power bar.
                    b.statsText = Ui.Text("Stats", b.button.transform, "", HudCaptionFontSize, SecondaryText);
                    // Wrapping is what keeps a long stat line inside the card. Disabling it let
                    // "9 DMG - 8 PELLETS - 75 RPM" render 279 units wide across a 270-unit card.
                    // Wrapping keeps every number at the same size rather than shrinking it to fit.
                    b.statsText.textWrappingMode = TextWrappingModes.Normal;
                    b.statsText.rectTransform.anchoredPosition = new Vector2(0, -53);
                    // Tall enough for the wrapped three lines at the current font, so autosizing has
                    // no reason to reduce the size.
                    b.statsText.rectTransform.sizeDelta = new Vector2(250, 68);
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
                    preparationText.fontSize = UiMetrics.ScaleTextSize(HudCaptionFontSize);
                    preparationText.enableAutoSizing = true;
                    preparationText.fontSizeMin = preparationText.fontSizeMax = UiMetrics.ScaleTextSize(HudCaptionFontSize);
                }
            }
            padStatusText = padChargeText;
            if (countdownText != null) countdownText.rectTransform.sizeDelta = new Vector2(200, 180);
            if (scoreAText != null && scoreBText != null)
            {
                // "ALLY 120" / "120 ENEMY" need room: widen the scoreboard and pin labels to its edges.
                var board = (RectTransform)scoreAText.transform.parent;
                board.sizeDelta = new Vector2(520, 64); board.anchoredPosition = new Vector2(0, -40);
                scoreAText.rectTransform.sizeDelta = scoreBText.rectTransform.sizeDelta = new Vector2(180, 36);
                scoreAText.rectTransform.anchoredPosition = new Vector2(-150, 4);
                scoreBText.rectTransform.anchoredPosition = new Vector2(150, 4);
                scoreAText.fontSize = scoreBText.fontSize = 24;
                scoreAText.fontSizeMax = scoreBText.fontSizeMax = 24;
                scoreAText.alignment = TextAlignmentOptions.Left;
                scoreBText.alignment = TextAlignmentOptions.Right;
                if (timerText != null)
                {
                    timerText.rectTransform.anchoredPosition = new Vector2(0, 4);
                    timerText.rectTransform.sizeDelta = new Vector2(100, 36);
                    timerText.fontSize = timerText.fontSizeMax = 24;
                }
            }
            if (activeZoneText != null)
            {
                var zone = (RectTransform)activeZoneText.transform.parent;
                zone.anchoredPosition = new Vector2(0, -108); zone.sizeDelta = new Vector2(340, 60);
                activeZoneText.rectTransform.sizeDelta = new Vector2(330, 30); activeZoneText.fontSizeMin = 16;
            }
            if (nextZoneText != null) { nextZoneText.rectTransform.sizeDelta = new Vector2(290, 26); nextZoneText.fontSizeMin = 14; }
            foreach (var slot in teammateSlots)
                if (slot?.nameText != null)
                {
                    slot.nameText.textWrappingMode = TextWrappingModes.NoWrap;
                    slot.nameText.overflowMode = TextOverflowModes.Ellipsis;
                }
            if (combatFeedbackText != null)
            {
                combatFeedbackText.rectTransform.anchoredPosition = new Vector2(0, CombatFeedbackBandY);
                UIFonts.Use(combatFeedbackText, UiFont.Heading);
                combatFeedbackText.fontSize = combatFeedbackText.fontSizeMax = UiMetrics.ScaleTextSize(HudValueFontSize);
                combatFeedbackText.characterSpacing = 8f;
                // No plate (it must not read as a panel), but a soft dark underlay keeps the line
                // readable over bright cladding and sky.
                var feedbackMaterial = combatFeedbackText.fontMaterial;
                feedbackMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                feedbackMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.02f, 0.04f, 0.08f, .85f));
                feedbackMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, .55f);
                feedbackMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, .35f);
            }
            foreach (var t in new[] { deathTitle, deathCountdown, deathSpectate })
                if (t != null) UIFonts.Use(t, t == deathTitle ? UiFont.Heading : UiFont.MonoMedium);
        }

        /// <summary>
        /// Screen 08's HUD replaces the scene-authored scoreboard, zone panel, teammate list and
        /// health slider. Those objects stay (tests and other screens hold references) but are
        /// never shown; every value they carried is pushed to the overlay instead.
        /// </summary>
        void BuildUiuxOverlay()
        {
            uiux = UiuxHud.Create(transform);
            if (scoreAText != null && scoreAText.transform.parent != transform) scoreAText.transform.parent.gameObject.SetActive(false);
            if (activeZoneText != null && activeZoneText.transform.parent != transform) activeZoneText.transform.parent.gameObject.SetActive(false);
            if (teammatePanel != null) teammatePanel.SetActive(false);
            if (healthSlider != null) healthSlider.gameObject.SetActive(false);
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
            player.SetMatchRulesConfig(matchRules);
            Bind(player, player.touchInput, weaponDefinitions ?? controller?.weaponDefinitions, controller != null,
                controller != null ? controller.OnPlayAgain : null,
                controller != null ? controller.OnReturnHome : null,
                controller != null ? controller.SetLocalWeapon : null);
            if (touch != null) touch.QuickChatEnabled = false;
            UpdateHealth(player.CurrentHealth / player.MaxHealth);
            UpdateAmmo(player.CurrentAmmo, player.MaxAmmo);
            HighlightWeapon(System.Array.IndexOf(weapons ?? new WeaponDefinition[0], player.CurrentWeapon));
        }

        /// <summary>Networked match: same HUD, actions routed to the network session.</summary>
        public void InitializeNetwork(IMatchParticipant local, Hotdrop.Input.TouchInputManager touchInput, WeaponDefinition[] weaponDefinitions,
            System.Action playAgain, System.Action home, System.Action<int> selectWeapon,
            System.Action<int> reportParticipant = null, System.Func<int, bool> reportParticipantEligible = null)
        {
            Bind(local, touchInput, weaponDefinitions, true, playAgain, home, selectWeapon,
                reportParticipant, reportParticipantEligible);
            if (touch != null) touch.QuickChatEnabled = QuickChatWheelView.AvailabilityGateOpen;
            UpdateHealth(local.HealthFraction);
        }

        void Bind(IMatchParticipant local, Hotdrop.Input.TouchInputManager touchInput, WeaponDefinition[] weaponDefinitions, bool match,
            System.Action playAgain, System.Action home, System.Action<int> selectWeapon,
            System.Action<int> reportParticipant = null, System.Func<int, bool> reportParticipantEligible = null)
        {
            localPlayer = local;
            if (touch != null) touch.OnQuickChatRequested -= OpenQuickChatWheel;
            touch = touchInput;
            if (touch != null) touch.OnQuickChatRequested += OpenQuickChatWheel;
            weapons = weaponDefinitions;
            hasMatch = match;
            playAgainAction = playAgain;
            homeAction = home;
            reportParticipantAction = reportParticipant;
            this.reportParticipantEligible = reportParticipantEligible;
            selectWeaponAction = selectWeapon;
            if (teammatePanel != null) teammatePanel.SetActive(false);
            SetZoneInfoVisible(false);
            if (uiux != null)
            {
                uiux.Local = local;
                uiux.Touch = touchInput;
                uiux.SetLiveMatch(match);
            }
            if (touchInput != null) { touchInput.PingEnabled = match; touchInput.RecalculateForComposedFrame(); }
            if (!match)
            {
                // Training has no score, clock or rotation.
                if (activeZoneText != null) { activeZoneText.text = L("training", "Training"); activeZoneText.color = Objective; }
                if (nextZoneText != null) nextZoneText.text = "";
                if (timerText != null) timerText.text = "";
                if (scoreAText != null) scoreAText.text = "";
                if (scoreBText != null) scoreBText.text = "";
            }
            if (weapons != null)
                for (int i = 0; i < weaponButtons.Length && i < weapons.Length; i++)
                {
                    if (weaponButtons[i]?.nameText != null) weaponButtons[i].nameText.text = PreparationView.WeaponName(weapons[i]);
                    if (weaponButtons[i]?.roleText != null) weaponButtons[i].roleText.text = PreparationView.Role(weapons[i]);
                    if (weaponButtons[i]?.statsText != null)
                    {
                        var w = weapons[i];
                        weaponButtons[i].statsText.text = FormatLoadoutStats(w);
                    }
                }
        }

        void OpenQuickChatWheel()
        {
            if (!hasMatch || touch == null || !touch.QuickChatEnabled || !QuickChatWheelView.AvailabilityGateOpen) return;
            quickChatWheel?.Open();
        }

        public void HighlightWeapon(int index)
        {
            selectedPreparationWeapon = index;
            PreparationView.SetSelection(this, index);
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                var image = weaponButtons[i]?.button != null ? weaponButtons[i].button.GetComponent<Image>() : null;
                if (image == null) continue;
                bool selected = i == index;
                image.color = selected ? UIDesignTokens.WithAlpha(UIDesignTokens.PrimaryAction, 0.35f) : PanelBg;
                if (weaponButtons[i].nameText != null)
                    weaponButtons[i].nameText.text = weapons != null && i < weapons.Length
                        ? (selected ? "> " : "") + PreparationView.WeaponName(weapons[i]) + (selected ? " <" : "")
                        : weaponButtons[i].nameText.text;
            }
        }

        void Update()
        {
            if (combatFeedbackText != null && combatFeedbackText.gameObject.activeSelf)
            {
                if (Time.unscaledTime >= combatFeedbackUntil)
                    HideCombatFeedback();
                else if (eliminationVictimParticipantId >= 0)
                    combatFeedbackText.text = F("combat.eliminatedTemplate", "Eliminated {0}", ResolveParticipantDisplayName(eliminationVictimParticipantId, eliminationVictimFallbackName));
            }
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

        /// <summary>
        /// Re-places every world-anchored marker against the canvas as it is right now.
        ///
        /// <para><b>Why this exists.</b> Markers are clamped in <c>LateUpdate</c> against the
        /// canvas rect at that moment. When the canvas geometry changes without a frame boundary
        /// — a resolution change, a safe-area inset, or the capture harness re-scaling the canvas
        /// before rendering — the markers keep coordinates that were valid for the old, larger
        /// canvas and can end up inside the reserved band. On a device the next <c>LateUpdate</c>
        /// papers over it within a frame; anything that re-renders mid-frame must ask for this
        /// explicitly instead.</para>
        /// </summary>
        public void RefreshHudMarkers()
        {
            // Panels first: markers avoid the panels' measured rects, so those must be at device size.
            if (uiux != null) uiux.RefreshPhysicalScale();
            UiDpScaler.RefreshAll();
            UpdateObjectiveMarker();
            var pings = GetComponent<PingSystem>();
            if (pings != null) pings.RefreshMarkers();
            ResolveMarkerCollisions();
        }

        /// <summary>
        /// Keeps the objective caption clear of the ping captions placed this frame.
        ///
        /// <para>Runs last so it sees the pings' final slots. The bands in <see cref="HudMarkerLayer"/>
        /// stop a marker landing on HUD chrome but not on another marker, and the capture shows why:
        /// an objective 34 m out and an enemy ping 52 m out projected close enough to print
        /// <c>ENEMY HERE 52m</c> and <c>CENTER 34m</c> through one another. Idempotent, so a frame
        /// that resolves twice cannot walk the caption further than the first pass did. Unity does
        /// not order this component's <c>LateUpdate</c> against the ping system's, so on a device the
        /// worst case is that it reads the previous frame's slots for one frame; nothing moves twice
        /// in a frame and nothing escapes the band either way.
        /// </summary>
        void ResolveMarkerCollisions()
        {
            if (objectiveMarker == null || pingSystem == null || canvasRect == null) return;
            if (!objectiveMarker.gameObject.activeSelf) return;
            var taken = pingSystem.PlacedMarkers;
            if (taken == null || taken.Count == 0) return;
            ActivePanelRects(markerExclusions);
            objectiveMarker.localPosition =
                HudMarkerLayer.SeparateFromPlaced(objectiveMarker.localPosition, taken, canvasRect.rect,
                    HudMarkerLayer.MarkerRowHeight, markerExclusions);
        }

        void LateUpdate() => ResolveMarkerCollisions();

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

            Vector2 local;
            if (HudMarkerLayer.ProjectToCanvas(mainCamera, canvasRect, world, out var projected))
            {
                // On-screen: clamp into the same reserved band the pings use.
                local = HudMarkerLayer.ClampToReadableBand(projected, canvasRect.rect);
            }
            else
            {
                // Behind the camera: the target is somewhere the player cannot see, so the
                // marker points at the safe edge in the direction the objective actually lies.
                // Direction comes from the camera's own axes, and the edge comes from SafeAreaRoot
                // - the same screen size, OS intrusions and dp reserves every other HUD element is
                // laid out from. This branch used to read Screen.safeArea and Screen.width itself
                // and convert screen units back into canvas units, which gave it a second copy of
                // where the screen is. That copy ignored the edge margin and the camera cutout, and
                // it is why the parity capture could not drive this branch: it simulates the screen
                // through SafeAreaRoot, which this path never consulted.
                Vector3 toObjective = mainCamera.transform.InverseTransformPoint(world);
                Rect content = SafeAreaRoot.ContentRectFor(canvasRect.rect);
                // The padding is HudMarkerLayer's number, not this component's. It used to be a
                // serialized field of the HUD, which meant the same quantity existed twice with
                // two values - 60 in code and 50 baked into Main.unity - and the scene file won,
                // so the edge the player sees was not the edge the placement rules are measured
                // against. One owner, one number, whatever the scene says.
                float padding = HudMarkerLayer.EdgePaddingReference * (canvasRect.rect.height / 1080f);
                local = HudMarkerLayer.ClampToEdgeBand(
                    content.center + new Vector2(toObjective.x, toObjective.y), content, padding);
            }

            // One placement policy for both paths, deliberately outside the branch above: the same
            // measured panels and the same required clearance whether the objective is on screen or
            // behind the player. This used to live inside the on-screen branch, which left the
            // edge marker - the state you are actually in for most of a match - with no panel
            // avoidance at all, able to sit on the teammate or weapon panel.
            ActivePanelRects(markerExclusions);
            local = HudMarkerLayer.AvoidPanels(local, markerExclusions, canvasRect.rect,
                HudMarkerLayer.MarkerRowHeight,
                pingSystem != null ? pingSystem.PlacedMarkers : null);

            // The loadout surface can occupy every readable placement during preparation. The
            // header already names the opening objective, so suppress the world marker when no
            // collision-free slot exists instead of drawing it across the weapon cards.
            if (HudMarkerLayer.ClearanceAt(local, markerExclusions,
                    pingSystem != null ? pingSystem.PlacedMarkers : null) < HudMarkerLayer.MarkerClearance)
            {
                objectiveMarker.gameObject.SetActive(false);
                return;
            }

            objectiveMarker.gameObject.SetActive(true);
            objectiveMarker.localPosition = local;
            if (objectiveMarkerBacking != null)
            {
                bool highContrast = PlayerPrefs.GetInt(Hotdrop.Core.OfflinePauseMenu.HighContrastObjectiveMarkerKey, 0) == 1;
                if (objectiveMarkerBacking.gameObject.activeSelf != highContrast) objectiveMarkerBacking.gameObject.SetActive(highContrast);
                // The palette follows the presentation profile, which can change mid-session.
                if (highContrast) objectiveMarkerBacking.color = UIDesignTokens.WithAlpha(UIDesignTokens.Background, 1f);
            }
            float distance = Vector3.Distance(localPlayer.Position, match.ActiveObjectivePosition);
            string objectiveLabel = match is IObjectivePresentation objective ? objective.ActiveObjectiveLabel : PreparationView.ZoneShortLabel(match.ScoringZone);
            objectiveMarkerText.text = F("objective.markerTemplate", "{0} • {1:0}m", objectiveLabel, distance);
            var plate = objectiveMarker.Find("Plate") as RectTransform;
            if (plate != null) plate.sizeDelta = new Vector2(Mathf.Min(HudMarkerLayer.MarkerCaptionSize.x, UiDraw.Width(objectiveMarkerText) + 20f), 20f);
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
                if (ui.nameText != null) ui.nameText.text = p.IsAlive ? p.DisplayName
                    : F("teammate.downTemplate", "{0}  DOWN {1:0}s", p.DisplayName, p.RespawnRemaining);
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
            string killerName = ResolveLastKillerName();
            deathTitle.text = string.IsNullOrEmpty(killerName)
                ? L("death.leftArena", "ELIMINATED — LEFT THE ARENA")
                : F("death.byTemplate", "ELIMINATED BY {0}  ·  {1}{2}", killerName, lastWeaponName,
                    string.IsNullOrEmpty(lastAttackDirection) ? "" : F("death.fromTemplate", "  ·  FROM {0}", lastAttackDirection));
            int seconds = Mathf.CeilToInt(localPlayer.RespawnRemaining);
            deathCountdown.text = hasMatch ? F("death.respawnWithLoadoutTemplate", "RESPAWN IN {0}   ·   CHANGE WEAPON BELOW", seconds)
                : F("death.respawnTemplate", "Respawn in {0}", seconds);
            var spectated = MatchView.Current?.SpectatedTeammate;
            deathSpectate.text = !hasMatch ? L("death.trainingTip", "TIP: STRAFE WHILE FIRING AND FIGHT FROM COVER — OPPONENTS AIM AT YOUR LAST POSITION")
                : spectated != null ? F("death.watchingTemplate", "Watching {0}", spectated.DisplayName) : "";
        }

        public void UpdateScores(int scoreA, int scoreB)
        {
            // Local team always on the left in friendly colour.
            bool localIsA = localPlayer == null || localPlayer.TeamId == 0;
            var lines = ScoreLines(localIsA, scoreA, scoreB);
            if (scoreAText != null) { scoreAText.text = lines.Left; scoreAText.color = Friendly; }
            if (scoreBText != null) { scoreBText.text = lines.Right; scoreBText.color = Enemy; }
            uiux?.SetScores(localIsA ? scoreA : scoreB, localIsA ? scoreB : scoreA);
        }

        /// <summary>
        /// The score bar's two captions, as (left, right).
        ///
        /// Pure so the wording can be asserted. Observed in the 1920x1080 capture: the bar read
        /// "ALLY 0" on the left and "0 ENEMY" on the right — the word order flipped between the
        /// sides and neither side named a team, so in a 3v3 "ENEMY" implied an opponent the
        /// player is actually fighting alongside. Both sides now follow one convention (name
        /// outside, score adjacent to the clock) and the local team is explicitly marked, so
        /// there is never a moment of "which one is me?".
        /// </summary>
        public static (string Left, string Right) ScoreLines(bool localIsA, int scoreA, int scoreB)
        {
            int ally = localIsA ? scoreA : scoreB;
            int rival = localIsA ? scoreB : scoreA;
            string allyName = localIsA ? L("score.teamA", "Team A") : L("score.teamB", "Team B");
            string rivalName = localIsA ? L("score.teamB", "Team B") : L("score.teamA", "Team A");
            return (F("score.allyTemplate", "{0} · YOU  {1}", allyName, ally),
                F("score.rivalTemplate", "{0}  {1}", rival, rivalName));
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
            timerText.text = (prefix.Length > 0 ? L("clock.overtimePrefix", "OT ") : "") + $"{total / 60}:{total % 60:00}";
            uiux?.SetClock(remaining, prefix.Length > 0);
        }

        DamageFeedbackView damageFeedback;
        FlightSpeedLinesView flightSpeedLines;
        public DamageFeedbackView DamageFeedback => damageFeedback;

        /// <summary>Bible 2.5 directional hit arc. The argument is a world-space direction from the local player toward the attacker.</summary>
        public void ShowDamageFrom(Vector3 directionToAttacker)
        {
            if (damageFeedback == null || mainCamera == null) return;
            damageFeedback.SetRules(matchRules);
            damageFeedback.ShowHit(mainCamera.transform.forward, directionToAttacker);
        }

        public void UpdateHealth(float normalizedHealth)
        {
            if (damageFeedback != null) { damageFeedback.SetRules(matchRules); damageFeedback.SetHealth(normalizedHealth); }
            if (healthSlider != null) healthSlider.value = normalizedHealth;
            uiux?.SetHealth(normalizedHealth);
            if (healthFill != null && healthGradient != null) healthFill.color = healthGradient.Evaluate(normalizedHealth);
            if (healthValueText != null)
            {
                int hp = Mathf.CeilToInt(normalizedHealth * 100f); // spec: 100 HP for everyone
                bool lowHealth = matchRules != null && normalizedHealth <= matchRules.lowHealthThresholdFraction;
                healthValueText.text = lowHealth ? F("health.lowTemplate", "Low HP {0}", hp) : hp.ToString();
                healthValueText.color = lowHealth ? Enemy : MainText;
            }
        }

        /// <summary>Bible 2.5 peripheral flight lines, driven by local planar speed.</summary>
        public void UpdateFlightMotion(float planarSpeed, bool airborne)
            => flightSpeedLines?.SetFlightMotion(planarSpeed, airborne, matchRules);

        public void UpdateAmmo(int current, int max)
            => UpdateAmmo(current, max, false, 0f);

        public void UpdateAmmo(int current, int max, bool reloading, float reloadProgress)
        {
            string label = reloading ? F("ammo.reloadingTemplate", "Reloading {0}%", Mathf.RoundToInt(Mathf.Clamp01(reloadProgress) * 100f)) : $"{current}/{max}";
            if (ammoText != null)
            {
                ammoText.text = label;
                // Ammo is drawn on the fire control itself so it never overlaps the button.
                var panel = ammoText.transform.parent;
                if (panel != null && panel != transform) panel.gameObject.SetActive(false);
            }
            if (touch != null)
            {
                touch.AmmoLabel = label;
                var weapon = localPlayer is PlayerController pc ? pc.CurrentWeapon : null;
                string shortName = weapon != null ? PreparationView.WeaponShortName(weapon) : "";
                touch.WeaponLabel = shortName.Length > 0 ? $"{shortName} {max}" : "";
            }
            if (ammoProgressFill != null) ammoProgressFill.fillAmount = max > 0 ? (float)current / max : 0f;
            if (reloadIndicator != null) reloadIndicator.SetActive(reloading);
        }

        /// <summary>Mirrors the actual pad state and health; this UI owns no pad timers or rules.</summary>
        public void UpdatePadState(LaunchPad pad)
        {
            if (pad == null) return;
            bool friendly = localPlayer == null || pad.TeamId == localPlayer.TeamId;
            string state = pad.State == PadState.Arming ? L("pad.arming", "Arming") :
                pad.State == PadState.Armed ? L("pad.armed", "ARMED — ANYONE CAN LAUNCH") :
                pad.State == PadState.Destroyed ? L("pad.destroyed", "Destroyed") : L("pad.expired", "Expired");
            string health = pad.State == PadState.Arming || pad.State == PadState.Armed
                ? F("pad.healthSuffix", "  ·  {0} HP", Mathf.CeilToInt(Mathf.Max(0f, pad.Health))) : "";
            string text = F("pad.statusTemplate", "{0} PAD  ·  {1}{2}", friendly ? L("team.ally", "Ally") : L("team.enemy", "Enemy"), state, health);
            if (padStatusText != null) padStatusText.text = text;
            if (touch != null) touch.PadStatusLabel = text;
            if (padButtonFill != null) padButtonFill.color = UIDesignTokens.TextPrimary;
        }

        /// <summary>Preparation-card copy sourced exclusively from the selected weapon config.</summary>
        public static string FormatLoadoutStats(WeaponDefinition weapon)
        {
            if (weapon == null) return "";
            string pellets = weapon.pelletsPerShot > 1 ? F("loadout.pelletsTemplate", "  ·  {0} PELLETS", weapon.pelletsPerShot) : "";
            string damage = F("loadout.damageTemplate", "{0:0} DMG{1}", weapon.bodyDamage, pellets);
            // A pellet weapon's damage row does not fit one 250-unit line - "9 DMG  ·  8 PELLETS  ·
            // 75 RPM" measured 279 wide against a 250 box and 270 card. Letting it wrap split
            // "75 RPM" across two lines, and a fire rate whose unit is separated from its number is
            // easy to misread, which is worse than a line that runs long. Break at the real boundary
            // instead: the pellet count belongs with the damage it modifies, so the rate moves to
            // its own line. The card has room for the extra row (measured ink 63.8 in a 68 box).
            string damageRow = weapon.pelletsPerShot > 1
                ? F("loadout.damageAndRateTemplate", "{0}\n{1:0} RPM", damage, weapon.roundsPerMinute)
                : F("loadout.damageAndRateTemplate", "{0}  ·  {1:0} RPM", damage, weapon.roundsPerMinute);
            return F("loadout.magReloadTemplate", "{0}\n{1} MAG  ·  {2:0.0}s RELOAD", damageRow, weapon.magazineSize, weapon.reloadSeconds);
        }

        /// <summary>Shared copy makes the pre-match restrictions explicit without inventing a gameplay rule.</summary>
        public static string PreparationCopy(float seconds) =>
            F("preparationTemplate", "LOADOUT  ·  {0:0}s\nSPAWN LOCKED  ·  DAMAGE BLOCKED  ·  PADS UNAVAILABLE", seconds);

        public void ShowPadReady() => ShowCombatFeedback(L("combat.padReady", "Pad Ready"), Objective, 0.8f);

        public void ShowPadActivated() => ShowCombatFeedback(L("combat.launch", "Launch"), Objective, 0.5f);

        public void ShowPadRejected(string reason)
        {
            GameAudio.Play2D(GameCue.UiNegative);
            ShowCombatFeedback(F("combat.padRejectedTemplate", "CAN'T PLACE PAD — {0}",
                LocalizedPadReason(reason)), SecondaryText, 1.2f);
        }

        public void ShowBlockedShot() => ShowCombatFeedback(L("combat.shotBlocked", "Shot Blocked"), UIDesignTokens.SignalWarning, 0.7f);

        public void ShowHitConfirmed(bool headshot = false, bool elimination = false)
        {
            if (hitMarkerText == null) return;
            hitMarkerText.text = elimination ? UIIcons.Glyph("stars") : headshot ? UIIcons.Glyph("diamond") : "X";
            hitMarkerText.color = elimination ? Objective : headshot ? Friendly : MainText;
            hitMarkerText.fontSize = UiMetrics.ScaleTextSize(HudEmphasisFontSize);
            hitMarkerText.gameObject.SetActive(true);
            hitMarkerUntil = Time.unscaledTime + 0.15f;
        }

        /// <summary>killerName empty = left the arena / no killer.</summary>
        public void ShowEliminated(IMatchParticipant killer, WeaponId weapon, string attackDirection = "")
        {
            lastKillerParticipantId = killer != null ? killer.ParticipantId : -1;
            lastKillerName = killer?.DisplayName ?? "";
            lastWeaponName = weapon.ToString();
            lastAttackDirection = attackDirection ?? "";
            damageFeedback?.Clear();
            flightSpeedLines?.Clear();
            HideCombatFeedback();
        }

        string ResolveLastKillerName()
        {
            if (lastKillerParticipantId < 0) return "";
            return ResolveParticipantDisplayName(lastKillerParticipantId, lastKillerName);
        }

        string ResolveParticipantDisplayName(int participantId, string fallbackName)
        {
            var participants = MatchView.Current?.Participants;
            if (participants != null)
                foreach (var participant in participants)
                    if (participant != null && participant.ParticipantId == participantId)
                        return participant.DisplayName;
            var offlineParticipant = PlayerController.Find(participantId);
            if (offlineParticipant != null) return offlineParticipant.DisplayName;
            return StreamSafePresentation.Enabled
                ? StreamSafePresentation.PlayerName(fallbackName, participantId)
                : fallbackName;
        }

        public void ShowEliminationConfirmed(IMatchParticipant victim)
        {
            if (victim == null)
            {
            ShowCombatFeedback(L("combat.eliminated", "Eliminated"), Friendly, 1.2f);
                return;
            }

            string fallbackName = victim.DisplayName ?? "";
            ShowCombatFeedback(F("combat.eliminatedTemplate", "Eliminated {0}", fallbackName), Friendly, 1.2f);
            eliminationVictimParticipantId = victim.ParticipantId;
            eliminationVictimFallbackName = fallbackName;
        }

        public void HideCombatFeedback()
        {
            if (combatFeedbackText != null) combatFeedbackText.gameObject.SetActive(false);
            eliminationVictimParticipantId = -1;
            eliminationVictimFallbackName = "";
        }

        public void ShowCombatFeedback(string message, Color color, float seconds)
        {
            if (combatFeedbackText == null) return;
            eliminationVictimParticipantId = -1;
            eliminationVictimFallbackName = "";
            // Transient messaging is laid out in the reserved strip above the marker band, never
            // inside it. At the old y=110 its ink measured y 97.71..122.29, overlapping the only
            // 40-unit gap markers have on the preparation screen; combined with a 33.94-tall marker
            // that left no clear placement at all, so the rule could only keep an overlapping one.
            // Set here as well as in the layout reflow, so the band is guaranteed wherever the toast
            // is shown from.
            combatFeedbackText.rectTransform.anchoredPosition = new Vector2(0, CombatFeedbackBandY);
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
            string zone = match != null ? PreparationView.ZoneShortLabel(match.ScoringZone) : L("zone.generic", "Zone");
            string status = ally && enemy ? L("zone.contested", "Contested") : ally ? L("zone.allyScoring", "Ally Scoring")
                : enemy ? L("zone.enemyScoring", "Enemy Scoring") : L("zone.open", "Open");
            activeZoneText.text = $"{zone}  ·  {status}";
            activeZoneText.color = ally && enemy ? Objective : ally ? Friendly : enemy ? Enemy : MainText;
            uiux?.SetZone(zone, status, ally && enemy ? UiDraw.Hex("#ffda88") : ally ? UiDraw.Hex("#7cefff") : enemy ? UiDraw.Hex("#ffb2b7") : UiDraw.Hex("#bbc9cb"));
        }

        public void ShowRotation(float matchTime)
        {
            if (nextZoneText == null || matchRules == null) return;
            var upcoming = matchRules.GetUpcomingZone(matchTime);
            if (upcoming.HasValue)
            {
                float seconds = matchRules.SecondsUntilRotation(matchTime);
                nextZoneText.text = F("rotation.nextTemplate", "NEXT: {0} IN {1}s", PreparationView.ZoneShortLabel(upcoming.Value), Mathf.CeilToInt(seconds));
                nextZoneText.color = Objective;
                uiux?.SetNext(PreparationView.ZoneShortLabel(upcoming.Value), Mathf.CeilToInt(seconds));
            }
            else
            {
                nextZoneText.text = matchTime >= matchRules.regulationDurationSeconds ? L("rotation.overtime", "OVERTIME — HOLD 3s TO WIN") : "";
                nextZoneText.color = SecondaryText;
                uiux?.SetNext(null, 0);
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
                        if (preparationText != null) preparationText.text = PreparationCopy(duration);
                    }
                    PreparationView.Show(this, weapons, selectedPreparationWeapon, duration,
                        i => { selectWeaponAction?.Invoke(i); HighlightWeapon(i); });
                    break;
                case MatchPhase.Countdown:
                    if (countdownPanel != null) countdownPanel.SetActive(true);
                    break;
            }
        }

        public void ShowPreparationCountdown(float elapsed)
        {
            if (preparationText != null && matchRules != null)
                preparationText.text = PreparationCopy(Mathf.CeilToInt(matchRules.preparationSeconds - elapsed));
            if (matchRules != null)
                PreparationView.SetCountdown(this, matchRules.preparationSeconds - elapsed);
        }

        public void ShowCountdown(float elapsed)
        {
            if (countdownPanel == null || !countdownPanel.activeSelf) return;
            int count = Mathf.CeilToInt((matchRules != null ? matchRules.countdownSeconds : 3f) - elapsed);
            if (countdownText != null) countdownText.text = count > 0 ? count.ToString() : L("countdown.go", "Go");
        }

        public void EnableWeaponSelect(bool enabled)
        {
            if (weaponSelectPanel != null && weaponSelectPanel.activeSelf != enabled) weaponSelectPanel.SetActive(enabled);
        }

        public void OnMatchStart()
        {
            HideAllPanels();
            ShowCombatFeedback(L("combat.matchStart", "GO — CAPTURE THE ZONE"), Objective, 1.2f);
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
                nextZoneText.text = F("rotation.nextZoneTemplate", "Next: {0}", PreparationView.ZoneShortLabel(zone.Value));
        }

        string resultsHeader = "";

        /// <summary>
        /// QA-017: the authoritative reward receipt (or a truthful pending state) under the score
        /// line of the results screen. Pass null to clear it.
        /// </summary>
        public void SetResultsReceipt(string receipt, Color color, string saveNote = null,
            ResultsTrustState? trustState = null)
        {
            resultsReceipt = receipt ?? "";
            resultsReceiptColor = color;
            if (resultStatsText == null) return;
            string visibleReceipt = Hotdrop.UI.StreamSafePresentation.Enabled && !string.IsNullOrEmpty(receipt)
                ? UiDraw.L("profile.accreditation.xpHidden", "Progress Hidden") : receipt;
            resultStatsText.text = UiPresentationProfile.NormalizeRichText(string.IsNullOrEmpty(receipt) ? resultsHeader
                : $"{resultsHeader}\n<color=#{ColorUtility.ToHtmlStringRGB(color)}>{visibleReceipt}</color>");
            ResultsView.SetReceipt(this, receipt, color, saveNote);
            if (trustState.HasValue)
                ResultsView.SetTrustState(this, trustState.Value, saveNote);
            else if (!string.IsNullOrEmpty(saveNote))
                ResultsView.SetSaveNote(this, saveNote);
        }

        /// <summary>Passes the exact backend receipt to the results reveal; the sticky replay
        /// action remains available while the short presentation runs.</summary>
        public void SetResultsReward(MatchReceiptDto receipt, Color color)
        {
            resultsReceipt = "";
            if (resultStatsText != null) resultStatsText.text = UiPresentationProfile.NormalizeRichText(resultsHeader);
            ResultsView.SetTrustState(this, ResultsTrustState.Verified);
            ResultsView.RevealReward(this, receipt, color);
        }

        public void ShowResults(bool isWin, bool isDraw, int scoreA, int scoreB, string endCondition) =>
            ShowResults(isWin, isDraw, scoreA, scoreB, endCondition, null, L("result.practiceFooter", "PRACTICE MATCH — NO ONLINE XP, CREDITS, CONTRACTS, OR MASTERY"));

        public void ShowResults(bool isWin, bool isDraw, int scoreA, int scoreB, string endCondition,
            System.Collections.Generic.IReadOnlyList<IMatchParticipant> players, string footer, string nextGoal = null,
            ResultsTrustState trustState = ResultsTrustState.Practice)
        {
            HideAllPanels();
            flightSpeedLines?.Clear();
            if (deathPanel != null) deathPanel.SetActive(false);
            if (objectiveMarker != null) objectiveMarker.gameObject.SetActive(false);
            // Observed in the 11_results parity capture: the teammate cards stayed live over the
            // full-screen results table and covered its PLAYER column, so the first name in each
            // row was unreadable. Bind(match: true) puts the panel back on the next match.
            if (teammatePanel != null) teammatePanel.SetActive(false);
            // The match is over: combat controls and the zone banner would only clutter the results.
            SetZoneInfoVisible(false);
            if (touch != null) touch.enabled = false;
            if (resultsPanel != null) resultsPanel.SetActive(true);
            if (resultTitleText != null)
            {
                resultTitleText.text = isDraw ? L("result.draw", "Draw") : isWin ? L("result.victory", "Victory") : L("result.defeat", "Defeat");
                resultTitleText.color = isDraw ? Objective : isWin ? Friendly : UIDesignTokens.SignalWarning;
            }

            bool localIsA = localPlayer == null || localPlayer.TeamId == 0;
            int ally = localIsA ? scoreA : scoreB, enemy = localIsA ? scoreB : scoreA;
            // Persist the record once at match end even while Stream-Safe hides it; the display
            // sites below and in ResultsView gate on the live preference, so toggling it off
            // during Results reveals this match's notice without a second record pass.
            string personalBest = localPlayer != null
                ? PersonalBests.Record(localPlayer, trustState == ResultsTrustState.Practice) : "";
            resultsParticipants = players;
            resultsFooter = footer;
            resultsNextGoal = nextGoal;
            resultsPersonalBest = personalBest;
            resultsAllyScore = ally;
            resultsEnemyScore = enemy;
            resultsReceipt = "";
            RebuildLegacyResultsText();
            ResultsView.Show(this, isWin, isDraw, ally, enemy, players, localPlayer,
                footer, nextGoal, playAgainAction, homeAction, trustState,
                reportParticipantAction, reportParticipantEligible, personalBest);
        }

        void RebuildLegacyResultsText()
        {
            resultsHeader = F("result.scoreTemplate", "ALLY {0}  —  {1} ENEMY\n{2}",
                resultsAllyScore, resultsEnemyScore, resultsFooter);
            // Amendment 1: outcome + one contribution + the next reachable goal, on one screen.
            if (!StreamSafePresentation.Enabled && !string.IsNullOrEmpty(resultsNextGoal))
                resultsHeader += $"\n<color=#FFD166>{resultsNextGoal}</color>";
            if (resultStatsText != null)
            {
                string visibleReceipt = StreamSafePresentation.Enabled && !string.IsNullOrEmpty(resultsReceipt)
                    ? UiDraw.L("profile.accreditation.xpHidden", "Progress Hidden") : resultsReceipt;
                string text = string.IsNullOrEmpty(visibleReceipt) ? resultsHeader
                    : $"{resultsHeader}\n<color=#{ColorUtility.ToHtmlStringRGB(resultsReceiptColor)}>{visibleReceipt}</color>";
                resultStatsText.text = UiPresentationProfile.NormalizeRichText(text);
            }

            if (resultTableText == null || resultsParticipants == null) return;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>" + L("result.tableHeader", "Player                           Elims   Deaths   Zone Time   Launches") + "</b>\n");
            bool localIsA = localPlayer == null || localPlayer.TeamId == 0;
            for (int team = 0; team < 2; team++)
            {
                int wanted = team == 0 ? (localIsA ? 0 : 1) : (localIsA ? 1 : 0);
                sb.Append(team == 0 ? "<color=#30D5E8>" + L("team.ally", "Ally") + "</color>\n" : "\n<color=#FF6376>" + L("team.enemy", "Enemy") + "</color>\n");
                foreach (var p in resultsParticipants)
                {
                    if (p == null || p.TeamId != wanted) continue;
                    string name = ReferenceEquals(p, localPlayer) ? F("result.youNameTemplate", "<b>{0} ({1})</b>", p.DisplayName, L("you", "You")) : p.DisplayName;
                    if (!p.IsConnected) name += " <color=#FFB45C>(" + L("result.disconnected", "Disconnected") + ")</color>";
                    sb.Append($"{name,-32}  {p.Eliminations,5}   {p.Deaths,6}   {Mathf.RoundToInt(p.ObjectiveSeconds),7}s   {p.PadLaunches,8}\n");
                }
            }
            if (!StreamSafePresentation.Enabled && localPlayer != null)
            {
                if (!string.IsNullOrEmpty(resultsPersonalBest))
                    sb.Append("\n<color=#FFD166>" + F("result.personalBestTemplate", "New Personal Best: {0}", resultsPersonalBest) + "</color>");
            }
            resultTableText.text = UiPresentationProfile.NormalizeRichText(sb.ToString());
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
            quickChatWheel?.Close();
            PreparationView.Hide(this);
            ResultsView.Hide(this);
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
            public TextMeshProUGUI statsText;
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
            tmp.text = text; tmp.fontSize = UiMetrics.ScaleTextSize(size);
            tmp.color = color == Color.white ? UIDesignTokens.TextPrimary : color;
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = UiMetrics.ScaleTextSize(12); tmp.fontSizeMax = UiMetrics.ScaleTextSize(size);
            return tmp;
        }
    }

    /// <summary>Spec section 18: personal bests (objective time, pad-assisted plays). Local only, no XP.</summary>
    public static class PersonalBests
    {
        public static string Record(IMatchParticipant player, bool localPractice)
        {
            // The old unscoped keys contain mixed historical values; their source mode is
            // unknowable, so keep them untouched rather than promoting an offline score into
            // the online context (or vice versa). New records are scoped by match context.
            string scope = localPractice ? "Offline" : "Online";
            var sb = new System.Text.StringBuilder();
            Check($"Best.{scope}.ObjectiveSeconds", Mathf.RoundToInt(player.ObjectiveSeconds), UiDraw.L("hud.best.zoneTime", "Zone Time"), sb);
            Check($"Best.{scope}.PadLaunches", player.PadLaunches, UiDraw.L("hud.best.padLaunches", "Pad Launches"), sb);
            Check($"Best.{scope}.Eliminations", player.Eliminations, UiDraw.L("hud.best.eliminations", "Eliminations"), sb);
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



