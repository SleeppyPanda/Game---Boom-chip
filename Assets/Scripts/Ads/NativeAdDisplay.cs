using UnityEngine;
using GoogleMobileAds.Api;

public class NativeAdDisplay : MonoBehaviour
{
    [Header("CẤU HÌNH VỊ TRÍ")]
    public AdPosition position = AdPosition.Bottom; // Bạn có thể chỉnh trong Inspector

    // Hàm này tự động chạy khi WinPanel được SetActive(true)
    private void OnEnable()
    {
        if (AdsManager.Instance != null)
        {
            // Load và hiển thị ngay lập tức tại vị trí đã chọn
            AdsManager.Instance.LoadNativeOverlayAd(position);
            Debug.Log("WinPanel hiện: Đang gọi Native Overlay Ad");
        }
    }

    // Hàm này tự động chạy khi WinPanel được SetActive(false) (nhấn Replay/Next)
    private void OnDisable()
    {
        HideNativeAd();
    }

    public void HideNativeAd()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideNativeOverlay();
            Debug.Log("WinPanel đóng: Đã ẩn Native Overlay Ad");
        }
    }
}