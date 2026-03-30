using GoogleMobileAds.Api;
using UnityEngine;
using System;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    [Header("CÀI ĐẶT CHUNG")]
    public bool isTestMode = true;
    public float timeBetweenInterAds = 120f;
    public float afkThreshold = 60f; // Treo máy 60s sẽ hiện ads

    private float _lastInterTime;
    private float _afkTimer;

    // --- ID QUẢNG CÁO THẬT (ANDROID) ---
    private string _appOpenIdReal = "ca-app-pub-1765309369619783/4381064565";
    private string _rewardedIdReal = "ca-app-pub-1765309369619783/7693148258";
    private string _interstitialIdReal = "ca-app-pub-1765309369619783/8767032135";

    // --- ID QUẢNG CÁO TEST (GOOGLE MẪU) ---
    private string _appOpenIdTest = "ca-app-pub-3940256099942544/9257395923";
    private string _rewardedIdTest = "ca-app-pub-3940256099942544/5224354917";
    private string _interstitialIdTest = "ca-app-pub-3940256099942544/1033173712";

    private AppOpenAd _appOpenAd;
    private RewardedAd _rewardedAd;
    private InterstitialAd _interstitialAd;
    private bool _isRemovedAds = false;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        // Kiểm tra xem người dùng đã mua xóa Ads chưa khi bắt đầu game
        _isRemovedAds = PlayerPrefs.GetInt("RemoveAds", 0) == 1;

        MobileAds.Initialize(s => {
            if (_isRemovedAds) return; // Nếu đã chặn thì không load ads nữa
            LoadAppOpenAd();
            LoadRewardedAd();
            LoadInterstitialAd();
        });
    }

    void Update()
    {
        // LOGIC TREO MÁY
        if (Input.anyKey || (Input.touchCount > 0))
        {
            _afkTimer = 0;
        }
        else
        {
            _afkTimer += Time.deltaTime;
        }

        if (_afkTimer >= afkThreshold)
        {
            _afkTimer = 0;
            Debug.Log("Người chơi treo máy, tự động hiện quảng cáo!");
            ShowInterstitialImmediate();
        }
    }

    private string GetAppOpenId() => isTestMode ? _appOpenIdTest : _appOpenIdReal;
    private string GetRewardedId() => isTestMode ? _rewardedIdTest : _rewardedIdReal;
    private string GetInterId() => isTestMode ? _interstitialIdTest : _interstitialIdReal;

    // 1. APP OPEN (Mở app hiện quảng cáo)
    public void LoadAppOpenAd()
    {
        AppOpenAd.Load(GetAppOpenId(), new AdRequest(), (ad, error) => {
            if (error == null) { _appOpenAd = ad; ShowAppOpenAd(); }
        });
    }
    public void ShowAppOpenAd() { if (_appOpenAd != null) _appOpenAd.Show(); }

    // 2. INTERSTITIAL (Quảng cáo giữa hiệp)
    public void LoadInterstitialAd()
    {
        InterstitialAd.Load(GetInterId(), new AdRequest(), (ad, error) => {
            if (error == null) _interstitialAd = ad;
        });
    }

    public void ShowInterstitialAuto()
    {
        if (_isRemovedAds) return;
        if (Time.time - _lastInterTime >= timeBetweenInterAds)
        {
            ShowInterstitialImmediate();
        }
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
        else { LoadInterstitialAd(); }
    }

    // 3. REWARDED (Xem quảng cáo để mở khóa Mode)
    public void LoadRewardedAd()
    {
        RewardedAd.Load(GetRewardedId(), new AdRequest(), (ad, error) => {
            if (error == null) _rewardedAd = ad;
        });
    }

    public void ShowRewardedAd(Action onSuccess)
    {
        if (_isRemovedAds) return;
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((reward) => {
                onSuccess?.Invoke();
                LoadRewardedAd(); // Load cái mới sau khi xem xong
            });
        }
        else
        {
            Debug.Log("Quảng cáo thưởng chưa sẵn sàng!");
            LoadRewardedAd();
        }
    }
}