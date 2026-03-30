using UnityEngine;
using Firebase.Analytics;

public static class AdEventTracker
{
    // 1. Ghi nhận doanh thu quảng cáo (Bắt buộc để theo dõi ROAS)
    public static void LogAdImpression(string adPlatform, string adSource, string adUnit, double revenue)
    {
        Parameter[] adParameters = {
            new Parameter("ad_platform", adPlatform),
            new Parameter("ad_source", adSource),
            new Parameter("ad_unit_name", adUnit),
            new Parameter("value", revenue),
            new Parameter("currency", "USD")
        };
        FirebaseAnalytics.LogEvent("ad_impression", adParameters);
        Debug.Log($"[Firebase] Tracked Revenue: {revenue} from {adUnit}");
    }

    // 2. Ghi nhận các sự kiện quan trọng trong Game
    public static void LogFirebaseEvent(string eventName)
    {
        FirebaseAnalytics.LogEvent(eventName);
        Debug.Log($"[Firebase] Log Event: {eventName}");
    }

    // 3. Helper ghi nhận Mode chơi (count_mode_01, count_mode_02...)
    public static void TrackModeEnter(int modeID)
    {
        LogFirebaseEvent($"count_mode_{modeID:00}");
    }

    // 4. Helper ghi nhận Hoàn thành Mode (count_complete_01...)
    public static void TrackModeComplete(int modeID)
    {
        LogFirebaseEvent($"count_complete_{modeID:00}");
    }

    // 5. Helper ghi nhận chọn Avatar
    public static void TrackAvatarChoose(int avatarID)
    {
        LogFirebaseEvent($"count_avatar_{avatarID:00}");
    }

    // --- CÁC HÀM TRỐNG CHO APPSFLYER (Để AdsManager không báo lỗi) ---
    // Khi nào bạn đăng ký tài khoản Appsflyer, chúng ta sẽ điền code vào đây sau.
    public static void TrackInterEligible() { }
    public static void TrackInterApiCalled() { }
    public static void TrackInterDisplayed() { }
    public static void TrackRewardEligible() { }
    public static void TrackRewardApiCalled() { }
    public static void TrackRewardDisplayed() { }
}