using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hotdrop.Tutorial;

namespace Hotdrop.UI
{
    /// <summary>
    /// Tutorial UI - shows current step instruction, skip button, progress.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        const float TutorialCaptionDp = UIDesignTokens.CaptionDp;
        const float TutorialBodyDp = UIDesignTokens.BodyLargeDp;
        const float TutorialTitleDp = UIDesignTokens.TitleMediumDp;

        [Header("References")]
        [SerializeField] TextMeshProUGUI instructionText;
        [SerializeField] TextMeshProUGUI stepCounterText;
        [SerializeField] TextMeshProUGUI stepTitleText;
        [SerializeField] Button skipButton;
        [SerializeField] Button showMeButton;
        [SerializeField] Slider progressSlider;
        [SerializeField] GameObject completePanel;
        [SerializeField] Button practiceButton;
        [SerializeField] Button replayButton;

        TutorialController controller;
        ModalView skipConfirmation;
        float stepShownAt;
        bool showMeRevealed;

        RectTransform card, stepChip;

        // UIUX screen 04: the single guided instruction card (448 wide, 16 px from the top) and the
        // SKIP TRAINING chip in the top-right; numbers are the reference's CSS px.
        public static TutorialUI CreateView(Transform canvas, TutorialController tutorial)
        {
            var panel = new GameObject("Training instruction", typeof(RectTransform), typeof(Image), typeof(TutorialUI));
            panel.transform.SetParent(canvas, false);
            var rect = panel.GetComponent<RectTransform>();
            UiDraw.Place(rect, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -16f), new Vector2(448f, 99f));
            var image = panel.GetComponent<Image>();
            image.color = UiDraw.Hex("#1b2940", .95f);
            image.sprite = UIRoundedSprites.RoundedRect(12f);
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            var frame = UiDraw.Box(panel.transform, "TrainingFrame", UiDraw.Hex("#30d5e8"));
            frame.sprite = UIRoundedSprites.RoundedOutline(12f, 2f);
            frame.type = Image.Type.Sliced;
            UiDraw.Fill(frame.rectTransform);
            var ui = panel.GetComponent<TutorialUI>();
            ui.card = rect;

            ui.stepCounterText = UiDraw.Text(panel.transform, "Progress", "", UiFont.Mono, TutorialCaptionDp, UiDraw.Hex("#30d5e8"), 0f, TextAlignmentOptions.Midline);
            var chip = UiDraw.Box(panel.transform, "ProgressChip", UiDraw.Hex("#30d5e8", .2f), 4f);
            chip.transform.SetSiblingIndex(ui.stepCounterText.transform.GetSiblingIndex());
            ui.stepChip = chip.rectTransform;
            ui.stepTitleText = UiDraw.Text(panel.transform, "StepTitle", "", UiFont.Heading, TutorialTitleDp, Color.white, .025f);
            ui.instructionText = UiDraw.Text(panel.transform, "Instruction", "", UiFont.Body, TutorialBodyDp, UiDraw.Hex("#bbc6d8"));
            ui.instructionText.textWrappingMode = TextWrappingModes.Normal;
            ui.instructionText.alignment = TextAlignmentOptions.TopLeft;
            UiDraw.Place(ui.instructionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -48f), new Vector2(412f, 40f));

            // SHOW ME: mono caption with the help glyph, right of the header row.
            ui.showMeButton = UiDraw.Tap(panel.transform, "Show me", new Color(0f, 0f, 0f, 0f), 4f, null);
            var showLabel = UiDraw.Text(ui.showMeButton.transform, "Show me label", UiDraw.L("tutorial.showMe", "Show Me"), UiFont.MonoRegular, TutorialCaptionDp, UiDraw.Hex("#bbc6d8"), .05f);
            float showW = UiDraw.Width(showLabel);
            UiDraw.Place((RectTransform)ui.showMeButton.transform, new Vector2(1f, 1f), new Vector2(1f, .5f), new Vector2(-14f, -29f), new Vector2(showW + 18f, 32f));
            UiDraw.Place(UiDraw.Icon(ui.showMeButton.transform, "Icon", "help", 12f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, new Vector2(12f, 12f));
            UiDraw.Place(showLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(showW + 2f, 14f));

            // SKIP TRAINING: 40 px, #1b2940/80 with an outline/40 rail, close glyph.
            ui.skipButton = UiDraw.Tap(canvas, "Skip training", UiDraw.Hex("#1b2940", .8f), 8f, null, UiDraw.Hex("#49627b", .4f));
            // In dp like the touch controls, directly under their top-right 40 dp row (24 dp margin +
            // 40 + 8 gap). Training shows no radar there, and the step card owns the top centre.
            UiDpScaler.Attach((RectTransform)ui.skipButton.transform);
            var skipLabel = UiDraw.Text(ui.skipButton.transform, "Skip training label", UiDraw.L("tutorial.skipTraining", "Skip Training"), UiFont.Mono, TutorialCaptionDp, UiDraw.Hex("#bbc6d8"));
            float skipW = UiDraw.Width(skipLabel);
            UiDraw.Place((RectTransform)ui.skipButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -72f), new Vector2(16f + skipW + 6f + 14f + 16f, 40f));
            UiDraw.Place(skipLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(16f, 0f), new Vector2(skipW + 2f, 16f));
            UiDraw.Place(UiDraw.Icon(ui.skipButton.transform, "Icon", "close", 14f, UiDraw.Hex("#bbc6d8")).rectTransform,
                new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-16f, 0f), new Vector2(14f, 14f));

            ui.skipConfirmation = ModalView.Create(canvas, UiDraw.L("tutorial.skipConfirmation.title", "Skip training?"),
                UiDraw.L("tutorial.skipConfirmation.body", "You can replay training later from More. Leave this lesson now?"), "SkipTrainingConfirmation");
            ui.skipConfirmation.AddAction(UiDraw.L("tutorial.skipConfirmation.keep", "Keep Training"), ButtonVariant.Secondary, () => ui.skipConfirmation.Close());
            ui.skipConfirmation.AddAction(UiDraw.L("tutorial.skipConfirmation.skip", "Skip"), ButtonVariant.Destructive, () =>
            {
                ui.skipConfirmation.Close();
                ui.controller?.SkipTutorial();
            });
            ui.skipConfirmation.gameObject.SetActive(false);
            return ui;
        }

        /// <summary>Header chip and title follow their text; the card grows with the instruction.</summary>
        void LayoutCard()
        {
            if (card == null) return;
            float chipW = UiDraw.Width(stepCounterText) + 16f;
            UiDraw.Place(stepChip, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(18f, -29f), new Vector2(chipW, 19f));
            UiDraw.Place(stepCounterText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(18f, -29f), new Vector2(chipW, 19f));
            UiDraw.Place(stepTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(18f + chipW + 8f, -29f), new Vector2(260f, 28f));
            float textH = Mathf.Max(16f, instructionText.GetPreferredValues(instructionText.text, 412f, 0f).y);
            instructionText.rectTransform.sizeDelta = new Vector2(412f, textH);
            card.sizeDelta = new Vector2(448f, 48f + textH + 18f);
        }

        void OnEnable()
        {
            if (skipButton != null) skipButton.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (skipButton != null) skipButton.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (skipButton != null && skipButton.transform.parent != transform)
                Destroy(skipButton.gameObject);
        }

        public void Initialize(TutorialController tutorialController)
        {
            controller = tutorialController;
            if (skipButton != null)
                skipButton.onClick.AddListener(RequestSkip);
            if (showMeButton != null)
                showMeButton.onClick.AddListener(() => controller?.ShowMe());
            if (practiceButton != null)
                practiceButton.onClick.AddListener(() => OnPracticeClicked());
            if (replayButton != null)
                replayButton.onClick.AddListener(() => controller?.RestartTutorial());
        }

        public void ShowStep(TutorialController.TutorialStep step)
        {
            stepShownAt = Time.unscaledTime;
            showMeRevealed = true;
            if (showMeButton != null) showMeButton.gameObject.SetActive(true);
            if (instructionText != null)
                instructionText.text = UiPresentationProfile.NormalizeRichText(GetInstructionForStep(step));

            if (stepCounterText != null)
            {
                int stepNum = (int)step + 1;
                int totalSteps = 9;
                stepCounterText.text = UiDraw.LFormat("tutorial.stepCounterTemplate", "Step {0} of {1}", stepNum, totalSteps);
            }
            if (stepTitleText != null) stepTitleText.text = GetTitleForStep(step);

            if (progressSlider != null)
            {
                progressSlider.value = (float)(int)step / 8f;
            }

            if (completePanel != null)
                completePanel.SetActive(false);
            LayoutCard();
        }

        public void ShowComplete()
        {
            if (completePanel != null)
                completePanel.SetActive(true);

            if (instructionText != null)
                instructionText.text = UiDraw.L("tutorial.complete", "Tutorial Complete!");
        }

        public void SetInstruction(string instruction)
        {
            if (instructionText != null)
                instructionText.text = UiPresentationProfile.NormalizeRichText(instruction);
            LayoutCard();
        }

        void Update()
        {
            // Help is offered after a quiet moment, never placed over crosshair, target, or the
            // thumb zones. It explains the next real action rather than moving the player.
            if (!showMeRevealed && controller != null && controller.CurrentStep != TutorialController.TutorialStep.Complete
                && Time.unscaledTime - stepShownAt >= 8f)
            {
                showMeRevealed = true;
                if (showMeButton != null) showMeButton.gameObject.SetActive(true);
            }
        }

        void RequestSkip()
        {
            if (skipConfirmation != null) skipConfirmation.Open();
            else controller?.SkipTutorial();
        }

        string GetInstructionForStep(TutorialController.TutorialStep step)
        {
            switch (step)
            {
                case TutorialController.TutorialStep.MoveToMarker:
                    return UiDraw.L("tutorial.step.instruction.moveToMarker", "Move to the marker using the left joystick");
                case TutorialController.TutorialStep.KillStationaryTarget:
                    return UiDraw.L("tutorial.step.instruction.killTarget", "Aim with right side drag and shoot the target using the fire button");
                case TutorialController.TutorialStep.ReloadWeapon:
                    return UiDraw.L("tutorial.step.instruction.reload", "Press the reload button near the ammo counter to reload");
                case TutorialController.TutorialStep.PlacePad:
                    return UiDraw.L("tutorial.step.instruction.placePad", "Press the pad button to place a launch pad ahead of you");
                case TutorialController.TutorialStep.RideOwnPad:
                    return UiDraw.L("tutorial.step.instruction.rideOwnPad", "Step onto your deployed pad. The Jump button transforms into <color=#30d5e8><b>LAUNCH</b></color>. Tap it to catch the air route!");
                case TutorialController.TutorialStep.UseEnemyPad:
                    return UiDraw.L("tutorial.step.instruction.useEnemyPad", "Enemy pads can be used too — launch from the red pad");
                case TutorialController.TutorialStep.DestroyEnemyPad:
                    return UiDraw.L("tutorial.step.instruction.destroyEnemyPad", "Shoot enemy pads to destroy them");
                case TutorialController.TutorialStep.CaptureZone:
                    return UiDraw.L("tutorial.step.instruction.captureZone", "Stand in the capture zone to score points for your team");
                case TutorialController.TutorialStep.BotFight:
                    return UiDraw.L("tutorial.step.instruction.botFight", "Defeat the practice bot to complete training");
                default:
                    return "";
            }
        }

        static string GetTitleForStep(TutorialController.TutorialStep step) => step switch
        {
            TutorialController.TutorialStep.MoveToMarker => UiDraw.L("tutorial.step.title.moveToMarker", "Move to the Marker"),
            TutorialController.TutorialStep.KillStationaryTarget => UiDraw.L("tutorial.step.title.killTarget", "Aim and Fire"),
            TutorialController.TutorialStep.ReloadWeapon => UiDraw.L("tutorial.step.title.reload", "Reload Your Weapon"),
            TutorialController.TutorialStep.PlacePad => UiDraw.L("tutorial.step.title.placePad", "Place a Launch Pad"),
            TutorialController.TutorialStep.RideOwnPad => UiDraw.L("tutorial.step.title.rideOwnPad", "Launch to the Platform"),
            TutorialController.TutorialStep.UseEnemyPad => UiDraw.L("tutorial.step.title.useEnemyPad", "Use the Enemy Pad"),
            TutorialController.TutorialStep.DestroyEnemyPad => UiDraw.L("tutorial.step.title.destroyEnemyPad", "Destroy an Enemy Pad"),
            TutorialController.TutorialStep.CaptureZone => UiDraw.L("tutorial.step.title.captureZone", "Capture the Zone"),
            TutorialController.TutorialStep.BotFight => UiDraw.L("tutorial.step.title.botFight", "Finish the Drill"),
            _ => UiDraw.L("tutorial.step.title.training", "Training")
        };

        void OnPracticeClicked()
        {
            // Load offline practice scene
            Hotdrop.Core.GameBootstrap.Instance.StartOfflinePractice();
        }
    }
}



