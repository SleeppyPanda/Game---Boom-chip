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
    private string _currentItemKey;
    private string _currentRawID;

    void Awake()
    {
        Instance = this;
        if (simpleAdPanel != null)
        {
            simpleAdPanel.DOKill();
            simpleAdPanel.alpha = 0;
            simpleAdPanel.interactable = false;
            simpleAdPanel.blocksRaycasts = false;
            simpleAdPanel.gameObject.SetActive(false);
        }
    }

    #region HÀM CẦU NỐI SỬA LỖI CS1501 & CS1061
    /// <summary>
    /// Khớp hoàn toàn với UnlockButton.cs: (string itemID, string logicKey, Sprite sprite, Action success)
    /// </summary>
    public void HandleUnlockFlow(string itemID, string logicConfigKey, Sprite itemSprite, Action onSuccess)
    {
        // Kiểm tra xem item đã được mở khóa chưa trước khi hiện Popup
        if (IsItemUnlocked(itemID))
        {
            onSuccess?.Invoke();
            return;
        }

        // Nếu chưa mở, hiện Popup và truyền logicConfigKey vào Ads
        OpenUnlockPopup(itemID, itemSprite, onSuccess, logicConfigKey);
    }

    // Bản dự phòng cho MenuManager hoặc các script khác (nếu dùng 2-3 tham số)
    public void HandleUnlockFlow(string itemID, Action onSuccess)
        => HandleUnlockFlow(itemID, "", null, onSuccess);

    public void HandleUnlockFlow(string itemID, Sprite itemSprite, Action onSuccess)
        => HandleUnlockFlow(itemID, "", itemSprite, onSuccess);
    #endregion

    public bool IsItemUnlocked(string itemID)
    {
        return PlayerPrefs.GetInt("Unlock_" + itemID, 0) == 1;
    }

    // Cập nhật hàm OpenUnlockPopup để nhận logicConfigKey từ Remote Config
    public void OpenUnlockPopup(string itemID, Sprite itemSprite, Action onSuccess, string adLogicKey = "")
    {
        if (simpleAdPanel == null)
        {
            Debug.LogError("Chưa gán simpleAdPanel!");
            onSuccess?.Invoke();
            return;
        }

        _currentRawID = itemID;
        _currentItemKey = "Unlock_" + itemID;
        _onSuccessCallback = onSuccess;

        if (displayAvatar != null && itemSprite != null)
        {
            displayAvatar.sprite = itemSprite;
        }

        // AppsFlyer: Eligible
        AdEventTracker.TrackRewardEligible();

        ToggleOtherPanels(false);
        simpleAdPanel.gameObject.SetActive(true);
        simpleAdPanel.DOKill();
        simpleAdPanel.DOFade(1, fadeDuration).OnComplete(() => {
            simpleAdPanel.interactable = true;
            simpleAdPanel.blocksRaycasts = true;
        });

        if (popupContent != null)
        {
            popupContent.DOKill();
            popupContent.localScale = Vector3.one * 0.7f;
            popupContent.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);
        }

        if (watchAdButton != null)
        {
            watchAdButton.onClick.RemoveAllListeners();
            watchAdButton.onClick.AddListener(() => {
                // AppsFlyer: Api Called
                AdEventTracker.TrackRewardApiCalled();

                if (AdsManager.Instance != null)
                {
                    // Truyền adLogicKey (ví dụ: "is_show_rw_challenge") vào hệ thống quảng cáo
                    string finalKey = string.IsNullOrEmpty(adLogicKey) ? _currentRawID : adLogicKey;
                    AdsManager.Instance.ShowRewardedAd(finalKey, () => {
                        ExecuteUnlockSuccess();
                    });
                }
                else
                {
                    ExecuteUnlockSuccess();
                }
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    private void ExecuteUnlockSuccess()
    {
        if (!string.IsNullOrEmpty(_currentItemKey))
        {
            PlayerPrefs.SetInt(_currentItemKey, 1);
            PlayerPrefs.Save();
        }

        AdEventTracker.TrackRewardDisplayed();
        ClosePopup();
        _onSuccessCallback?.Invoke();
    }

    public void ClosePopup()
    {
        if (simpleAdPanel == null) return;
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