using System.Runtime.InteropServices;
using UnityEngine;

namespace Hotdrop.Core
{
    /// <summary>Player-controlled haptic strength with native mobile amplitude support.</summary>
    public static class HapticFeedback
    {
        public const string IntensityKey = "Haptics.Intensity";
        const string EnabledKey = "VibrationEnabled";

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void HotdropHapticPulse(float intensity);
#endif
        static bool androidUnavailableLogged;
        static float lastPulseTime = -1f;

        public static float Intensity => Mathf.Clamp01(PlayerPrefs.GetFloat(IntensityKey, 0.7f));

        public static void SetIntensity(float value)
        {
            PlayerPrefs.SetFloat(IntensityKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        public static void Pulse(float eventStrength = 1f)
        {
            float strength = Intensity * Mathf.Clamp01(eventStrength);
            if (PlayerPrefs.GetInt(EnabledKey, 1) != 1 || strength <= 0.01f || !Application.isMobilePlatform) return;
            if (lastPulseTime >= 0f && Time.unscaledTime - lastPulseTime < 0.08f) return;
            lastPulseTime = Time.unscaledTime;
#if UNITY_ANDROID && !UNITY_EDITOR
            PulseAndroid(strength);
#elif UNITY_IOS && !UNITY_EDITOR
            HotdropHapticPulse(strength);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static void PulseAndroid(float strength)
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null) return;
                    if (version.GetStatic<int>("SDK_INT") >= 26)
                    {
                        using (var effectType = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (var effect = effectType.CallStatic<AndroidJavaObject>("createOneShot", 28L,
                            Mathf.Clamp(Mathf.RoundToInt(strength * 255f), 1, 255)))
                            vibrator.Call("vibrate", effect);
                    }
                    else vibrator.Call("vibrate", 28L);
                }
            }
            catch (System.Exception exception)
            {
                if (!androidUnavailableLogged)
                {
                    androidUnavailableLogged = true;
                    Debug.LogWarning("Haptic feedback is unavailable on this Android device: " + exception.Message);
                }
            }
        }
#endif
    }
}
