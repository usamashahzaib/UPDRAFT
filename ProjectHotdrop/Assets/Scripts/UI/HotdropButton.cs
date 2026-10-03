using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hotdrop.UI
{
    /// <summary>Visual weight of a button. Disabled, cooldown and loading are states, not variants.</summary>
    public enum ButtonVariant { Primary, Secondary, Quiet, Destructive }

    /// <summary>The states a button can be in. Exactly one is active at a time.</summary>
    public enum HotdropButtonState { Normal, Pressed, Held, Disabled, Cooldown, Loading }

    /// <summary>
    /// The one button the rest of the game uses.
    ///
    /// It owns six states - normal, pressed, held, disabled, cooldown and loading - because
    /// each of them needs a different answer to "can I use this right now, and what will happen
    /// if I tap it". Colour alone never carries that: the loading state shows a sweep, the
    /// cooldown state shows remaining time, and disabled state dims and stops accepting taps.
    ///
    /// It also enforces the 48 dp interactive floor from the design system, whether it is
    /// laid out by a layout group or pinned by anchors.
    /// </summary>
    [DisallowMultipleComponent]
    public class HotdropButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Style")]
        [SerializeField] ButtonVariant variant = ButtonVariant.Primary;
        [SerializeField] UiTone accent = UiTone.Friendly;
        [SerializeField] bool iconOnly;
        [Tooltip("Seconds a press must be held before the button reports Held instead of Pressed.")]
        [SerializeField] float holdThresholdSeconds = 0.18f;

        [Header("Parts (assigned by UIKit.Button)")]
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] UiBorder border;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] CanvasGroup contentGroup;
        [SerializeField] Image cooldownSweep;
        [SerializeField] GameObject spinner;

        float holdElapsed;
        float cooldownRemaining;
        float cooldownTotal;
        bool pointerDown;
        bool loadingFlag;
        bool interactableFlag = true;
        LayoutElement layoutElement;

        /// <summary>Raised when the press is held past <see cref="HoldThresholdSeconds"/>.</summary>
        public event System.Action Held;

        /// <summary>Raised when a press that started on this button ends on it.</summary>
        public event System.Action Pressed;

        /// <summary>Which visual weight this button uses.</summary>
        public ButtonVariant Variant
        {
            get => variant;
            set { if (variant == value) return; variant = value; ApplyStyle(); }
        }

        /// <summary>Signal tone for borders, sweep and spinner.</summary>
        public UiTone Accent { get => accent; set { if (accent == value) return; accent = value; ApplyStyle(); } }

        /// <summary>Icon-only buttons keep a text label for accessibility but hide it.</summary>
        public bool IconOnly { get => iconOnly; set { iconOnly = value; ApplyStyle(); } }

        /// <summary>Seconds before a press counts as Held.</summary>
        public float HoldThresholdSeconds { get => holdThresholdSeconds; set => holdThresholdSeconds = Mathf.Max(0f, value); }

        /// <summary>The state currently displayed.</summary>
        public HotdropButtonState State { get; private set; } = HotdropButtonState.Normal;

        /// <summary>Seconds left on the cooldown; zero when not cooling down.</summary>
        public float CooldownRemaining => cooldownRemaining;

        /// <summary>Total length of the running cooldown, for the sweep.</summary>
        public float CooldownTotal => cooldownTotal;

        /// <summary>True while the button refuses taps for a reason it displays.</summary>
        public bool IsBlocked => State == HotdropButtonState.Disabled || State == HotdropButtonState.Loading || State == HotdropButtonState.Cooldown;

        /// <summary>True while a cooldown is running.</summary>
        public bool IsCoolingDown => State == HotdropButtonState.Cooldown;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            // The state machine here owns the visuals; Unity's tint transition would fight it.
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                if (button.targetGraphic == null) button.targetGraphic = background;
            }
            interactableFlag = button == null || button.interactable;
            EnforceMinimumSize();
            ApplyStyle();
        }

        void OnEnable() => EnforceMinimumSize();

        void OnRectTransformDimensionsChange() => EnforceMinimumSize();

        void Update() => Tick(Time.unscaledDeltaTime);

        /// <summary>Advances hold and cooldown timers. Exposed so tests can step deterministically.</summary>
        public void Tick(float unscaledDelta)
        {
            if (unscaledDelta <= 0f) return;

            if (State == HotdropButtonState.Pressed && pointerDown)
            {
                holdElapsed += unscaledDelta;
                if (holdElapsed >= holdThresholdSeconds)
                {
                    SetState(HotdropButtonState.Held);
                    Held?.Invoke();
                }
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - unscaledDelta);
                UpdateCooldownVisual();
                if (cooldownRemaining <= 0f) SetState(interactableFlag ? HotdropButtonState.Normal : HotdropButtonState.Disabled);
            }
        }

        // ------------------------------------------------------------ public control

        /// <summary>
        /// Hands the button its parts. <see cref="UIKit.Button"/> builds the geometry; this keeps
        /// the component usable when a scene authors the parts by hand instead.
        /// </summary>
        public void Initialize(Image backgroundImage, TextMeshProUGUI captionLabel, CanvasGroup content,
            GameObject loadingSpinner, RectTransform borderFrame)
        {
            background = backgroundImage;
            label = captionLabel;
            contentGroup = content;
            spinner = loadingSpinner;
            border = UIKit.FrameOf(borderFrame);
            if (button == null) button = GetComponent<Button>();
            if (button != null && button.targetGraphic == null) button.targetGraphic = background;
            EnforceMinimumSize();
            ApplyStyle();
        }

        /// <summary>Replaces the caption. Icon-only buttons keep it for accessibility.</summary>
        public void SetCaption(string caption)
        {
            if (label != null) label.text = caption ?? "";
        }

        /// <summary>
        /// Enables or disables the button. Disabled buttons dim, stop accepting taps and report
        /// <see cref="HotdropButtonState.Disabled"/>.
        /// </summary>
        public void SetInteractable(bool value)
        {
            interactableFlag = value;
            if (button != null) button.interactable = value;
            SetState(value ? HotdropButtonState.Normal : HotdropButtonState.Disabled);
        }

        /// <summary>Shows or hides the loading affordance and blocks taps while it is on.</summary>
        public void SetLoading(bool loading)
        {
            loadingFlag = loading;
            if (spinner != null) spinner.SetActive(loading);
            if (button != null) button.interactable = !loading && interactableFlag && cooldownRemaining <= 0f;
            // Resting rather than Normal: a cooldown may still be running underneath the spinner.
            SetState(loading ? HotdropButtonState.Loading : RestingState());
        }

        /// <summary>True while the loading affordance is showing.</summary>
        public bool IsLoading => State == HotdropButtonState.Loading;

        /// <summary>
        /// Starts a cooldown: the button is unusable for <paramref name="seconds"/> and shows the
        /// time remaining, so a player is never left tapping a dead control.
        /// </summary>
        public void BeginCooldown(float seconds)
        {
            cooldownTotal = Mathf.Max(0f, seconds);
            cooldownRemaining = cooldownTotal;
            if (cooldownRemaining <= 0f) { CancelCooldown(); return; }
            if (button != null) button.interactable = false;
            SetState(HotdropButtonState.Cooldown);
            UpdateCooldownVisual();
        }

        /// <summary>Clears a running cooldown and returns to normal or disabled.</summary>
        public void CancelCooldown()
        {
            cooldownRemaining = 0f;
            cooldownTotal = 0f;
            UpdateCooldownVisual();
            SetState(loadingFlag ? HotdropButtonState.Loading
                : interactableFlag ? HotdropButtonState.Normal : HotdropButtonState.Disabled);
        }

        /// <summary>Forces the Held state for as long as a control is physically held.</summary>
        public void SetHeld(bool held)
        {
            if (held) SetState(HotdropButtonState.Held);
            else if (State == HotdropButtonState.Held) SetState(pointerDown ? HotdropButtonState.Pressed : RestingState());
        }

        // ------------------------------------------------------------ state machine

        void SetState(HotdropButtonState next)
        {
            // Loading wins over cooldown, and every blocked state wins over the press states.
            if (next == HotdropButtonState.Pressed && !AcceptsPress()) return;
            if (next == HotdropButtonState.Held && !AcceptsPress()) return;
            if (State == next) return;
            State = next;
            if (next == HotdropButtonState.Pressed) holdElapsed = 0f;
            ApplyStyle();
        }

        bool AcceptsPress() => interactableFlag && !loadingFlag && cooldownRemaining <= 0f;

        HotdropButtonState RestingState()
        {
            if (loadingFlag) return HotdropButtonState.Loading;
            if (cooldownRemaining > 0f) return HotdropButtonState.Cooldown;
            return interactableFlag ? HotdropButtonState.Normal : HotdropButtonState.Disabled;
        }

        // ------------------------------------------------------------ pointer input

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!AcceptsPress()) return;
            pointerDown = true;
            SetState(HotdropButtonState.Pressed);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pointerDown) return;
            pointerDown = false;
            bool wasHeld = State == HotdropButtonState.Held;
            SetState(RestingState());
            if (!wasHeld) Pressed?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Dragging off a held control releases it, as every native control does.
            if (!pointerDown) return;
            pointerDown = false;
            if (State == HotdropButtonState.Pressed || State == HotdropButtonState.Held) SetState(RestingState());
        }

        // ------------------------------------------------------------ presentation

        /// <summary>Repaints every part for the current variant, tone and state.</summary>
        public void ApplyStyle()
        {
            if (background != null)
            {
                background.color = BackgroundColor();
                var sprite = UIRoundedSprites.RoundedRect(UIDesignTokens.Dp(UIDesignTokens.RadiusSmDp),
                    CanvasPixelScale(), (int)RadiusPixels());
                if (background.sprite != sprite) background.sprite = sprite;
                background.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            }

            if (label != null)
            {
                label.color = LabelColor();
                if (iconOnly) label.gameObject.SetActive(false);
            }

            if (border != null)
            {
                border.Tint(BorderColor());
                border.Visible = BorderVisible();
                border.SetWeight(BorderWeightUnits());
            }

            if (contentGroup != null) contentGroup.alpha = ContentAlpha();
            if (spinner != null) spinner.SetActive(loadingFlag || State == HotdropButtonState.Loading);
            UpdateCooldownVisual();
        }

        /// <summary>Pixels per canvas unit, so generated corners stay crisp at any density.</summary>
        public float CanvasPixelScale()
        {
            var canvas = GetComponentInParent<Canvas>();
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            float factor = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize
                ? scaler.scaleFactor
                : 1f;
            return Mathf.Max(0.05f, factor);
        }

        float RadiusPixels() => UIDesignTokens.Dp(UIDesignTokens.RadiusSmDp) * CanvasPixelScale();

        Color BackgroundColor()
        {
            if (State == HotdropButtonState.Disabled) return UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceDisabled, UIDesignTokens.DisabledOpacity);
            if (State == HotdropButtonState.Loading) return UIDesignTokens.WithAlpha(NormalFill(), UIDesignTokens.DisabledOpacity);
            if (State == HotdropButtonState.Cooldown) return UIDesignTokens.WithAlpha(NormalFill(), 0.35f);

            switch (variant)
            {
                case ButtonVariant.Primary:
                    return State == HotdropButtonState.Normal ? NormalFill() : UIDesignTokens.ActionCyanPressed;
                case ButtonVariant.Destructive:
                    return State == HotdropButtonState.Normal ? UIDesignTokens.HazardCoral : UIDesignTokens.HazardCoralPressed;
                case ButtonVariant.Quiet:
                    return State == HotdropButtonState.Normal ? new Color(0f, 0f, 0f, 0f) : UIDesignTokens.WithAlpha(UIDesignTokens.SurfacePanel, 0.7f);
                default:
                    return State == HotdropButtonState.Normal ? UIDesignTokens.SurfacePanel : UIDesignTokens.SurfaceSelected;
            }
        }

        Color NormalFill() => variant switch
        {
            ButtonVariant.Primary => UIDesignTokens.ActionCyan,
            ButtonVariant.Destructive => UIDesignTokens.HazardCoral,
            ButtonVariant.Quiet => new Color(0f, 0f, 0f, 0f),
            _ => UIDesignTokens.SurfacePanel,
        };

        Color LabelColor()
        {
            if (State == HotdropButtonState.Disabled) return UIDesignTokens.TextMuted;
            if (State == HotdropButtonState.Loading || State == HotdropButtonState.Cooldown) return UIDesignTokens.TextSecondary;
            switch (variant)
            {
                case ButtonVariant.Primary: return UIDesignTokens.TextOn(UIDesignTokens.ActionCyan);
                case ButtonVariant.Destructive: return UIDesignTokens.TextOn(UIDesignTokens.HazardCoral);
                case ButtonVariant.Quiet: return State == HotdropButtonState.Normal ? UIDesignTokens.TextSecondary : UIDesignTokens.TextPrimary;
                default: return UIDesignTokens.TextPrimary;
            }
        }

        Color BorderColor()
        {
            if (State == HotdropButtonState.Disabled) return UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceDisabled, UIDesignTokens.BorderOpacity);
            if (State == HotdropButtonState.Held || State == HotdropButtonState.Pressed)
                return UIDesignTokens.ForTone(accent);
            if (State == HotdropButtonState.Cooldown) return UIDesignTokens.BorderColor();
            return variant == ButtonVariant.Primary ? UIDesignTokens.ActionCyan
                : variant == ButtonVariant.Destructive ? UIDesignTokens.HazardCoral
                : variant == ButtonVariant.Quiet ? UIDesignTokens.WithAlpha(UIDesignTokens.SurfaceStructural, 0.4f)
                : UIDesignTokens.BorderColor();
        }

        bool BorderVisible() => variant != ButtonVariant.Quiet || State != HotdropButtonState.Normal;

        float BorderWeightUnits() => State == HotdropButtonState.Held || State == HotdropButtonState.Pressed
            ? UIDesignTokens.BorderBoldUnits
            : UIDesignTokens.BorderNormalUnits;

        float ContentAlpha()
        {
            if (State == HotdropButtonState.Disabled) return UIDesignTokens.DisabledOpacity;
            if (State == HotdropButtonState.Loading) return 0.6f;
            return 1f;
        }

        void UpdateCooldownVisual()
        {
            if (cooldownSweep == null) return;
            bool running = cooldownRemaining > 0f && cooldownTotal > 0f;
            cooldownSweep.gameObject.SetActive(running);
            if (running) cooldownSweep.fillAmount = Mathf.Clamp01(cooldownRemaining / cooldownTotal);
        }

        // ------------------------------------------------------------ sizing

        /// <summary>
        /// Holds the button at or above the 48 dp interactive floor. Under a layout group the
        /// floor goes into a <see cref="LayoutElement"/> instead of the RectTransform, because a
        /// layout group owns the transform and would overwrite it.
        /// </summary>
        public void EnforceMinimumSize()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            bool layoutDriven = rt.GetComponentInParent<LayoutGroup>() != null;
            if (layoutDriven)
            {
                if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
                layoutElement.minWidth = Mathf.Max(layoutElement.minWidth, UiMetrics.MinTouchUnits);
                layoutElement.minHeight = Mathf.Max(layoutElement.minHeight, UiMetrics.MinTouchUnits);
                if (layoutElement.preferredHeight <= 0f) layoutElement.preferredHeight = UiMetrics.MinTouchUnits;
                return;
            }

            var size = rt.sizeDelta;
            // Only axes the anchors do not stretch can be corrected here.
            if (Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x) && size.x < UiMetrics.MinTouchUnits) size.x = UiMetrics.MinTouchUnits;
            if (Mathf.Approximately(rt.anchorMin.y, rt.anchorMax.y) && size.y < UiMetrics.MinTouchUnits) size.y = UiMetrics.MinTouchUnits;
            if (size != rt.sizeDelta) rt.sizeDelta = size;
        }
    }
}
