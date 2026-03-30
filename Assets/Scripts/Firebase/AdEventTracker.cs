using UnityEngine;
using Firebase.Analytics;
using Firebase.RemoteConfig;
using AppsFlyerSDK;

public static class AdEventTracker
{
    // --- DANH SÁCH ID MÀN CHƠI (MAPPING) ---
    public enum GameMode
    {
        None = 0,
        ChipBomb = 1,
        WillYouMarry = 2,
        LetsMakeBaby = 3,
        ConsoleOrShopping = 4,
        BurgerOrPizza = 5,
        CarVsShopping = 6,
        KissOrSlap = 7,
        StayHomeOrTravel = 8,
        MysteryRoom = 9,
        RollAndCollect = 10,
        Challenge = 11,
        Prediction = 12
    }

    // --- REMOTE CONFIG KEYS ---
    public const string KEY_ADS_INTERVAL = "ads_interval";
    public const string KEY_RATING_POPUP = "rating_popup";
    public const string KEY_SHOW_OPEN_ADS = "show_open_ads";
    public const string KEY_SHOW_OPEN_ADS_FIRST = "show_open_ads_first_open";
    public const string KEY_SHOW_RESUME_ADS = "show_resume_ads";

    // Interstitial Keys
    public const string KEY_INTER_P1_CHOOSE = "is_show_inter_p1_choose";
    public const string KEY_INTER_P2_CHOOSE = "is_show_inter_p2_choose";
    public const string KEY_INTER_BACK_HOME = "is_show_inter_back_home";
    public const string KEY_INTER_RETRY = "is_show_inter_retry";

    // Banner & MREC Keys
    public const string KEY_SHOW_BANNER = "is_show_banner";
    public const string KEY_TIME_RELOAD_COLLAP = "time_reload_collap_ad";
    public const string KEY_MREC_P1_CHOOSE = "is_show_mrec_p1_choose";
    public const string KEY_MREC_P2_CHOOSE = "is_show_mrec_p2_choose";
    public const string KEY_MREC_LOADING = "is_show_mrec_loading_game";
    public const string KEY_MREC_GAMEPLAY = "is_show_mrec_gameplay";
    public const string KEY_MREC_COMPLETE = "is_show_mrec_complete_game";

    // Reward & Profile Keys
    public const string KEY_RW_CHALLENGE = "is_show_rw_challenge";
    public const string KEY_RW_PREDICTION = "is_show_rw_prediction";
    public const string KEY_RW_PROFILE = "is_show_rw_profile";

    private const string KEY_FIRST_LOAD_DONE = "first_loading_complete_tracked";

    // --- FIREBASE & APPSFLYER ANALYTICS LOGGING ---

    /// <summary>
    /// Ghi nhận khi lần đầu loading thành công (Chỉ ghi nhận 1 lần duy nhất mỗi user)
    /// </summary>
    public static void LogFirstLoadingComplete()
    {
        if (PlayerPrefs.GetInt(KEY_FIRST_LOAD_DONE, 0) == 0)
        {
            LogFirebaseEvent("first_loading_complete");
            PlayerPrefs.SetInt(KEY_FIRST_LOAD_DONE, 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Bắn ad revenue lên firebase theo chuẩn thủ công
    /// </summary>
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
    }

    /// <summary>
    /// Gửi sự kiện đồng thời lên cả Firebase và AppsFlyer
    /// </summary>
    public static void LogFirebaseEvent(string eventName)
    {
        // Gửi lên Firebase
        FirebaseAnalytics.LogEvent(eventName);

        // Gửi lên AppsFlyer (không có tham số)
        AppsFlyer.sendEvent(eventName, null);

        Debug.Log($"<color=green>[Analytics]</color> Sent Event: {eventName}");
    }

    // --- TRACKING MÀN CHƠI & AVATAR ---

    public static void TrackModeEnter(GameMode mode)
    {
        if (mode == GameMode.None) return;
        LogFirebaseEvent($"count_mode_{(int)mode:00}");
    }

    public static void TrackModeComplete(GameMode mode)
    {
        if (mode == GameMode.None) return;
        LogFirebaseEvent($"count_complete_{(int)mode:00}");
    }

    public static void TrackAvatarChoose(int avatarID)
    {
        LogFirebaseEvent($"count_avatar_{avatarID:00}");
    }

    // --- TRACKING QUẢNG CÁO (CHỈNH SỬA THEO YÊU CẦU APPSFLYER MỚI) ---

    // Interstitial Events
    public static void TrackInterEligible() { LogFirebaseEvent("af_inters_ad_eligible"); }
    public static void TrackInterApiCalled() { LogFirebaseEvent("af_inters_api_called"); }
    public static void TrackInterDisplayed() { LogFirebaseEvent("af_inters_displayed"); }

    // Rewarded Events
    public static void TrackRewardEligible() { LogFirebaseEvent("af_rewarded_ad_eligible"); }
    public static void TrackRewardApiCalled() { LogFirebaseEvent("af_rewarded_api_called"); }
    public static void TrackRewardDisplayed() { LogFirebaseEvent("af_rewarded_ad_displayed"); }

    // --- REMOTE CONFIG GETTERS ---

    // --- REMOTE CONFIG GETTERS ---

    public static bool GetBool(string key, bool defaultValue = false)
    {
        if (FirebaseRemoteConfig.DefaultInstance == null) return defaultValue;
        return FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
    }

    // THÊM TỪ KHÓA static VÀO ĐÂY
    public static float GetFloat(string key, float defaultValue)
    {
        try
        {
            if (FirebaseRemoteConfig.DefaultInstance == null) return defaultValue;

            var config = FirebaseRemoteConfig.DefaultInstance;
            string value = config.GetValue(key).StringValue;

            if (string.IsNullOrEmpty(value)) return defaultValue;

            // Ép kiểu an toàn, bất chấp dấu phẩy hay dấu chấm
            return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return defaultValue; // Nếu lỗi (FormatException), trả về số mặc định để game KHÔNG BỊ LIỆT
        }
    }

    public static string GetString(string key, string defaultValue = "")
    {
        if (FirebaseRemoteConfig.DefaultInstance == null) return defaultValue;
        return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
    }

    /// <summary>
    /// Kiểm tra xem Avatar index có nằm trong danh sách yêu cầu xem Ads không
    /// </summary>
    public static bool IsAvatarInRwList(int avatarID)
    {
        string rawList = GetString(KEY_RW_PROFILE, "");
        if (string.IsNullOrEmpty(rawList)) return false;

        string[] ids = rawList.Split(',');
        foreach (string id in ids)
        {
            if (id.Trim() == avatarID.ToString()) return true;
        }
        return false;
    }
}