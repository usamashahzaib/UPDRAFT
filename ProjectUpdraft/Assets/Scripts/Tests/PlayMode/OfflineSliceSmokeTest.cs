using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Updraft.Match;
using Updraft.Player;
using Updraft.Tutorial;

namespace Updraft.Tests.PlayMode
{
    /// <summary>
    /// Milestone B acceptance check: does the generated offline-slice scene
    /// (Assets/Scenes/Main.unity) actually run without throwing? This is the
    /// first genuine Play Mode verification — everything before this only
    /// proved the project compiles, not that GameBootstrap/SceneSetup's
    /// generated hierarchy survives Awake/Start/Update.
    ///
    /// Unity's Test Framework fails a PlayMode test automatically on any
    /// Debug.LogError/LogException during the test, so "no exception" here
    /// is a real, checked assertion, not just an absence of a manual check.
    /// </summary>
    public class OfflineSliceSmokeTest
    {
        [UnityTest]
        public IEnumerator MainScene_LoadsAndRunsForFiveSeconds_WithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;

            Assert.AreEqual(1, Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length,
                "Tutorial startup must create one player, not also auto-start a practice match.");
            Assert.AreEqual(1, Object.FindObjectsByType<TutorialController>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(0, Object.FindFirstObjectByType<MatchController>().ActivePlayerCount,
                "MatchController must wait for GameBootstrap to choose a mode.");
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);

            float elapsed = 0f;
            while (elapsed < 5f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            Assert.Pass("Scene ran for 5 seconds without a logged error or exception.");
        }
    }
}
