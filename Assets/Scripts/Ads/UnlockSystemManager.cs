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

    [Header("Quản lý Panel Phụ")]
    public List<CanvasGroup> panelsToHide;
    public List<CanvasGroup> ignorePanels;

    private Action _onSuccessCallback;
    private string _currentRawID; // Ví dụ: "Mode2", "Avatar_5"

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (simpleAdPanel != null)
        {
            simpleAdPanel.DOKill();
            simpleAdPanel.alpha = 0;
            simpleAdPanel.interactable = false;
            simpleAdPanel.blocksRaycasts = false;
            simpleAdPanel.gameObject.SetActive(false);
        }
    }

    #region HÀM CẦU NỐI (BRIDGE METHODS)

    /// <summary>
    /// Hàm chính xử lý luồng mở khóa.
    /// </summary>
    public void HandleUnlockFlow(string itemID, string logicConfigKey, Sprite itemSprite, Action onSuccess)
    {
        // 1. Nếu đã mở khóa thì thực hiện hành động ngay (ví dụ: Chuyển scene)
        if (IsItemUnlocked(itemID))
        {
            onSuccess?.Invoke();
            return;
        }

        // 2. Nếu chưa, hiện Popup bắt xem quảng cáo
        OpenUnlockPopup(itemID, itemSprite, onSuccess, logicConfigKey);
    }

    // Các hàm overload hỗ trợ gọi nhanh
    public void HandleUnlockFlow(string itemID, Action onSuccess)
        => HandleUnlockFlow(itemID, "", null, onSuccess);

    #endregion

    /// <summary>
    /// Kiểm tra trạng thái mở khóa chuẩn xác nhất
    /// </summary>
    public bool IsItemUnlocked(string itemID)
    {
        string key = GetUnlockKey(itemID);
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    /// <summary>
    /// Logic tạo Key lưu trữ: Mode -> ModeX_Unlocked | Avatar -> Unlock_Avatar_X
    /// </summary>
    private string GetUnlockKey(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return "Unknown_Item";

        // Đồng bộ với MenuManager: "Mode2_Unlocked"
        if (itemID.Contains("Mode"))
        {
            return itemID.Contains("_Unlocked") ? itemID : itemID + "_Unlocked";
        }

        // Đồng bộ với UnlockButton: "Unlock_Avatar_5"
        return itemID.StartsWith("Unlock_") ? itemID : "Unlock_" + itemID;
    }

    public void OpenUnlockPopup(string itemID, Sprite itemSprite, Action onSuccess, string adLogicKey = "")
    {
        if (simpleAdPanel == null)
        {
            onSuccess?.Invoke();
            return;
        }

        _currentRawID = itemID;
        _onSuccessCallback = onSuccess;

        // Hiển thị hình ảnh vật phẩm định mở khóa
        if (displayAvatar != null && itemSprite != null)
        {
            displayAvatar.sprite = itemSprite;
        }

        // Tracking Analytics
        // AdEventTracker.TrackRewardEligible();

        // 1. Ẩn các Panel nền để tập trung vào Popup
        ToggleOtherPanels(false);

        // 2. Hiệu ứng Fade In cho toàn bộ Panel
        simpleAdPanel.gameObject.SetActive(true);
        simpleAdPanel.DOKill();
        simpleAdPanel.DOFade(1, fadeDuration).OnComplete(() => {
            simpleAdPanel.interactable = true;
            simpleAdPanel.blocksRaycasts = true;
        });

        // 3. Hiệu ứng Scale cho khung Popup
        if (popupContent != null)
        {
            popupContent.DOKill();
            popupContent.localScale = Vector3.one * 0.7f;
            popupContent.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);
        }

        // 4. Gán sự kiện xem Ads
        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(() => {
                // AdEventTracker.TrackRewardApiCalled();

                if (AdsManager.Instance != null)
                {
                    // Ưu tiên key từ Remote Config (ví dụ: "is_show_rw_challenge")
                    string finalKey = string.IsNullOrEmpty(adLogicKey) ? _currentRawID : adLogicKey;

                    AdsManager.Instance.ShowRewardedAd(finalKey, () => {
                        ExecuteUnlockSuccess(); // Callback khi xem hết quảng cáo
                    });
                }
                else
                {
                    // Fallback trong Editor để test nhanh
                    Debug.Log("<color=green>AdsManager không tìm thấy, tự động mở khóa (Editor Mode)</color>");
                    ExecuteUnlockSuccess();
                }
            });
        }

        // 5. Nút đóng
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    private void ExecuteUnlockSuccess()
    {
        // Lưu trạng thái vĩnh viễn
        string key = GetUnlockKey(_currentRawID);
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

        // Đóng popup trước
        ClosePopup();

        // Sau đó mới thực hiện callback thành công (Update UI hoặc Chuyển scene)
        _onSuccessCallback?.Invoke();
    }

    public void ClosePopup()
    {
        if (simpleAdPanel == null) return;

        // Hiện lại các panel cũ
        ToggleOtherPanels(true);

        simpleAdPanel.DOKill();
        simpleAdPanel.interactable = false;
        simpleAdPanel.blocksRaycasts = false;

        if (popupContent != null)
        {
            popupContent.DOKill();
            popupContent.DOScale(0.7f, fadeDuration).SetEase(Ease.InBack);
        }

        simpleAdPanel.DOFade(0, fadeDuration).OnComplete(() => {
            simpleAdPanel.gameObject.SetActive(false);
        });
    }

    private void ToggleOtherPanels(bool show)
    {
        if (panelsToHide == null) return;

        float targetAlpha = show ? 1f : 0f;
        foreach (var panel in panelsToHide)
        {
            if (panel == null || ignorePanels.Contains(panel)) continue;

            panel.DOKill();
            panel.DOFade(targetAlpha, fadeDuration);
            panel.blocksRaycasts = show;
            panel.interactable = show;
        }
    }
}