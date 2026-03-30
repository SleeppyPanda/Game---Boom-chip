using Firebase.Extensions;
using Firebase.RemoteConfig;
using GoogleMobileAds.Api;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("CÀI ĐẶT CHUNG (Mặc định)")]
    public bool isTestMode = true;
    public float timeBetweenInterAds = 120f;
    public float afkThreshold = 60f;
    public bool showAppOpenOnStart = true;

    private float _lastInterTime;
    private float _afkTimer;
    private bool _isRemovedAds = false;
    private bool _isFirebaseInitialized = false;

    // --- ID QUẢNG CÁO THẬT (ANDROID) ---
    private string _appOpenIdReal = "ca-app-pub-1765309369619783/4381064565";
    private string _rewardedIdReal = "ca-app-pub-1765309369619783/7693148258";
    private string _interstitialIdReal = "ca-app-pub-1765309369619783/8767032135";
    private string _nativeIdReal = "ca-app-pub-1765309369619783/4720810068";

    // --- ID QUẢNG CÁO TEST ---
    private string _appOpenIdTest = "ca-app-pub-3940256099942544/9257395923";
    private string _rewardedIdTest = "ca-app-pub-3940256099942544/5224354917";
    private string _interstitialIdTest = "ca-app-pub-3940256099942544/1033173712";
    private string _nativeIdTest = "ca-app-pub-3940256099942544/2247696110";

    private AppOpenAd _appOpenAd;
    private RewardedAd _rewardedAd;
    private InterstitialAd _interstitialAd;
    private NativeOverlayAd _nativeOverlayAd;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        _isRemovedAds = PlayerPrefs.GetInt("RemoveAds", 0) == 1;
        _lastInterTime = -timeBetweenInterAds;

        // Bắt đầu khởi tạo Firebase trước khi load Ads
        InitializeFirebase();
    }

    // --- FIREBASE REMOTE CONFIG LOGIC ---
    private void InitializeFirebase()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                _isFirebaseInitialized = true;
                FetchRemoteConfig();
            }
            else
            {
                Debug.LogError($"Firebase Error: {dependencyStatus}");
                InitializeAds(); // Nếu lỗi vẫn khởi tạo Ads với cấu hình mặc định
            }
        });
    }

    private void FetchRemoteConfig()
    {
        // Thiết lập giá trị mặc định cho Remote Config
        var defaults = new System.Collections.Generic.Dictionary<string, object> {
            { "is_test_mode", isTestMode },
            { "inter_interval", timeBetweenInterAds },
            { "afk_threshold", afkThreshold },
            { "show_app_open_start", showAppOpenOnStart }
        };

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task => {
            return FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync();
        }).Unwrap().ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                ApplyRemoteConfig();
            }
            InitializeAds();
        });
    }

    private void ApplyRemoteConfig()
    {
        isTestMode = FirebaseRemoteConfig.DefaultInstance.GetValue("is_test_mode").BooleanValue;
        timeBetweenInterAds = (float)FirebaseRemoteConfig.DefaultInstance.GetValue("inter_interval").DoubleValue;
        afkThreshold = (float)FirebaseRemoteConfig.DefaultInstance.GetValue("afk_threshold").DoubleValue;
        showAppOpenOnStart = FirebaseRemoteConfig.DefaultInstance.GetValue("show_app_open_start").BooleanValue;
        Debug.Log("Remote Config đã được áp dụng!");
    }

    private void InitializeAds()
    {
        MobileAds.Initialize(s => {
            if (_isRemovedAds) return;

            LoadInterstitialAd();
            LoadRewardedAd();
            LoadNativeOverlayAd();

            if (showAppOpenOnStart)
            {
                LoadAppOpenAd();
            }
        });
    }

    void Update()
    {
        if (_isRemovedAds) return;

        // LOGIC TREO MÁY HIỆN ADS
        if (Input.anyKey || (Input.touchCount > 0)) _afkTimer = 0;
        else _afkTimer += Time.deltaTime;

        if (_afkTimer >= afkThreshold)
        {
            _afkTimer = 0;
            ShowInterstitialImmediate();
        }
    }

    private string GetNativeId() => isTestMode ? _nativeIdTest : _nativeIdReal;
    private string GetAppOpenId() => isTestMode ? _appOpenIdTest : _appOpenIdReal;
    private string GetRewardedId() => isTestMode ? _rewardedIdTest : _rewardedIdReal;
    private string GetInterId() => isTestMode ? _interstitialIdTest : _interstitialIdReal;

    // --- 1. NATIVE OVERLAY ADS ---
    public void LoadNativeOverlayAd(AdPosition pos = AdPosition.Bottom)
    {
        if (_isRemovedAds) return;
        if (_nativeOverlayAd != null) _nativeOverlayAd.Destroy();

        NativeAdOptions options = new NativeAdOptions();

        NativeOverlayAd.Load(GetNativeId(), new AdRequest(), options, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("NativeOverlay load lỗi: " + error.GetMessage());
                return;
            }
            _nativeOverlayAd = ad;
            _nativeOverlayAd.SetTemplatePosition(pos);
            _nativeOverlayAd.Show();
        });
    }

    public void HideNativeOverlay() { if (_nativeOverlayAd != null) _nativeOverlayAd.Hide(); }
    public void ShowNativeOverlay() { if (!_isRemovedAds && _nativeOverlayAd != null) _nativeOverlayAd.Show(); }

    // --- 2. REWARDED ADS ---
    public void LoadRewardedAd()
    {
        RewardedAd.Load(GetRewardedId(), new AdRequest(), (ad, error) => {
            if (error == null) _rewardedAd = ad;
        });
    }

    public void ShowRewardedAd(Action onSuccess)
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((reward) => {
                onSuccess?.Invoke();
                LoadRewardedAd();
            });
        }
        else LoadRewardedAd();
    }

    // --- 3. INTERSTITIAL ADS ---
    public void LoadInterstitialAd()
    {
        if (_isRemovedAds) return;
        InterstitialAd.Load(GetInterId(), new AdRequest(), (ad, error) => {
            if (error == null) _interstitialAd = ad;
        });
    }

    public void ShowInterstitialImmediate()
    {
        if (_isRemovedAds) return;
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            if (Time.time - _lastInterTime >= timeBetweenInterAds)
            {
                _interstitialAd.Show();
                _lastInterTime = Time.time;
                LoadInterstitialAd();
            }
        }
        else LoadInterstitialAd();
    }

    // --- 4. APP OPEN ADS ---
    public void LoadAppOpenAd()
    {
        if (_isRemovedAds) return;
        AppOpenAd.Load(GetAppOpenId(), new AdRequest(), (ad, error) => {
            if (error == null)
            {
                _appOpenAd = ad;
                _appOpenAd.Show();
            }
        });
    }
}