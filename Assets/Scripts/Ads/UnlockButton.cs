using UnityEngine;
using UnityEngine.UI;

public enum UnlockType { Mode, Avatar }

public class UnlockButton : MonoBehaviour
{
    [Header("Cấu hình Loại mở khóa")]
    public UnlockType type;
    public string unlockKey; // Cho Mode: dùng key như "Challenge", cho Avatar: dùng "Avatar_1"
    public int indexValue;   // ID của Mode hoặc ID của Avatar

    [Header("Giao diện thành phần")]
    public GameObject lockIcon;
    public Image blurOverlay;
    public Image displayAvatar;

    [Header("Kết nối Hệ thống")]
    public MenuManager menuManager;
    public AccountManager accountManager;

    private void Start()
    {
        // Tự động tìm AccountManager nếu chưa gán trong Inspector
        if (accountManager == null) accountManager = AccountManager.Instance;
        UpdateUI();
    }

    private void OnEnable()
    {
        UpdateUI();
        // Đăng ký sự kiện để tự động làm mới UI khi có bất kỳ Avatar nào được mở khóa
        AccountManager.OnAvatarUnlocked += UpdateUI;
    }

    private void OnDisable()
    {
        // Hủy đăng ký để tránh lỗi bộ nhớ
        AccountManager.OnAvatarUnlocked -= UpdateUI;
    }

    public void OnClick()
    {
        // 1. Xác định logicKey từ Remote Config tương ứng với loại nút
        string logicConfigKey = "";
        string finalItemID = unlockKey;

        if (type == UnlockType.Mode)
        {
            if (unlockKey.Contains("Challenge")) logicConfigKey = "is_show_rw_challenge";
            else if (unlockKey.Contains("Prediction")) logicConfigKey = "is_show_rw_prediction";
            else logicConfigKey = "is_show_rw_challenge";
        }
        else if (type == UnlockType.Avatar)
        {
            logicConfigKey = "is_show_rw_profile";
            finalItemID = "Avatar_" + indexValue;
        }

        // 2. Sử dụng HandleUnlockFlow để xử lý toàn bộ quy trình
        if (UnlockSystemManager.Instance != null)
        {
            Sprite spriteToDisplay = (displayAvatar != null) ? displayAvatar.sprite : null;

            UnlockSystemManager.Instance.HandleUnlockFlow(finalItemID, logicConfigKey, spriteToDisplay, () =>
            {
                // Hành động khi mở khóa thành công
                UpdateUI();
                ExecuteAction();
            });
        }
        else
        {
            // Fallback nếu thiếu Manager
            ExecuteAction();
        }
    }

    private void ExecuteAction()
    {
        if (type == UnlockType.Mode)
        {
            if (menuManager != null)
            {
                menuManager.DirectSwitchToMode(indexValue);
            }
        }
        else if (type == UnlockType.Avatar)
        {
            // Sử dụng Instance của AccountManager để đảm bảo gọi đúng người chơi hiện tại (P1 hoặc P2)
            if (AccountManager.Instance != null)
            {
                AccountManager.Instance.SelectAvatar(indexValue);
            }
            else if (accountManager != null)
            {
                accountManager.SelectAvatar(indexValue);
            }
        }
    }

    public void UpdateUI()
    {
        bool isUnlocked = false;
        string finalItemID = (type == UnlockType.Avatar) ? "Avatar_" + indexValue : unlockKey;

        if (UnlockSystemManager.Instance != null)
        {
            isUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(finalItemID);
        }
        else
        {
            isUnlocked = PlayerPrefs.GetInt("Unlock_" + finalItemID, 0) == 1;
        }

        // Cập nhật giao diện Khóa/Mở khóa
        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);
        if (blurOverlay != null) blurOverlay.enabled = !isUnlocked;
    }
}