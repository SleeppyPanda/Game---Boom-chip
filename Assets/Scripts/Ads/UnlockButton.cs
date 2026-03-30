using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UnlockButton : MonoBehaviour
{
    [Header("Cài đặt định danh")]
    public string unlockKey;
    public Sprite buttonAvatar;

    [Header("Giao diện")]
    public GameObject lockIcon;
    public Image blurOverlay;

    [Header("Cấu hình Animation")]
    public MenuManager menuManager; // Kéo MenuManager vào đây
    public int buttonIndex;         // Index của nút (0, 1, hoặc 2)

    [Header("Sự kiện sau khi mở khóa")]
    public UnityEvent onUnlockSuccess;

    void Start() { UpdateUI(); }

    public void OnClick()
    {
        if (menuManager != null)
        {
            menuManager.HandleButtonAnimationOnly(buttonIndex);
        }

        if (PlayerPrefs.GetInt(unlockKey, 0) == 1)
        {
            onUnlockSuccess?.Invoke();
        }
        else
        {
            // TRUYỀN THÊM: buttonAvatar vào hàm gọi Popup
            UnlockSystemManager.Instance.OpenUnlockPopup(unlockKey, buttonAvatar, () => {
                UpdateUI();
                onUnlockSuccess?.Invoke();
            });
        }
    }

    private void UpdateUI()
    {
        bool isUnlocked = PlayerPrefs.GetInt(unlockKey, 0) == 1;
        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);
        if (blurOverlay != null) blurOverlay.enabled = !isUnlocked;
    }
}