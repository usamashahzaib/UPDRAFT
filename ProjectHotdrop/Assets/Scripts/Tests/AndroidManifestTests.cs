using System.IO;
using System.Xml;
using NUnit.Framework;
using UnityEngine;

namespace Hotdrop.Tests
{
    /// <summary>
    /// Regression: a custom Assets/Plugins/Android/AndroidManifest.xml replaces Unity's
    /// main manifest template. A version holding only the VIBRATE permission produced
    /// APKs that installed on the device but had no launchable activity.
    /// </summary>
    public class AndroidManifestTests
    {
        const string AndroidNs = "http://schemas.android.com/apk/res/android";

        [Test]
        public void CustomMainManifest_DeclaresGameActivityLauncher_AndVibrate()
        {
            string path = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
            if (!File.Exists(path)) Assert.Pass("No custom main manifest; Unity's template supplies the launcher.");

            var doc = new XmlDocument();
            doc.Load(path);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("android", AndroidNs);

            var launcher = doc.SelectSingleNode(
                "/manifest/application/activity[@android:name='com.unity3d.player.UnityPlayerGameActivity']" +
                "[intent-filter/action/@android:name='android.intent.action.MAIN']" +
                "[intent-filter/category/@android:name='android.intent.category.LAUNCHER']", ns);
            Assert.IsNotNull(launcher, "The custom manifest must keep the GameActivity MAIN/LAUNCHER block.");

            Assert.IsNotNull(doc.SelectSingleNode("/manifest/uses-permission[@android:name='android.permission.VIBRATE']", ns),
                "Haptics need the VIBRATE permission.");
        }
    }
}
