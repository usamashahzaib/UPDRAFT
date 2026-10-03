using NUnit.Framework;
using Hotdrop.Core;

namespace Hotdrop.Tests
{
    /// <summary>QA-027 regression: events carry the real region / party size / match, not constants.</summary>
    public class TelemetryContextTests
    {
        string[] savedQueue;

        [SetUp] public void SaveQueue() => savedQueue = Telemetry.ReadQueued();

        [TearDown]
        public void RestoreQueue()
        {
            Telemetry.ReplaceQueue(savedQueue);
            Telemetry.Region = "offline"; Telemetry.PartySize = 1; Telemetry.CurrentMatchId = "";
        }

        [Test]
        public void OnlineContext_IsWrittenIntoTheEnvelope()
        {
            Telemetry.Region = "eu";
            Telemetry.PartySize = 3;
            Telemetry.CurrentMatchId = "m-123";
            Telemetry.Track("online_match_joined", ("slot", "2"));
            var lines = Telemetry.ReadQueued();
            string last = lines[lines.Length - 1];
            StringAssert.Contains("\"region\":\"eu\"", last);
            StringAssert.Contains("\"partySize\":3", last);
            StringAssert.Contains("\"matchId\":\"m-123\"", last);
        }

        [Test]
        public void OfflineContext_DefaultsToOffline()
        {
            Telemetry.Track("first_open");
            var lines = Telemetry.ReadQueued();
            StringAssert.Contains("\"region\":\"offline\"", lines[lines.Length - 1]);
        }
    }
}


