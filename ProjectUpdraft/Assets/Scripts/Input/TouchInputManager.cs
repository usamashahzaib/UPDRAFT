using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.EnhancedTouch;
using RecordedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Updraft.Config;

namespace Updraft.Input
{
    /// <summary>Shared spec limits for the slowdown-only aim assist in offline and online play.</summary>
    public static class AimAssistRules
    {
        public const float ConeDegrees = 3f;
        public const float MaxDistance = 25f;
        public const float SensitivityMultiplier = 0.7f;
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
        [Header("Config")]
        [SerializeField] public GraphicsConfig graphicsConfig;

        [Header("Joystick Settings")]
        [SerializeField] float joystickSize = 120f;
        [SerializeField] float joystickDeadzone = 0.15f;
        [SerializeField] float maxJoystickDistance = 60f; // dp
        [SerializeField] float aimDegreesPerInch = 45f; // camera turn per inch of swipe at sensitivity 1

        [Header("Aim Settings")]
        [SerializeField] float aimSensitivity = 1f;
        [SerializeField] bool invertY = false;
        [SerializeField] bool gyroAimEnabled = false;
        [SerializeField] bool vibrationEnabled = true;

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
        public bool FireHeld { get; private set; }
        public bool FirePressedThisFrame { get; private set; }
        public bool FireReleasedThisFrame { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool PadPressed { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool PingPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public float AimSensitivityScale { get; set; } = 1f;
        public bool LaunchAvailable { get; set; }
        public bool PadAvailable { get; set; } = true;
        public float PadCooldown { get; set; }
        public string AmmoLabel { get; set; } = "";

        // Spec section 6: hold the pad button > 180 ms to preview; drag off it to cancel.
        public const float PadHoldPreviewSeconds = 0.18f;
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

        bool leftHandedMode = false;
        float dpiScale = 1f;
        readonly float[] buttonScale = { 1f, 1f, 1f, 1f, 1f, 1f };
        readonly float[] buttonOpacity = { 0.65f, 0.65f, 0.65f, 0.65f, 0.65f, 0.65f };
        readonly Vector2[] buttonOffsetDp = new Vector2[6];
        readonly System.Collections.Generic.HashSet<int> consumedTouches = new System.Collections.Generic.HashSet<int>();
        readonly System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> uiHits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.PointerEventData pointerData;
        int lastWidth, lastHeight;
        Texture2D controlDisc;
        GUIStyle controlLabel;

        public enum ControlButton { Fire, Jump, Pad, Reload, Ping, Pause }

        // Events
        public System.Action<Vector2> OnPingWorldPositionRequested;

        void Awake()
        {
            dpiScale = Screen.dpi > 0f ? Mathf.Max(1f, Screen.dpi / 160f) : 1f; // mdpi baseline
            RecalculateButtonRects();
            LoadSettings();
        }

        void OnEnable()
        {
            // Record transitions: direct Touchscreen polling can lose Began
            // when several native events arrive before the same render frame.
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            consumedTouches.Clear();
            moveTouchId = aimTouchId = fireTouchId = padTouchId = -1;
            PadPreviewActive = PadCancelled = false;
            MoveInput = AimDelta = Vector2.zero;
            FireHeld = false;
            // Per-frame presses must not survive while disabled, or the press that
            // opened a menu fires again the moment the menu re-enables input.
            FirePressedThisFrame = FireReleasedThisFrame = JumpPressed = PadPressed = false;
            ReloadPressed = PingPressed = PausePressed = false;
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
            padTouchId = -1;
            PadPreviewActive = false;
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
            PausePressed = false;
            AimDelta = Vector2.zero;

            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                lastWidth = Screen.width; lastHeight = Screen.height;
                RecalculateButtonRects();
            }

            var touches = RecordedTouch.activeTouches;
            if (EditingLayout) { UpdateLayoutEditor(touches); return; }

            // Process each touch
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var phase = touch.phase;
                var pos = touch.screenPosition;

                if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;

                int touchId = touch.touchId;
                if (Debug.isDebugBuild && phase == UnityEngine.InputSystem.TouchPhase.Began)
                    Debug.Log($"[TouchDiag] began id={touchId} pos={pos} overMenu={IsOverMenu(pos)} firstHit={(uiHits.Count > 0 ? uiHits[0].gameObject.name : "-")}");
                if (consumedTouches.Contains(touchId))
                {
                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                        consumedTouches.Remove(touchId);
                    continue;
                }
                if (phase == UnityEngine.InputSystem.TouchPhase.Began && IsOverMenu(pos))
                {
                    consumedTouches.Add(touchId);
                    continue;
                }

                // Check UI buttons first (they consume the touch)
                if (CheckButtonTouch(touch, phase, pos)) continue;

                // Joystick (left side)
                bool isLeftSide = leftHandedMode ? pos.x > Screen.width * 0.5f : pos.x < Screen.width * 0.5f;

                if (isLeftSide && (moveTouchId == -1 || moveTouchId == touch.touchId))
                {
                    HandleMoveTouch(touch, phase, pos);
                    continue;
                }

                // Aim/Fire (right side)
                bool isRightSide = leftHandedMode ? pos.x < Screen.width * 0.5f : pos.x > Screen.width * 0.5f;

                if (isRightSide)
                {
                    HandleAimFireTouch(touch, phase, pos);
                    continue;
                }
            }

            // Handle gyro aim if enabled
            if (gyroAimEnabled && GyroAvailable)
            {
                var gyro = UnityEngine.InputSystem.Gyroscope.current;
                if (!gyro.enabled) InputSystem.EnableDevice(gyro);
                // Device axes are portrait-relative; in landscape the device X axis is
                // world-vertical (yaw) and the device Y axis runs along the screen (pitch).
                Vector3 w = gyro.angularVelocity.ReadValue() * Mathf.Rad2Deg * Time.deltaTime;
                float sign = Screen.orientation == ScreenOrientation.LandscapeRight ? -1f : 1f;
                Vector2 gyroDelta = new Vector2(-w.x * sign, w.y * sign) * aimSensitivity * AimSensitivityScale;
                if (invertY) gyroDelta.y = -gyroDelta.y;
                AimDelta += gyroDelta;
            }

            if (PingPressed)
                OnPingWorldPositionRequested?.Invoke(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }

        bool CheckButtonTouch(RecordedTouch touch, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
            int id = touch.touchId;
            bool insideFire = fireButtonRect.Contains(pos);
            bool insideJump = jumpButtonRect.Contains(pos);
            bool insidePad = padButtonRect.Contains(pos);
            bool insideReload = reloadButtonRect.Contains(pos);
            bool insidePing = pingButtonRect.Contains(pos);
            bool insidePause = pauseButtonRect.Contains(pos);

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (insideFire) { fireTouchId = id; fireStartPos = pos; FireHeld = true; FirePressedThisFrame = true; return true; }
                if (insideJump) { consumedTouches.Add(id); JumpPressed = true; Vibrate(); return true; }
                if (insidePad && Updraft.Core.StudySettings.PadsEnabled) { padTouchId = id; padTouchStart = Time.unscaledTime; padLastPos = pos; PadCancelled = false; Vibrate(); return true; }
                if (insideReload) { consumedTouches.Add(id); ReloadPressed = true; Vibrate(); return true; }
                if (insidePing) { consumedTouches.Add(id); PingPressed = true; Vibrate(); return true; }
                if (insidePause) { consumedTouches.Add(id); PausePressed = true; return true; }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (id == fireTouchId) { fireTouchId = -1; FireHeld = false; FireReleasedThisFrame = true; return true; }
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

        void HandleMoveTouch(RecordedTouch touch, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
            int id = touch.touchId;

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

        void HandleAimFireTouch(RecordedTouch touch, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
            int id = touch.touchId;

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

        float JoystickRadiusPx => maxJoystickDistance * dpiScale;

        Vector2 ScaleAimDelta(Vector2 delta)
        {
            // Convert pixels to physical inches so a swipe turns the camera equally on every screen density.
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            Vector2 scaled = delta / dpi * aimDegreesPerInch * aimSensitivity * AimSensitivityScale;
            if (invertY) scaled.y = -scaled.y;
            return scaled;
        }

        void RecalculateButtonRects()
        {
            float fireSize = Mathf.Max(48f, fireButtonSize * buttonScale[(int)ControlButton.Fire]) * dpiScale;
            float jumpSize = Mathf.Max(48f, jumpButtonSize * buttonScale[(int)ControlButton.Jump]) * dpiScale;
            float padSize = Mathf.Max(48f, padButtonSize * buttonScale[(int)ControlButton.Pad]) * dpiScale;
            float reloadSize = Mathf.Max(48f, reloadButtonSize * buttonScale[(int)ControlButton.Reload]) * dpiScale;
            float pingSize = Mathf.Max(48f, pingButtonSize * buttonScale[(int)ControlButton.Ping]) * dpiScale;
            float pauseSize = Mathf.Max(48f, pauseButtonSize * buttonScale[(int)ControlButton.Pause]) * dpiScale;
            float padding = 20f * dpiScale;

            // In batch mode / headless environments, Screen.safeArea can be zero.
            // Fall back to full screen rect based on Screen.width/height (which use the
            // default game view resolution in batch mode).
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f)
            {
                float w = Screen.width > 0 ? Screen.width : 1920f;  // Landscape default
                float h = Screen.height > 0 ? Screen.height : 1080f;
                safe = new Rect(0f, 0f, w, h);
            }

            if (!leftHandedMode)
            {
                fireButtonRect = new Rect(safe.xMax - fireSize - padding, safe.yMin + padding, fireSize, fireSize);
                jumpButtonRect = new Rect(safe.xMax - jumpSize - padding, fireButtonRect.yMax + 10f * dpiScale, jumpSize, jumpSize);
                padButtonRect = new Rect(fireButtonRect.x - padSize - 10f * dpiScale, fireButtonRect.y, padSize, padSize);
                reloadButtonRect = new Rect(padButtonRect.x, padButtonRect.yMax + 10f * dpiScale, reloadSize, reloadSize);
                pingButtonRect = new Rect(safe.xMax - pingSize - padding, safe.yMax - pingSize - padding, pingSize, pingSize);
                pauseButtonRect = new Rect(safe.xMin + padding, safe.yMax - pauseSize - padding, pauseSize, pauseSize);
            }
            else
            {
                fireButtonRect = new Rect(safe.xMin + padding, safe.yMin + padding, fireSize, fireSize);
                jumpButtonRect = new Rect(safe.xMin + padding, fireButtonRect.yMax + 10f * dpiScale, jumpSize, jumpSize);
                padButtonRect = new Rect(fireButtonRect.xMax + 10f * dpiScale, fireButtonRect.y, padSize, padSize);
                reloadButtonRect = new Rect(padButtonRect.x, padButtonRect.yMax + 10f * dpiScale, reloadSize, reloadSize);
                pingButtonRect = new Rect(safe.xMin + padding, safe.yMax - pingSize - padding, pingSize, pingSize);
                pauseButtonRect = new Rect(safe.xMax - pauseSize - padding, safe.yMax - pauseSize - padding, pauseSize, pauseSize);
            }

            fireButtonRect.position += buttonOffsetDp[(int)ControlButton.Fire] * dpiScale;
            jumpButtonRect.position += buttonOffsetDp[(int)ControlButton.Jump] * dpiScale;
            padButtonRect.position += buttonOffsetDp[(int)ControlButton.Pad] * dpiScale;
            reloadButtonRect.position += buttonOffsetDp[(int)ControlButton.Reload] * dpiScale;
            pingButtonRect.position += buttonOffsetDp[(int)ControlButton.Ping] * dpiScale;
            pauseButtonRect.position += buttonOffsetDp[(int)ControlButton.Pause] * dpiScale;
        }

        public void SetLeftHanded(bool enabled)
        {
            leftHandedMode = enabled;
            RecalculateButtonRects();
            SaveSettings();
        }

        public void SetAimSensitivity(float sens)
        {
            aimSensitivity = sens;
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

        public static bool GyroAvailable => UnityEngine.InputSystem.Gyroscope.current != null;

        public void SetVibration(bool enabled)
        {
            vibrationEnabled = enabled;
            SaveSettings();
        }

        public void SetButtonLayout(ControlButton button, Vector2 offsetDp, float scale, float opacity)
        {
            int index = (int)button;
            buttonOffsetDp[index] = offsetDp;
            buttonScale[index] = Mathf.Clamp(scale, 0.6f, 2f);
            buttonOpacity[index] = Mathf.Clamp(opacity, 0.15f, 1f);
            RecalculateButtonRects();
            SaveSettings();
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
            SaveSettings();
        }

        // ---- Control layout editor (spec section 8: full button move/size/opacity/reset) ----

        public bool EditingLayout { get; private set; }
        public ControlButton SelectedButton { get; private set; } = ControlButton.Fire;
        System.Action onLayoutEditDone;
        int editTouchId = -1;
        Vector2 editLastPos;
        static readonly string[] EditActions = { "SIZE -", "SIZE +", "FADE -", "FADE +", "RESET", "DONE" };

        public void BeginLayoutEdit(System.Action onDone)
        {
            onLayoutEditDone = onDone;
            EditingLayout = true;
            editTouchId = -1;
            enabled = true;
        }

        public void EndLayoutEdit()
        {
            EditingLayout = false;
            SaveSettings();
            var done = onLayoutEditDone;
            onLayoutEditDone = null;
            done?.Invoke();
        }

        Rect EditActionRect(int index)
        {
            float w = 130f * dpiScale * 0.6f, h = 48f * dpiScale, gap = 8f * dpiScale;
            float total = EditActions.Length * w + (EditActions.Length - 1) * gap;
            Rect safe = Screen.safeArea;
            // Bottom-left origin, top-centre of the screen.
            return new Rect(safe.center.x - total * 0.5f + index * (w + gap), safe.yMax - h - 12f * dpiScale, w, h);
        }

        Rect ButtonRect(ControlButton b) => b switch
        {
            ControlButton.Fire => fireButtonRect, ControlButton.Jump => jumpButtonRect, ControlButton.Pad => padButtonRect,
            ControlButton.Reload => reloadButtonRect, ControlButton.Ping => pingButtonRect, _ => pauseButtonRect,
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
                        for (int b = 0; b < 6; b++)
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
                            buttonOffsetDp[index] += (pos - editLastPos) / dpiScale;
                            editLastPos = pos;
                            RecalculateButtonRects();
                            KeepOnScreen(SelectedButton);
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
            Rect r = ButtonRect(button), safe = Screen.safeArea;
            Vector2 shift = Vector2.zero;
            if (r.xMin < safe.xMin) shift.x = safe.xMin - r.xMin;
            if (r.xMax > safe.xMax) shift.x = safe.xMax - r.xMax;
            if (r.yMin < safe.yMin) shift.y = safe.yMin - r.yMin;
            if (r.yMax > safe.yMax) shift.y = safe.yMax - r.yMax;
            if (shift == Vector2.zero) return;
            buttonOffsetDp[(int)button] += shift / dpiScale;
            RecalculateButtonRects();
        }

        void ApplyEditAction(int action)
        {
            int i = (int)SelectedButton;
            switch (action)
            {
                case 0: buttonScale[i] = Mathf.Clamp(buttonScale[i] - 0.1f, 0.6f, 2f); break;
                case 1: buttonScale[i] = Mathf.Clamp(buttonScale[i] + 0.1f, 0.6f, 2f); break;
                case 2: buttonOpacity[i] = Mathf.Clamp(buttonOpacity[i] - 0.1f, 0.15f, 1f); break;
                case 3: buttonOpacity[i] = Mathf.Clamp(buttonOpacity[i] + 0.1f, 0.15f, 1f); break;
                case 4: ResetButtonLayout(); return;
                case 5: EndLayoutEdit(); return;
            }
            RecalculateButtonRects();
            KeepOnScreen(SelectedButton);
        }

        void DrawLayoutEditor()
        {
            GUI.color = new Color(0.063f, 0.094f, 0.153f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            for (int a = 0; a < EditActions.Length; a++)
            {
                Rect r = EditActionRect(a);
                Rect g = new Rect(r.x, Screen.height - r.yMax, r.width, r.height);
                GUI.color = a == 5 ? new Color(0.188f, 0.835f, 0.91f, 0.95f) : new Color(0.106f, 0.161f, 0.251f, 0.95f);
                GUI.DrawTexture(g, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(g, EditActions[a], controlLabel);
            }
            Rect sel = ButtonRect(SelectedButton);
            GUI.color = new Color(1f, 0.82f, 0.4f, 1f);
            float t = 3f;
            Rect s = new Rect(sel.x - t, Screen.height - sel.yMax - t, sel.width + 2 * t, sel.height + 2 * t);
            GUI.DrawTexture(new Rect(s.x, s.y, s.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(s.x, s.yMax - t, s.width, t), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(s.x, s.y, t, s.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(s.xMax - t, s.y, t, s.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var hint = new Rect(0, Screen.height - EditActionRect(0).yMin + 8f * dpiScale, Screen.width, 30f * dpiScale);
            GUI.Label(hint, $"DRAG A BUTTON TO MOVE IT  ·  SELECTED: {SelectedButton.ToString().ToUpperInvariant()}  " +
                $"SIZE {buttonScale[(int)SelectedButton]:0.0}x  OPACITY {buttonOpacity[(int)SelectedButton] * 100:0}%", controlLabel);
        }

        void Vibrate()
        {
            if (vibrationEnabled && Application.isMobilePlatform) Handheld.Vibrate();
        }

        void LoadSettings()
        {
            leftHandedMode = PlayerPrefs.GetInt("LeftHandedMode", 0) == 1;
            aimSensitivity = PlayerPrefs.GetFloat("AimSensitivity", 1f);
            invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
            gyroAimEnabled = PlayerPrefs.GetInt("GyroAim", 0) == 1;
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
            PlayerPrefs.SetInt("InvertY", invertY ? 1 : 0);
            PlayerPrefs.SetInt("GyroAim", gyroAimEnabled ? 1 : 0);
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

        void OnGUI()
        {
            if (controlDisc == null)
            {
                controlDisc = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                var pixels = new Color[64 * 64];
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32));
                        float alpha = Mathf.Clamp01(32 - distance);
                        pixels[y * 64 + x] = new Color(1, 1, 1, alpha * (distance > 29 ? 1f : 0.35f));
                    }
                controlDisc.SetPixels(pixels); controlDisc.Apply();
                controlLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            GUI.color = Color.white;
            if (EditingLayout)
            {
                controlLabel.fontSize = Mathf.RoundToInt(13 * dpiScale);
                DrawLayoutEditor();
            }
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 8, Screen.height * 0.5f - 1, 16, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 1, Screen.height * 0.5f - 8, 2, 16), Texture2D.whiteTexture);
            DrawButton(fireButtonRect, string.IsNullOrEmpty(AmmoLabel) ? "FIRE" : $"FIRE\n{AmmoLabel}", ControlButton.Fire);
            DrawButton(jumpButtonRect, LaunchAvailable ? "LAUNCH" : "JUMP", ControlButton.Jump);
            int padSeconds = PadAvailable ? 0 : Mathf.CeilToInt(PadCooldown);
            if (padSeconds != lastPadSeconds)
            {
                lastPadSeconds = padSeconds;
                padLabel = padSeconds == 0 ? "PAD" : $"{padSeconds}s";
            }
            if (Updraft.Core.StudySettings.PadsEnabled || EditingLayout)
                DrawButton(padButtonRect, PadCancelled && padTouchId != -1 ? "CANCEL" : padLabel, ControlButton.Pad);
            DrawButton(reloadButtonRect, "RELOAD", ControlButton.Reload);
            DrawButton(pingButtonRect, "PING", ControlButton.Ping);
            DrawButton(pauseButtonRect, "PAUSE", ControlButton.Pause);

            // Joystick visualization
            if (moveTouchId != -1)
            {
                float maxJoystickDistance = JoystickRadiusPx;
                GUI.color = new Color(0.188f, 0.835f, 0.91f, 0.5f);
                GUI.DrawTexture(new Rect(moveStartPos.x - maxJoystickDistance, Screen.height - moveStartPos.y - maxJoystickDistance, maxJoystickDistance * 2, maxJoystickDistance * 2), controlDisc);
                var stick = moveStartPos + Vector2.ClampMagnitude(moveCurrentPos - moveStartPos, maxJoystickDistance);
                GUI.color = new Color(0.188f, 0.835f, 0.91f, 0.9f);
                GUI.DrawTexture(new Rect(stick.x - 20, Screen.height - stick.y - 20, 40, 40), controlDisc);
            }
            GUI.color = Color.white;
        }

        void DrawButton(Rect rect, string label, ControlButton button)
        {
            Rect guiRect = new Rect(rect.x, Screen.height - rect.yMax, rect.width, rect.height);
            GUI.color = button == ControlButton.Pad ? new Color(1f, 0.82f, 0.4f, buttonOpacity[(int)button])
                : new Color(0.188f, 0.835f, 0.91f, buttonOpacity[(int)button]);
            GUI.DrawTexture(guiRect, controlDisc);
            // Shrink long labels (RELOAD, CANCEL) so they stay inside the disc's inner 80%.
            int longest = 1;
            foreach (var line in label.Split('\n')) longest = Mathf.Max(longest, line.Length);
            float fit = rect.width * 0.8f / (longest * 0.68f);
            controlLabel.fontSize = Mathf.RoundToInt(Mathf.Min(13 * dpiScale, fit));
            controlLabel.normal.textColor = new Color(0.957f, 0.969f, 0.984f);
            GUI.color = Color.white;
            GUI.Label(guiRect, label, controlLabel);
        }
    }
}
