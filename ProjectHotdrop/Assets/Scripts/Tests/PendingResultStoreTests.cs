using System.IO;
using NUnit.Framework;
using Hotdrop.Core;

namespace Hotdrop.Tests
{
    /// <summary>
    /// QA-017 regression: a result the dedicated server could not deliver survives on disk
    /// (including a process restart) until it is delivered, and is then removed.
    /// </summary>
    public class PendingResultStoreTests
    {
        string dir;

        [SetUp]
        public void SetUp() => dir = Path.Combine(Path.GetTempPath(), "updraft_outbox_" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown() { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

        [Test]
        public void SavedResult_SurvivesRestart_UntilRemoved()
        {
            var matchId = System.Guid.NewGuid().ToString();
            new PendingResultStore(dir).Save(matchId, "{\"Draw\":false}");

            var afterRestart = new PendingResultStore(dir); // new server process
            var pending = afterRestart.Pending();
            Assert.AreEqual(1, pending.Count);
            Assert.AreEqual(matchId, pending[0].matchId);
            Assert.AreEqual("{\"Draw\":false}", pending[0].json);

            afterRestart.Remove(matchId);
            Assert.AreEqual(0, new PendingResultStore(dir).Pending().Count);
        }

        [Test]
        public void SavingTwice_KeepsOneLatestCopy()
        {
            var store = new PendingResultStore(dir);
            store.Save("m1", "{\"a\":1}");
            store.Save("m1", "{\"a\":2}");
            Assert.AreEqual(1, store.Pending().Count);
            Assert.AreEqual("{\"a\":2}", store.Pending()[0].json);
        }

        [Test]
        public void HostileMatchId_CannotEscapeTheOutboxDirectory()
        {
            var store = new PendingResultStore(dir);
            store.Save("../../evil", "{}");
            Assert.AreEqual(1, Directory.GetFiles(dir).Length);
        }
    }
}


