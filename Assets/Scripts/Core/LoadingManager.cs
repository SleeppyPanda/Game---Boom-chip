using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using AppsFlyerSDK;

#if !UNITY_EDITOR
using Firebase;
using Firebase.Analytics;
using Firebase.RemoteConfig;
using Firebase.Extensions;
#endif

public class LoadingManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadingScreen;
    public Image fillImage;

    [Header("Settings")]
    public string sceneToLoad = "Main scene";
    [Range(0.1f, 2.0f)]
    public float loadSpeedMultiplier = 0.5f;

    private const string FIRST_TIME_KEY = "FirstTimeLoadingComplete";
    private bool _isFirebaseReady = false;
    private bool _isRemoteConfigReady = false;
    private bool _hasShownMREC = false;

    void Start()
    {
        InitializeFirebase();
        StartCoroutine(LoadAsynchronously());
    }

    private void InitializeFirebase()
    {
#if UNITY_EDITOR
        Debug.LogWarning("<color=yellow>[LoadingManager]</color> Editor Mode: Bypassing Firebase initialization.");
        _isFirebaseReady = true;
        _isRemoteConfigReady = true;
#else
        try
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                DependencyStatus dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    _isFirebaseReady = true;

                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

                    ConfigSettings settings = new ConfigSettings
                    {
                        MinimumFetchIntervalInMilliseconds = 0
                    };
                    FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(settings);

                    if (FirebaseManager.Instance != null)
                    {
                        FirebaseManager.Instance.MarkFirebaseInitialized();
                    }

                    FetchRemoteConfig();
                }
                else
                {
                    Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
                    if (NetworkErrorUI.Instance != null)
                        NetworkErrorUI.Instance.Show(() => InitializeFirebase());
                    else
                    {
                        _isFirebaseReady = true;
                        _isRemoteConfigReady = true;
                    }
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError("Critical Firebase Error: " + e.Message);
            if (NetworkErrorUI.Instance != null)
                NetworkErrorUI.Instance.Show(() => InitializeFirebase());
            else
            {
                _isFirebaseReady = true;
                _isRemoteConfigReady = true;
            }
        }
#endif
    }

    private void FetchRemoteConfig()
    {
#if !UNITY_EDITOR
        if (FirebaseApp.DefaultInstance == null)
        {
            _isRemoteConfigReady = true;
            return;
        }

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(FirebaseManager.GetRemoteConfigDefaults()).ContinueWithOnMainThread(t => {
            if (FirebaseApp.DefaultInstance == null)
            {
                _isRemoteConfigReady = true;
                return;
            }

            FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                {
                    Debug.LogWarning("[LoadingManager] RC FetchAndActivate FAILED: " + task.Exception);
                    if (NetworkErrorUI.Instance != null)
                        NetworkErrorUI.Instance.Show(() => FetchRemoteConfig());
                    return;
                }

                if (task.IsCanceled)
                {
                    _isRemoteConfigReady = true;
                    return;
                }

                _isRemoteConfigReady = true;

                if (AdsManager.Instance != null)
                {
                    AdsManager.Instance.OnRemoteConfigReady();
                }
            });
        });
#endif
    }

    IEnumerator LoadAsynchronously()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        float fakeProgress = 0f;

        while (!operation.isDone)
        {
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);
            fakeProgress = Mathf.MoveTowards(fakeProgress, realProgress, Time.deltaTime * loadSpeedMultiplier);

            if (fillImage != null)
                fillImage.fillAmount = fakeProgress;

            if (operation.progress >= 0.9f && _isFirebaseReady && _isRemoteConfigReady && fakeProgress >= 0.95f)
            {
                if (!_hasShownMREC)
                {
                    _hasShownMREC = true;

                    HandleFirstLoadingEvent();

                    if (AdsManager.Instance != null)
                    {
                        AdsManager.Instance.ShowMREC(AdEventTracker.KEY_MREC_LOADING);
                    }

                    yield return new WaitForSeconds(0.8f);
                }

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void HandleFirstLoadingEvent()
    {
        if (PlayerPrefs.GetInt(FIRST_TIME_KEY, 0) == 0)
        {
#if !UNITY_EDITOR
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.LogModeEnter(0);
            }
#endif

            Dictionary<string, string> afParams = new Dictionary<string, string>();
            afParams.Add("description", "User finished first loading screen");
            afParams.Add("version", Application.version);

            AppsFlyer.sendEvent("first_loading_complete", afParams);

            PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
            PlayerPrefs.Save();
        }
    }
}
