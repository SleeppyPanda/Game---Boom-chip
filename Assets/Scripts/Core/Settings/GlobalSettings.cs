using UnityEngine;

/// <summary>
/// Quản lý cài đặt toàn cục của trò chơi, lưu trữ thông qua PlayerPrefs.
/// </summary>
public static class GlobalSettings
{
    // Các từ khóa để lưu trữ vào bộ nhớ máy
    private const string MusicKey = "MusicVolume";
    private const string SFXKey = "SFXVolume";
    private const string VibrateKey = "VibrateEnable";

    // --- NHẠC NỀN ---
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.75f);
        set => PlayerPrefs.SetFloat(MusicKey, value);
    }

    // --- HIỆU ỨNG ÂM THANH ---
    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat(SFXKey, 0.75f);
        set => PlayerPrefs.SetFloat(SFXKey, value);
    }

    // --- CÔNG TẮC RUNG TỔNG ---
    // Trả về true nếu giá trị lưu là 1 (mặc định là 1), ngược lại là false
    public static bool IsVibrate
    {
        get => PlayerPrefs.GetInt(VibrateKey, 1) == 1;
        set => PlayerPrefs.SetInt(VibrateKey, value ? 1 : 0);
    }

    /// <summary>
    /// Chuyển đổi giá trị Slider (0 đến 1) sang Decibel (-80 đến 20) để dùng cho AudioMixer.
    /// </summary>
    public static float SliderToDecibel(float sliderValue)
    {
        if (sliderValue <= 0) return -80f;
        return Mathf.Log10(sliderValue) * 20;
    }

    /// <summary>
    /// Hàm thực thi rung duy nhất trong game. 
    /// Nó sẽ kiểm tra công tắc IsVibrate trước khi gọi lệnh hệ thống.
    /// </summary>
    public static void PlayVibrate()
    {
        // Chỉ thực hiện nếu công tắc tổng đang bật
        if (IsVibrate)
        {
            // Chỉ chạy lệnh rung trên thiết bị di động thật (Android/iOS)
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            try 
            {
                Handheld.Vibrate();
            }
            catch (System.Exception e) 
            {
                Debug.LogWarning("Vibration không hỗ trợ hoặc lỗi: " + e.Message);
            }
#endif
        }
    }

    /// <summary>
    /// Lưu thủ công các thay đổi vào bộ nhớ (Thường gọi khi đóng Setting Panel).
    /// </summary>
    public static void SaveAllSettings()
    {
        PlayerPrefs.Save();
    }
}