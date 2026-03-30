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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            isFirebaseInitialized = false;
            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    FirebaseApp.DefaultInstance.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Firebase] Error during dispose: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Được gọi bởi LoadingManager sau khi Firebase init thành công.
    /// </summary>
    public void MarkFirebaseInitialized()
    {
        isFirebaseInitialized = true;
    }

    /// <summary>
    /// RC defaults dùng chung cho LoadingManager (lần đầu) và FirebaseManager (resume).
    /// </summary>
    public static Dictionary<string, object> GetRemoteConfigDefaults()
    {
        return new Dictionary<string, object> {
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
    }

    // Tự động cập nhật Remote Config khi người chơi mở lại app từ background
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && isFirebaseInitialized)
        {
            FetchRemoteConfig();
        }
    }

    /// <summary>
    /// Fetch Remote Config khi resume app.
    /// </summary>
    public void FetchRemoteConfig()
    {
        if (FirebaseApp.DefaultInstance == null) return;

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(GetRemoteConfigDefaults()).ContinueWithOnMainThread(t => {
            if (FirebaseApp.DefaultInstance == null) return;

            FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[Firebase RC] FetchAndActivate FAILED (resume): " + task.Exception);
                    return;
                }
                Debug.Log("<color=green>[Firebase RC] Remote Config refreshed (resume)</color>");
            });
        });
    }

    #region CORE EVENTS

    public void LogFirstLoadingComplete()
    {
        if (PlayerPrefs.GetInt("FiredFirstLoading", 0) == 0)
        {
            AdEventTracker.LogFirstLoadingComplete();
            PlayerPrefs.SetInt("FiredFirstLoading", 1);
            PlayerPrefs.Save();
        }
    }

    public void LogModeEnter(int modeID)
    {
        AdEventTracker.TrackModeEnter((AdEventTracker.GameMode)modeID);
    }

    public void LogModeComplete(int modeID)
    {
        AdEventTracker.TrackModeComplete((AdEventTracker.GameMode)modeID);
    }

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
    }

    public void LogCustomEvent(string eventName, string paramName, string paramValue)
    {
        if (!isFirebaseInitialized) return;
        FirebaseAnalytics.LogEvent(eventName, paramName, paramValue);
    }
    #endregion
}
