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

        // 3. (Tùy chọn) Uninstall Tracking cho Android (nếu bạn dùng Firebase Messaging)
        // AppsFlyer.updateServerUninstallToken("YOUR_GCM_TOKEN");

        // 4. Bắt đầu tracking
        AppsFlyer.startSDK();

        Debug.Log("<color=cyan>[AppsFlyer]</color> SDK Started.");
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