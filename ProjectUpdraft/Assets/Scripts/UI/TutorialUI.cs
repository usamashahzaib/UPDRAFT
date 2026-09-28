using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Updraft.Tutorial;

namespace Updraft.UI
{
    /// <summary>
    /// Tutorial UI - shows current step instruction, skip button, progress.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TextMeshProUGUI instructionText;
        [SerializeField] TextMeshProUGUI stepCounterText;
        [SerializeField] Button skipButton;
        [SerializeField] Slider progressSlider;
        [SerializeField] GameObject completePanel;
        [SerializeField] Button practiceButton;
        [SerializeField] Button replayButton;

        TutorialController controller;

        public static TutorialUI CreateView(Transform canvas, TutorialController tutorial)
        {
            var panel = new GameObject("Training instruction", typeof(RectTransform), typeof(Image), typeof(TutorialUI));
            panel.transform.SetParent(canvas, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -280);
            rect.sizeDelta = new Vector2(850, 120);
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.063f, 0.094f, 0.153f, 0.93f);
            image.raycastTarget = false;
            var ui = panel.GetComponent<TutorialUI>();
            ui.instructionText = Label(panel.transform, "Instruction", new Vector2(0, -10), new Vector2(650, 65), 24);
            ui.stepCounterText = Label(panel.transform, "Progress", new Vector2(0, 38), new Vector2(650, 25), 16);
            var buttonObj = new GameObject("Skip training", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel.transform, false);
            var br = buttonObj.GetComponent<RectTransform>();
            br.anchoredPosition = new Vector2(345, 0); br.sizeDelta = new Vector2(135, 90);
            buttonObj.GetComponent<Image>().color = new Color(0.106f, 0.161f, 0.251f);
            ui.skipButton = buttonObj.GetComponent<Button>();
            ui.skipButton.targetGraphic = buttonObj.GetComponent<Image>();
            Label(buttonObj.transform, "Skip label", Vector2.zero, new Vector2(125, 60), 20).text = "SKIP";
            return ui;
        }

        static TextMeshProUGUI Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>(); rt.anchoredPosition = position; rt.sizeDelta = size;
            var text = obj.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.color = new Color(0.957f, 0.969f, 0.984f);
            return text;
        }

        public void Initialize(TutorialController tutorialController)
        {
            controller = tutorialController;
            if (skipButton != null)
                skipButton.onClick.AddListener(() => controller?.SkipTutorial());
            if (practiceButton != null)
                practiceButton.onClick.AddListener(() => OnPracticeClicked());
            if (replayButton != null)
                replayButton.onClick.AddListener(() => controller?.RestartTutorial());
        }

        public void ShowStep(TutorialController.TutorialStep step)
        {
            if (instructionText != null)
                instructionText.text = GetInstructionForStep(step);

            if (stepCounterText != null)
            {
                int stepNum = (int)step + 1;
                int totalSteps = 9;
                stepCounterText.text = $"Step {stepNum} of {totalSteps}";
            }

            if (progressSlider != null)
            {
                progressSlider.value = (float)(int)step / 8f;
            }

            if (completePanel != null)
                completePanel.SetActive(false);
        }

        public void ShowComplete()
        {
            if (completePanel != null)
                completePanel.SetActive(true);

            if (instructionText != null)
                instructionText.text = "Tutorial Complete!";
        }

        public void SetInstruction(string instruction)
        {
            if (instructionText != null)
                instructionText.text = instruction;
        }

        string GetInstructionForStep(TutorialController.TutorialStep step)
        {
            switch (step)
            {
                case TutorialController.TutorialStep.MoveToMarker:
                    return "Move to the marker using the left joystick";
                case TutorialController.TutorialStep.KillStationaryTarget:
                    return "Aim with right side drag and shoot the target using the fire button";
                case TutorialController.TutorialStep.ReloadWeapon:
                    return "Press the reload button near the ammo counter to reload";
                case TutorialController.TutorialStep.PlacePad:
                    return "Press the pad button to place a launch pad ahead of you";
                case TutorialController.TutorialStep.RideOwnPad:
                    return "Stand on your pad and jump to launch yourself to the platform";
                case TutorialController.TutorialStep.UseEnemyPad:
                    return "Enemy pads can be used too — launch from the red pad";
                case TutorialController.TutorialStep.DestroyEnemyPad:
                    return "Shoot enemy pads to destroy them";
                case TutorialController.TutorialStep.CaptureZone:
                    return "Stand in the capture zone to score points for your team";
                case TutorialController.TutorialStep.BotFight:
                    return "Defeat the practice bot to complete training";
                default:
                    return "";
            }
        }

        void OnPracticeClicked()
        {
            // Load offline practice scene
            Updraft.Core.GameBootstrap.Instance.StartOfflinePractice();
        }
    }
}
