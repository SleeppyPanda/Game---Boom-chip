using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UnlockButton : MonoBehaviour
{
    [Header("Cài đặt định danh")]
    public string unlockKey;

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
        // BƯỚC 1: Luôn chạy hiệu ứng hình ảnh của Button (Scale, PosY, Color)
        // để người chơi thấy nút vẫn hoạt động mượt mà.
        if (menuManager != null)
        {
            // Chúng ta gọi hàm xử lý hiệu ứng hình ảnh nhưng CHƯA hiện Panel
            menuManager.HandleButtonAnimationOnly(buttonIndex);
        }

        // BƯỚC 2: Kiểm tra khóa
        if (PlayerPrefs.GetInt(unlockKey, 0) == 1)
        {
            onUnlockSuccess?.Invoke();
        }
        else
        {
            UnlockSystemManager.Instance.OpenUnlockPopup(unlockKey, () => {
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