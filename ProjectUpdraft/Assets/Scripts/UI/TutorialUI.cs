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
            UnityEngine.SceneManagement.SceneManager.LoadScene("OfflinePractice");
        }
    }
}