using System.IO;
using NUnit.Framework;
using UnityEngine;
using Hotdrop.Config;
using Hotdrop.Core;

namespace Hotdrop.Tests
{
    [TestFixture]
    public class OfflineChallengeSaveTests
    {
        string testSavePath;

        [SetUp]
        public void SetUp()
        {
            testSavePath = Path.Combine(Application.temporaryCachePath, "test_offline_challenges_save.json");
            if (File.Exists(testSavePath))
            {
                File.Delete(testSavePath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(testSavePath))
            {
                File.Delete(testSavePath);
            }
            if (File.Exists(testSavePath + ".tmp"))
            {
                File.Delete(testSavePath + ".tmp");
            }
        }

        [Test]
        public void AuthoredConfig_ContainsAll12EntriesAcross3Chapters()
        {
            var config = OfflineChallengeConfig.CreateDefaultAuthoredConfig();
            Assert.IsNotNull(config);
            Assert.AreEqual(12, config.entries.Count);

            var firstFlight = config.GetChallengesForChapter(ChallengeChapter.FirstFlight);
            Assert.AreEqual(4, firstFlight.Count);
            Assert.AreEqual("ch_basic_route", firstFlight[0].id);
            Assert.AreEqual("ch_own_pad_route", firstFlight[1].id);
            Assert.AreEqual("ch_enemy_pad_route", firstFlight[2].id);
            Assert.AreEqual("ch_safe_landing", firstFlight[3].id);

            var rooftopOp = config.GetChallengesForChapter(ChallengeChapter.RooftopOperator);
            Assert.AreEqual(4, rooftopOp.Count);
            Assert.AreEqual("ch_stationary_targets", rooftopOp[0].id);
            Assert.AreEqual("ch_moving_targets", rooftopOp[1].id);
            Assert.AreEqual("ch_reload_discipline", rooftopOp[2].id);
            Assert.AreEqual("ch_launch_and_hit", rooftopOp[3].id);

            var relayCaptain = config.GetChallengesForChapter(ChallengeChapter.RelayCaptain);
            Assert.AreEqual(4, relayCaptain.Count);
            Assert.AreEqual("ch_capture_one_zone", relayCaptain[0].id);
            Assert.AreEqual("ch_rotate_between_zones", relayCaptain[1].id);
            Assert.AreEqual("ch_defend_against_bots", relayCaptain[2].id);
            Assert.AreEqual("ch_complete_offline_relay", relayCaptain[3].id);
        }

        [Test]
        public void UntunedThresholds_SafelyReturnBronzeMedalOnly()
        {
            var entry = new OfflineChallengeEntry
            {
                id = "ch_test",
                isTuned = true,
                medalThresholds = new ChallengeMedalThresholds
                {
                    thresholdsValidated = false, // Untuned in dev build
                    goldTimeSeconds = 10f,
                    silverTimeSeconds = 20f
                }
            };

            // Fast time (5s) would normally earn Gold, but unvalidated threshold forces Bronze
            ChallengeMedal medal = entry.CalculateMedal(completionTime: 5f, accuracyPercent: 100f, score: 500f);
            Assert.AreEqual(ChallengeMedal.Bronze, medal);
        }

        [Test]
        public void LocalChallengeSave_AtomicSaveAndLoad_PreservesRecord()
        {
            var save = new LocalChallengeSave(testSavePath);
            var record = new ChallengeRecord
            {
                challengeId = "ch_basic_route",
                isCompleted = true,
                medal = ChallengeMedal.Bronze,
                personalBestTimeSeconds = 45.2f,
                personalBestScore = 300f,
                challengeVersion = 1
            };

            save.SaveRecord(record);
            Assert.IsTrue(File.Exists(testSavePath), "Save file must exist after SaveRecord");

            var loadedSave = new LocalChallengeSave(testSavePath);
            var loadedRecord = loadedSave.GetRecord("ch_basic_route");

            Assert.IsNotNull(loadedRecord, "Record must survive round-trip save/load");
            Assert.IsTrue(loadedRecord.isCompleted);
            Assert.AreEqual(ChallengeMedal.Bronze, loadedRecord.medal);
            Assert.AreEqual(45.2f, loadedRecord.personalBestTimeSeconds, 0.01f);
        }

        [Test]
        public void LocalChallengeSave_CorruptFile_RecoversSafelyWithoutGrantingMedals()
        {
            // Create a mangled invalid save file
            File.WriteAllText(testSavePath, "{ \"version\": 1, \"checksum\": \"INVALID_CHECKSUM_HASH\", \"records\": [ { \"challengeId\": \"ch_basic_route\", \"isCompleted\": true, \"medal\": 3 } ] }");

            var save = new LocalChallengeSave(testSavePath);
            Assert.IsNotNull(save.Data);

            // Corrupt save should reset to default and NOT grant Gold (3) medal
            var record = save.GetRecord("ch_basic_route");
            Assert.IsNull(record, "Corrupt save must recover cleanly with empty records.");
        }

        [Test]
        public void LocalChallengeSave_SnapshotSaveAndClear()
        {
            var save = new LocalChallengeSave(testSavePath);
            var snapshot = new LocalChallengeSaveSnapshot
            {
                challengeId = "ch_stationary_targets",
                elapsedTime = 25.5f,
                targetsCompleted = 2,
                score = 200f
            };

            save.SaveSnapshot(snapshot);

            var loadedSnapshot = save.GetSnapshot("ch_stationary_targets");
            Assert.IsNotNull(loadedSnapshot);
            Assert.AreEqual(25.5f, loadedSnapshot.elapsedTime, 0.01f);
            Assert.AreEqual(2, loadedSnapshot.targetsCompleted);

            save.ClearSnapshot();
            Assert.IsNull(save.GetSnapshot("ch_stationary_targets"));
        }

        [Test]
        public void LocalChallengeSave_IsolatedFromOnlineState()
        {
            Assert.IsTrue(LocalChallengeSave.VerifyIsIsolatedOfflineState());
        }
    }
}
