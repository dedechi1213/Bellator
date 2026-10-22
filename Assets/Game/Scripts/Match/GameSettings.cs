using UnityEngine;

namespace Bellator.Match
{
    /// <summary>Local preferences, shared by all scenes and applied immediately.</summary>
    public static class GameSettings
    {
        const string Prefix = "Bellator.Settings.";
        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", .8f));
        public static float Sensitivity => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Sensitivity", 1), .25f, 3);
        public static int Quality => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        public static bool SupportsScreenMode => Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.OSXPlayer;
        public static int ScreenMode => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Screen", Screen.fullScreen ? 1 : 0), 0, 1);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            AudioListener.volume = Volume;
            QualitySettings.SetQualityLevel(Quality, true);
            if (SupportsScreenMode) Screen.fullScreenMode = ScreenMode == 0 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
        }
        public static void SetVolume(float value) { PlayerPrefs.SetFloat(Prefix + "Volume", Mathf.Clamp01(value)); AudioListener.volume = Volume; }
        public static void SetSensitivity(float value) => PlayerPrefs.SetFloat(Prefix + "Sensitivity", Mathf.Clamp(value, .25f, 3));
        public static void SetQuality(int value) { PlayerPrefs.SetInt(Prefix + "Quality", Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1))); QualitySettings.SetQualityLevel(Quality, true); }
        public static void SetScreenMode(int value) { PlayerPrefs.SetInt(Prefix + "Screen", Mathf.Clamp(value, 0, 1)); if (SupportsScreenMode) Screen.fullScreenMode = ScreenMode == 0 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow; }
        public static void Save() => PlayerPrefs.Save();
    }
}
