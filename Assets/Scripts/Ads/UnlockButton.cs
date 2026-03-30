using UnityEngine;
using UnityEngine.UI;

public enum UnlockType { Mode, Avatar }

public class UnlockButton : MonoBehaviour
{
    [Header("Cấu hình Loại mở khóa")]
    public UnlockType type;
    public string unlockKey; // Cho Mode: dùng key như "Challenge"
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
        if (accountManager == null) accountManager = AccountManager.Instance;
        UpdateUI();
    }

    private void OnEnable()
    {
        UpdateUI();
        AccountManager.OnAvatarUnlocked += UpdateUI;
    }

    private void OnDisable()
    {
        AccountManager.OnAvatarUnlocked -= UpdateUI;
    }

    public void OnClick()
    {
        // 1. Xác định ID duy nhất của vật phẩm
        string finalItemID = (type == UnlockType.Avatar) ? "Avatar_" + indexValue : unlockKey;

        // 2. KIỂM TRA TRẠNG THÁI MỞ KHÓA
        bool isUnlocked = false;
        if (UnlockSystemManager.Instance != null)
        {
            isUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(finalItemID);
        }
        else
        {
            isUnlocked = PlayerPrefs.GetInt("Unlock_" + finalItemID, 0) == 1;
        }

        // --- TRƯỜNG HỢP 1: ĐÃ MỞ KHÓA RỒI -> CHỈ CHỌN ---
        if (isUnlocked)
        {
            Debug.Log($"<color=cyan>[UnlockButton]</color> {finalItemID} đã mở. Thực hiện chọn.");
            ExecuteAction();
            return;
        }

        // --- TRƯỜNG HỢP 2: CHƯA MỞ KHÓA -> XỬ LÝ QUY TRÌNH MỞ ---
        string logicConfigKey = "";

        if (type == UnlockType.Mode)
        {
            if (unlockKey.Contains("Challenge")) logicConfigKey = "is_show_rw_challenge";
            else if (unlockKey.Contains("Prediction")) logicConfigKey = "is_show_rw_prediction";
            else logicConfigKey = "is_show_rw_challenge";
        }
        else if (type == UnlockType.Avatar)
        {
            // Kiểm tra xem Avatar này có bắt buộc xem Ads không
            if (!AdEventTracker.IsAvatarInRwList(indexValue))
            {
                Debug.Log($"<color=green>[UnlockButton]</color> {finalItemID} không yêu cầu Ads. Mở trực tiếp.");
                SaveUnlockState(finalItemID); // Tự lưu trạng thái mở khóa
                UpdateUI();
                ExecuteAction();
                return;
            }
            logicConfigKey = "is_show_rw_profile";
        }

        // 3. GỌI POPUP MỞ KHÓA (XEM ADS)
        if (UnlockSystemManager.Instance != null)
        {
            Sprite spriteToDisplay = (displayAvatar != null) ? displayAvatar.sprite : null;

            UnlockSystemManager.Instance.HandleUnlockFlow(finalItemID, logicConfigKey, spriteToDisplay, () =>
            {
                // Sau khi xem quảng cáo thành công:
                Debug.Log($"<color=yellow>[UnlockButton]</color> Mở khóa thành công {finalItemID}.");
                UpdateUI();
                ExecuteAction(); // Tự động chọn luôn sau khi mở khóa
            });
        }
        else
        {
            // Fallback nếu không có hệ thống quảng cáo
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
            // Ưu tiên sử dụng Instance mới nhất của AccountManager
            var targetManager = AccountManager.Instance != null ? AccountManager.Instance : accountManager;
            if (targetManager != null)
            {
                targetManager.SelectAvatar(indexValue);
            }
        }
    }

    // Hàm hỗ trợ để lưu trạng thái mở khóa mà không cần sửa UnlockSystemManager
    private void SaveUnlockState(string itemId)
    {
        PlayerPrefs.SetInt("Unlock_" + itemId, 1);
        PlayerPrefs.Save();
    }

    public void UpdateUI()
    {
        // 1. Xác định ID vật phẩm
        string finalItemID = (type == UnlockType.Avatar) ? "Avatar_" + indexValue : unlockKey;

        // 2. Lấy trạng thái mở khóa từ bộ nhớ (PlayerPrefs)
        bool isSavedUnlocked = false;
        if (UnlockSystemManager.Instance != null)
        {
            isSavedUnlocked = UnlockSystemManager.Instance.IsItemUnlocked(finalItemID);
        }
        else
        {
            isSavedUnlocked = PlayerPrefs.GetInt("Unlock_" + finalItemID, 0) == 1;
        }

        // 3. LOGIC MỚI: Kiểm tra xem nó có phải hàng "miễn phí" (không cần Ads) không
        bool isFreeItem = false;
        if (type == UnlockType.Avatar)
        {
            // Nếu KHÔNG nằm trong danh sách cần xem Ads -> Coi như Free
            isFreeItem = !AdEventTracker.IsAvatarInRwList(indexValue);
        }

        // TỔNG HỢP: Chỉ hiện Lock/Cover nếu: CHƯA mở khóa VÀ KHÔNG phải hàng miễn phí
        // Nói cách khác: Nếu đã mở HOẶC là hàng free thì ẨN LOCK
        bool shouldShowLock = !(isSavedUnlocked || isFreeItem);

        // 4. Cập nhật giao diện
        if (lockIcon != null) lockIcon.SetActive(shouldShowLock);
        if (blurOverlay != null) blurOverlay.enabled = shouldShowLock;

        // Debug để bạn dễ theo dõi
        if (isFreeItem && !isSavedUnlocked)
        {
            // Debug.Log($"<color=white>[UnlockButton]</color> {finalItemID} là hàng Free, ẩn Lock mặc định.");
        }
    }
}