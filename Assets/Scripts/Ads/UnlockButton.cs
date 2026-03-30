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

    /// <summary>
    /// Hàm gắn vào OnClick của Button
    /// </summary>
    public void OnClick()
    {
        // 1. Kiểm tra xem đã mở khóa cái này chưa
        bool isUnlocked = false;
        if (UnlockSystemManager.Instance != null)
            isUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(unlockKey);
        else
            isUnlocked = PlayerPrefs.GetInt("Unlock_" + unlockKey, 0) == 1;

        if (isUnlocked)
        {
            // Đã mở rồi thì chỉ việc thực hiện hành động
            ExecuteAction();
        }
        else
        {
            // 2. Chưa mở -> Gọi Popup quảng cáo cho RIÊNG cái này
            Sprite spriteToDisplay = displayAvatar != null ? displayAvatar.sprite : null;

            if (UnlockSystemManager.Instance != null)
            {
                // Mở popup với đúng ID và đúng Hình ảnh của nút này
                UnlockSystemManager.Instance.OpenUnlockPopup(unlockKey, spriteToDisplay, () => {
                    // Khi xem xong quảng cáo thành công:
                    UpdateUI();       // Ẩn ổ khóa của chính nút này
                    ExecuteAction();  // Thực hiện chọn luôn
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