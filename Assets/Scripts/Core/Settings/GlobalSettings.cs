using UnityEngine;

public static class GlobalSettings
{
    private const string MusicKey = "MusicVolume";
    private const string SFXKey = "SFXVolume";
    private const string VibrateKey = "VibrateEnable";

    public static float MusicVolume { get => PlayerPrefs.GetFloat(MusicKey, 0.75f); set => PlayerPrefs.SetFloat(MusicKey, value); }
    public static float SFXVolume { get => PlayerPrefs.GetFloat(SFXKey, 0.75f); set => PlayerPrefs.SetFloat(SFXKey, value); }
    public static bool IsVibrate { get => PlayerPrefs.GetInt(VibrateKey, 1) == 1; set => PlayerPrefs.SetInt(VibrateKey, value ? 1 : 0); }

    // Chuyển đổi Slider (0-1) sang Decibel (-80 đến 20)
    public static float SliderToDecibel(float sliderValue)
    {
        if (sliderValue <= 0) return -80f;
        return Mathf.Log10(sliderValue) * 20;
    }

    public static void PlayVibrate()
    {
        if (IsVibrate)
        {
            #if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
            #endif
        }
    }
}