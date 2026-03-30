using UnityEngine;
using UnityEngine.UI;
using System;

public class UnlockSystemManager : MonoBehaviour
{
    public static UnlockSystemManager Instance;

    [Header("Giao diện Popup Duy Nhất")]
    public GameObject simpleAdPanel;
    public Button watchAdButton;

    private Action _onSuccessCallback; // Lưu lại hành động của nút đang gọi

    void Awake()
    {
        Instance = this;
        if (simpleAdPanel != null) simpleAdPanel.SetActive(false);
    }

    // Hàm này được gọi bởi bất kỳ nút nào muốn mở khóa
    public void OpenUnlockPopup(string unlockKey, Action onSuccess)
    {
        _onSuccessCallback = onSuccess;

        simpleAdPanel.SetActive(true);

        watchAdButton.onClick.RemoveAllListeners();
        watchAdButton.onClick.AddListener(() => {
            AdsManager.Instance.ShowRewardedAd(() => {
                // Lưu trạng thái mở khóa
                PlayerPrefs.SetInt(unlockKey, 1);
                PlayerPrefs.Save();

                simpleAdPanel.SetActive(false);
                _onSuccessCallback?.Invoke(); // Chạy chức năng tương ứng của nút đó
            });
        });
    }

    public void ClosePopup() => simpleAdPanel.SetActive(false);
}