using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Updraft.Tests
{
    public class MobileBuildConfigurationTests
    {
        [Test]
        public void NativeInputBackend_IsNewInputSystem_NotLegacyOnly()
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            StringAssert.Contains("activeInputHandler: 1", File.ReadAllText(Path.Combine(project, "ProjectSettings", "ProjectSettings.asset")),
                "The phone controls and UGUI module require native new-Input-System events.");
        }
    }
}
