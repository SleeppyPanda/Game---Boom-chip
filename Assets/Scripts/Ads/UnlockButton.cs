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
        UpdateUI();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    public void OnClick()
    {
        // 1. Xác định logicKey từ Remote Config tương ứng với loại nút
        string logicConfigKey = "";
        string finalItemID = unlockKey;

        if (type == UnlockType.Mode)
        {
            // Tự động map key dựa trên ID hoặc key nhập vào
            if (unlockKey.Contains("Challenge")) logicConfigKey = "is_show_rw_challenge";
            else if (unlockKey.Contains("Prediction")) logicConfigKey = "is_show_rw_prediction";
            else logicConfigKey = "is_show_rw_challenge"; // Default
        }
        else if (type == UnlockType.Avatar)
        {
            logicConfigKey = "is_show_rw_profile";
            finalItemID = "Avatar_" + indexValue; // Đảm bảo ID đồng nhất: Unlock_Avatar_1
        }

        // 2. Sử dụng HandleUnlockFlow để xử lý toàn bộ quy trình
        if (UnlockSystemManager.Instance != null)
        {
            Sprite spriteToDisplay = (displayAvatar != null) ? displayAvatar.sprite : null;

            UnlockSystemManager.Instance.HandleUnlockFlow(finalItemID, logicConfigKey, spriteToDisplay, () =>
            {
                // Hành động khi mở khóa thành công (hoặc đã mở từ trước)
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
            if (accountManager != null)
            {
                accountManager.SelectAvatar(indexValue);
            }
            // Nếu bạn dùng AccountManager, hãy đảm bảo nó cũng cập nhật lại UI profile
        }
    }

    public void UpdateUI()
    {
        bool isUnlocked = false;
        string finalItemID = (type == UnlockType.Avatar) ? "Avatar_" + indexValue : unlockKey;

        // Ưu tiên check qua UnlockSystemManager để đồng bộ logic PlayerPrefs
        if (UnlockSystemManager.Instance != null)
        {
            isUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(finalItemID);
        }
        else
        {
            isUnlocked = PlayerPrefs.GetInt("Unlock_" + finalItemID, 0) == 1;
        }

        // Cập nhật giao diện
        if (lockIcon != null) lockIcon.SetActive(!isUnlocked);
        if (blurOverlay != null) blurOverlay.enabled = !isUnlocked;
    }
}