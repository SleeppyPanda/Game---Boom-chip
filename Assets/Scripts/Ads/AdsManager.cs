using GoogleMobileAds.Api;
using UnityEngine;
using System;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("CÀI ĐẶT CHUNG")]
    public bool isTestMode = true;
    public float timeBetweenInterAds = 120f;
    public float afkThreshold = 60f;

    private float _lastInterTime;
    private float _afkTimer;
    private bool _isRemovedAds = false;

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
    private NativeOverlayAd _nativeOverlayAd; // Khai báo đúng theo thư viện

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        // Kiểm tra trạng thái mua "Remove Ads"
        _isRemovedAds = PlayerPrefs.GetInt("RemoveAds", 0) == 1;

        MobileAds.Initialize(s => {
            if (_isRemovedAds) return;
            LoadAppOpenAd();
            LoadRewardedAd();
            LoadInterstitialAd();
            LoadNativeOverlayAd();
        });
        _lastInterTime = -timeBetweenInterAds;
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

        if (_nativeOverlayAd != null) _nativeOverlayAd.Destroy(); // Dùng hàm Destroy() có sẵn

        // Dùng đúng class NativeAdOptions theo hình ảnh thư viện
        NativeAdOptions options = new NativeAdOptions();

        NativeOverlayAd.Load(GetNativeId(), new AdRequest(), options, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("NativeOverlay load lỗi: " + error.GetMessage());
                return;
            }
            _nativeOverlayAd = ad;

            // Đặt vị trí quảng cáo
            _nativeOverlayAd.SetTemplatePosition(pos);
            _nativeOverlayAd.Show(); // Hiện quảng cáo
        });
    }

    public void HideNativeOverlay() { if (_nativeOverlayAd != null) _nativeOverlayAd.Hide(); } //
    public void ShowNativeOverlay() { if (!_isRemovedAds && _nativeOverlayAd != null) _nativeOverlayAd.Show(); }

    // --- 2. REWARDED ADS (Vẫn load để lấy thưởng mở Mode) ---
    public void LoadRewardedAd()
    {
        RewardedAd.Load(GetRewardedId(), new AdRequest(), (ad, error) => { if (error == null) _rewardedAd = ad; });
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
        InterstitialAd.Load(GetInterId(), new AdRequest(), (ad, error) => { if (error == null) _interstitialAd = ad; });
    }

    public void ShowInterstitialImmediate()
    {
        if (_isRemovedAds) return;
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            _interstitialAd.Show();
            _lastInterTime = Time.time;
            LoadInterstitialAd();
        }
    }

    // --- 4. APP OPEN ADS ---
    public void LoadAppOpenAd()
    {
        if (_isRemovedAds) return;
        AppOpenAd.Load(GetAppOpenId(), new AdRequest(), (ad, error) => {
            if (error == null) { _appOpenAd = ad; ShowAppOpenAd(); }
        });
    }

    public void ShowAppOpenAd()
    {
        if (!_isRemovedAds && _appOpenAd != null) _appOpenAd.Show();
    }
}