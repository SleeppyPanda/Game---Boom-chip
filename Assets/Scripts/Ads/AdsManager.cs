using UnityEngine;
using GoogleMobileAds.Api;
using Firebase.RemoteConfig;
using System;
using System.Collections.Generic;
using Firebase.Extensions;
using System.Collections;
using AppsFlyerSDK;

#if UNITY_ANDROID
using Google.Play.Review; 
#endif

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("AppsFlyer Settings")]
    public string appsFlyerDevKey = "YOUR_DEV_KEY_HERE";
    public string appId = "com.your.package.name";

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
    private bool _isFirstTimeTruly = true;
    private float _lastTimeShowInterstitial = -100f;
    private DateTime _aoaExpireTime;
    private Coroutine _bannerReloadCoroutine;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        _isFirstTimeTruly = PlayerPrefs.GetInt("Truly_First_Open_Completed", 0) == 0;
    }

    void Start()
    {
        // 1. Khởi tạo AppsFlyer
        AppsFlyer.initSDK(appsFlyerDevKey, appId);
        AppsFlyer.startSDK();

        // 2. Khởi tạo AdMob
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // Dùng Coroutine thay cho MobileAdsEventExecutor để an toàn 100%
            StartCoroutine(LoadAdsAfterInit());
        });

        InitializeFirebase();
    }

    private IEnumerator LoadAdsAfterInit()
    {
        // Đợi một chút để đảm bảo SDK đã sẵn sàng trên Android
        yield return new WaitForSeconds(0.5f);
        LoadInterstitialAd();
        LoadRewardedAd();
        LoadAppOpenAd();
    }

    // Cách tạo Request đơn giản nhất để tránh lỗi Builder trên bản 11.x
    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }

    private void InitializeFirebase()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == Firebase.DependencyStatus.Available)
                FetchRemoteConfig();
            else
                Debug.LogError("AdsManager: Firebase dependencies error: " + task.Result);
        });
    }

    private void FetchRemoteConfig()
    {
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
            FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync().ContinueWithOnMainThread(task => {
                if (task.IsCompleted)
                {
                    Debug.Log("AdsManager: Remote Config Synchronized!");
                    AdEventTracker.LogFirstLoadingComplete();
                    ShowBanner();
                    CheckShowRating();
                    ShowAppOpenAd(false);
                }
            });
        });
    }

    #region REVENUE LOGGING
    private void SendRevenueToAll(string format, AdValue adValue)
    {
        double revenue = adValue.Value / 1000000f;
        string currency = adValue.CurrencyCode;

        AdEventTracker.LogAdImpression("AdMob", "admob", format, revenue);

        Dictionary<string, string> afParams = new Dictionary<string, string> {
            { AFInAppEvents.CURRENCY, currency },
            { AFInAppEvents.REVENUE, revenue.ToString() },
            { "af_ad_platform", "admob" },
            { "af_ad_format", format },
            { "af_ad_unit_name", GetUnitIDByFormat(format) }
        };
        AppsFlyer.sendEvent("af_ad_revenue", afParams);
    }

    private string GetUnitIDByFormat(string format)
    {
        return format switch
        {
            "INTER" => adUnitIdInter,
            "REWARDED" => adUnitIdRewarded,
            "BANNER" => adUnitIdBanner,
            "AOA" => adUnitIdAOA,
            "MREC" => adUnitIdMREC,
            _ => "unknown",
        };
    }
    #endregion

    #region INTERSTITIAL
    public void LoadInterstitialAd()
    {
        if (_interstitialAd != null) { _interstitialAd.Destroy(); _interstitialAd = null; }

        InterstitialAd.Load(adUnitIdInter, CreateAdRequest(), (ad, error) => {
            if (error != null || ad == null) return;
            _interstitialAd = ad;
            _interstitialAd.OnAdPaid += (adValue) => SendRevenueToAll("INTER", adValue);
        });
    }

    public void ShowInterstitial(string placementConfigKey, Action onAdClosed)
    {
        AdEventTracker.TrackInterEligible();

        bool isEnable = AdEventTracker.GetBool(placementConfigKey);
        float interval = AdEventTracker.GetFloat(AdEventTracker.KEY_ADS_INTERVAL, 45f);

        if (isEnable && (Time.time - _lastTimeShowInterstitial >= interval))
        {
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                AdEventTracker.TrackInterApiCalled();
                _interstitialAd.OnAdFullScreenContentOpened += () => {
                    _isAdShowing = true;
                    _lastTimeShowInterstitial = Time.time;
                    AdEventTracker.TrackInterDisplayed();
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
        if (_rewardedAd != null) { _rewardedAd.Destroy(); _rewardedAd = null; }

        RewardedAd.Load(adUnitIdRewarded, CreateAdRequest(), (ad, error) => {
            if (error != null || ad == null) return;
            _rewardedAd = ad;
            _rewardedAd.OnAdPaid += (adValue) => SendRevenueToAll("REWARDED", adValue);
        });
    }

    public void ShowRewardedAd(string logicKey, Action onRewardEarned, Action onAdFailed = null)
    {
        AdEventTracker.TrackRewardEligible();
        if (!AdEventTracker.GetBool(logicKey)) { onRewardEarned?.Invoke(); return; }

        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            AdEventTracker.TrackRewardApiCalled();
            _rewardedAd.OnAdFullScreenContentOpened += () => {
                _isAdShowing = true;
                AdEventTracker.TrackRewardDisplayed();
            };
            _rewardedAd.OnAdFullScreenContentClosed += () => {
                _isAdShowing = false;
                LoadRewardedAd();
            };
            _rewardedAd.Show((reward) => onRewardEarned?.Invoke());
        }
        else { onAdFailed?.Invoke(); LoadRewardedAd(); }
    }
    #endregion

    #region APP OPEN ADS
    public void LoadAppOpenAd()
    {
        if (_appOpenAd != null) { _appOpenAd.Destroy(); _appOpenAd = null; }
        AppOpenAd.Load(adUnitIdAOA, CreateAdRequest(), (ad, error) => {
            if (error != null || ad == null) return;
            _appOpenAd = ad;
            _aoaExpireTime = DateTime.Now.AddHours(4);
            _appOpenAd.OnAdPaid += (adValue) => SendRevenueToAll("AOA", adValue);
        });
    }

    public void ShowAppOpenAd(bool isResume)
    {
        if (!AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_OPEN_ADS) || _isAdShowing) return;
        if (!isResume && _isFirstTimeTruly && !AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_OPEN_ADS_FIRST))
        {
            PlayerPrefs.SetInt("Truly_First_Open_Completed", 1);
            _isFirstTimeTruly = false;
            return;
        }

        if (_appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _aoaExpireTime)
        {
            _appOpenAd.OnAdFullScreenContentOpened += () => _isAdShowing = true;
            _appOpenAd.OnAdFullScreenContentClosed += () => { _isAdShowing = false; LoadAppOpenAd(); };
            _appOpenAd.Show();
        }
        else LoadAppOpenAd();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_RESUME_ADS)) ShowAppOpenAd(true);
    }
    #endregion

    #region BANNER & MREC
    public void ShowBanner()
    {
        if (!AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_BANNER)) { HideBanner(); return; }
        if (_bannerView != null) _bannerView.Destroy();

        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        _bannerView = new BannerView(adUnitIdBanner, adaptiveSize, AdPosition.Bottom);

        AdRequest request = CreateAdRequest();
        float reloadTime = AdEventTracker.GetFloat(AdEventTracker.KEY_TIME_RELOAD_COLLAP, 10f);

        if (reloadTime > 0)
        {
            request.Extras.Add("collapsible", "bottom");
            if (_bannerReloadCoroutine != null) StopCoroutine(_bannerReloadCoroutine);
            _bannerReloadCoroutine = StartCoroutine(ReloadBannerAfterTime(reloadTime));
        }

        _bannerView.OnAdPaid += (adValue) => SendRevenueToAll("BANNER", adValue);
        _bannerView.LoadAd(request);
    }

    public void HideBanner()
    {
        if (_bannerReloadCoroutine != null) StopCoroutine(_bannerReloadCoroutine);
        if (_bannerView != null) _bannerView.Destroy();
    }

    private IEnumerator ReloadBannerAfterTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_BANNER)) ShowBanner();
    }

    public void ShowMREC(string configKey)
    {
        if (!AdEventTracker.GetBool(configKey)) { HideMREC(); return; }
        if (_mrecView != null) _mrecView.Destroy();

        _mrecView = new BannerView(adUnitIdMREC, AdSize.MediumRectangle, AdPosition.Bottom);
        _mrecView.OnAdPaid += (adValue) => SendRevenueToAll("MREC", adValue);
        _mrecView.LoadAd(CreateAdRequest());
    }

    public void HideMREC() { if (_mrecView != null) _mrecView.Destroy(); }
    #endregion

    #region RATING
    public void CheckShowRating()
    {
        if (AdEventTracker.GetBool(AdEventTracker.KEY_RATING_POPUP)) StartCoroutine(RequestReviewProcedure());
    }

    private IEnumerator RequestReviewProcedure()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var reviewManager = new ReviewManager();
        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        if (requestFlowOperation.Error != Google.Play.Review.ReviewErrorCode.NoError) yield break;
        var reviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = reviewManager.LaunchReviewFlow(reviewInfo);
        yield return launchFlowOperation;
#else
        yield return null;
#endif
    }
    #endregion
}