using System.Collections.Generic;
using UnityEngine;
using AppsFlyerSDK;

public class AppsflyerManager : MonoBehaviour, IAppsFlyerConversionData
{
    public string devKey = "hC5LWLvLcmVr4SAqj97qwR"; // Lấy từ dashboard Appsflyer
    public string appId = "com.codegym.Boomchip2player"; // Ví dụ: com.yourgame.name

    void Start()
    {
        // 1. Cấu hình SDK
        AppsFlyer.initSDK(devKey, appId, this);

        // 2. Bật debug mode để xem log trong Android Logcat
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AppsFlyer.setIsDebug(true);
#endif

        // 3. Bắt đầu tracking
        AppsFlyer.startSDK();
    }

    // --- CÁC HÀM CALLBACK BẮT BUỘC CỦA INTERFACE ---
    public void onConversionDataSuccess(string conversionInfo)
    {
        Debug.Log("AppsFlyer: Conversion Data Success");
    }
    public void onConversionDataFail(string error)
    {
        Debug.Log("AppsFlyer: Conversion Data Fail: " + error);
    }
    public void onAppOpenAttribution(string attributionData) { }
    public void onAppOpenAttributionFailure(string error) { }
}