using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections.Generic;
using System.Collections;
using AppsFlyerSDK;
using UnityEngine.SceneManagement;

#if UNITY_ANDROID
using Google.Play.Review;
#endif

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("AppsFlyer Settings")]
    public string appsFlyerDevKey = "YOUR_DEV_KEY_HERE";
    public string appId = "com.your.package.name";

    [Header("Ad Unit IDs (gán Real IDs trong Inspector cho release build)")]
    public string adUnitIdBanner = "ca-app-pub-3940256099942544/6300978111";
    public string adUnitIdInter = "ca-app-pub-3940256099942544/1033173712";
    public string adUnitIdRewarded = "ca-app-pub-3940256099942544/5224354917";
    public string adUnitIdAOA = "ca-app-pub-3940256099942544/9257395921";
    public string adUnitIdMREC = "ca-app-pub-3940256099942544/6300978111";

    private const string TEST_BANNER = "ca-app-pub-3940256099942544/6300978111";
    private const string TEST_INTER = "ca-app-pub-3940256099942544/1033173712";
    private const string TEST_REWARDED = "ca-app-pub-3940256099942544/5224354917";
    private const string TEST_AOA = "ca-app-pub-3940256099942544/9257395921";
    private const string TEST_MREC = "ca-app-pub-3940256099942544/6300978111";

    private const int ADMOB_ERROR_NETWORK = 2;

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
    private int _bannerHeightDP = 60;
    private bool _isBannerLoaded = false;
    private bool _isMrecLoaded = false;
    private bool _isAdMobInitialized = false;
    private bool _isRemoteConfigReady = false;
    private bool _hasPreloadedAds = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else { Destroy(gameObject); }

        _isFirstTimeTruly = PlayerPrefs.GetInt("Truly_First_Open_Completed", 0) == 0;

        if (Debug.isDebugBuild || Application.isEditor)
        {
            adUnitIdBanner = TEST_BANNER;
            adUnitIdInter = TEST_INTER;
            adUnitIdRewarded = TEST_REWARDED;
            adUnitIdAOA = TEST_AOA;
            adUnitIdMREC = TEST_MREC;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HideMREC();
        if (_bannerReloadCoroutine != null) StopCoroutine(_bannerReloadCoroutine);
    }

    void Start()
    {
        AppsFlyer.initSDK(appsFlyerDevKey, appId);
        AppsFlyer.startSDK();

        // Khởi tạo AdMob sớm (song song với Firebase/RC)
        // Chỉ preload ads khi CẢ AdMob init + RC đều sẵn sàng
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            Debug.Log("<color=green>[AdsManager] AdMob Initialized</color>");
            _isAdMobInitialized = true;
            TryPreloadAllAds();
        });
    }

    /// <summary>
    /// Chỉ preload khi CẢ AdMob init VÀ Remote Config đều sẵn sàng.
    /// </summary>
    private void TryPreloadAllAds()
    {
        if (!_isAdMobInitialized || !_isRemoteConfigReady || _hasPreloadedAds) return;
        _hasPreloadedAds = true;

        Debug.Log("<color=green>[AdsManager] Both AdMob + RC ready → Preloading all ads</color>");
        LoadInterstitialAd();
        LoadRewardedAd();
        LoadAppOpenAd(() => ShowAppOpenAd(false));
        PreloadBanner(() => ShowBanner());
        PreloadMREC();
    }

    #region NETWORK ERROR POPUP
    private void ShowNetworkError(Action retryAction)
    {
        HideBanner();
        HideMREC();
        if (NetworkErrorUI.Instance != null)
        {
            NetworkErrorUI.Instance.Show(retryAction);
        }
    }

    private bool IsNetworkError(LoadAdError error)
    {
        if (error == null) return false;
        int code = error.GetCode();
        Debug.Log($"<color=orange>[AdsManager] Ad load failed: code={code}, message={error.GetMessage()}</color>");
        // Code 2 = network error, Code 0 = internal error (thường do mất mạng)
        // Code 1 = invalid request, Code 3 = no fill → KHÔNG phải lỗi network
        return code == ADMOB_ERROR_NETWORK || code == 0;
    }
    #endregion

    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }

    #region REMOTE CONFIG CALLBACK
    /// <summary>
    /// Được gọi bởi LoadingManager sau khi Remote Config fetch & activate thành công.
    /// </summary>
    public void OnRemoteConfigReady()
    {
        _isRemoteConfigReady = true;
        AdEventTracker.LogFirstLoadingComplete();
        CheckShowRating();
        TryPreloadAllAds();
    }
    #endregion

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
            if (error != null || ad == null)
            {
                if (IsNetworkError(error)) ShowNetworkError(() => LoadInterstitialAd());
                return;
            }
            _interstitialAd = ad;
            _interstitialAd.OnAdPaid += (adValue) => SendRevenueToAll("INTER", adValue);
        });
    }

    public void ShowInterstitialWithDelay(string placementConfigKey, Action onAdClosed, float delay = 1.0f)
    {
        StartCoroutine(ExecuteDelayShow(placementConfigKey, onAdClosed, delay));
    }

    private IEnumerator ExecuteDelayShow(string key, Action onClosed, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowInterstitial(key, onClosed);
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
                HideBanner();
                HideMREC();

                AdEventTracker.TrackInterApiCalled();
                _interstitialAd.OnAdFullScreenContentOpened += () => {
                    _isAdShowing = true;
                    _lastTimeShowInterstitial = Time.time;
                    AdEventTracker.TrackInterDisplayed();
                };

                _interstitialAd.OnAdFullScreenContentClosed += () => {
                    StartCoroutine(HandleAdClosedMainThread(onAdClosed));
                };

                _interstitialAd.Show();
            }
            else { onAdClosed?.Invoke(); LoadInterstitialAd(); }
        }
        else onAdClosed?.Invoke();
    }

    private IEnumerator HandleAdClosedMainThread(Action onAdClosed)
    {
        _isAdShowing = false;
        LoadInterstitialAd();
        yield return null;
        onAdClosed?.Invoke();
        ShowBanner();
    }
    #endregion

    #region REWARDED
    public void LoadRewardedAd()
    {
        if (_rewardedAd != null) { _rewardedAd.Destroy(); _rewardedAd = null; }

        RewardedAd.Load(adUnitIdRewarded, CreateAdRequest(), (ad, error) => {
            if (error != null || ad == null)
            {
                if (IsNetworkError(error)) ShowNetworkError(() => LoadRewardedAd());
                return;
            }
            _rewardedAd = ad;
            _rewardedAd.OnAdPaid += (adValue) => SendRevenueToAll("REWARDED", adValue);
        });
    }

    public void ShowRewardedAd(string logicKey, Action onRewardEarned, Action onAdFailed = null)
    {
        bool shouldShowAd = (logicKey == AdEventTracker.KEY_RW_PROFILE)
            ? !string.IsNullOrEmpty(AdEventTracker.GetString(logicKey))
            : AdEventTracker.GetBool(logicKey);

        if (!shouldShowAd) { onRewardEarned?.Invoke(); return; }

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

    #region APP OPEN ADS (AOA)
    public void LoadAppOpenAd(Action onLoaded = null)
    {
        if (_appOpenAd != null) { _appOpenAd.Destroy(); _appOpenAd = null; }
        AppOpenAd.Load(adUnitIdAOA, CreateAdRequest(), (ad, error) => {
            if (error != null || ad == null)
            {
                if (IsNetworkError(error)) ShowNetworkError(() => LoadAppOpenAd(onLoaded));
                return;
            }
            _appOpenAd = ad;
            _aoaExpireTime = DateTime.Now.AddHours(4);
            _appOpenAd.OnAdPaid += (adValue) => SendRevenueToAll("AOA", adValue);
            onLoaded?.Invoke();
        });
    }

    public void ShowAppOpenAd(bool isResume)
    {
        if (_isAdShowing) return;

        if (isResume)
        {
            if (!AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_RESUME_ADS)) return;
        }
        else
        {
            if (!AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_OPEN_ADS)) return;

            if (_isFirstTimeTruly)
            {
                if (!AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_OPEN_ADS_FIRST))
                {
                    PlayerPrefs.SetInt("Truly_First_Open_Completed", 1);
                    PlayerPrefs.Save();
                    _isFirstTimeTruly = false;
                    return;
                }
            }
        }

        if (_appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _aoaExpireTime)
        {
            _appOpenAd.OnAdFullScreenContentOpened += () => {
                _isAdShowing = true;
                HideBanner();
                HideMREC();
            };

            _appOpenAd.OnAdFullScreenContentClosed += () => {
                _isAdShowing = false;
                LoadAppOpenAd();
                ShowBanner();
            };

            _appOpenAd.Show();
        }
        else
        {
            LoadAppOpenAd();
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus) ShowAppOpenAd(true);
    }
    #endregion

    #region BANNER & MREC

    private void PreloadBanner(Action onLoaded = null)
    {
        if (_bannerView != null) { _bannerView.Destroy(); _bannerView = null; }
        _isBannerLoaded = false;

        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        _bannerHeightDP = adaptiveSize.Height > 0 ? adaptiveSize.Height : 60;
        _bannerView = new BannerView(adUnitIdBanner, adaptiveSize, AdPosition.Bottom);

        _bannerView.OnAdPaid += (adValue) => SendRevenueToAll("BANNER", adValue);
        _bannerView.OnBannerAdLoaded += () => {
            _isBannerLoaded = true;
            onLoaded?.Invoke();
        };
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) => {
            _isBannerLoaded = false;
            if (IsNetworkError(error)) ShowNetworkError(() => PreloadBanner(onLoaded));
        };
        _bannerView.LoadAd(CreateAdRequest());
        _bannerView.Hide();
    }

    private void PreloadMREC()
    {
        if (_mrecView != null) { _mrecView.Destroy(); _mrecView = null; }
        _isMrecLoaded = false;

        float density = Screen.dpi / 160f;
        if (density <= 0) density = 1;
        float screenWidthDP = Screen.width / density;
        float screenHeightDP = Screen.height / density;
        int xPos = (int)((screenWidthDP - 300) / 2);
        int mrecHeight = 250;
        int yPos = (int)(screenHeightDP - mrecHeight - _bannerHeightDP);

        _mrecView = new BannerView(adUnitIdMREC, AdSize.MediumRectangle, xPos, yPos);

        _mrecView.OnAdPaid += (adValue) => SendRevenueToAll("MREC", adValue);
        _mrecView.OnBannerAdLoaded += () => { _isMrecLoaded = true; };
        _mrecView.OnBannerAdLoadFailed += (LoadAdError error) => {
            _isMrecLoaded = false;
            if (IsNetworkError(error)) ShowNetworkError(() => PreloadMREC());
        };
        _mrecView.LoadAd(CreateAdRequest());
        _mrecView.Hide();
    }

    public void ShowBanner()
    {
        if (!AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_BANNER)) { HideBanner(); return; }

        if (_isBannerLoaded && _bannerView != null)
        {
            _bannerView.Show();

            float reloadTime = AdEventTracker.GetFloat(AdEventTracker.KEY_TIME_RELOAD_COLLAP, 10f);
            if (reloadTime > 0)
            {
                if (_bannerReloadCoroutine != null) StopCoroutine(_bannerReloadCoroutine);
                _bannerReloadCoroutine = StartCoroutine(ReloadBannerAfterTime(reloadTime));
            }
            return;
        }

        // Chưa có preload → load mới (kèm collapsible)
        if (_bannerView != null) { _bannerView.Destroy(); _bannerView = null; }
        _isBannerLoaded = false;

        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        _bannerHeightDP = adaptiveSize.Height > 0 ? adaptiveSize.Height : 60;
        _bannerView = new BannerView(adUnitIdBanner, adaptiveSize, AdPosition.Bottom);

        AdRequest request = CreateAdRequest();
        float rt = AdEventTracker.GetFloat(AdEventTracker.KEY_TIME_RELOAD_COLLAP, 10f);

        if (rt > 0)
        {
            request.Extras.Add("collapsible", "bottom");
            if (_bannerReloadCoroutine != null) StopCoroutine(_bannerReloadCoroutine);
            _bannerReloadCoroutine = StartCoroutine(ReloadBannerAfterTime(rt));
        }

        _bannerView.OnAdPaid += (adValue) => SendRevenueToAll("BANNER", adValue);
        _bannerView.OnBannerAdLoaded += () => { _isBannerLoaded = true; };
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) => {
            _isBannerLoaded = false;
            if (IsNetworkError(error)) ShowNetworkError(() => ShowBanner());
        };
        _bannerView.LoadAd(request);
    }

    public void HideBanner()
    {
        if (_bannerReloadCoroutine != null) StopCoroutine(_bannerReloadCoroutine);
        if (_bannerView != null) _bannerView.Hide();
    }

    private IEnumerator ReloadBannerAfterTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (AdEventTracker.GetBool(AdEventTracker.KEY_SHOW_BANNER) && !_isAdShowing)
        {
            _isBannerLoaded = false;
            if (_bannerView != null) { _bannerView.Destroy(); _bannerView = null; }
            ShowBanner();
        }
    }

    public void ShowMREC(string configKey)
    {
        if (!AdEventTracker.GetBool(configKey)) { HideMREC(); return; }

        ShowBanner();

        if (_isMrecLoaded && _mrecView != null)
        {
            _mrecView.Show();
            return;
        }

        // Chưa có preload → load mới
        if (_mrecView != null) { _mrecView.Destroy(); _mrecView = null; }
        _isMrecLoaded = false;

        float density = Screen.dpi / 160f;
        if (density <= 0) density = 1;

        float screenWidthDP = Screen.width / density;
        float screenHeightDP = Screen.height / density;

        int xPos = (int)((screenWidthDP - 300) / 2);
        int mrecHeight = 250;
        int yPos = (int)(screenHeightDP - mrecHeight - _bannerHeightDP);

        _mrecView = new BannerView(adUnitIdMREC, AdSize.MediumRectangle, xPos, yPos);

        _mrecView.OnAdPaid += (adValue) => SendRevenueToAll("MREC", adValue);
        _mrecView.OnBannerAdLoaded += () => { _isMrecLoaded = true; };
        _mrecView.OnBannerAdLoadFailed += (LoadAdError error) => {
            _isMrecLoaded = false;
            if (IsNetworkError(error)) ShowNetworkError(() => ShowMREC(configKey));
        };
        _mrecView.LoadAd(CreateAdRequest());
    }

    public void HideMREC()
    {
        if (_mrecView != null) _mrecView.Hide();
    }
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
