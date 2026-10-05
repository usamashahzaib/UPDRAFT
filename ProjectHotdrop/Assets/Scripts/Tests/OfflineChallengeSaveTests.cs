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
            if (File.Exists(testSavePath + ".bak")) File.Delete(testSavePath + ".bak");
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
            if (File.Exists(testSavePath + ".bak")) File.Delete(testSavePath + ".bak");
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
            var save = new LocalChallengeSave(testSavePath);
            // Local completion serializes a ChallengeRecord, never a server receipt,
            // account/entitlement identifier or online wallet grant.
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", isCompleted = true, medal = ChallengeMedal.Bronze });
            var json = File.ReadAllText(testSavePath);
            StringAssert.DoesNotContain("accountId", json);
            StringAssert.DoesNotContain("entitlement", json);
            StringAssert.DoesNotContain("wallet", json);
            Assert.AreEqual(1, new LocalChallengeSave(testSavePath).Data.records.Count);
        }

        [Test]
        public void Checksum_ProtectsSnapshotAndRecordContext_NotJustCompletedCount()
        {
            var data = new LocalChallengeSaveData();
            data.records.Add(new ChallengeRecord { challengeId = "ch_basic_route", controlPreset = "Classic", difficulty = "Standard" });
            data.activeSnapshot = new LocalChallengeSaveSnapshot { challengeId = "ch_basic_route", customStateJson = "{\"recoverable\":true}" };
            string original = LocalChallengeSave.ComputeChecksum(data);
            data.activeSnapshot.elapsedTime = 12.3456f;
            Assert.AreNotEqual(original, LocalChallengeSave.ComputeChecksum(data));
            data.activeSnapshot.elapsedTime = 0;
            data.activeSnapshot.customStateJson = "{\"recoverable\":false}";
            Assert.AreNotEqual(original, LocalChallengeSave.ComputeChecksum(data));
            data.activeSnapshot.customStateJson = "{\"recoverable\":true}";
            data.records[0].controlPreset = "Classic Left";
            Assert.AreNotEqual(original, LocalChallengeSave.ComputeChecksum(data));
        }

        [Test]
        public void Records_AreSeparatedByChallengeVersionControlsAndDifficulty()
        {
            var save = new LocalChallengeSave(testSavePath);
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", challengeVersion = 1 });
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", challengeVersion = 2 });
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", challengeVersion = 2, controlPreset = "Classic Left" });
            var loaded = new LocalChallengeSave(testSavePath);
            Assert.AreEqual(3, loaded.Data.records.Count);
            Assert.IsNotNull(loaded.GetRecord("ch_basic_route", 2, "Classic Left", "Standard"));
        }

        [Test]
        public void AtomicReplacement_KeepsLastCommittedBackup_AndRecoversCorruptPrimary()
        {
            var save = new LocalChallengeSave(testSavePath);
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", isCompleted = true, medal = ChallengeMedal.Bronze, personalBestTimeSeconds = 20 });
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", isCompleted = true, medal = ChallengeMedal.Bronze, personalBestTimeSeconds = 10 });
            Assert.IsTrue(save.LastSaveSucceeded);
            Assert.IsTrue(File.Exists(testSavePath + ".bak"));
            File.WriteAllText(testSavePath, "interrupted or corrupt write");
            var recovered = new LocalChallengeSave(testSavePath);
            Assert.AreEqual(20, recovered.GetRecord("ch_basic_route").personalBestTimeSeconds);
            Assert.IsNotEmpty(recovered.RecoveryMessage);
        }

        [Test]
        public void FutureSchema_IsNotLoadedOrOverwritten()
        {
            string future = "{\"version\":999,\"records\":[],\"checksum\":\"future\"}";
            File.WriteAllText(testSavePath, future);
            var save = new LocalChallengeSave(testSavePath);
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route" });
            Assert.IsFalse(save.LastSaveSucceeded);
            Assert.AreEqual(future, File.ReadAllText(testSavePath));
        }

        [Test]
        public void FailedReplacement_DoesNotDeletePreviousCommittedSave()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor) Assert.Ignore("Windows sharing-lock failure injection.");
            var save = new LocalChallengeSave(testSavePath);
            save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", isCompleted = true, medal = ChallengeMedal.Bronze, personalBestTimeSeconds = 20 });
            using (var locked = new FileStream(testSavePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                save.SaveRecord(new ChallengeRecord { challengeId = "ch_basic_route", isCompleted = true, medal = ChallengeMedal.Bronze, personalBestTimeSeconds = 10 });
                Assert.IsFalse(save.LastSaveSucceeded);
            }
            Assert.AreEqual(20, new LocalChallengeSave(testSavePath).GetRecord("ch_basic_route").personalBestTimeSeconds);
        }

        [Test]
        public void AuthoredEntries_ExposeReachableSilverAndGold()
        {
            var config = OfflineChallengeConfig.CreateDefaultAuthoredConfig();
            foreach (var entry in config.entries)
            {
                // Every shipped challenge must offer a real Silver/Gold path, not Bronze-only.
                Assert.IsTrue(entry.medalThresholds.thresholdsValidated, entry.id + " thresholds must be validated");
                var thresholds = entry.medalThresholds;
                Assert.IsTrue(thresholds.goldTimeSeconds > 0 || thresholds.goldScore > 0 || thresholds.goldAccuracy > 0,
                    entry.id + " must have an authored Gold condition");
                Assert.IsTrue(thresholds.silverTimeSeconds > 0 || thresholds.silverScore > 0 || thresholds.silverAccuracy > 0,
                    entry.id + " must have an authored Silver condition");
            }
            var route = config.GetChallengeById("ch_basic_route");
            Assert.AreEqual(ChallengeMedal.Gold, route.CalculateMedal(20f, 100f, 500f));
            Assert.AreEqual(ChallengeMedal.Silver, route.CalculateMedal(40f, 100f, 300f));
            Assert.AreEqual(ChallengeMedal.Bronze, route.CalculateMedal(70f, 100f, 100f));
        }

        [Test]
        public void ValidatedMedal_RequiresEveryConfiguredConditionIncludingAccuracy()
        {
            var entry = new OfflineChallengeEntry { isTuned = true, medalThresholds = new ChallengeMedalThresholds
            { thresholdsValidated = true, goldTimeSeconds = 10, goldScore = 200, goldAccuracy = 90 } };
            Assert.AreEqual(ChallengeMedal.Bronze, entry.CalculateMedal(5, 80, 300));
            Assert.AreEqual(ChallengeMedal.Bronze, entry.CalculateMedal(5, 100, 100));
            Assert.AreEqual(ChallengeMedal.Gold, entry.CalculateMedal(5, 100, 300));
        }
    }
}
