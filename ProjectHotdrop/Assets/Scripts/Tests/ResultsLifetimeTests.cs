using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Hotdrop.Tests
{
    /// <summary>
    /// QA-016 regression: the dedicated server closes the session ~12 s after results, which
    /// destroys the client's NetSession GameObject. The results screen's HOME / PLAY AGAIN
    /// actions must still work afterwards instead of throwing on the destroyed session.
    /// Network types are resolved by name so this assembly needs no Fusion references.
    /// </summary>
    public class ResultsLifetimeTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Home_AfterServerClosedSession_ReturnsHomeWithoutThrowing()
        {
            var presenterType = Type.GetType("Hotdrop.Network.NetClientPresenter, Hotdrop.Network");
            var sessionType = Type.GetType("Hotdrop.Network.NetSession, Hotdrop.Network");
            Assert.IsNotNull(presenterType);
            Assert.IsNotNull(sessionType);

            var presenterGo = new GameObject("Presenter");
            presenterGo.SetActive(false);
            var presenter = presenterGo.AddComponent(presenterType);
            var sessionGo = new GameObject("Session");
            sessionGo.SetActive(false);
            var session = sessionGo.AddComponent(sessionType);

            bool wentHome = false;
            presenterType.GetField("session", Hidden).SetValue(presenter, session);
            presenterType.GetField("returnHome", Hidden).SetValue(presenter, (Action)(() => wentHome = true));

            UnityEngine.Object.DestroyImmediate(sessionGo); // server teardown while results are up

            Assert.DoesNotThrow(() => presenterType.GetMethod("Home", Hidden).Invoke(presenter, null));
            Assert.IsTrue(wentHome, "HOME must still return to the home screen after the session is gone");

            UnityEngine.Object.DestroyImmediate(presenterGo);
        }
    }
}


