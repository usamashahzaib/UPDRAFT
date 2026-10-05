using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Hotdrop.Input;

namespace Hotdrop.Tests
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
            // UIUX screen 08: pause and ping live in the top-right system cluster in both layouts;
            // only the combat buttons mirror for left-handed play.
            Assert.Greater(pauseRight.center.x, Screen.width * 0.5f, "Pause sits in the top-right system cluster.");

            input.SetLeftHanded(true);

            Rect fireLeft = GetRect(input, "fireButtonRect");
            Rect pauseLeft = GetRect(input, "pauseButtonRect");
            Assert.Less(fireLeft.center.x, Screen.width * 0.5f, "Left-handed layout must mirror fire to the left.");
            Assert.AreEqual(pauseRight.center.x, pauseLeft.center.x, 0.01f, "Pause stays in the system cluster in the left-handed layout.");

            foreach (var name in new[] { "fireButtonRect", "jumpButtonRect", "padButtonRect", "reloadButtonRect", "pingButtonRect", "pauseButtonRect" })
            {
                Rect rect = GetRect(input, name);
                Assert.GreaterOrEqual(rect.width, 48f * dpiScale, $"{name} must respect the 48dp minimum target.");
                Assert.GreaterOrEqual(rect.height, 48f * dpiScale, $"{name} must respect the 48dp minimum target.");
            }
        }

        // ---- QA-003: ownership by finger ID across the screen midpoint ----

        static readonly string[] ButtonRects = { "fireButtonRect", "jumpButtonRect", "padButtonRect", "reloadButtonRect", "pingButtonRect", "pauseButtonRect" };

        static void Touch(TouchInputManager input, int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 pos) =>
            typeof(TouchInputManager).GetMethod("ProcessTouch", Hidden).Invoke(input, new object[] { id, phase, pos });

        /// <summary>A point on the requested half that no on-screen control covers.</summary>
        static Vector2 FreePoint(TouchInputManager input, bool leftHalf)
        {
            for (float fy = 0.45f; fy < 0.95f; fy += 0.05f)
                for (float fx = 0.1f; fx < 0.5f; fx += 0.05f)
                {
                    var p = new Vector2((leftHalf ? fx : 1f - fx) * Screen.width, fy * Screen.height);
                    if (ButtonRects.All(r => !GetRect(input, r).Contains(p))) return p;
                }
            Assert.Fail("No free touch point found on the " + (leftHalf ? "left" : "right") + " half.");
            return default;
        }

        static int Field(TouchInputManager input, string name) => (int)typeof(TouchInputManager).GetField(name, Hidden).GetValue(input);

        [Test]
        public void MoveFinger_CrossingMidpoint_ReleasesMovementOnLift()
        {
            var input = CreateManager(false);
            var start = FreePoint(input, leftHalf: true);
            var across = FreePoint(input, leftHalf: false);

            Touch(input, 7, UnityEngine.InputSystem.TouchPhase.Began, start);
            Touch(input, 7, UnityEngine.InputSystem.TouchPhase.Moved, across);
            Assert.AreNotEqual(Vector2.zero, input.MoveInput, "Dragging the stick must produce movement.");
            Assert.AreEqual(-1, Field(input, "aimTouchId"), "A move finger crossing the midpoint must not become aim.");

            Touch(input, 7, UnityEngine.InputSystem.TouchPhase.Ended, across);
            Assert.AreEqual(Vector2.zero, input.MoveInput, "Releasing on the aim half must stop movement (no latch).");
            Assert.AreEqual(-1, Field(input, "moveTouchId"));
        }

        [Test]
        public void AimFinger_CrossingMidpoint_ReleasesAimOnLift()
        {
            var input = CreateManager(false);
            var start = FreePoint(input, leftHalf: false);
            var across = FreePoint(input, leftHalf: true);

            Touch(input, 8, UnityEngine.InputSystem.TouchPhase.Began, start);
            Assert.AreEqual(8, Field(input, "aimTouchId"));
            Touch(input, 8, UnityEngine.InputSystem.TouchPhase.Moved, across);
            Assert.AreEqual(-1, Field(input, "moveTouchId"), "An aim finger crossing the midpoint must not become movement.");
            Assert.AreEqual(Vector2.zero, input.MoveInput);

            Touch(input, 8, UnityEngine.InputSystem.TouchPhase.Canceled, across);
            Assert.AreEqual(-1, Field(input, "aimTouchId"), "Aim ownership must end on its own finger's cancel.");

            // A fresh finger on the right can now acquire aim again.
            Touch(input, 9, UnityEngine.InputSystem.TouchPhase.Began, start);
            Assert.AreEqual(9, Field(input, "aimTouchId"));
        }

        [Test]
        public void LostFingers_WithoutEndedEvent_AreReleased()
        {
            var input = CreateManager(false);
            Set(input, "moveTouchId", 1);
            SetProperty(input, "MoveInput", Vector2.one);
            Set(input, "aimTouchId", 2);
            Set(input, "fireTouchId", 3);
            SetProperty(input, "FireHeld", true);

            var live = new System.Collections.Generic.HashSet<int> { 2 }; // only the aim finger is still down
            typeof(TouchInputManager).GetMethod("ReleaseOwnersNotIn", Hidden).Invoke(input, new object[] { live });

            Assert.AreEqual(Vector2.zero, input.MoveInput, "A vanished move finger must stop movement.");
            Assert.IsFalse(input.FireHeld, "A vanished fire finger must release the trigger.");
            Assert.AreEqual(2, Field(input, "aimTouchId"), "Fingers that are still down keep their ownership.");
        }

        [Test]
        public void SecondFireFinger_DoesNotStealTrigger()
        {
            var input = CreateManager(false);
            var fire = GetRect(input, "fireButtonRect").center;

            Touch(input, 1, UnityEngine.InputSystem.TouchPhase.Began, fire);
            Touch(input, 2, UnityEngine.InputSystem.TouchPhase.Began, fire);
            Touch(input, 2, UnityEngine.InputSystem.TouchPhase.Ended, fire);
            Assert.IsTrue(input.FireHeld, "Lifting a second finger must not stop fire while the first still holds.");

            Touch(input, 1, UnityEngine.InputSystem.TouchPhase.Ended, fire);
            Assert.IsFalse(input.FireHeld);
        }

        static Rect GetRect(TouchInputManager input, string field) =>
            (Rect)typeof(TouchInputManager).GetField(field, Hidden).GetValue(input);
    }
}


