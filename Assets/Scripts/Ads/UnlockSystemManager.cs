using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;

public class UnlockSystemManager : MonoBehaviour
{
    public static UnlockSystemManager Instance;

    [Header("Giao diện Popup (Cần gắn CanvasGroup)")]
    public CanvasGroup simpleAdPanel;
    public RectTransform popupContent;
    public Button watchAdButton;
    public Button closeButton;
    public Image displayAvatar;

    [Header("Cấu hình hiệu ứng")]
    public float fadeDuration = 0.3f;

    [Header("Quản lý Panel")]
    public List<CanvasGroup> panelsToHide;
    public List<CanvasGroup> ignorePanels;

    private Action _onSuccessCallback;
    private string _currentItemKey; // Key dùng để lưu trạng thái mở khóa (có prefix Unlock_)
    private string _currentRawID;   // ID gốc (ví dụ: "avatar_1") để truyền làm rewardType cho AdsManager

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Khởi tạo trạng thái ẩn ban đầu
        if (simpleAdPanel != null)
        {
            simpleAdPanel.alpha = 0;
            simpleAdPanel.blocksRaycasts = false;
            simpleAdPanel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Kiểm tra xem một Item đã được mở khóa vĩnh viễn chưa
    /// </summary>
    public bool IsItemUnlocked(string itemID)
    {
        // PlayerPrefs trả về 1 là đã mở, 0 là chưa mở
        return PlayerPrefs.GetInt("Unlock_" + itemID, 0) == 1;
    }

    /// <summary>
    /// Hàm chính để mở Popup Mở Khóa
    /// </summary>
    /// <param name="itemID">ID duy nhất của item (ví dụ: "avatar_01", "mode_02")</param>
    /// <param name="itemSprite">Hình ảnh hiển thị trên popup</param>
    /// <param name="onSuccess">Hành động thực hiện sau khi mở khóa thành công</param>
    public void OpenUnlockPopup(string itemID, Sprite itemSprite, Action onSuccess)
    {
        _currentRawID = itemID;
        _currentItemKey = "Unlock_" + itemID;
        _onSuccessCallback = onSuccess;

        // 1. Cập nhật hình ảnh hiển thị
        if (displayAvatar != null && itemSprite != null)
        {
            displayAvatar.sprite = itemSprite;
        }

        // 2. Tracking: Ghi nhận user có đủ điều kiện/thấy nút xem Reward
        AdEventTracker.TrackRewardEligible();

        // 3. Ẩn các panel nền phía sau
        ToggleOtherPanels(false);

        // 4. Hiệu ứng Fade In và Scale Out nẩy nhẹ
        simpleAdPanel.gameObject.SetActive(true);
        simpleAdPanel.DOKill();
        simpleAdPanel.DOFade(1, fadeDuration);
        simpleAdPanel.blocksRaycasts = true;

        if (popupContent != null)
        {
            popupContent.DOKill();
            popupContent.localScale = Vector3.one * 0.7f;
            popupContent.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);
        }

        // 5. Gán sự kiện cho nút Xem Quảng Cáo
        watchAdButton.onClick.RemoveAllListeners();
        watchAdButton.onClick.AddListener(() => {

            // Tracking: User nhấn vào nút xem quảng cáo
            AdEventTracker.TrackRewardApiCalled();

            if (AdsManager.Instance != null)
            {
                // SỬA LỖI CS7036: Truyền _currentRawID làm tham số thứ nhất
                AdsManager.Instance.ShowRewardedAd(_currentRawID, () => {
                    ExecuteUnlockSuccess();
                });
            }
            else
            {
                Debug.LogWarning("Không tìm thấy AdsManager! Tự động mở khóa (Test)");
                ExecuteUnlockSuccess();
            }
        });

        // 6. Gán sự kiện cho nút đóng (X)
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    private void ExecuteUnlockSuccess()
    {
        // 1. Lưu trạng thái mở khóa vĩnh viễn vào thiết bị
        if (!string.IsNullOrEmpty(_currentItemKey))
        {
            PlayerPrefs.SetInt(_currentItemKey, 1);
            PlayerPrefs.Save();
        }

        // 2. Tracking: Ghi nhận đã xem xong và trả thưởng thành công
        AdEventTracker.TrackRewardDisplayed();

        // 3. Đóng popup mượt mà và thực hiện logic tiếp theo
        ClosePopup();
        _onSuccessCallback?.Invoke();
    }

    /// <summary>
    /// Đóng Popup mượt mà và hiện lại các Panel cũ
    /// </summary>
    public void ClosePopup()
    {
        if (simpleAdPanel == null) return;

        // Hiện lại các panel cũ
        ToggleOtherPanels(true);

        simpleAdPanel.DOKill();
        simpleAdPanel.DOFade(0, fadeDuration);
        simpleAdPanel.blocksRaycasts = false;

        if (popupContent != null)
        {
            popupContent.DOKill();
            popupContent.DOScale(0.7f, fadeDuration).SetEase(Ease.InBack).OnComplete(() => {
                simpleAdPanel.gameObject.SetActive(false);
            });
        }
    }

    /// <summary>
    /// Hàm điều phối ẩn/hiện các thành phần giao diện khác
    /// </summary>
    private void ToggleOtherPanels(bool show)
    {
        float targetAlpha = show ? 1f : 0f;

        foreach (var panel in panelsToHide)
        {
            if (panel == null || ignorePanels.Contains(panel)) continue;

            panel.DOKill();
            panel.DOFade(targetAlpha, fadeDuration);
            panel.blocksRaycasts = show;

            if (show) panel.gameObject.SetActive(true);
        }
    }
}