using UnityEngine;
using UnityEngine.UI;

public enum UnlockType { Mode, Avatar }

public class UnlockButton : MonoBehaviour
{
    [Header("Cấu hình Loại mở khóa")]
    public UnlockType type;
    public string unlockKey; // Nhập: "Mode2" hoặc "Mode3"
    public int indexValue;   // 1 cho Mode 2, 2 cho Mode 3 (khớp với MenuManager)

    [Header("Giao diện thành phần")]
    public GameObject lockIcon;
    public Image blurOverlay;
    public Image displayAvatar;

    [Header("Kết nối Hệ thống")]
    public MenuManager menuManager;
    public AccountManager accountManager;

    private void Start()
    {
        if (accountManager == null) accountManager = AccountManager.Instance;
        // Tự động tìm MenuManager nếu chưa gán
        if (menuManager == null) menuManager = Object.FindFirstObjectByType<MenuManager>();
        UpdateUI();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    public void OnClick()
    {
        // 1. Xác định ID duy nhất
        string finalItemID = GetFinalItemID();

        // 2. KIỂM TRA TRẠNG THÁI MỞ KHÓA
        bool isUnlocked = false;
        if (UnlockSystemManager.Instance != null)
        {
            isUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(finalItemID);
        }
        else
        {
            // Fallback nếu không có Manager
            string key = finalItemID.Contains("Mode") ? finalItemID + "_Unlocked" : "Unlock_" + finalItemID;
            isUnlocked = PlayerPrefs.GetInt(key, 0) == 1;
        }

        // --- TRƯỜNG HỢP 1: ĐÃ MỞ KHÓA RỒI -> THỰC HIỆN CHUYỂN SCENE/CHỌN ---
        if (isUnlocked)
        {
            Debug.Log($"<color=cyan>[UnlockButton]</color> {finalItemID} đã mở. Thực hiện hành động chính.");
            ExecuteAction();
            return;
        }

        // --- TRƯỜNG HỢP 2: CHƯA MỞ KHÓA -> XỬ LÝ QUY TRÌNH MỞ (ADS) ---
        string logicConfigKey = "";

        if (type == UnlockType.Mode)
        {
            if (finalItemID == "Mode2") logicConfigKey = "is_show_rw_challenge";
            else if (finalItemID == "Mode3") logicConfigKey = "is_show_rw_prediction";
            else logicConfigKey = "is_show_rw_challenge";
        }
        else if (type == UnlockType.Avatar)
        {
            if (!AdEventTracker.IsAvatarInRwList(indexValue))
            {
                SaveUnlockState(finalItemID);
                UpdateUI();
                return; // Mở xong chỉ hiện UI, không chuyển scene ngay
            }
            logicConfigKey = "is_show_rw_profile";
        }

        // 3. GỌI POPUP MỞ KHÓA
        if (UnlockSystemManager.Instance != null)
        {
            Sprite spriteToDisplay = (displayAvatar != null) ? displayAvatar.sprite : null;

            UnlockSystemManager.Instance.HandleUnlockFlow(finalItemID, logicConfigKey, spriteToDisplay, () =>
            {
                // SAU KHI XEM ADS XONG:
                Debug.Log($"<color=yellow>[UnlockButton]</color> Mở khóa thành công {finalItemID}.");
                UpdateUI(); // Cập nhật để ẩn ổ khóa
                // Không gọi ExecuteAction ở đây để người dùng bấm lại lần nữa mới vào game
            });
        }
    }

    private void ExecuteAction()
    {
        if (type == UnlockType.Mode)
        {
            if (menuManager != null)
            {
                // Kích hoạt hiệu ứng transition và load scene trực tiếp
                if (indexValue == 1) menuManager.StartGameMode2();
                else if (indexValue == 2) menuManager.StartGameMode3();
                else menuManager.DirectSwitchToMode(indexValue);
            }
        }
        else if (type == UnlockType.Avatar)
        {
            var targetManager = AccountManager.Instance != null ? AccountManager.Instance : accountManager;
            if (targetManager != null)
            {
                targetManager.SelectAvatar(indexValue);
            }
        }
    }

    private void SaveUnlockState(string itemId)
    {
        string key = itemId.Contains("Mode") ? itemId + "_Unlocked" : "Unlock_" + itemId;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public void UpdateUI()
    {
        string finalItemID = GetFinalItemID();

        bool isSavedUnlocked = false;
        if (UnlockSystemManager.Instance != null)
        {
            isSavedUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(finalItemID);
        }
        else
        {
            string key = finalItemID.Contains("Mode") ? finalItemID + "_Unlocked" : "Unlock_" + finalItemID;
            isSavedUnlocked = PlayerPrefs.GetInt(key, 0) == 1;
        }

        bool isFreeItem = false;
        if (type == UnlockType.Avatar)
        {
            isFreeItem = !AdEventTracker.IsAvatarInRwList(indexValue);
        }

        bool shouldShowLock = !(isSavedUnlocked || isFreeItem);

        if (lockIcon != null) lockIcon.SetActive(shouldShowLock);
        if (blurOverlay != null) blurOverlay.enabled = shouldShowLock;
    }

    private string GetFinalItemID()
    {
        if (type == UnlockType.Avatar) return "Avatar_" + indexValue;
        return string.IsNullOrEmpty(unlockKey) ? "Mode" + indexValue : unlockKey;
    }
}