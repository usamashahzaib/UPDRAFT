using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Updraft.Tests
{
    /// <summary>
    /// Server-authority and privacy contracts for the networked match that the QA audit's
    /// fixes left open: AFK removal is final (spec 18), account ids never replicate, and the
    /// server never trusts a client-chosen shot direction. Network types are resolved by
    /// name so this assembly needs no Fusion references.
    /// </summary>
    public class NetAuthorityTests
    {
        static Type NetPlayerType
        {
            get
            {
                var t = Type.GetType("Updraft.Network.NetPlayer, Updraft.Network");
                Assert.IsNotNull(t, "NetPlayer type not found");
                return t;
            }
        }

        static bool IsNetworked(PropertyInfo p) => p.GetCustomAttributes(true).Any(a => a.GetType().Name == "NetworkedAttribute");

        [Test]
        public void AccountId_IsServerOnly_NotReplicatedToClients()
        {
            var accountId = NetPlayerType.GetProperty("AccountId");
            Assert.IsNotNull(accountId);
            Assert.IsFalse(IsNetworked(accountId), "every client would receive every opponent's internal account id");
        }

        [Test]
        public void AfkRemoved_IsReplicated_SoTheRemovedClientCanLeave()
        {
            var removed = NetPlayerType.GetProperty("AfkRemoved");
            Assert.IsNotNull(removed);
            Assert.IsTrue(IsNetworked(removed), "the removed player's client must learn it was removed");
        }

        [Test]
        public void Reconnect_RefusesSeatRemovedForInactivity()
        {
            string source = System.IO.File.ReadAllText("Assets/Scripts/Network/NetSession.cs");
            StringAssert.Contains("existing.AfkRemoved", source,
                "a removed AFK player must not reclaim the seat from the substitute bot");
        }

        [Test]
        public void ShotDirection_ComesFromReplicatedYawPitch_NotClientAimPoint()
        {
            string source = System.IO.File.ReadAllText("Assets/Scripts/Network/NetPlayer.cs");
            Assert.IsFalse(Regex.IsMatch(source, @"cameraAimPoint\s*=\s*input\.AimPoint"),
                "the server must not aim at a raw client-supplied point (silent aim)");
            StringAssert.Contains("Quaternion.Euler(input.Pitch, input.Yaw, 0f) * Vector3.forward", source);
        }
    }
}
