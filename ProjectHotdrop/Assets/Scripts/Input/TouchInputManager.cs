using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.EnhancedTouch;
using RecordedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Hotdrop.Config;
using Hotdrop.UI;
using Hotdrop.Match;

namespace Hotdrop.Input
{
    /// <summary>Shared spec limits for the slowdown-only aim assist in offline and online play.</summary>
    public static class AimAssistRules
    {
        // Spec 8: "slowdown only, <=3 degree cone, <=25m, sensitivity drops to 70% inside
        // region, no snap, no tracking through cover, no magnetism, no auto-fire, off switch."
        // These are spec-pinned accessibility limits, not balance values, so they are
        // named constants here rather than balance config. The behaviour they describe
        // lives in Qualifies/SensitivityScale below so it can be tested once and reused
        // by both the offline and online aim paths.
        public const float ConeDegrees = 3f;
        public const float MaxDistance = 25f;
        public const float SensitivityMultiplier = 0.7f;

        /// <summary>Normal aim speed when no assist region is active.</summary>
        public const float FullSensitivityScale = 1f;

        /// <summary>
        /// True when a target point sits inside the assist region: within the cone, within
        /// range, and with nothing in the way. Cover never grants assist ("no tracking
        /// through cover"), which is why line of sight is a required input rather than an
        /// optional refinement.
        /// </summary>
        public static bool Qualifies(Vector3 eye, Vector3 forward, Vector3 targetPoint, bool lineOfSightClear)
        {
            if (!lineOfSightClear) return false;
            Vector3 toTarget = targetPoint - eye;
            float distance = toTarget.magnitude;
            // Below a centimetre the direction is numerically meaningless.
            if (distance < 0.01f || distance > MaxDistance) return false;
            return Vector3.Angle(forward, toTarget) <= ConeDegrees;
        }

        /// <summary>
        /// The only effect aim assist has: aim sensitivity drops to 70% inside the region
        /// and is untouched everywhere else. There is no snap, magnetism or auto-fire —
        /// returning one of two scales makes that structural rather than a promise.
        /// </summary>
        public static float SensitivityScale(bool insideAssistRegion) =>
            insideAssistRegion ? SensitivityMultiplier : FullSensitivityScale;
    }

    /// <summary>
    /// Spec section 8: Mobile touch controls.
    /// Floating left joystick (move), right-side drag (aim), fire button (drag also aims),
    /// jump/launch above fire, pad button left of fire, reload near ammo, ping upper-right, pause upper corner.
    /// Simultaneous move+aim+fire mandatory. One touch = one owner. UI touches never rotate camera.
    /// Left-handed layout supported. Full button move/size/opacity/reset. Settings save locally.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TouchInputManager : MonoBehaviour
    {
        public const int AimSensitivityStepCount = 10;
        public const float AimSensitivityMinimum = 0.5f;
        public const float AimSensitivityMaximum = 2.5f;
        public const float GyroSensitivityMinimum = 0.5f;
        public const float GyroSensitivityMaximum = 2f;

        public static float AimSensitivityAtStep(int step)
        {
            step = Mathf.Clamp(step, 0, AimSensitivityStepCount - 1);
            if (step <= 4)
                return Mathf.Pow(0.5f, (4 - step) / 4f);
            return Mathf.Pow(AimSensitivityMaximum, (step - 4) / 5f);
        }

        public static int AimSensitivityStepFor(float sensitivity)
        {
            sensitivity = Mathf.Clamp(sensitivity, AimSensitivityMinimum, AimSensitivityMaximum);
            int closestStep = 0;
            float closestDistance = float.MaxValue;
            for (int step = 0; step < AimSensitivityStepCount; step++)
            {
                float distance = Mathf.Abs(AimSensitivityAtStep(step) - sensitivity);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStep = step;
                }
            }
            return closestStep;
        }

        public static float NormalizeAimSensitivity(float sensitivity) =>
            AimSensitivityAtStep(AimSensitivityStepFor(sensitivity));

        [Header("Config")]
        [SerializeField] public GraphicsConfig graphicsConfig;

        [Header("Joystick Settings")]
        [SerializeField] float joystickSize = 120f;
        [SerializeField] float joystickDeadzone = 0.15f;
        [SerializeField] float maxJoystickDistance = 60f; // dp
        [SerializeField] float aimDegreesPerInch = 45f; // camera turn per inch of swipe at sensitivity 1

        [Header("Aim Settings")]
        [SerializeField] float aimSensitivity = 1f;
        [SerializeField] float focusAimSensitivity = 1f;
        [SerializeField] float gyroSensitivity = 1f;
        [SerializeField] bool invertY = false;
        [SerializeField] bool gyroAimEnabled = false;
        [SerializeField] bool vibrationEnabled = true;
        // Amendment 2: Focus Aim is "optional hold/toggle" - both interaction modes are offered,
        // selectable in Settings; hold is the default so existing muscle memory is unaffected.
        [SerializeField] bool focusAimToggleMode = false;
        bool focusAimToggled;
        Vector3 gyroBias;
        bool gyroCalibrating;

        [Header("Button Sizes (dp, converted to screen pixels)")]
        [SerializeField] float fireButtonSize = 80f;
        [SerializeField] float jumpButtonSize = 70f;
        [SerializeField] float padButtonSize = 70f;
        [SerializeField] float reloadButtonSize = 60f;
        [SerializeField] float pingButtonSize = 60f;
        [SerializeField] float pauseButtonSize = 50f;

        // Input state
        public Vector2 MoveInput { get; private set; }
        public Vector2 AimDelta { get; private set; }
        /// <summary>Last unscaled time the player touched, aimed, moved, or used a gameplay control.</summary>
        public float LastGameplayInputAt { get; private set; }
        public bool FocusAimHeld { get; private set; }
        public bool FireHeld { get; private set; }
        public bool FirePressedThisFrame { get; private set; }
        public bool FireReleasedThisFrame { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool PadPressed { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool PingPressed { get; private set; }
        public bool QuickChatPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public float AimSensitivityScale { get; set; } = 1f;
        public bool LaunchAvailable { get; set; }
        public bool PadAvailable { get; set; } = true;
        public float PadCooldown { get; set; }
        public string AmmoLabel { get; set; } = "";
        /// <summary>Short weapon name and magazine (e.g. "ARC 24") for the reload control.</summary>
        public string WeaponLabel { get; set; } = "";
        /// <summary>False in training: there are no teammates to ping, so the control is removed.</summary>
        public bool PingEnabled { get; set; } = true;
        public bool QuickChatEnabled { get; set; }
        public string PadStatusLabel { get; set; } = "";

        // Spec section 6: hold the pad button > 180 ms to preview; drag off it to cancel.
        public const float PadHoldPreviewSeconds = 0.18f;
        public const float QuickChatHoldSeconds = 0.28f;
        public bool PadPreviewActive { get; private set; }
        public bool PadCancelled { get; private set; }
        public string PadPreviewReason { get; set; }
        int padTouchId = -1;
        float padTouchStart;
        Vector2 padLastPos;
        int lastPadSeconds = -1;
        string padLabel = "PAD";

        // Touch tracking
        int moveTouchId = -1;
        int aimTouchId = -1;
        int fireTouchId = -1;
        int focusAimTouchId = -1;
        int pingTouchId = -1;
        float pingTouchStartedAt;
        bool pingOpenedQuickChat;
        Vector2 moveStartPos;
        Vector2 moveCurrentPos;
        Vector2 aimStartPos;
        Vector2 aimCurrentPos;
        Vector2 fireStartPos;

        // Button rects (screen space, bottom-left origin)
        Rect fireButtonRect;
        Rect jumpButtonRect;
        Rect padButtonRect;
        Rect reloadButtonRect;
        Rect pingButtonRect;
        Rect pauseButtonRect;
        Rect focusAimButtonRect;

        bool leftHandedMode = false;
        float dpiScale = 1f;

        /// <summary>
        /// How much larger than one canvas unit per design px the touch controls are drawn, so
        /// their design px land as physical dp. 1 wherever a canvas unit is already at least a dp.
        /// </summary>
        public float ControlScale { get; private set; } = 1f;
        /// <summary>Screen 08's bottom inset for the action cluster: 24 px margin + 8 px padding.</summary>
        const float safeBottomDesign = 32f;
        float reservedBottomDesign;

        /// <summary>
        /// Design px at the bottom edge occupied by a full-width bar (the preparation footer).
        /// The action cluster sits above it so neither control steals the other's taps.
        /// </summary>
        public void SetReservedBottom(float designPx)
        {
            designPx = Mathf.Max(0f, designPx);
            if (Mathf.Approximately(designPx, reservedBottomDesign)) return;
            reservedBottomDesign = designPx;
            RecalculateButtonRects();
        }
        readonly float[] buttonScale = { 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        readonly float[] buttonOpacity = { 0.65f, 0.65f, 0.65f, 0.65f, 0.65f, 0.65f, 0.65f };
        readonly Vector2[] buttonOffsetDp = new Vector2[7];
        readonly System.Collections.Generic.HashSet<int> consumedTouches = new System.Collections.Generic.HashSet<int>();
        readonly System.Collections.Generic.HashSet<int> liveTouchIds = new System.Collections.Generic.HashSet<int>();
        readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> uiHits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.PointerEventData pointerData;
        int lastWidth, lastHeight;
        Hotdrop.UI.TouchControlsView view;

        public enum ControlButton { Fire, Jump, Pad, Reload, Ping, Pause, FocusAim }

        // ---------------------------------------------------------------- read-only view surface
        //
        // The controls are drawn by Hotdrop.UI.TouchControlsView as real Unity UI. It needs to
        // read the same rectangles and labels this class already computes, and nothing else: the
        // view never hit-tests, so a tap still reaches ProcessTouch against these exact rects and
        // moving the pixels onto a canvas cannot change what a touch does.

        /// <summary>Screen-space rect (pixels, bottom-left origin) of a control.</summary>
        public Rect RectOf(ControlButton button) => ButtonRect(button);

        /// <summary>Draw opacity the player set for a control.</summary>
        public float OpacityOf(ControlButton button) => buttonOpacity[(int)button];

        public Rect FireRect => fireButtonRect;
        public Rect JumpRect => jumpButtonRect;
        public Rect PadRect => padButtonRect;
        public Rect ReloadRect => reloadButtonRect;
        public Rect PingRect => pingButtonRect;
        public Rect PauseRect => pauseButtonRect;
        public Rect FocusAimRect => focusAimButtonRect;
        public Rect SelectedRect => ButtonRect(SelectedButton);
        public float FireOpacity => buttonOpacity[(int)ControlButton.Fire];
        public float JumpOpacity => buttonOpacity[(int)ControlButton.Jump];
        public float PadOpacity => buttonOpacity[(int)ControlButton.Pad];
        public float ReloadOpacity => buttonOpacity[(int)ControlButton.Reload];
        public float PingOpacity => buttonOpacity[(int)ControlButton.Ping];
        public float PauseOpacity => buttonOpacity[(int)ControlButton.Pause];
        public float FocusAimOpacity => buttonOpacity[(int)ControlButton.FocusAim];

        /// <summary>The PAD button's live caption: its cooldown, or its lifecycle state.</summary>
        public string PadDisplayLabel => PadCaptionForFrame();

        /// <summary>Whether the PAD control belongs on screen at all.</summary>
        public bool PadVisible => (MatchView.Current?.PadsEnabled ?? Hotdrop.Core.StudySettings.PadsEnabled) || EditingLayout;

        /// <summary>The pad-placement preview line shown while the PAD button is held.</summary>
        public string PadPreviewCaption => PadPreviewReason == null
            ? MatchHUD.Localized("touch.releasePad", "Release to Place Pad")
            : UiDraw.LFormat("hud.touch.padRejectedTemplate", "Can't Place - {0}", MatchHUD.LocalizedPadReason(PadPreviewReason));

        /// <summary>Whether a centre reticle is drawn.</summary>
        public bool ShowCrosshair => true;

        /// <summary>Whether a movement joystick is being dragged right now.</summary>
        public bool JoystickActive => moveTouchId != -1;

        /// <summary>Centre of the live movement joystick, in screen pixels.</summary>
        public Vector2 JoystickCenter => moveStartPos;

        /// <summary>Stick offset from the joystick centre, clamped to its radius, in screen pixels.</summary>
        public Vector2 JoystickOffset => Vector2.ClampMagnitude(moveCurrentPos - moveStartPos, JoystickRadiusPx);

        /// <summary>The captions on the layout-editor action strip, in order.</summary>
        public static string[] EditActionCaptions
        {
            get
            {
                var captions = new string[EditActions.Length];
                for (int i = 0; i < captions.Length; i++) captions[i] = EditActionCaption(i);
                return captions;
            }
        }

        public static string EditActionCaption(int index) => index switch
        {
            0 => MatchHUD.Localized("touch.action.sizeMinus", "Size -"),
            1 => MatchHUD.Localized("touch.action.sizePlus", "Size +"),
            2 => MatchHUD.Localized("touch.action.fadeMinus", "Fade -"),
            3 => MatchHUD.Localized("touch.action.fadePlus", "Fade +"),
            4 => MatchHUD.Localized("touch.action.reset", "Reset"),
            5 => MatchHUD.Localized("touch.action.leftHanded", "Left hand"),
            6 => MatchHUD.Localized("touch.action.cancel", "Cancel"),
            7 => MatchHUD.Localized("touch.action.save", "Save"),
            _ => string.Empty,
        };

        /// <summary>Screen-space rect of one layout-editor action button.</summary>
        public Rect EditActionRectFor(int index) => EditActionRect(index);

        /// <summary>The line under the layout-editor strip describing the selected control.</summary>
        public string EditHintText => UiDraw.LFormat("hud.touch.editHintTemplate",
            "DRAG A BUTTON TO MOVE IT  |  SELECTED: {0}  SIZE {1:0.0}x  OPACITY {2:0}%",
            MatchHUD.Localized("touch.button." + SelectedButton.ToString().ToLowerInvariant(), SelectedButton.ToString().ToUpperInvariant()),
            buttonScale[(int)SelectedButton], buttonOpacity[(int)SelectedButton] * 100f);

        public string LayoutWarningText => layoutOverlapWarning
            ? MatchHUD.Localized("touch.layoutOverlap", "Controls cannot overlap") : string.Empty;
        public Rect LayoutSafeRect => ComposedSafeRect();

        // Events
        public System.Action<Vector2> OnPingWorldPositionRequested;
        public System.Action OnQuickChatRequested;

        void Awake()
        {
            dpiScale = Screen.dpi > 0f ? Mathf.Max(1f, Screen.dpi / 160f) : 1f; // mdpi baseline
            RecalculateButtonRects();
            LoadSettings();
        }

        void OnEnable()
        {
            LastGameplayInputAt = Time.unscaledTime;
            // Record transitions: direct Touchscreen polling can lose Began
            // when several native events arrive before the same render frame.
            EnhancedTouchSupport.Enable();
            EnsureView();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            consumedTouches.Clear();
            moveTouchId = aimTouchId = fireTouchId = padTouchId = -1;
            focusAimTouchId = -1;
            pingTouchId = -1;
            pingOpenedQuickChat = false;
            focusAimToggled = false;
            FocusAimHeld = false;
            PadPreviewActive = PadCancelled = false;
            MoveInput = AimDelta = Vector2.zero;
            FireHeld = false;
            // Per-frame presses must not survive while disabled, or the press that
            // opened a menu fires again the moment the menu re-enables input.
            FirePressedThisFrame = FireReleasedThisFrame = JumpPressed = PadPressed = false;
            ReloadPressed = PingPressed = QuickChatPressed = PausePressed = false;
            // The touchscreen is a shared system device. Disabling this component
            // must not disable touch input for menus or the next scene.
        }

        // Android backgrounds the app mid-touch, so the matching Ended/Canceled
        // events never arrive and fire/move would stay stuck after resuming
        // (Gate A background/foreground requirement). Drop every owned touch.
        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) return;
            fireTouchId = -1;
            FireHeld = false;
            focusAimTouchId = -1;
            focusAimToggled = false;
            FocusAimHeld = false;
            padTouchId = -1;
            PadPreviewActive = false;
            pingTouchId = -1;
            pingOpenedQuickChat = false;
            moveTouchId = -1;
            MoveInput = Vector2.zero;
            aimTouchId = -1;
            consumedTouches.Clear();
        }

        void Update()
        {
            // Reset frame-specific flags
            FirePressedThisFrame = false;
            FireReleasedThisFrame = false;
            JumpPressed = false;
            PadPressed = false;
            ReloadPressed = false;
            PingPressed = false;
            QuickChatPressed = false;
            PausePressed = false;
            AimDelta = Vector2.zero;

            // A view attached at OnEnable can be destroyed with its canvas (a scene change, or a
            // test tearing the HUD down), so re-attach rather than silently drawing nothing.
            if (view == null) EnsureView();

            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                lastWidth = Screen.width; lastHeight = Screen.height;
                RecalculateButtonRects();
            }

            var touches = RecordedTouch.activeTouches;
            if (EditingLayout) { UpdateLayoutEditor(touches); return; }

            // Process each touch
            liveTouchIds.Clear();
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.None) continue;
                liveTouchIds.Add(touch.touchId);
                ProcessTouch(touch.touchId, touch.phase, touch.screenPosition);
            }
            ReleaseOwnersNotIn(liveTouchIds);
            // Hold mode tracks a live finger; toggle mode tracks a latched flip, since the
            // finger that triggered it lifts immediately.
            FocusAimHeld = focusAimToggleMode ? focusAimToggled : focusAimTouchId != -1;

            // Handle gyro aim if enabled
            if (gyroAimEnabled && GyroAvailable)
            {
                var gyro = UnityEngine.InputSystem.Gyroscope.current;
                if (!gyro.enabled) InputSystem.EnableDevice(gyro);
                // Device axes are portrait-relative; in landscape the device X axis is
                // world-vertical (yaw) and the device Y axis runs along the screen (pitch).
                Vector3 rawRate = gyro.angularVelocity.ReadValue() - gyroBias;
                // Ignore sensor noise while the phone is resting; calibrating stores its
                // measured stationary bias so users can establish a neutral hold position.
                if (rawRate.sqrMagnitude < 0.0004f) rawRate = Vector3.zero;
                Vector3 w = rawRate * Mathf.Rad2Deg * Time.deltaTime;
                float sign = Screen.orientation == ScreenOrientation.LandscapeRight ? -1f : 1f;
                Vector2 gyroDelta = new Vector2(-w.x * sign, w.y * sign) * gyroSensitivity * AimSensitivityScale;
                if (invertY) gyroDelta.y = -gyroDelta.y;
                AimDelta += gyroDelta;
            }

            // Keep the teammate roster visible while the player is actively interacting;
            // the HUD uses this timestamp for its low-priority idle fade only.
            if (touches.Count > 0 || MoveInput.sqrMagnitude > 0.01f || AimDelta.sqrMagnitude > 0f ||
                FocusAimHeld || FireHeld || FirePressedThisFrame || JumpPressed || PadPressed ||
                ReloadPressed || PingPressed || QuickChatPressed || PausePressed)
                LastGameplayInputAt = Time.unscaledTime;

            if (PingPressed)
                OnPingWorldPositionRequested?.Invoke(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            if (QuickChatPressed) OnQuickChatRequested?.Invoke();
        }

        /// <summary>
        /// Routes one touch event. QA-003: ownership persists by finger ID until Ended/Cancelled;
        /// the screen half is consulted only when a NEW finger is acquired, so a finger that
        /// drags across the midpoint still releases into the handler that owns it.
        /// </summary>
#if DEVELOPMENT_BUILD
        int diagFrames;
        string diagLastEvent = "-";
#endif

        void ProcessTouch(int touchId, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
#if DEVELOPMENT_BUILD
            diagLastEvent = $"{touchId}:{phase}@{pos.x:0},{pos.y:0}";
#endif
            bool ending = phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled;
            if (Debug.isDebugBuild && phase == UnityEngine.InputSystem.TouchPhase.Began)
                Debug.Log($"[TouchDiag] began id={touchId} pos={pos} overMenu={IsOverMenu(pos)} firstHit={(uiHits.Count > 0 ? uiHits[0].gameObject.name : "-")}");
            if (consumedTouches.Contains(touchId))
            {
                if (ending) consumedTouches.Remove(touchId);
                return;
            }
            if (phase == UnityEngine.InputSystem.TouchPhase.Began && IsOverMenu(pos))
            {
                consumedTouches.Add(touchId);
                return;
            }

            // Owned fingers first: a move/aim finger sliding over a control stays move/aim.
            if (touchId == moveTouchId) { HandleMoveTouch(touchId, phase, pos); return; }
            if (touchId == aimTouchId) { HandleAimFireTouch(touchId, phase, pos); return; }

            // Check UI buttons (they consume the touch)
            if (CheckButtonTouch(touchId, phase, pos)) return;

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                bool isLeftSide = leftHandedMode ? pos.x > Screen.width * 0.5f : pos.x < Screen.width * 0.5f;
                if (isLeftSide && moveTouchId == -1) HandleMoveTouch(touchId, phase, pos);
                else if (!isLeftSide && aimTouchId == -1) HandleAimFireTouch(touchId, phase, pos);
            }
        }

        /// <summary>
        /// QA-003: the OS can drop a finger without delivering Ended/Cancelled (notification
        /// shade, system gesture). Any owner whose finger is no longer active is released so
        /// movement, aim, fire and pad can never stay latched.
        /// </summary>
        void ReleaseOwnersNotIn(System.Collections.Generic.HashSet<int> live)
        {
            if (moveTouchId != -1 && !live.Contains(moveTouchId)) { moveTouchId = -1; MoveInput = Vector2.zero; }
            if (aimTouchId != -1 && !live.Contains(aimTouchId)) aimTouchId = -1;
            if (fireTouchId != -1 && !live.Contains(fireTouchId)) { fireTouchId = -1; FireHeld = false; FireReleasedThisFrame = true; }
            if (padTouchId != -1 && !live.Contains(padTouchId)) { padTouchId = -1; PadPreviewActive = false; }
            if (pingTouchId != -1 && !live.Contains(pingTouchId)) { pingTouchId = -1; pingOpenedQuickChat = false; }
            // Same QA-003 rule as move/aim/fire/pad: a dropped Focus Aim finger (no Ended/Canceled
            // delivered) must not leave the zoom and reduced sensitivity latched indefinitely.
            if (focusAimTouchId != -1 && !live.Contains(focusAimTouchId)) focusAimTouchId = -1;
            consumedTouches.IntersectWith(live);
        }

        bool CheckButtonTouch(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
            bool insideFire = fireButtonRect.Contains(pos);
            bool insideJump = jumpButtonRect.Contains(pos);
            bool insidePad = padButtonRect.Contains(pos);
            bool insideReload = reloadButtonRect.Contains(pos);
            bool insidePing = pingButtonRect.Contains(pos);
            bool insidePause = pauseButtonRect.Contains(pos);
            bool insideFocusAim = focusAimButtonRect.Contains(pos);

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // A second finger on an already-held Fire is swallowed rather than stealing
                // ownership (otherwise lifting it would stop fire while the first finger holds).
                if (insideFire && fireTouchId != -1) { consumedTouches.Add(id); return true; }
                if (insideFire) { fireTouchId = id; fireStartPos = pos; FireHeld = true; FirePressedThisFrame = true; return true; }
                if (insideJump) { consumedTouches.Add(id); JumpPressed = true; Vibrate(); return true; }
                if (insidePad && PadVisible) { padTouchId = id; padTouchStart = Time.unscaledTime; padLastPos = pos; PadCancelled = false; Vibrate(); return true; }
                if (insideReload) { consumedTouches.Add(id); ReloadPressed = true; Vibrate(); return true; }
                if (insidePing)
                {
                    if (QuickChatEnabled)
                    {
                        pingTouchId = id;
                        pingTouchStartedAt = Time.unscaledTime;
                        pingOpenedQuickChat = false;
                    }
                    else
                    {
                        consumedTouches.Add(id);
                        PingPressed = true;
                    }
                    Vibrate();
                    return true;
                }
                if (insidePause) { consumedTouches.Add(id); PausePressed = true; return true; }
                if (insideFocusAim && focusAimToggleMode)
                {
                    // Toggle mode: the tap flips a latched state and releases immediately,
                    // rather than claiming the finger for the rest of the hold.
                    consumedTouches.Add(id);
                    focusAimToggled = !focusAimToggled;
                    FocusAimHeld = focusAimToggled;
                    Hotdrop.Core.Telemetry.Track("focus_aim_used",
                        ("input", "touch_toggle"), ("state", focusAimToggled ? "on" : "off"));
                    Vibrate();
                    return true;
                }
                if (insideFocusAim && focusAimTouchId == -1)
                {
                    focusAimTouchId = id;
                    FocusAimHeld = true;
                    Hotdrop.Core.Telemetry.Track("focus_aim_used", ("input", "touch_hold"));
                    Vibrate();
                    return true;
                }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (id == pingTouchId)
                {
                    pingTouchId = -1;
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended && QuickChatEnabled && !pingOpenedQuickChat &&
                        Time.unscaledTime - pingTouchStartedAt >= QuickChatHoldSeconds)
                    {
                        pingOpenedQuickChat = true;
                        QuickChatPressed = true;
                    }
                    else if (phase == UnityEngine.InputSystem.TouchPhase.Ended && !pingOpenedQuickChat) PingPressed = true;
                    pingOpenedQuickChat = false;
                    return true;
                }
                if (id == fireTouchId) { fireTouchId = -1; FireHeld = false; FireReleasedThisFrame = true; return true; }
                if (id == focusAimTouchId) { focusAimTouchId = -1; FocusAimHeld = false; return true; }
                if (id == padTouchId)
                {
                    // Tap or held release places; dragging off the button cancelled it.
                    padTouchId = -1;
                    PadPreviewActive = false;
                    if (!PadCancelled && phase == UnityEngine.InputSystem.TouchPhase.Ended) PadPressed = true;
                    return true;
                }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                if (id == pingTouchId)
                {
                    if (QuickChatEnabled && !pingOpenedQuickChat && Time.unscaledTime - pingTouchStartedAt >= QuickChatHoldSeconds)
                    {
                        pingOpenedQuickChat = true;
                        QuickChatPressed = true;
                        Vibrate();
                    }
                    return true;
                }
                if (id == padTouchId)
                {
                    PadPreviewActive = !PadCancelled && Time.unscaledTime - padTouchStart >= PadHoldPreviewSeconds;
                    Rect cancelBounds = new Rect(padButtonRect.x - padButtonRect.width * 0.5f, padButtonRect.y - padButtonRect.height * 0.5f,
                        padButtonRect.width * 2f, padButtonRect.height * 2f);
                    if (!cancelBounds.Contains(pos) && !PadCancelled) { PadCancelled = true; PadPreviewActive = false; Vibrate(); }
                    // Spec 6: while the preview shows, the same finger steers the camera (and so the launch direction).
                    else if (PadPreviewActive) AimDelta += ScaleAimDelta(pos - padLastPos);
                    padLastPos = pos;
                    return true;
                }
                if (id == fireTouchId)
                {
                    // Fire button drag also aims
                    AimDelta += ScaleAimDelta(pos - fireStartPos);
                    fireStartPos = pos;
                    return true;
                }
            }

            return false;
        }

        bool IsOverMenu(Vector2 position)
        {
            var system = UnityEngine.EventSystems.EventSystem.current;
            if (system == null) return false;
            if (pointerData == null)
                pointerData = new UnityEngine.EventSystems.PointerEventData(system);
            pointerData.position = position;
            uiHits.Clear();
            system.RaycastAll(pointerData, uiHits);
            return uiHits.Count > 0;
        }

        void HandleMoveTouch(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {

            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    moveTouchId = id;
                    moveStartPos = pos;
                    moveCurrentPos = pos;
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (id == moveTouchId)
                    {
                        moveCurrentPos = pos;
                        Vector2 delta = moveCurrentPos - moveStartPos;
                        float dist = delta.magnitude;

                        float radius = JoystickRadiusPx;
                        MoveInput = dist < joystickDeadzone * radius
                            ? Vector2.zero
                            : delta.normalized * (Mathf.Min(dist, radius) / radius);
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (id == moveTouchId)
                    {
                        moveTouchId = -1;
                        MoveInput = Vector2.zero;
                    }
                    break;
            }
        }

        void HandleAimFireTouch(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {

            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (aimTouchId == -1 && fireTouchId != id)
                    {
                        aimTouchId = id;
                        aimStartPos = pos;
                        aimCurrentPos = pos;
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (id == aimTouchId)
                    {
                        aimCurrentPos = pos;
                        Vector2 delta = aimCurrentPos - aimStartPos;
                        AimDelta += ScaleAimDelta(delta);
                        aimStartPos = aimCurrentPos;
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (id == aimTouchId)
                    {
                        aimTouchId = -1;
                    }
                    break;
            }
        }

        // Throw stays in physical dp so movement feel does not depend on the HUD's layout scale.
        float JoystickRadiusPx => maxJoystickDistance * ComposedDpiScale();

        Vector2 ScaleAimDelta(Vector2 delta)
        {
            // Convert pixels to physical inches so a swipe turns the camera equally on every screen density.
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            Vector2 scaled = delta / dpi * aimDegreesPerInch * aimSensitivity * AimSensitivityScale;
            if (invertY) scaled.y = -scaled.y;
            return scaled;
        }

        /// <summary>
        /// The safe rect, in pixels with a bottom-left origin, that the controls lay out against.
        ///
        /// <para>This is the single place in this file that answers "where is the screen". It asks
        /// <see cref="SafeAreaRoot.ScreenSize"/> - the frame actually being composed for - rather
        /// than <c>Screen</c>. A capture renders 1920x1080 inside whatever window the editor
        /// happens to own, and in batch mode <c>Screen.safeArea</c> is 640x480, so laying the
        /// controls out against that put the entire cluster inside one eighth of the published
        /// frame. This is the same disagreement the startup frame label had, which is why it goes
        /// through the same source. When nothing is simulated - a real device - the branch below
        /// reads the real screen, exactly as before.</para>
        /// </summary>
        /// <summary>
        /// The density scale the controls are sized by, in canvas/dp terms.
        ///
        /// <para>Reads the composed frame's dpi, not <c>Screen.dpi</c>, for the same reason
        /// <see cref="ComposedSafeRect"/> reads the composed frame rather than the screen. A
        /// capture composes a 400 dpi frame inside a window that reports 96, so the controls were
        /// sized as if for an mdpi device while every other element on the frame was sized for the
        /// density it claims: the discs came out 2.5x too small and their captions could not fit
        /// inside them at the project's 12 dp legibility floor. On a device nothing is simulated and
        /// this returns exactly what it returned before.</para>
        /// </summary>
        static float ComposedDpiScale()
        {
            float dpi = SafeAreaRoot.SimulatedScreen?.z ?? Screen.dpi;
            return dpi > 0f ? Mathf.Max(1f, dpi / 160f) : 1f;
        }

        static Rect ComposedSafeRect()
        {
            var frame = SafeAreaRoot.ScreenSize;
            if (SafeAreaRoot.SimulatedScreen.HasValue)
            {
                var insets = SafeAreaRoot.SimulatedInsets ?? ScreenInsets.None;
                return new Rect(insets.left, insets.bottom,
                    Mathf.Max(0f, frame.x - insets.left - insets.right),
                    Mathf.Max(0f, frame.y - insets.top - insets.bottom));
            }
            var safe = Screen.safeArea;
            // In batch mode / headless environments Screen.safeArea can be zero. Fall back to the
            // full frame so the controls still lay out somewhere sane rather than at zero size.
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, frame.x, frame.y);
            return safe;
        }

        void RecalculateButtonRects()
        {
            // Re-read every frame this is called, so a newly pinned frame resizes the controls with
            // it rather than only moving them.
            // UIUX screen 08 (classic preset) in its own CSS px, converted with the canvas scale so
            // the controls land exactly where the HUD draws them: FIRE 80, LAUNCH 64, PAD and
            // RELOAD 56 stacked, all bottom-aligned 32 px up and 24 px in; ping and menu 40 in the
            // top-right row. Customization may shrink a hit rect, so retain the 48 dp minimum;
            // the drawn control stays at its design size.
            //
            // Design px are laid out in canvas units, and on a dense phone a canvas unit is smaller
            // than a dp (Galaxy A06: 1 unit = 1 px = 0.53 dp), so the controls were drawn at about
            // half their physical design size and the "48 dp" hit floor was really 26 dp. Thumbs are
            // physical, so the action cluster uses true dp wherever a unit is smaller than one; where
            // the canvas is already larger (tablets, desktop captures) nothing changes.
            var frame = SafeAreaRoot.ScreenSize;
            float u = Hotdrop.UI.UiMetrics.ScaleFor(frame.x, frame.y, 0f);
            float px = Mathf.Max(u, ComposedDpiScale());
            ControlScale = px / u;
            dpiScale = px;
            float S(ControlButton b, float design) => Mathf.Max(Hotdrop.UI.UiMetrics.MinTouchDp,
                design * buttonScale[(int)b]) * px;
            float fireSize = S(ControlButton.Fire, 80f);
            float jumpSize = S(ControlButton.Jump, 64f);
            float padSize = S(ControlButton.Pad, 56f);
            float reloadSize = S(ControlButton.Reload, 56f);
            float pingSize = S(ControlButton.Ping, 40f);
            float pauseSize = S(ControlButton.Pause, 40f);
            float focusAimSize = S(ControlButton.FocusAim, 40f);
            float margin = 24f * px;
            // A reserved footer is drawn in canvas units, so clear it in canvas units.
            float bottom = reservedBottomDesign > 0f ? (reservedBottomDesign + 8f) * u : safeBottomDesign * px;
            u = px;

            Rect safe = ComposedSafeRect();
            // Laid out right-handed from the right edge, then mirrored for left-handed play.
            float right = safe.xMax - margin;
            fireButtonRect = new Rect(right - fireSize, safe.yMin + bottom, fireSize, fireSize);
            jumpButtonRect = new Rect(fireButtonRect.x - 12f * u - jumpSize, safe.yMin + bottom, jumpSize, jumpSize);
            reloadButtonRect = new Rect(jumpButtonRect.x - 8f * u - reloadSize, safe.yMin + bottom, reloadSize, reloadSize);
            padButtonRect = new Rect(reloadButtonRect.center.x - padSize * .5f, reloadButtonRect.yMax + 4f * u, padSize, padSize);
            // The 40 px drawn controls centre in their (possibly larger) hit rects.
            float top = safe.yMax - margin - 20f * u;
            pauseButtonRect = new Rect(right - 20f * u - pauseSize * .5f, top - pauseSize * .5f, pauseSize, pauseSize);
            pingButtonRect = new Rect(right - 76f * u - pingSize * .5f, top - pingSize * .5f, pingSize, pingSize);
            focusAimButtonRect = new Rect(right - 132f * u - focusAimSize * .5f, top - focusAimSize * .5f, focusAimSize, focusAimSize);
            if (!PingEnabled) pingButtonRect = Rect.zero;
            if (leftHandedMode)
            {
                Rect Mirror(Rect r) => new Rect(safe.xMin + (safe.xMax - r.xMax), r.y, r.width, r.height);
                fireButtonRect = Mirror(fireButtonRect);
                jumpButtonRect = Mirror(jumpButtonRect);
                reloadButtonRect = Mirror(reloadButtonRect);
                padButtonRect = Mirror(padButtonRect);
            }

            fireButtonRect.position += buttonOffsetDp[(int)ControlButton.Fire] * u;
            jumpButtonRect.position += buttonOffsetDp[(int)ControlButton.Jump] * u;
            padButtonRect.position += buttonOffsetDp[(int)ControlButton.Pad] * u;
            reloadButtonRect.position += buttonOffsetDp[(int)ControlButton.Reload] * u;
            pingButtonRect.position += buttonOffsetDp[(int)ControlButton.Ping] * u;
            pauseButtonRect.position += buttonOffsetDp[(int)ControlButton.Pause] * u;
            focusAimButtonRect.position += buttonOffsetDp[(int)ControlButton.FocusAim] * u;
        }

        public void SetLeftHanded(bool enabled)
        {
            leftHandedMode = enabled;
            RecalculateButtonRects();
            SaveSettings();
        }

        public void SetAimSensitivity(float sens)
        {
            aimSensitivity = NormalizeAimSensitivity(sens);
            SaveSettings();
        }

        public float FocusAimSensitivityScale => focusAimSensitivity;

        public void SetFocusAimSensitivity(float sens)
        {
            focusAimSensitivity = NormalizeAimSensitivity(sens);
            SaveSettings();
        }

        /// <summary>True when Focus Aim is a tap-to-latch toggle rather than a press-and-hold.</summary>
        public bool FocusAimToggleMode => focusAimToggleMode;

        public void SetFocusAimMode(bool toggleMode)
        {
            focusAimToggleMode = toggleMode;
            // Switching modes mid-hold/mid-toggle must not leave the zoom latched by the
            // interaction style that is no longer active.
            focusAimTouchId = -1;
            focusAimToggled = false;
            FocusAimHeld = false;
            SaveSettings();
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
            SaveSettings();
        }

        public void SetGyroAim(bool enabled)
        {
            gyroAimEnabled = enabled && GyroAvailable;
            SaveSettings();
        }

        public void SetGyroSensitivity(float sensitivity)
        {
            gyroSensitivity = Mathf.Clamp(sensitivity, GyroSensitivityMinimum, GyroSensitivityMaximum);
            SaveSettings();
        }

        public void ResetGyroCalibration()
        {
            gyroBias = Vector3.zero;
            PlayerPrefs.DeleteKey("GyroBiasX");
            PlayerPrefs.DeleteKey("GyroBiasY");
            PlayerPrefs.DeleteKey("GyroBiasZ");
            PlayerPrefs.Save();
        }

        public static bool GyroAvailable => UnityEngine.InputSystem.Gyroscope.current != null;

        public void CalibrateGyroNeutral()
        {
            if (!GyroAvailable || gyroCalibrating) return;
            StartCoroutine(CalibrateGyroNeutralRoutine());
        }

        System.Collections.IEnumerator CalibrateGyroNeutralRoutine()
        {
            gyroCalibrating = true;
            var gyro = UnityEngine.InputSystem.Gyroscope.current;
            if (!gyro.enabled) InputSystem.EnableDevice(gyro);
            Vector3 sum = Vector3.zero;
            const int samples = 40;
            for (int i = 0; i < samples; i++)
            {
                sum += gyro.angularVelocity.ReadValue();
                yield return null;
            }
            gyroBias = sum / samples;
            PlayerPrefs.SetFloat("GyroBiasX", gyroBias.x);
            PlayerPrefs.SetFloat("GyroBiasY", gyroBias.y);
            PlayerPrefs.SetFloat("GyroBiasZ", gyroBias.z);
            PlayerPrefs.Save();
            gyroCalibrating = false;
        }

        public void SetVibration(bool enabled)
        {
            vibrationEnabled = enabled;
            SaveSettings();
        }

        public void SetButtonLayout(ControlButton button, Vector2 offsetDp, float scale, float opacity)
        {
            int index = (int)button;
            Vector2 previousOffset = buttonOffsetDp[index];
            float previousScale = buttonScale[index];
            buttonOffsetDp[index] = offsetDp;
            buttonScale[index] = Mathf.Clamp(scale, 0.6f, 2f);
            buttonOpacity[index] = Mathf.Clamp(opacity, 0.15f, 1f);
            RecalculateButtonRects();
            KeepOnScreen(button);
            if (OverlapsAnotherControl(button))
            {
                buttonOffsetDp[index] = previousOffset;
                buttonScale[index] = previousScale;
                RecalculateButtonRects();
            }
            if (!EditingLayout) SaveSettings();
        }

        public void ResetButtonLayout()
        {
            for (int i = 0; i < buttonScale.Length; i++)
            {
                buttonOffsetDp[i] = Vector2.zero;
                buttonScale[i] = 1f;
                buttonOpacity[i] = 0.65f;
            }
            RecalculateButtonRects();
            if (!EditingLayout) SaveSettings();
        }

        // ---- Control layout editor (spec section 8: full button move/size/opacity/reset) ----

        public bool EditingLayout { get; private set; }
        public bool LayoutEditCommitted { get; private set; }
        public bool IsGyroCalibrating => gyroCalibrating;
        public ControlButton SelectedButton { get; private set; } = ControlButton.Fire;
        System.Action onLayoutEditDone;
        int editTouchId = -1;
        Vector2 editLastPos;
        static readonly string[] EditActions = { "Size -", "Size +", "Fade -", "Fade +", "Reset", "Left hand", "Cancel", "Save" };
        Vector2[] editOriginalOffsets;
        float[] editOriginalScales, editOriginalOpacities;
        bool editOriginalLeftHanded;
        bool layoutOverlapWarning;

        public void BeginLayoutEdit(System.Action onDone)
        {
            onLayoutEditDone = onDone;
            editOriginalOffsets = (Vector2[])buttonOffsetDp.Clone();
            editOriginalScales = (float[])buttonScale.Clone();
            editOriginalOpacities = (float[])buttonOpacity.Clone();
            editOriginalLeftHanded = leftHandedMode;
            layoutOverlapWarning = false;
            LayoutEditCommitted = false;
            EditingLayout = true;
            editTouchId = -1;
            enabled = true;
        }

        public void EndLayoutEdit()
        {
            EditingLayout = false;
            LayoutEditCommitted = true;
            SaveSettings();
            FinishLayoutEdit();
        }

        public void CancelLayoutEdit()
        {
            if (editOriginalOffsets != null)
            {
                System.Array.Copy(editOriginalOffsets, buttonOffsetDp, buttonOffsetDp.Length);
                System.Array.Copy(editOriginalScales, buttonScale, buttonScale.Length);
                System.Array.Copy(editOriginalOpacities, buttonOpacity, buttonOpacity.Length);
                leftHandedMode = editOriginalLeftHanded;
                RecalculateButtonRects();
            }
            EditingLayout = false;
            LayoutEditCommitted = false;
            FinishLayoutEdit();
        }

        void FinishLayoutEdit()
        {
            var done = onLayoutEditDone;
            onLayoutEditDone = null;
            editOriginalOffsets = null;
            editOriginalScales = null;
            editOriginalOpacities = null;
            done?.Invoke();
        }

        /// <summary>
        /// Screen-space rect of one layout-editor action button. The strip's geometry lives here
        /// with the rest of the control geometry; the view draws it.
        /// </summary>
        Rect EditActionRect(int index)
        {
            float w = 130f * dpiScale * 0.6f, h = 48f * dpiScale, gap = 8f * dpiScale;
            float total = EditActions.Length * w + (EditActions.Length - 1) * gap;
            Rect safe = ComposedSafeRect();
            // Bottom-left origin, top-centre of the screen.
            return new Rect(safe.center.x - total * 0.5f + index * (w + gap), safe.yMax - h - 12f * dpiScale, w, h);
        }

        Rect ButtonRect(ControlButton b) => b switch
        {
            ControlButton.Fire => fireButtonRect, ControlButton.Jump => jumpButtonRect, ControlButton.Pad => padButtonRect,
            ControlButton.Reload => reloadButtonRect, ControlButton.Ping => pingButtonRect,
            ControlButton.Pause => pauseButtonRect, _ => focusAimButtonRect,
        };

        void UpdateLayoutEditor(UnityEngine.InputSystem.Utilities.ReadOnlyArray<RecordedTouch> touches)
        {
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var pos = touch.screenPosition;
                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        for (int a = 0; a < EditActions.Length; a++)
                            if (EditActionRect(a).Contains(pos)) { ApplyEditAction(a); goto nextTouch; }
                        for (int b = 0; b < buttonScale.Length; b++)
                            if (ButtonRect((ControlButton)b).Contains(pos))
                            {
                                SelectedButton = (ControlButton)b;
                                editTouchId = touch.touchId;
                                editLastPos = pos;
                                break;
                            }
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Moved:
                        if (touch.touchId == editTouchId)
                        {
                            int index = (int)SelectedButton;
                            Vector2 previousOffset = buttonOffsetDp[index];
                            buttonOffsetDp[index] += (pos - editLastPos) / dpiScale;
                            editLastPos = pos;
                            RecalculateButtonRects();
                            KeepOnScreen(SelectedButton);
                            if (OverlapsAnotherControl(SelectedButton))
                            {
                                buttonOffsetDp[index] = previousOffset;
                                RecalculateButtonRects();
                                layoutOverlapWarning = true;
                            }
                            else layoutOverlapWarning = false;
                        }
                        break;
                    case UnityEngine.InputSystem.TouchPhase.Ended:
                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        if (touch.touchId == editTouchId) editTouchId = -1;
                        break;
                }
                nextTouch:;
            }
        }

        void KeepOnScreen(ControlButton button)
        {
            Rect r = ButtonRect(button), safe = ComposedSafeRect();
            Vector2 shift = Vector2.zero;
            if (r.xMin < safe.xMin) shift.x = safe.xMin - r.xMin;
            if (r.xMax > safe.xMax) shift.x = safe.xMax - r.xMax;
            if (r.yMin < safe.yMin) shift.y = safe.yMin - r.yMin;
            if (r.yMax > safe.yMax) shift.y = safe.yMax - r.yMax;
            if (shift == Vector2.zero) return;
            buttonOffsetDp[(int)button] += shift / dpiScale;
            RecalculateButtonRects();
        }

        bool OverlapsAnotherControl(ControlButton button)
        {
            Rect candidate = ButtonRect(button);
            if (candidate.width <= 0f || candidate.height <= 0f) return false;
            float gap = 2f * dpiScale;
            candidate = new Rect(candidate.xMin - gap, candidate.yMin - gap,
                candidate.width + gap * 2f, candidate.height + gap * 2f);
            for (int i = 0; i < buttonScale.Length; i++)
            {
                if (i == (int)button) continue;
                Rect other = ButtonRect((ControlButton)i);
                if (other.width > 0f && other.height > 0f && candidate.Overlaps(other)) return true;
            }
            return false;
        }

        void ApplyEditAction(int action)
        {
            int i = (int)SelectedButton;
            float previousScale = buttonScale[i];
            switch (action)
            {
                case 0: buttonScale[i] = Mathf.Clamp(buttonScale[i] - 0.1f, 0.6f, 2f); break;
                case 1: buttonScale[i] = Mathf.Clamp(buttonScale[i] + 0.1f, 0.6f, 2f); break;
                case 2: buttonOpacity[i] = Mathf.Clamp(buttonOpacity[i] - 0.1f, 0.15f, 1f); break;
                case 3: buttonOpacity[i] = Mathf.Clamp(buttonOpacity[i] + 0.1f, 0.15f, 1f); break;
                case 4: ResetButtonLayout(); layoutOverlapWarning = false; return;
                case 5: leftHandedMode = !leftHandedMode; RecalculateButtonRects(); layoutOverlapWarning = false; return;
                case 6: CancelLayoutEdit(); return;
                case 7: EndLayoutEdit(); return;
            }
            RecalculateButtonRects();
            KeepOnScreen(SelectedButton);
            if (OverlapsAnotherControl(SelectedButton))
            {
                buttonScale[i] = previousScale;
                RecalculateButtonRects();
                layoutOverlapWarning = true;
            }
            else layoutOverlapWarning = false;
        }

        void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Handheld only exists in mobile player builds; the Linux server build excludes it.
            if (vibrationEnabled && Application.isMobilePlatform) Hotdrop.Core.HapticFeedback.Pulse();
#endif
        }

        void LoadSettings()
        {
            leftHandedMode = PlayerPrefs.GetInt("LeftHandedMode", 0) == 1;
            float savedAimSensitivity = PlayerPrefs.GetFloat("AimSensitivity", 1f);
            float savedFocusAimSensitivity = PlayerPrefs.GetFloat("FocusAimSensitivity", 1f);
            aimSensitivity = NormalizeAimSensitivity(savedAimSensitivity);
            focusAimSensitivity = NormalizeAimSensitivity(savedFocusAimSensitivity);
            bool migratedSensitivity = !Mathf.Approximately(aimSensitivity, savedAimSensitivity) ||
                !Mathf.Approximately(focusAimSensitivity, savedFocusAimSensitivity);
            if (migratedSensitivity)
            {
                PlayerPrefs.SetFloat("AimSensitivity", aimSensitivity);
                PlayerPrefs.SetFloat("FocusAimSensitivity", focusAimSensitivity);
                PlayerPrefs.Save();
            }
            invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
            focusAimToggleMode = PlayerPrefs.GetInt("FocusAimToggleMode", 0) == 1;
            gyroAimEnabled = PlayerPrefs.GetInt("GyroAim", 0) == 1;
            gyroSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("GyroSensitivity", 1f), GyroSensitivityMinimum, GyroSensitivityMaximum);
            gyroBias = new Vector3(PlayerPrefs.GetFloat("GyroBiasX"), PlayerPrefs.GetFloat("GyroBiasY"), PlayerPrefs.GetFloat("GyroBiasZ"));
            vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
            for (int i = 0; i < buttonScale.Length; i++)
            {
                buttonScale[i] = PlayerPrefs.GetFloat($"Control.{i}.Scale", 1f);
                buttonOpacity[i] = PlayerPrefs.GetFloat($"Control.{i}.Opacity", 0.65f);
                buttonOffsetDp[i] = new Vector2(
                    PlayerPrefs.GetFloat($"Control.{i}.OffsetX", 0f),
                    PlayerPrefs.GetFloat($"Control.{i}.OffsetY", 0f));
            }
            RecalculateButtonRects();
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("LeftHandedMode", leftHandedMode ? 1 : 0);
            PlayerPrefs.SetFloat("AimSensitivity", aimSensitivity);
            PlayerPrefs.SetFloat("FocusAimSensitivity", focusAimSensitivity);
            PlayerPrefs.SetInt("InvertY", invertY ? 1 : 0);
            PlayerPrefs.SetInt("FocusAimToggleMode", focusAimToggleMode ? 1 : 0);
            PlayerPrefs.SetInt("GyroAim", gyroAimEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("GyroSensitivity", gyroSensitivity);
            PlayerPrefs.SetInt("VibrationEnabled", vibrationEnabled ? 1 : 0);
            for (int i = 0; i < buttonScale.Length; i++)
            {
                PlayerPrefs.SetFloat($"Control.{i}.Scale", buttonScale[i]);
                PlayerPrefs.SetFloat($"Control.{i}.Opacity", buttonOpacity[i]);
                PlayerPrefs.SetFloat($"Control.{i}.OffsetX", buttonOffsetDp[i].x);
                PlayerPrefs.SetFloat($"Control.{i}.OffsetY", buttonOffsetDp[i].y);
            }
            PlayerPrefs.Save();
        }

        void OnRectTransformDimensionsChange()
        {
            RecalculateButtonRects();
        }

        /// <summary>
        /// Recomputes the control geometry against the frame currently being composed for.
        ///
        /// <para>Public so a synchronous exporter can pin a frame and then ask for the layout of
        /// that same frame. A capture renders 1920x1080 inside whatever window the editor owns and
        /// changes nothing about <c>Screen</c>, so the screen-change check in <c>Update</c> never
        /// fires and the geometry would otherwise still describe the editor's window when the
        /// evidence frame is written. Same reason <c>StartupConnectionScreen.RefreshFrameLabel</c>
        /// exists.</para>
        /// </summary>
        public void RecalculateForComposedFrame()
        {
            RecalculateButtonRects();
        }

        /// <summary>
        /// Makes sure the controls have a real Unity UI view to draw into, once, on first enable.
        ///
        /// The controls used to be painted from <c>OnGUI</c>. Immediate-mode drawing happens in
        /// the game view's own pass, so it never appears in a frame captured by rendering the HUD
        /// canvas into a RenderTexture - which is how this project's screen evidence is produced -
        /// and it cannot be scaled by the canvas or styled from a design token. Attaching the view
        /// here keeps the wiring in one place: the input class still owns every rectangle, and the
        /// view only reads them.
        /// </summary>
        void EnsureView()
        {
            if (view != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            view = Hotdrop.UI.TouchControlsView.Ensure(canvas, this);
        }

        /// <summary>Visual feedback for an actual weapon discharge, including shots stopped by cover.</summary>
        public void PulseFireFeedback()
        {
            EnsureView();
            view?.PulseFire();
        }

        /// <summary>Radius of the live movement joystick, in screen pixels.</summary>
        public float JoystickReachPx => JoystickRadiusPx;

        /// <summary>
        /// Keeps the PAD control's caption current. This used to be computed inside the
        /// immediate-mode drawing pass, which no longer runs; the state still has to advance on
        /// its own clock because <see cref="PadDisplayLabel"/> is read by the view each frame.
        /// </summary>
        void UpdatePadLabel()
        {
            int padSeconds = PadAvailable ? 0 : Mathf.CeilToInt(PadCooldown);
            if (padSeconds == lastPadSeconds) return;
            lastPadSeconds = padSeconds;
            padLabel = padSeconds == 0 ? MatchHUD.Localized("touch.pad", "Pad")
                : UiDraw.LFormat("hud.touch.cooldownTemplate", "{0}s", padSeconds);
        }

        /// <summary>
        /// The full PAD caption for the current frame: cancellation, cooldown, or the pad's
        /// real lifecycle state. Kept here rather than in the view because it is input state.
        /// </summary>
        string PadCaptionForFrame()
        {
            UpdatePadLabel();
            string display = PadCancelled && padTouchId != -1 ? MatchHUD.Localized("touch.cancel", "Cancel") : padLabel;
            // Keep the control short enough to remain tappable, while exposing the real
            // lifecycle state (the full identity/health readout stays in the HUD).
            if (!PadPreviewActive && PadAvailable && PadStatusLabel.Contains(MatchHUD.Localized("pad.arming", "Arming")))
                display = UiDraw.LFormat("hud.touch.padStateTemplate", "{0}\n{1}", MatchHUD.Localized("touch.pad", "Pad"), MatchHUD.Localized("pad.arming", "Arming"));
            else if (!PadPreviewActive && PadAvailable && PadStatusLabel.Contains(MatchHUD.Localized("pad.armed", "ARMED")))
                display = UiDraw.LFormat("hud.touch.padStateTemplate", "{0}\n{1}", MatchHUD.Localized("touch.pad", "Pad"), MatchHUD.Localized("touch.padArmedShort", "Armed"));
            return display;
        }
    }
}



