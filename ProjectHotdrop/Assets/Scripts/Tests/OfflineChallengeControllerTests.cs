using System.IO;
using NUnit.Framework;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Core;

namespace Hotdrop.Tests
{
    [TestFixture]
    public class OfflineChallengeControllerTests
    {
        GameObject controllerObj;
        OfflineChallengeController controller;
        string testSavePath;

        [SetUp]
        public void SetUp()
        {
            testSavePath = Path.Combine(Application.temporaryCachePath, "test_controller_save.json");
            if (File.Exists(testSavePath)) File.Delete(testSavePath);

            controllerObj = new GameObject("OfflineChallengeController_Test");
            controller = controllerObj.AddComponent<OfflineChallengeController>();
            controller.customSavePath = testSavePath;
            controller.InitializeController();
        }

        [TearDown]
        public void TearDown()
        {
            if (controllerObj != null) Object.DestroyImmediate(controllerObj);
            if (File.Exists(testSavePath)) File.Delete(testSavePath);
        }

        [Test]
        public void Controller_InitialStateIsLibrary()
        {
            Assert.AreEqual(ChallengeFlowState.Library, controller.CurrentState);
        }

        [Test]
        public void SelectChallenge_OpensDetailView()
        {
            controller.SelectChallenge("ch_basic_route");
            Assert.AreEqual(ChallengeFlowState.Detail, controller.CurrentState);
            Assert.IsNotNull(controller.SelectedEntry);
            Assert.AreEqual("ch_basic_route", controller.SelectedEntry.id);
        }

        [Test]
        public void StartChallenge_EntersGameplayStateAndResetsProgress()
        {
            bool started = controller.StartChallenge("ch_basic_route");
            Assert.IsTrue(started);
            Assert.AreEqual(ChallengeFlowState.Gameplay, controller.CurrentState);
            Assert.AreEqual(0f, controller.CurrentElapsedTime);
            Assert.AreEqual(0, controller.CurrentTargetsCompleted);
        }

        [Test]
        public void GameplayUpdate_AccumulatesTimeAndTriggersSnapshotOnTargetCompletion()
        {
            controller.StartChallenge("ch_stationary_targets");
            controller.UpdateGameplay(10f);

            Assert.AreEqual(10f, controller.CurrentElapsedTime, 0.01f);

            controller.RegisterTargetCompleted(1);
            Assert.AreEqual(1, controller.CurrentTargetsCompleted);

            // Verify snapshot was saved on step completion
            var snapshot = controller.SaveSystem.GetSnapshot("ch_stationary_targets");
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(1, snapshot.targetsCompleted);
        }

        [Test]
        public void CompleteChallenge_LabelsResultsLocalRecordAndSavesRecord()
        {
            ChallengeResultData receivedResult = default;
            controller.OnResultCompleted += res => receivedResult = res;

            controller.StartChallenge("ch_basic_route");
            controller.RegisterTargetCompleted(3); // Completes target count (3)

            Assert.AreEqual(ChallengeFlowState.Result, controller.CurrentState);
            Assert.IsTrue(receivedResult.isSuccess);
            Assert.AreEqual("LOCAL RECORD", receivedResult.resultLabel);

            // Check save system updated
            var record = controller.SaveSystem.GetRecord("ch_basic_route");
            Assert.IsNotNull(record);
            Assert.IsTrue(record.isCompleted);
        }

        [Test]
        public void PauseAndResumeFlow_SavesSnapshotAndRestoresGameplay()
        {
            controller.StartChallenge("ch_moving_targets");
            controller.UpdateGameplay(15f);
            controller.PauseChallenge();

            Assert.AreEqual(ChallengeFlowState.Paused, controller.CurrentState);

            controller.UnpauseChallenge();
            Assert.AreEqual(ChallengeFlowState.Gameplay, controller.CurrentState);
            Assert.AreEqual(15f, controller.CurrentElapsedTime, 0.01f);
        }

        [Test]
        public void SnapshotRecoveryFailure_TriggersCleanRestart()
        {
            controller.StartChallenge("ch_safe_landing");
            controller.UpdateGameplay(20f);
            controller.PauseChallenge();

            // Clear snapshot artificially to simulate corrupt snapshot recovery failure
            controller.SaveSystem.ClearSnapshot();

            bool resumed = controller.ResumeFromSnapshot();
            Assert.IsTrue(resumed, "Failure should trigger clean restart and return true for new gameplay state.");
            Assert.AreEqual(ChallengeFlowState.Gameplay, controller.CurrentState);
            Assert.AreEqual(0f, controller.CurrentElapsedTime, "Clean restart resets elapsed time to zero.");
        }

        [Test]
        public void All12AuthoredEntries_CanBeStartedAndCompleted()
        {
            var config = OfflineChallengeConfig.CreateDefaultAuthoredConfig();
            foreach (var entry in config.entries)
            {
                bool started = controller.StartChallenge(entry.id);
                Assert.IsTrue(started, $"Challenge {entry.id} must start successfully.");
                controller.RegisterTargetCompleted(entry.targetCount);
                Assert.AreEqual(ChallengeFlowState.Result, controller.CurrentState, $"Challenge {entry.id} must complete.");
            }
        }
    }
}
