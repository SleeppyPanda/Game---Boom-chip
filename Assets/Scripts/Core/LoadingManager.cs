using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using AppsFlyerSDK;

// Chỉ sử dụng các thư viện Firebase khi KHÔNG ở trong Editor để tránh lỗi DllNotFoundException
#if !UNITY_EDITOR
using Firebase;
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
    private bool _hasShownMREC = false;

    void Start()
    {
        // Khởi tạo trạng thái và bắt đầu quá trình
        InitializeFirebase();
        StartCoroutine(LoadAsynchronously());
    }

    private void InitializeFirebase()
    {
        // TRƯỜNG HỢP 1: Chạy trong Unity Editor
#if UNITY_EDITOR
        Debug.LogWarning("<color=yellow>[LoadingManager]</color> Editor Mode Detected: Bypassing Firebase initialization to prevent DLL errors.");
        // Giả lập Firebase đã sẵn sàng để không làm kẹt thanh Loading
        _isFirebaseReady = true; 
#else
        // TRƯỜNG HỢP 2: Chạy trên thiết bị thật (Android/iOS)
        try
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                DependencyStatus dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    _isFirebaseReady = true;
                    Debug.Log("<color=green>Firebase Initialized Successfully on Device.</color>");
                }
                else
                {
                    Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
                    // Firebase dependency fail → hiện popup retry
                    if (NetworkErrorUI.Instance != null)
                        NetworkErrorUI.Instance.Show(() => InitializeFirebase());
                    else
                        _isFirebaseReady = true; // Fallback nếu popup chưa sẵn sàng
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError("Critical Firebase Error: " + e.Message);
            // Critical error → hiện popup retry
            if (NetworkErrorUI.Instance != null)
                NetworkErrorUI.Instance.Show(() => InitializeFirebase());
            else
                _isFirebaseReady = true; // Fallback nếu popup chưa sẵn sàng
        }
#endif
    }

    IEnumerator LoadAsynchronously()
    {
        // Bắt đầu nạp Scene mới ở chế độ nền
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        float fakeProgress = 0f;

        while (!operation.isDone)
        {
            // Tính toán tiến trình thực (0.9 là mức tối đa trước khi cho phép kích hoạt scene)
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Di chuyển thanh tiến trình ảo để tạo hiệu ứng mượt mà
            fakeProgress = Mathf.MoveTowards(fakeProgress, realProgress, Time.deltaTime * loadSpeedMultiplier);

            if (fillImage != null)
                fillImage.fillAmount = fakeProgress;

            // ĐIỀU KIỆN CHUYỂN CẢNH: 
            // 1. Scene đã load xong (0.9f)
            // 2. Firebase đã xử lý xong (hoặc đã bypass)
            // 3. Thanh tiến trình ảo đã chạy gần hết
            if (operation.progress >= 0.9f && _isFirebaseReady && fakeProgress >= 0.95f)
            {
                if (!_hasShownMREC)
                {
                    _hasShownMREC = true;

                    // Ghi nhận sự kiện load thành công lần đầu
                    HandleFirstLoadingEvent();

                    // Hiển thị quảng cáo MREC nếu có AdsManager
                    if (AdsManager.Instance != null)
                    {
                        AdsManager.Instance.ShowMREC("is_show_mrec_loading_game");
                    }

                    // Chờ một khoảng ngắn để quảng cáo kịp render hoặc user kịp nhìn
                    yield return new WaitForSeconds(0.8f);
                }

                // Cuối cùng, cho phép chuyển sang Scene chính
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void HandleFirstLoadingEvent()
    {
        if (PlayerPrefs.GetInt(FIRST_TIME_KEY, 0) == 0)
        {
            // 1. Bắn sự kiện lên Firebase (Chỉ thực hiện trên máy thật)
#if !UNITY_EDITOR
            if (FirebaseManager.Instance != null)
            {
                FirebaseManager.Instance.LogModeEnter(0);
            }
#endif

            // 2. Bắn sự kiện lên AppsFlyer (AppsFlyer thường chạy ổn định trong Editor)
            Dictionary<string, string> afParams = new Dictionary<string, string>();
            afParams.Add("description", "User finished first loading screen");
            afParams.Add("version", Application.version);

            AppsFlyer.sendEvent("first_loading_complete", afParams);
            Debug.Log("<color=orange>[AppsFlyer]</color> Event 'first_loading_complete' sent.");

            // Lưu trạng thái đã hoàn thành load lần đầu
            PlayerPrefs.SetInt(FIRST_TIME_KEY, 1);
            PlayerPrefs.Save();
        }
    }
}