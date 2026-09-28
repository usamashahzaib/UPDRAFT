using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Updraft.Input;

namespace Updraft.Tests
{
    /// <summary>
    /// Gate A input-hygiene regressions that do not need a running scene.
    /// Full multitouch ownership (second finger acquires fire while the first owns
    /// movement), drag-fire and pause integration are covered by the PlayMode
    /// input regression; these tests pin the background/foreground release and
    /// the layout/handedness rules at unit level.
    /// </summary>
    public class TouchInputHygieneTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        static TouchInputManager CreateManager(bool naturalLifecycle)
        {
            var go = new GameObject("TouchInputHygiene");
            go.SetActive(false); // EditMode runs no lifecycle; tests drive callbacks explicitly
            var input = go.AddComponent<TouchInputManager>();
            Invoke(input, "Awake");
            return input;
        }

        static void Set(TouchInputManager input, string field, object value) =>
            typeof(TouchInputManager).GetField(field, Hidden).SetValue(input, value);

        static void SetProperty(TouchInputManager input, string name, object value)
        {
            var property = typeof(TouchInputManager).GetProperty(name, Hidden | BindingFlags.Public);
            if (property != null) { property.SetValue(input, value); return; }
            // Auto-property backed by a compiler field (e.g. "<FireHeld>k__BackingField").
            var field = typeof(TouchInputManager).GetField($"<{name}>k__BackingField", Hidden);
            Assert.IsNotNull(field, $"Neither a settable property nor backing field '{name}' exists.");
            field.SetValue(input, value);
        }

        static void Invoke(TouchInputManager input, string method, object arg = null)
        {
            var candidate = typeof(TouchInputManager).GetMethods(Hidden).FirstOrDefault(m =>
            {
                if (m.Name != method) return false;
                var parameters = m.GetParameters();
                return arg == null ? parameters.Length == 0
                    : parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(arg);
            });
            Assert.IsNotNull(candidate, $"TouchInputManager.{method} not found.");
            candidate.Invoke(input, arg == null ? null : new[] { arg });
        }

        [SetUp]
        public void ClearPrefs()
        {
            PlayerPrefs.DeleteKey("LeftHandedMode");
            PlayerPrefs.SetFloat("AimSensitivity", 1f);
        }

        [TearDown]
        public void DestroyManager()
        {
            var input = Object.FindFirstObjectByType<TouchInputManager>();
            if (input != null) Object.DestroyImmediate(input.gameObject);
        }

        [Test]
        public void LosingFocus_ReleasesStuckFireMoveAimAndPad()
        {
            var input = CreateManager(false);

            Set(input, "fireTouchId", 3);
            SetProperty(input, "FireHeld", true);
            SetProperty(input, "MoveInput", new Vector2(0.8f, 0.2f));
            Set(input, "moveTouchId", 1);
            Set(input, "aimTouchId", 2);
            Set(input, "padTouchId", 4);
            SetProperty(input, "PadPreviewActive", true);

            // Android calls this when the app loses focus mid-touch; the matching
            // Ended/Canceled events never arrive after a resume.
            Invoke(input, "OnApplicationFocus", false);

            Assert.IsFalse(input.FireHeld, "Backgrounding mid-fire must release the trigger.");
            Assert.AreEqual(Vector2.zero, input.MoveInput, "Backgrounding mid-move must stop movement.");
            Assert.IsFalse(input.PadPreviewActive, "Backgrounding must drop an in-progress pad hold.");
        }

        [Test]
        public void Disabling_ClearsPerFramePressesAndOwnedTouches()
        {
            // EditMode runs no MonoBehaviour lifecycle, so OnDisable is invoked
            // directly; the contract under test is its cleanup.
            var input = CreateManager(false);

            SetProperty(input, "FirePressedThisFrame", true);
            SetProperty(input, "JumpPressed", true);
            SetProperty(input, "PausePressed", true);
            Set(input, "moveTouchId", 1);
            Set(input, "fireTouchId", 2);

            Invoke(input, "OnDisable"); // what Unity runs when the component is disabled

            Assert.IsFalse(input.FirePressedThisFrame, "A stale press must not fire again when input is re-enabled.");
            Assert.IsFalse(input.JumpPressed);
            Assert.IsFalse(input.PausePressed);
            Assert.IsFalse(input.FireHeld);
            Assert.AreEqual(Vector2.zero, input.MoveInput);
        }

        [Test]
        public void LeftHandedLayout_MirrorsButtons_KeepsMinimumTargets()
        {
            var input = CreateManager(false);

            float dpiScale = (float)typeof(TouchInputManager).GetField("dpiScale", Hidden).GetValue(input);
            Rect fireRight = GetRect(input, "fireButtonRect");
            Rect pauseRight = GetRect(input, "pauseButtonRect");
            Assert.Greater(fireRight.center.x, Screen.width * 0.5f, "Default layout puts fire on the right.");
            Assert.Less(pauseRight.center.x, Screen.width * 0.5f, "Default layout puts pause on the left.");

            input.SetLeftHanded(true);

            Rect fireLeft = GetRect(input, "fireButtonRect");
            Rect pauseLeft = GetRect(input, "pauseButtonRect");
            Assert.Less(fireLeft.center.x, Screen.width * 0.5f, "Left-handed layout must mirror fire to the left.");
            Assert.Greater(pauseLeft.center.x, Screen.width * 0.5f, "Left-handed layout must mirror pause to the right.");

            foreach (var name in new[] { "fireButtonRect", "jumpButtonRect", "padButtonRect", "reloadButtonRect", "pingButtonRect", "pauseButtonRect" })
            {
                Rect rect = GetRect(input, name);
                Assert.GreaterOrEqual(rect.width, 48f * dpiScale, $"{name} must respect the 48dp minimum target.");
                Assert.GreaterOrEqual(rect.height, 48f * dpiScale, $"{name} must respect the 48dp minimum target.");
            }
        }

        static Rect GetRect(TouchInputManager input, string field) =>
            (Rect)typeof(TouchInputManager).GetField(field, Hidden).GetValue(input);
    }
}
