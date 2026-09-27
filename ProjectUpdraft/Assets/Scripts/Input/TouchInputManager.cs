using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Updraft.Config;

namespace Updraft.Input
{
    /// <summary>
    /// Spec section 8: Mobile touch controls.
    /// Floating left joystick (move), right-side drag (aim), fire button (drag also aims),
    /// jump/launch above fire, pad button left of fire, reload near ammo, ping upper-right, pause upper corner.
    /// Simultaneous move+aim+fire mandatory. One touch = one owner. UI touches never rotate camera.
    /// Left-handed layout supported. Full button move/size/opacity/reset. Settings save locally.
    /// </summary>
    public class TouchInputManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] public GraphicsConfig graphicsConfig;

        [Header("Joystick Settings")]
        [SerializeField] float joystickSize = 120f;
        [SerializeField] float joystickDeadzone = 0.15f;
        [SerializeField] float maxJoystickDistance = 60f;

        [Header("Aim Settings")]
        [SerializeField] float aimSensitivity = 1f;
        [SerializeField] bool invertY = false;
        [SerializeField] bool gyroAimEnabled = false;

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

        // Touch tracking
        Touchscreen touchscreen;
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

        // Events
        public System.Action<Vector2> OnPingWorldPositionRequested;

        void Awake()
        {
            touchscreen = Touchscreen.current;
            dpiScale = Screen.dpi / 160f; // mdpi baseline
            RecalculateButtonRects();
            LoadSettings();
        }

        void OnEnable()
        {
            if (Touchscreen.current != null)
                InputSystem.EnableDevice(Touchscreen.current);
        }

        void OnDisable()
        {
            if (Touchscreen.current != null)
                InputSystem.DisableDevice(Touchscreen.current);
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

            if (touchscreen == null) return;

            var touches = touchscreen.touches;
            int activeTouchCount = 0;

            // Count active touches
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.None)
                    activeTouchCount++;
            }

            // Process each touch
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var phase = touch.phase.ReadValue();
                var pos = touch.position.ReadValue();

                if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;

                // Check UI buttons first (they consume the touch)
                if (CheckButtonTouch(touch, phase, pos)) continue;

                // Joystick (left side)
                bool isLeftSide = leftHandedMode ? pos.x > Screen.width * 0.5f : pos.x < Screen.width * 0.5f;

                if (isLeftSide && (moveTouchId == -1 || moveTouchId == touch.touchId.ReadValue()))
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
            if (gyroAimEnabled && SystemInfo.supportsGyroscope)
            {
                UnityEngine.Input.gyro.enabled = true;
                var rot = UnityEngine.Input.gyro.attitude;
                // Convert gyro to aim delta (simplified)
                AimDelta += new Vector2(rot.y, -rot.x) * aimSensitivity * 50f * Time.deltaTime;
            }
        }

        bool CheckButtonTouch(TouchControl touch, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
            int id = touch.touchId.ReadValue();
            bool insideFire = fireButtonRect.Contains(pos);
            bool insideJump = jumpButtonRect.Contains(pos);
            bool insidePad = padButtonRect.Contains(pos);
            bool insideReload = reloadButtonRect.Contains(pos);
            bool insidePing = pingButtonRect.Contains(pos);
            bool insidePause = pauseButtonRect.Contains(pos);

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (insideFire) { fireTouchId = id; FireHeld = true; FirePressedThisFrame = true; return true; }
                if (insideJump) { JumpPressed = true; return true; }
                if (insidePad) { PadPressed = true; return true; }
                if (insideReload) { ReloadPressed = true; return true; }
                if (insidePing) { PingPressed = true; return true; }
                if (insidePause) { PausePressed = true; return true; }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (id == fireTouchId) { fireTouchId = -1; FireHeld = false; FireReleasedThisFrame = true; return true; }
                if (insideJump) { JumpPressed = true; return true; }
                if (insidePad) { PadPressed = true; return true; }
                if (insideReload) { ReloadPressed = true; return true; }
                if (insidePing) { PingPressed = true; return true; }
                if (insidePause) { PausePressed = true; return true; }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                if (id == fireTouchId)
                {
                    // Fire button drag also aims
                    AimDelta += (pos - fireStartPos) * aimSensitivity * 0.01f * dpiScale;
                    fireStartPos = pos;
                    return true;
                }
            }

            return false;
        }

        void HandleMoveTouch(TouchControl touch, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
            int id = touch.touchId.ReadValue();

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

                        if (dist < joystickDeadzone * maxJoystickDistance)
                        {
                            MoveInput = Vector2.zero;
                        }
                        else
                        {
                            float clampedDist = Mathf.Min(dist, maxJoystickDistance);
                            MoveInput = delta.normalized * (clampedDist / maxJoystickDistance);
                        }
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

        void HandleAimFireTouch(TouchControl touch, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos)
        {
            int id = touch.touchId.ReadValue();

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
                        AimDelta += delta * aimSensitivity * 0.01f * dpiScale;
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

        void RecalculateButtonRects()
        {
            float fireSize = fireButtonSize * dpiScale;
            float jumpSize = jumpButtonSize * dpiScale;
            float padSize = padButtonSize * dpiScale;
            float reloadSize = reloadButtonSize * dpiScale;
            float pingSize = pingButtonSize * dpiScale;
            float pauseSize = pauseButtonSize * dpiScale;
            float padding = 20f * dpiScale;

            if (!leftHandedMode)
            {
                // Right-handed (default)
                fireButtonRect = new Rect(Screen.width - fireSize - padding, padding, fireSize, fireSize);
                jumpButtonRect = new Rect(fireButtonRect.x - jumpSize - 10f * dpiScale, padding + (fireSize - jumpSize) * 0.5f, jumpSize, jumpSize);
                padButtonRect = new Rect(fireButtonRect.x - padSize - 10f * dpiScale, fireButtonRect.y + fireSize + 10f * dpiScale, padSize, padSize);
                reloadButtonRect = new Rect(padding, Screen.height - reloadSize - padding, reloadSize, reloadSize);
                pingButtonRect = new Rect(Screen.width - pingSize - padding, Screen.height - pingSize - padding, pingSize, pingSize);
                pauseButtonRect = new Rect(padding, padding, pauseSize, pauseSize);
            }
            else
            {
                // Left-handed (mirrored)
                fireButtonRect = new Rect(padding, padding, fireSize, fireSize);
                jumpButtonRect = new Rect(fireButtonRect.x + fireSize + 10f * dpiScale, padding + (fireSize - jumpSize) * 0.5f, jumpSize, jumpSize);
                padButtonRect = new Rect(fireButtonRect.x + fireSize + 10f * dpiScale, fireButtonRect.y + fireSize + 10f * dpiScale, padSize, padSize);
                reloadButtonRect = new Rect(Screen.width - reloadSize - padding, Screen.height - reloadSize - padding, reloadSize, reloadSize);
                pingButtonRect = new Rect(padding, Screen.height - pingSize - padding, pingSize, pingSize);
                pauseButtonRect = new Rect(Screen.width - pauseSize - padding, padding, pauseSize, pauseSize);
            }
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
            gyroAimEnabled = enabled;
            if (enabled && SystemInfo.supportsGyroscope)
                UnityEngine.Input.gyro.enabled = true;
            SaveSettings();
        }

        void LoadSettings()
        {
            leftHandedMode = PlayerPrefs.GetInt("LeftHandedMode", 0) == 1;
            aimSensitivity = PlayerPrefs.GetFloat("AimSensitivity", 1f);
            invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
            gyroAimEnabled = PlayerPrefs.GetInt("GyroAim", 0) == 1;
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("LeftHandedMode", leftHandedMode ? 1 : 0);
            PlayerPrefs.SetFloat("AimSensitivity", aimSensitivity);
            PlayerPrefs.SetInt("InvertY", invertY ? 1 : 0);
            PlayerPrefs.SetInt("GyroAim", gyroAimEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        void OnRectTransformDimensionsChange()
        {
            RecalculateButtonRects();
        }

        // Debug visualization (editor only)
        void OnGUI()
        {
            if (!Application.isEditor) return;

            GUI.color = new Color(0, 1, 0, 0.3f);
            GUI.Box(fireButtonRect, "FIRE");
            GUI.Box(jumpButtonRect, "JUMP");
            GUI.Box(padButtonRect, "PAD");
            GUI.Box(reloadButtonRect, "RELOAD");
            GUI.Box(pingButtonRect, "PING");
            GUI.Box(pauseButtonRect, "PAUSE");

            // Joystick visualization
            if (moveTouchId != -1)
            {
                GUI.color = new Color(0, 1, 0, 0.3f);
                GUI.DrawTexture(new Rect(moveStartPos.x - maxJoystickDistance, moveStartPos.y - maxJoystickDistance, maxJoystickDistance * 2, maxJoystickDistance * 2), Texture2D.whiteTexture);
                GUI.color = new Color(0, 1, 0, 0.6f);
                GUI.DrawTexture(new Rect(moveCurrentPos.x - 20, moveCurrentPos.y - 20, 40, 40), Texture2D.whiteTexture);
            }
        }
    }
}