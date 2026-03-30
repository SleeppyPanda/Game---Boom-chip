using UnityEngine;

public class AutoAdCaller : MonoBehaviour
{
    void Start()
    {
        // Gọi hàm hiện quảng cáo ngay lập tức không cần check thời gian
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowInterstitialImmediate();
        }
    }
}