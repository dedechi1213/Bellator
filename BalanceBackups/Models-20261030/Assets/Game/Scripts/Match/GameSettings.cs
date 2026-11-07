using UnityEngine;

namespace Bellator.Match
{
    /// <summary>Local preferences, shared by all scenes and applied immediately.</summary>
    public static class GameSettings
    {
        const string Prefix = "Bellator.Settings.";
        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", .8f));
        public static float Sensitivity => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Sensitivity", 1), .25f, 3);
        public static float HorizontalSensitivity => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "HorizontalSensitivity", 1), .25f, 3);
        public static float VerticalSensitivity => Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "VerticalSensitivity", 1), .25f, 3);
        public static bool InvertVertical => PlayerPrefs.GetInt(Prefix + "InvertVertical", 0) == 1;
        public static float CameraShake => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "CameraShake", .65f));
        public static void SetHorizontalSensitivity(float value) => PlayerPrefs.SetFloat(Prefix + "HorizontalSensitivity", Mathf.Clamp(value, .25f, 3));
        public static void SetVerticalSensitivity(float value) => PlayerPrefs.SetFloat(Prefix + "VerticalSensitivity", Mathf.Clamp(value, .25f, 3));
        public static void SetInvertVertical(bool value) => PlayerPrefs.SetInt(Prefix + "InvertVertical", value ? 1 : 0);
        public static void SetCameraShake(float value) => PlayerPrefs.SetFloat(Prefix + "CameraShake", Mathf.Clamp01(value));
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
