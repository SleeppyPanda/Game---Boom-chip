using System.Collections.Generic;
using UnityEngine;
using AppsFlyerSDK;

public class AppsflyerManager : MonoBehaviour, IAppsFlyerConversionData
{
    public static AppsflyerManager Instance;

    [Header("AppsFlyer Configuration")]
    public string devKey = "hC5LWLvLcmVr4SAqj97qwR";
    public string appId = "com.codegym.Boomchip2player";

    void Awake()
    {
        // Khởi tạo Singleton để đảm bảo chỉ có 1 AppsflyerManager duy nhất
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

    void Start()
    {
        // 1. Cấu hình SDK
        // Chế độ deep link và conversion data
        AppsFlyer.initSDK(devKey, appId, this);

        // 2. Bật debug mode
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AppsFlyer.setIsDebug(true);
#endif

        // 3. Bắt đầu tracking
        AppsFlyer.startSDK();

        Debug.Log("<color=cyan>[AppsFlyer]</color> SDK Started.");
    }

    // Tối ưu: Đảm bảo tracking session chính xác khi người chơi quay lại app
    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            // App quay lại từ background (Resume)
            AppsFlyer.startSDK();
            Debug.Log("<color=cyan>[AppsFlyer]</color> SDK Resumed.");
        }
    }

    /// <summary>
    /// Hàm helper để FirebaseManager hoặc các script khác gọi gửi event sang AppsFlyer
    /// </summary>
    public void SendCustomEvent(string eventName, Dictionary<string, string> eventValues = null)
    {
        // AppsFlyer yêu cầu Dictionary<string, string> cho event values
        AppsFlyer.sendEvent(eventName, eventValues);
        Debug.Log($"<color=cyan>[AppsFlyer Event]</color>: {eventName}");
    }

    // --- CÁC HÀM CALLBACK BẮT BUỘC ---
    public void onConversionDataSuccess(string conversionInfo)
    {
        // Chuyển string thành Dictionary nếu bạn muốn đọc dữ liệu nguồn (Organic/Non-Organic)
        Dictionary<string, object> conversionData = AppsFlyer.CallbackStringToDictionary(conversionInfo);
        Debug.Log("AppsFlyer: Conversion Data Success");
    }

    public void onConversionDataFail(string error)
    {
        Debug.LogError("AppsFlyer: Conversion Data Fail: " + error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
        Debug.Log("AppsFlyer: onAppOpenAttribution: " + attributionData);
    }

    public void onAppOpenAttributionFailure(string error)
    {
        Debug.LogError("AppsFlyer: onAppOpenAttributionFailure: " + error);
    }
}