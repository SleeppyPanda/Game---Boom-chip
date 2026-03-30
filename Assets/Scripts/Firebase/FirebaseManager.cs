using UnityEngine;
using Firebase;
using Firebase.Analytics;
using System.Collections.Generic;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    private bool isFirebaseInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirebase()
    {
        // Sử dụng ContinueWithOnMainThread để đảm bảo an toàn cho Unity
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                isFirebaseInitialized = true;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Debug.Log("<color=green>Firebase Analytics Initialized</color>");

                // Ghi nhận sự kiện loading thành công lần đầu
                LogFirstLoadingComplete();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    #region CORE EVENTS
    // --- 1. first_loading_complete ---
    public void LogFirstLoadingComplete()
    {
        if (PlayerPrefs.GetInt("FiredFirstLoading", 0) == 0)
        {
            LogEvent("first_loading_complete");
            PlayerPrefs.SetInt("FiredFirstLoading", 1);
            PlayerPrefs.Save();
        }
    }

    // --- 2. count_mode_xx (Khi người chơi vào một Mode) ---
    public void LogModeEnter(int modeID)
    {
        LogEvent($"count_mode_{modeID:D2}"); // Ví dụ: count_mode_09
    }

    // --- 3. count_complete_xx (Khi người chơi thắng/xong một Mode) ---
    public void LogModeComplete(int modeID)
    {
        LogEvent($"count_complete_{modeID:D2}"); // Ví dụ: count_complete_09
    }

    // --- 4. count_avatar_xx ---
    public void LogCountAvatar(int avatarID)
    {
        LogEvent($"count_avatar_{avatarID:D2}");
    }
    #endregion

    #region ADS REVENUE (AD_IMPRESSION)
    /// <summary>
    /// Hàm bắn doanh thu quảng cáo chuẩn theo schema của Firebase/Applovin/Admob
    /// </summary>
    public void LogAdImpression(string format, string platform, string source, string unitName, double value, string currency)
    {
        if (!isFirebaseInitialized) return;

        Parameter[] AdParameters = {
            new Parameter("ad_format", format),        // INTER, REWARDED, BANNER, AOA...
            new Parameter("ad_platform", platform),    // admob, maxads...
            new Parameter("ad_source", source),        // AdMob_Mediation...
            new Parameter("ad_unit_name", unitName),   // ID hoặc tên Unit
            new Parameter("value", value),             // Giá trị doanh thu (0.00x)
            new Parameter("currency", currency)        // USD, VND...
        };

        FirebaseAnalytics.LogEvent("ad_impression", AdParameters);
        Debug.Log($"<color=yellow>[Firebase Ads]</color> {format} | {value} {currency}");
    }
    #endregion

    #region HELPER METHODS
    // Hàm bổ trợ ghi sự kiện nhanh (chỉ có tên event)
    private void LogEvent(string eventName)
    {
        if (!isFirebaseInitialized)
        {
            Debug.LogWarning($"Firebase not init. Cannot log: {eventName}");
            return;
        }
        FirebaseAnalytics.LogEvent(eventName);
        Debug.Log($"<color=cyan>Firebase Event:</color> {eventName}");
    }

    // Hàm bổ trợ ghi sự kiện có tham số tùy chỉnh nếu cần sau này
    public void LogCustomEvent(string eventName, string paramName, string paramValue)
    {
        if (!isFirebaseInitialized) return;
        FirebaseAnalytics.LogEvent(eventName, paramName, paramValue);
    }
    #endregion
}