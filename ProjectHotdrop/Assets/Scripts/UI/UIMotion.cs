using System;
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

        const string ReduceMotionKey = "hotdrop.reduceMotion";

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
                    rect.anchoredPosition = Vector2.LerpUnclamped(fromOffset, toOffset, Progress);
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

        void Update() => Tick(Time.unscaledDeltaTime);
    }
}
