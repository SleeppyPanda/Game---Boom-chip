using UnityEngine;
using UnityEngine.UI; // Cần thiết cho Button, Image
using System;         // Cần thiết cho Action

public class UnlockSystemManager : MonoBehaviour
{
    public static UnlockSystemManager Instance;

    [Header("Giao diện Popup Duy Nhất")]
    public GameObject simpleAdPanel;
    public Button watchAdButton;
    public Image displayAvatar; // Chỗ để hiển thị hình ảnh item/nhân vật

    private Action _onSuccessCallback;

    void Awake()
    {
        Instance = this;
        if (simpleAdPanel != null) simpleAdPanel.SetActive(false);
    }

    public void OpenUnlockPopup(string unlockKey, Sprite itemSprite, Action onSuccess)
    {
        _onSuccessCallback = onSuccess;

        // Cập nhật hình ảnh hiển thị trong Popup
        if (displayAvatar != null && itemSprite != null)
        {
            displayAvatar.sprite = itemSprite;
        }

        simpleAdPanel.SetActive(true);

        watchAdButton.onClick.RemoveAllListeners();
        watchAdButton.onClick.AddListener(() => {
            // Lưu ý: Đảm bảo AdsManager của bạn cũng đã sẵn sàng
            AdsManager.Instance.ShowRewardedAd(() => {
                PlayerPrefs.SetInt(unlockKey, 1);
                PlayerPrefs.Save();

                simpleAdPanel.SetActive(false);
                if (_onSuccessCallback != null) _onSuccessCallback.Invoke();
            });
        });
    }
    
    public void ClosePopup() => simpleAdPanel.SetActive(false);
}