using UnityEngine;
using UnityEngine.UI;

public enum UnlockType { Mode, Avatar }

public class UnlockButton : MonoBehaviour
{
    [Header("Cấu hình Loại mở khóa")]
    public UnlockType type;
    public string unlockKey; // ID định danh (VD: "Avatar_8", "Mode2")
    public int indexValue;    // Index để dùng khi chọn Avatar hoặc Mode

    [Header("Giao diện thành phần")]
    public GameObject lockIcon;
    public Image blurOverlay;
    public Image displayAvatar;   // Ảnh đại diện trong nút (sẽ hiện lên Popup)

    [Header("Kết nối Hệ thống")]
    public MenuManager menuManager;
    public AccountManager accountManager;

    private void Start()
    {
        UpdateUI();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    public void OnClick()
    {
        // 1. Kiểm tra trạng thái mở khóa
        bool isUnlocked = false;
        if (type == UnlockType.Avatar && AccountManager.Instance != null)
            isUnlocked = AccountManager.Instance.IsAvatarUnlocked(indexValue);
        else if (UnlockSystemManager.Instance != null)
            isUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(unlockKey);
        else
            isUnlocked = PlayerPrefs.GetInt("Unlock_" + unlockKey, 0) == 1;

        if (isUnlocked)
        {
            ExecuteAction();
        }
        else
        {
            // 2. Xác định hình ảnh sẽ hiển thị trên Popup
            Sprite spriteToDisplay = null;

            // Nếu nút có thành phần Image displayAvatar, lấy sprite từ đó
            if (displayAvatar != null)
            {
                spriteToDisplay = displayAvatar.sprite;
            }

            if (UnlockSystemManager.Instance != null)
            {
                // Mở Popup với hình ảnh đã được cập nhật theo nút vừa bấm
                UnlockSystemManager.Instance.OpenUnlockPopup(unlockKey, spriteToDisplay, () => {

                    // Lưu trạng thái mở khóa vào PlayerPrefs
                    if (type == UnlockType.Avatar)
                    {
                        PlayerPrefs.SetInt("Unlock_Avatar_" + indexValue, 1);
                    }
                    else
                    {
                        PlayerPrefs.SetInt("Unlock_" + unlockKey, 1);
                    }
                    PlayerPrefs.Save();

                    UpdateUI();
                    ExecuteAction();
                });
            }
        }
    }

    private void ExecuteAction()
    {
        if (type == UnlockType.Mode)
        {
            if (menuManager == null) return;
            // Gọi thẳng hàm chuyển Mode, không để MenuManager check quảng cáo lần nữa
            if (indexValue == 1) menuManager.ShowPanel2WithAvatar(null);
            else if (indexValue == 2) menuManager.ShowPanel3WithAvatar(null);
        }
        else if (type == UnlockType.Avatar)
        {
            if (accountManager != null)
            {
                // Gọi hàm chọn Avatar trực tiếp
                // Lưu ý: Phải chắc chắn AccountManager.SelectAvatar không gọi thêm quảng cáo nữa
                accountManager.SelectAvatar(indexValue);
            }
        }
    }

    public void UpdateUI()
    {
        bool isUnlocked = false;

        if (UnlockSystemManager.Instance != null)
            isUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(unlockKey);
        else
            isUnlocked = PlayerPrefs.GetInt("Unlock_" + unlockKey, 0) == 1;

        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);
        if (blurOverlay != null) blurOverlay.enabled = !isUnlocked;
    }
}