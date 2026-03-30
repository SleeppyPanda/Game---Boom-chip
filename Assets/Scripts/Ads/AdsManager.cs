using UnityEngine;
using GoogleMobileAds.Api;
using Firebase.RemoteConfig;
using System;
using System.Collections.Generic;
using Firebase.Extensions;
using System.Collections;
using AppsFlyerSDK; // Tích hợp AppsFlyer

#if UNITY_ANDROID
using Google.Play.Review; 
#endif

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("AppsFlyer Settings")]
    public string appsFlyerDevKey = "YOUR_DEV_KEY_HERE"; // Thay bằng Dev Key của bạn
    public string appId = "com.your.package.name";       // Thay bằng Package Name của bạn

    [Header("Ad Unit IDs (Test IDs)")]
    public string adUnitIdBanner = "ca-app-pub-3940256099942544/6300978111";
    public string adUnitIdInter = "ca-app-pub-3940256099942544/1033173712";
    public string adUnitIdRewarded = "ca-app-pub-3940256099942544/5224354917";
    public string adUnitIdAOA = "ca-app-pub-3940256099942544/3419835294";
    public string adUnitIdMREC = "ca-app-pub-3940256099942544/6300978111";

    private BannerView _bannerView;
    private BannerView _mrecView;
    private InterstitialAd _interstitialAd;
    private RewardedAd _rewardedAd;
    private AppOpenAd _appOpenAd;

    private bool _isAdShowing = false;
    private bool _isFirstOpenSession = true;
    private float _lastTimeShowInterstitial = -100f;
    private DateTime _aoaExpireTime;
    private Coroutine _bannerReloadCoroutine;

    // Properties để các Script khác truy cập nhanh
    public bool IsShowRwChallenge => GetConfigBool("is_show_rw_challenge");
    public bool IsShowRwPrediction => GetConfigBool("is_show_rw_prediction");
    public string RwProfileAvatars => GetConfigString("is_show_rw_profile");

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        // 1. Khởi tạo AppsFlyer
        AppsFlyer.initSDK(appsFlyerDevKey, appId);
        AppsFlyer.startSDK();

        // 2. Khởi tạo AdMob
        MobileAds.Initialize(initStatus => {
            LoadInterstitialAd();
            LoadRewardedAd();
            LoadAppOpenAd();
        });

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
#if UNITY_EDITOR
        Debug.LogWarning("<color=yellow>[AdsManager]</color> Editor Mode: Bypassing Firebase check to avoid DLL error.");
        FetchRemoteConfig(); // Chạy thẳng Remote Config (sẽ dùng Default values nếu DLL lỗi)
        return;
#endif

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == Firebase.DependencyStatus.Available)
                FetchRemoteConfig();
            else
                Debug.LogError("AdsManager: Could not resolve Firebase dependencies: " + task.Result);
        });
    }

    private void FetchRemoteConfig()
    {
        Dictionary<string, object> defaults = new Dictionary<string, object> {
            { "ads_interval", 45 },
            { "rating_popup", false },
            { "show_open_ads", false },
            { "show_open_ads_first_open", false },
            { "show_resume_ads", false },
            { "is_show_inter_p1_choose", false },
            { "is_show_inter_p2_choose", false },
            { "is_show_inter_back_home", false },
            { "is_show_inter_retry", false },
            { "is_show_banner", false },
            { "time_reload_collap_ad", 10 },
            { "is_show_mrec_p1_choose", false },
            { "is_show_mrec_p2_choose", false },
            { "is_show_mrec_loading_game", false },
            { "is_show_mrec_gameplay", false },
            { "is_show_mrec_complete_game", false },
            { "is_show_rw_challenge", false },
            { "is_show_rw_prediction", false },
            { "is_show_rw_profile", "" }
        };

        // Tránh gọi Firebase logic nếu đang ở Editor và bị lỗi DLL
        try
        {
            FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(t => {
                FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().ContinueWithOnMainThread(task => {
                    if (task.IsCompleted)
                    {
                        Debug.Log("AdsManager: Remote Config Synchronized!");
                        ShowBanner();
                        CheckShowRating();
                    }
                });
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning("AdsManager: Firebase DLL not found, using local defaults. " + e.Message);
        }
    }

    public bool GetConfigBool(string key)
    {
        try { return FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue; }
        catch { return false; }
    }
    public long GetConfigLong(string key)
    {
        try { return FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue; }
        catch { return 0; }
    }
    public string GetConfigString(string key)
    {
        try { return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue; }
        catch { return ""; }
    }

    #region APPSFLYER LOGGING HELPERS
    private void LogAppsFlyer(string eventName, Dictionary<string, string> parameters = null)
    {
        AppsFlyer.sendEvent(eventName, parameters);
        Debug.Log($"<color=orange>[AppsFlyer Log]</color> {eventName}");
    }
    #endregion

    #region INTERSTITIAL
    public void LoadInterstitialAd()
    {
        if (_interstitialAd != null) _interstitialAd.Destroy();
        InterstitialAd.Load(adUnitIdInter, new AdRequest(), (ad, error) => {
            if (error != null) return;
            _interstitialAd = ad;

            // AppsFlyer: API Called (Ad is ready)
            LogAppsFlyer("af_inters_api_called");

            _interstitialAd.OnAdPaid += (adValue) => {
                SendRevenueToAll("INTER", "admob", adValue);
            };
        });
    }

    public void ShowInterstitial(string placementConfigKey, Action onAdClosed)
    {
        // AppsFlyer: Eligible
        LogAppsFlyer("af_inters_ad_eligible");

        bool canShowByConfig = GetConfigBool(placementConfigKey);
        float interval = (float)GetConfigLong("ads_interval");

        if (canShowByConfig && (Time.time - _lastTimeShowInterstitial >= interval))
        {
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                _interstitialAd.OnAdFullScreenContentOpened += () => {
                    _isAdShowing = true;
                    _lastTimeShowInterstitial = Time.time;
                    // AppsFlyer: Displayed
                    LogAppsFlyer("af_inters_displayed");
                };
                _interstitialAd.OnAdFullScreenContentClosed += () => {
                    _isAdShowing = false;
                    LoadInterstitialAd();
                    onAdClosed?.Invoke();
                };
                _interstitialAd.Show();
            }
            else { onAdClosed?.Invoke(); LoadInterstitialAd(); }
        }
        else onAdClosed?.Invoke();
    }
    #endregion

    #region REWARDED
    public void LoadRewardedAd()
    {
        if (_rewardedAd != null) _rewardedAd.Destroy();
        RewardedAd.Load(adUnitIdRewarded, new AdRequest(), (ad, error) => {
            if (error != null) return;
            _rewardedAd = ad;

            // AppsFlyer: API Called
            LogAppsFlyer("af_rewarded_api_called");

            _rewardedAd.OnAdPaid += (adValue) => {
                SendRevenueToAll("REWARDED", "admob", adValue);
            };
        });
    }

    public void ShowRewardedAd(string rewardType, Action onRewardEarned)
    {
        // AppsFlyer: Eligible
        LogAppsFlyer("af_rewarded_ad_eligible");

        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.OnAdFullScreenContentOpened += () => {
                _isAdShowing = true;
                // AppsFlyer: Displayed
                LogAppsFlyer("af_rewarded_ad_displayed");
            };
            _rewardedAd.OnAdFullScreenContentClosed += () => {
                _isAdShowing = false;
                LoadRewardedAd();
            };

            _rewardedAd.Show((reward) => {
                onRewardEarned?.Invoke();
            });
        }
        else LoadRewardedAd();
    }
    #endregion

    #region REVENUE LOGGING (Firebase + AppsFlyer)
    private void SendRevenueToAll(string format, string platform, AdValue adValue)
    {
        double revenue = adValue.Value / 1000000f;
        string currency = adValue.CurrencyCode;

        // 1. Firebase Log
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.LogAdImpression(format, platform, "AdMob_Mediation", "Direct_Unit", revenue, currency);
        }

        // 2. AppsFlyer Ad Revenue (af_ad_revenue)
        Dictionary<string, string> afParams = new Dictionary<string, string>();
        afParams.Add(AFInAppEvents.CURRENCY, currency);
        afParams.Add(AFInAppEvents.REVENUE, revenue.ToString());
        afParams.Add("af_ad_platform", platform);
        afParams.Add("af_ad_format", format);
        LogAppsFlyer("af_ad_revenue", afParams);

        // 3. AppsFlyer In-App Purchase/Revenue Style (af_revenue)
        Dictionary<string, string> afRev = new Dictionary<string, string>();
        afRev.Add(AFInAppEvents.REVENUE, revenue.ToString());
        afRev.Add(AFInAppEvents.CURRENCY, currency);
        LogAppsFlyer(AFInAppEvents.REVENUE, afRev);
    }
    #endregion

    #region OTHERS (Banner, AOA, Rating)
    // Các hàm ShowBanner, LoadAppOpenAd, CheckShowRating... giữ nguyên logic cũ 
    // nhưng đã được bọc bởi try-catch trong Remote Config để không crash.

    public void LoadAppOpenAd()
    {
        if (_appOpenAd != null) _appOpenAd.Destroy();
        AppOpenAd.Load(adUnitIdAOA, new AdRequest(), (ad, error) => {
            if (error != null) return;
            _appOpenAd = ad;
            _aoaExpireTime = DateTime.Now.AddHours(4);
            _appOpenAd.OnAdPaid += (adValue) => SendRevenueToAll("AOA", "admob", adValue);
        });
    }

    public void ShowAppOpenAd()
    {
        if (!GetConfigBool("show_open_ads") || _isAdShowing) return;
        if (_appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _aoaExpireTime)
        {
            _appOpenAd.OnAdFullScreenContentOpened += () => _isAdShowing = true;
            _appOpenAd.OnAdFullScreenContentClosed += () => { _isAdShowing = false; LoadAppOpenAd(); };
            _appOpenAd.Show();
        }
        else LoadAppOpenAd();
    }

    public void ShowBanner()
    {
        if (!GetConfigBool("is_show_banner")) { if (_bannerView != null) _bannerView.Destroy(); return; }
        if (_bannerView != null) _bannerView.Destroy();
        AdSize adaptiveSize = AdSize.GetPortraitAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        _bannerView = new BannerView(adUnitIdBanner, adaptiveSize, AdPosition.Bottom);
        AdRequest request = new AdRequest();
        long reloadTime = GetConfigLong("time_reload_collap_ad");
        if (reloadTime > 0)
        {
            request.Extras.Add("collapsible", "bottom");
            if (_bannerReloadCoroutine != null) StopCoroutine(_bannerReloadCoroutine);
            _bannerReloadCoroutine = StartCoroutine(ReloadBannerAfterTime((float)reloadTime));
        }
        _bannerView.OnAdPaid += (adValue) => SendRevenueToAll("BANNER", "admob", adValue);
        _bannerView.LoadAd(request);
    }

    private IEnumerator ReloadBannerAfterTime(float seconds) { yield return new WaitForSeconds(seconds); ShowBanner(); }

    public void ShowMREC(string configKey)
    {
        if (!GetConfigBool(configKey)) { HideMREC(); return; }
        if (_mrecView != null) _mrecView.Destroy();
        _mrecView = new BannerView(adUnitIdMREC, AdSize.MediumRectangle, AdPosition.Bottom);
        _mrecView.OnAdPaid += (adValue) => SendRevenueToAll("MREC", "admob", adValue);
        _mrecView.LoadAd(new AdRequest());
    }

    public void HideMREC() { if (_mrecView != null) { _mrecView.Destroy(); _mrecView = null; } }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (_isFirstOpenSession)
            {
                _isFirstOpenSession = false;
                if (!GetConfigBool("show_open_ads_first_open")) return;
            }
            if (GetConfigBool("show_resume_ads")) ShowAppOpenAd();
        }
    }

    public void CheckShowRating() { if (GetConfigBool("rating_popup")) StartCoroutine(RequestReviewProcedure()); }

    private IEnumerator RequestReviewProcedure()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var reviewManager = new ReviewManager();
        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        if (requestFlowOperation.Error != ReviewErrorCode.NoError) yield break;
        var reviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = reviewManager.LaunchReviewFlow(reviewInfo);
        yield return launchFlowOperation;
#else
        yield return null;
#endif
    }
    #endregion
}