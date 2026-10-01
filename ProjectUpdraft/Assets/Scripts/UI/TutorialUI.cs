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
            // QA-019/020: text column and SKIP never overlap; text wraps/shrinks but stays >= ~12 dp;
            // SKIP is >= 48 dp. Sits below the score/timer block on the density-scaled canvas.
            rect.anchoredPosition = new Vector2(0, -210);
            rect.sizeDelta = new Vector2(900, 128);
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.063f, 0.094f, 0.153f, 0.93f);
            image.raycastTarget = false;
            var ui = panel.GetComponent<TutorialUI>();
            const float skipWidth = 150f, pad = 14f;
            float textWidth = 900f - skipWidth - pad * 3;
            float textCentre = -450f + pad + textWidth * 0.5f;
            ui.instructionText = Label(panel.transform, "Instruction", new Vector2(textCentre, -14), new Vector2(textWidth, 84), 26);
            ui.instructionText.enableWordWrapping = true;
            ui.instructionText.enableAutoSizing = true; ui.instructionText.fontSizeMin = UiMetrics.MinTextUnits; ui.instructionText.fontSizeMax = 26;
            ui.stepCounterText = Label(panel.transform, "Progress", new Vector2(textCentre, 44), new Vector2(textWidth, 30), UiMetrics.MinTextUnits + 1);
            var buttonObj = new GameObject("Skip training", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel.transform, false);
            var br = buttonObj.GetComponent<RectTransform>();
            br.anchoredPosition = new Vector2(450f - pad - skipWidth * 0.5f, 0); br.sizeDelta = new Vector2(skipWidth, 100);
            buttonObj.GetComponent<Image>().color = new Color(0.106f, 0.161f, 0.251f);
            ui.skipButton = buttonObj.GetComponent<Button>();
            ui.skipButton.targetGraphic = buttonObj.GetComponent<Image>();
            Label(buttonObj.transform, "Skip label", Vector2.zero, new Vector2(skipWidth - 10, 60), 24).text = "SKIP";
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
