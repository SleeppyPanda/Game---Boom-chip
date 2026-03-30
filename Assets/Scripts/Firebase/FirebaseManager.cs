using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.RemoteConfig;
using System;
using System.Collections.Generic;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;

    private bool isFirebaseInitialized = false;
    private bool _hasNotifiedAdsManager = false;

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

    // --- FIX LỖI OnAppDisposed & Finalize ---
    private void OnDestroy()
    {
        if (Instance == this)
        {
            isFirebaseInitialized = false;
            try
            {
                // Giải phóng tài nguyên Firebase khi ứng dụng đóng hoặc Object bị hủy
                if (FirebaseApp.DefaultInstance != null)
                {
                    FirebaseApp.DefaultInstance.Dispose();
                    Debug.Log("<color=red>[Firebase] Disposed Successfully</color>");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Firebase] Error during dispose: " + e.Message);
            }
        }
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // 1. Khởi tạo Analytics
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

                // 2. Cấu hình Fetch Setting (Fix lỗi cache)
                ConfigSettings settings = new ConfigSettings
                {
                    MinimumFetchIntervalInMilliseconds = 0
                };
                FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(settings);

                // 3. Khởi tạo Remote Config
                InitializeRemoteConfig();

                isFirebaseInitialized = true;
                Debug.Log("<color=green>[Firebase] Initialized Successfully</color>");

                // 4. Ghi nhận sự kiện loading lần đầu
                LogFirstLoadingComplete();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    private void InitializeRemoteConfig()
    {
        FetchRemoteConfig();
    }

    // Tự động cập nhật khi người chơi mở lại app từ background
    void OnApplicationPause(bool pauseStatus)
    {
        // pauseStatus = false nghĩa là Resume (quay lại app)
        if (!pauseStatus && isFirebaseInitialized)
        {
            Debug.Log("<color=orange>[Firebase] App Resumed - Refreshing Remote Config...</color>");
            FetchRemoteConfig();
        }
    }

    public void FetchRemoteConfig()
    {
        // Kiểm tra an toàn trước khi truy cập DefaultInstance
        if (FirebaseApp.DefaultInstance == null) return;

        // Set defaults tập trung
        Dictionary<string, object> defaults = new Dictionary<string, object> {
            { AdEventTracker.KEY_ADS_INTERVAL, 45 },
            { AdEventTracker.KEY_RATING_POPUP, false },
            { AdEventTracker.KEY_SHOW_OPEN_ADS, false },
            { AdEventTracker.KEY_SHOW_OPEN_ADS_FIRST, false },
            { AdEventTracker.KEY_SHOW_RESUME_ADS, false },
            { AdEventTracker.KEY_SHOW_BANNER, false },
            { AdEventTracker.KEY_TIME_RELOAD_COLLAP, 10 },
            { AdEventTracker.KEY_INTER_P1_CHOOSE, false },
            { AdEventTracker.KEY_INTER_P2_CHOOSE, false },
            { AdEventTracker.KEY_INTER_BACK_HOME, false },
            { AdEventTracker.KEY_INTER_RETRY, false },
            { AdEventTracker.KEY_RW_CHALLENGE, false },
            { AdEventTracker.KEY_RW_PREDICTION, false },
            { AdEventTracker.KEY_RW_PROFILE, "" },
            { AdEventTracker.KEY_MREC_P1_CHOOSE, false },
            { AdEventTracker.KEY_MREC_P2_CHOOSE, false },
            { AdEventTracker.KEY_MREC_LOADING, false },
            { AdEventTracker.KEY_MREC_GAMEPLAY, false },
            { AdEventTracker.KEY_MREC_COMPLETE, false }
        };

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(t => {
            // Kiểm tra instance lần nữa đề phòng app đóng trong lúc chờ Task
            if (FirebaseApp.DefaultInstance == null) return;

            FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[Firebase] Remote Config Fetch Failed: " + task.Exception);
                    if (NetworkErrorUI.Instance != null)
                        NetworkErrorUI.Instance.Show(() => FetchRemoteConfig());
                    return;
                }

                Debug.Log("<color=green>[Firebase] Remote Config Activated & Updated</color>");

                // Chỉ notify AdsManager lần đầu tiên
                if (!_hasNotifiedAdsManager && AdsManager.Instance != null)
                {
                    _hasNotifiedAdsManager = true;
                    AdsManager.Instance.OnRemoteConfigReady();
                }
            });
        });
    }

    #region CORE EVENTS (Giữ nguyên cho BoomChipManager)

    // --- 1. first_loading_complete ---
    public void LogFirstLoadingComplete()
    {
        if (PlayerPrefs.GetInt("FiredFirstLoading", 0) == 0)
        {
            AdEventTracker.LogFirstLoadingComplete();
            PlayerPrefs.SetInt("FiredFirstLoading", 1);
            PlayerPrefs.Save();
        }
    }

    // --- 2. count_mode_xx ---
    public void LogModeEnter(int modeID)
    {
        AdEventTracker.TrackModeEnter((AdEventTracker.GameMode)modeID);
    }

    // --- 3. count_complete_xx ---
    public void LogModeComplete(int modeID)
    {
        AdEventTracker.TrackModeComplete((AdEventTracker.GameMode)modeID);
    }

    // --- 4. count_avatar_xx ---
    public void LogCountAvatar(int avatarID)
    {
        AdEventTracker.TrackAvatarChoose(avatarID);
    }
    #endregion

    #region ADS REVENUE
    public void LogAdImpression(string format, string platform, string source, string unitName, double value, string currency)
    {
        if (!isFirebaseInitialized) return;

        Parameter[] AdParameters = {
            new Parameter("ad_format", format),
            new Parameter("ad_platform", platform),
            new Parameter("ad_source", source),
            new Parameter("ad_unit_name", unitName),
            new Parameter("value", value),
            new Parameter("currency", currency)
        };

        FirebaseAnalytics.LogEvent("ad_impression", AdParameters);
        Debug.Log($"<color=yellow>[Firebase Ads]</color> {format} | {value} {currency}");
    }
    #endregion

    #region HELPER METHODS
    public void LogEvent(string eventName)
    {
        if (!isFirebaseInitialized) return;
        FirebaseAnalytics.LogEvent(eventName);

        if (AppsflyerManager.Instance != null)
        {
            AppsflyerManager.Instance.SendCustomEvent(eventName, null);
        }

        Debug.Log($"<color=cyan>Firebase Event:</color> {eventName}");
    }

    public void LogCustomEvent(string eventName, string paramName, string paramValue)
    {
        if (!isFirebaseInitialized) return;
        FirebaseAnalytics.LogEvent(eventName, paramName, paramValue);
    }
    #endregion
}