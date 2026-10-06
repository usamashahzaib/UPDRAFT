using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hotdrop.UI
{
    /// <summary>How long a transition is allowed to take.</summary>
    public enum MotionRole { Quick, Standard, Emphasis }

    /// <summary>What a transition is animating.</summary>
    public enum MotionChannel { Opacity, Translation, Scale }

    /// <summary>
    /// The whole interface's motion vocabulary: ease-out curves, three durations all inside the
    /// 180-240 ms band the design system asks for, and a Reduce Motion mode that replaces every
    /// translation and scale with a plain fade.
    ///
    /// Nothing here allocates per frame: <see cref="UiMotion"/> is a plain struct advanced by a
    /// single driver, so a screen can run several transitions for the cost of one component.
    /// </summary>
    public static class UIMotionSettings
    {
        /// <summary>
        /// Player-facing Reduce Motion preference. Persisted by the settings screen (Prompt 06)
        /// and honoured by every transition from this foundation onwards.
        /// </summary>
        public static bool ReduceMotion
        {
            get => PlayerPrefs.GetInt(ReduceMotionKey, 0) == 1;
            set { PlayerPrefs.SetInt(ReduceMotionKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public const string ReduceMotionKey = "hotdrop.reduceMotion";

        /// <summary>Test hook: forces Reduce Motion without touching disk.</summary>
        public static bool? OverrideForTests;

        /// <summary>The value transitions should actually obey.</summary>
        public static bool ReduceMotionEnabled => OverrideForTests ?? ReduceMotion;
    }

    public static class UIMotion
    {
        /// <summary>Duration in seconds for a role. Always within 180-240 ms.</summary>
        public static float Duration(MotionRole role) => role switch
        {
            MotionRole.Quick => UIDesignTokens.MotionQuickSeconds,
            MotionRole.Emphasis => UIDesignTokens.MotionEmphasisSeconds,
            _ => UIDesignTokens.MotionStandardSeconds,
        };

        /// <summary>The ease-out curve every transition uses: fast start, soft landing.</summary>
        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        /// <summary>Linear ramp, for fades where ease-out adds nothing.</summary>
        public static float Linear(float t) => Mathf.Clamp01(t);

        /// <summary>True when the channel should animate at all under the current preference.</summary>
        public static bool Animates(MotionChannel channel) =>
            channel == MotionChannel.Opacity || !UIMotionSettings.ReduceMotionEnabled;

        /// <summary>
        /// Value for a channel at normalised time <paramref name="t"/>. Reduce Motion holds
        /// translation and scale at their destination's *start* value - the element simply
        /// appears in place - while opacity still fades, so nothing moves for a player who asked
        /// for less movement.
        /// </summary>
        public static float Value(float from, float to, float t, MotionChannel channel)
        {
            t = Mathf.Clamp01(t);
            if (channel != MotionChannel.Opacity && !Animates(channel))
                return from;
            float eased = channel == MotionChannel.Opacity ? Linear(t) : EaseOut(t);
            return Mathf.LerpUnclamped(from, to, eased);
        }

        /// <summary>How far a panel travels while entering, as a fraction of its own height.</summary>
        public static float EnterOffset(RectTransform rect) =>
            rect == null ? 0f : Mathf.Abs(rect.rect.height) * UIDesignTokens.MotionEnterSlideFraction;

        /// <summary>
        /// Begins a transition. Under Reduce Motion the travel distance collapses to zero and the
        /// duration stays inside the same band, so a fade is never slower than the movement it replaces.
        /// </summary>
        public static UiMotion Play(RectTransform rect, CanvasGroup group, float from, float to, MotionRole role,
            MotionChannel channel, Vector2 fromOffset, Vector2 toOffset, float fromScale, float toScale)
        {
            bool reduce = !Animates(channel);
            return new UiMotion
            {
                rect = rect,
                group = group,
                channel = channel,
                role = role,
                elapsed = 0f,
                duration = Duration(role),
                from = from,
                to = to,
                fromOffset = reduce ? Vector2.zero : fromOffset,
                toOffset = reduce ? Vector2.zero : toOffset,
                fromScale = reduce ? 1f : fromScale,
                toScale = reduce ? 1f : toScale,
                active = true,
            };
        }

        /// <summary>Shorthand for a fade, the only channel Reduce Motion keeps.</summary>
        public static UiMotion Fade(CanvasGroup group, RectTransform rect, float from, float to, MotionRole role,
            System.Action onComplete = null)
        {
            var motion = Play(rect, group, from, to, role, MotionChannel.Opacity, Vector2.zero, Vector2.zero, 1f, 1f);
            motion.onComplete = onComplete;
            return motion;
        }
    }

    /// <summary>
    /// One running transition. Advance it with <see cref="Tick"/>; a single driver component
    /// advances all of a screen's transitions.
    /// </summary>
    public struct UiMotion
    {
        public RectTransform rect;
        public CanvasGroup group;
        public MotionChannel channel;
        public MotionRole role;
        public float elapsed;
        public float duration;
        public float from, to;
        public Vector2 fromOffset, toOffset;
        public float fromScale, toScale;
        public bool active;
        /// <summary>Run once when the transition reaches its end state.</summary>
        public System.Action onComplete;

        /// <summary>Normalised progress so far.</summary>
        public float Progress => duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

        /// <summary>True once the transition has run its course.</summary>
        public bool Complete => !active || Progress >= 1f;

        /// <summary>Advances the transition and writes it onto its target.</summary>
        public void Tick(float unscaledDelta)
        {
            if (!active) return;
            elapsed += Mathf.Max(0f, unscaledDelta);
            float value = UIMotion.Value(from, to, Progress, channel);

            if (channel == MotionChannel.Opacity)
            {
                if (group != null) group.alpha = value;
            }
            else
            {
                if (rect != null)
                {
                    float travel = UIMotion.Animates(MotionChannel.Translation) ? UIMotion.EaseOut(Progress) : 0f;
                    rect.anchoredPosition = Vector2.LerpUnclamped(fromOffset, toOffset, travel);
                    float scale = UIMotion.Value(fromScale, toScale, Progress, MotionChannel.Scale);
                    rect.localScale = new Vector3(scale, scale, 1f);
                }
            }

            if (elapsed >= duration) Settle();
        }

        /// <summary>Jumps to the end state and stops.</summary>
        public void Finish()
        {
            elapsed = duration;
            Settle();
        }

        void Settle()
        {
            active = false;
            if (channel == MotionChannel.Opacity)
            {
                if (group != null) group.alpha = to;
            }
            else if (rect != null)
            {
                rect.anchoredPosition = toOffset;
                rect.localScale = new Vector3(toScale, toScale, 1f);
            }
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }

    /// <summary>Advances a screen's transitions on unscaled time, so a paused game still animates UI.</summary>
    public class UiMotionDriver : MonoBehaviour
    {
        readonly System.Collections.Generic.List<UiMotion> running = new System.Collections.Generic.List<UiMotion>();

        /// <summary>Transitions currently running.</summary>
        public int Count => running.Count;

        /// <summary>Queues a transition and returns its index.</summary>
        public int Add(UiMotion motion)
        {
            running.Add(motion);
            return running.Count - 1;
        }

        /// <summary>Queues a transition and drops it when it finishes.</summary>
        public UiMotion Play(UiMotion motion)
        {
            Add(motion);
            return motion;
        }

        /// <summary>Advances every running transition. Call from tests to step deterministically.</summary>
        public void Tick(float unscaledDelta)
        {
            for (int i = running.Count - 1; i >= 0; i--)
            {
                var motion = running[i];
                motion.Tick(unscaledDelta);
                if (!motion.active) running.RemoveAt(i);
                else running[i] = motion;
            }
        }

        /// <summary>Settles every active motion before a screen begins its outgoing transition.</summary>
        public void FinishAll()
        {
            for (int i = 0; i < running.Count; i++)
            {
                var motion = running[i];
                motion.Finish();
            }
            running.Clear();
        }

        /// <summary>Stops active motions without settling or invoking completion callbacks.</summary>
        public void CancelAll() => running.Clear();

        void Update() => Tick(Time.unscaledDeltaTime);
    }

    /// <summary>
    /// Gives shared full-screen menus their entry motion after the caller has finished building
    /// the page. The opaque root stays still while its visual layers enter at different depths.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiScreenEnterMotion : MonoBehaviour
    {
        Coroutine pendingEntry;
        bool exiting;
        readonly Dictionary<RectTransform, Vector2> restingPositions = new Dictionary<RectTransform, Vector2>();

        public bool IsExiting => exiting;

        void OnEnable()
        {
            exiting = false;
            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            // Delay one frame so factory callers can finish adding their direct visual layers.
            if (pendingEntry == null) pendingEntry = StartCoroutine(EnterAfterBuild());
        }

        void OnDisable()
        {
            if (pendingEntry == null) return;
            StopCoroutine(pendingEntry);
            pendingEntry = null;
        }

        IEnumerator EnterAfterBuild()
        {
            yield return null;
            pendingEntry = null;
            Enter();
        }

        void Enter()
        {
            if (exiting) return;
            // MenuLayout callers populate the root synchronously after Create returns. This runs
            // before the next rendered frame, so the complete page enters as one transition.
            Canvas.ForceUpdateCanvases();
            var root = transform as RectTransform;
            if (root == null) return;

            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
            var driver = GetComponent<UiMotionDriver>();
            if (driver == null) driver = gameObject.AddComponent<UiMotionDriver>();
            driver.FinishAll();
            CaptureRestingPositions();

            if (UIMotionSettings.ReduceMotionEnabled)
            {
                group.alpha = 0f;
                driver.Play(UIMotion.Fade(group, null, 0f, 1f, MotionRole.Standard));
                return;
            }

            group.alpha = 1f;
            float travel = UIMotion.EnterOffset(root);
            if (travel <= 0f) return;

            foreach (var pair in restingPositions)
            {
                var layer = pair.Key;
                if (layer == null) continue;
                Vector2 restingPosition = pair.Value;
                Vector2 enteringPosition = restingPosition + Vector2.down * (travel * ParallaxDepth(layer.name));
                layer.anchoredPosition = enteringPosition;
                driver.Play(UIMotion.Play(layer, null, 0f, 1f, MotionRole.Standard,
                    MotionChannel.Translation, enteringPosition, restingPosition, 1f, 1f));
            }
        }

        /// <summary>Slides the menu layers out, then lets the caller hide or destroy this root.</summary>
        public void Exit(Action onComplete)
        {
            if (exiting) return;
            exiting = true;
            if (pendingEntry != null)
            {
                StopCoroutine(pendingEntry);
                pendingEntry = null;
            }
            if (!gameObject.activeInHierarchy)
            {
                CompleteExit(onComplete);
                return;
            }
            var root = transform as RectTransform;
            if (root == null) { onComplete?.Invoke(); return; }

            var group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;

            var driver = GetComponent<UiMotionDriver>();
            if (driver == null) driver = gameObject.AddComponent<UiMotionDriver>();
            driver.FinishAll();
            CaptureRestingPositions();

            if (UIMotionSettings.ReduceMotionEnabled)
            {
                group.alpha = 1f;
                driver.Play(UIMotion.Fade(group, null, 1f, 0f, MotionRole.Standard,
                    () => CompleteExit(onComplete)));
                return;
            }

            group.alpha = 1f;
            float travel = UIMotion.EnterOffset(root);
            if (restingPositions.Count == 0 || travel <= 0f) { CompleteExit(onComplete); return; }

            int remaining = restingPositions.Count;
            foreach (var pair in restingPositions)
            {
                var layer = pair.Key;
                if (layer == null) { remaining--; continue; }
                Vector2 leavingPosition = layer.anchoredPosition;
                Vector2 exitPosition = leavingPosition + Vector2.up * (travel * ParallaxDepth(layer.name));
                Action layerDone = --remaining == 0 ? () => CompleteExit(onComplete) : null;
                var motion = UIMotion.Play(layer, null, 0f, 1f, MotionRole.Standard,
                    MotionChannel.Translation, leavingPosition, exitPosition, 1f, 1f);
                motion.onComplete = layerDone;
                driver.Play(motion);
            }
        }

        /// <summary>Reverses an in-flight close when a reusable menu is shown again.</summary>
        public void Reopen()
        {
            if (!exiting) return;
            exiting = false;
            GetComponent<UiMotionDriver>()?.CancelAll();
            RestoreRestingPositions();
            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            Enter();
        }

        void CaptureRestingPositions()
        {
            restingPositions.Clear();
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                var layer = child as RectTransform;
                if (layer != null) restingPositions[layer] = layer.anchoredPosition;
            }
        }

        void RestoreRestingPositions()
        {
            foreach (var pair in restingPositions)
                if (pair.Key != null) pair.Key.anchoredPosition = pair.Value;
        }

        void CompleteExit(Action onComplete)
        {
            RestoreRestingPositions();
            onComplete?.Invoke();
        }

        /// <summary>Runs the outgoing motion when available, or completes immediately for older roots.</summary>
        public static void Exit(GameObject root, Action onComplete)
        {
            var transition = root != null ? root.GetComponent<UiScreenEnterMotion>() : null;
            if (transition == null) onComplete?.Invoke();
            else transition.Exit(onComplete);
        }

        /// <summary>Reverses a close in progress for reusable full-screen menu roots.</summary>
        public static void Reopen(GameObject root) => root?.GetComponent<UiScreenEnterMotion>()?.Reopen();

        static float ParallaxDepth(string layerName)
        {
            if (string.Equals(layerName, "Nav_Scroll", StringComparison.Ordinal)) return 1f;
            if (string.Equals(layerName, "Nav_Bar", StringComparison.Ordinal)) return 0.45f;
            if (string.Equals(layerName, "StatusFill", StringComparison.Ordinal) ||
                string.Equals(layerName, "Nav_Status", StringComparison.Ordinal)) return 0.25f;
            if (layerName.IndexOf("backdrop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                layerName.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0) return 0.15f;
            return 0.65f;
        }
    }
}
