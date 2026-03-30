using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using System; // Cần thiết để dùng Action

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance;

    // Sự kiện để các nút Avatar cập nhật giao diện (mất icon khóa) đồng loạt
    public static Action OnAvatarUnlocked;

    [Header("Cấu hình Tên")]
    public TMP_InputField nameInputField;
    public Button editButton;

    [Header("Cấu hình Avatar")]
    public Image avatarDisplayInPanel;
    public Image mainMenuBarAvatar;
    public List<Sprite> allAvatars;

    private int currentTempIndex;
    private const string NAME_KEY = "PlayerName";
    private const string AVATAR_KEY = "SelectedAvatarIndex";
    private const string UNLOCK_PREFIX = "Unlock_Avatar_";

    void Awake()
    {
        if (Instance == null) Instance = this;

        // 1. Mặc định mở khóa Avatar đầu tiên (Index 0)
        if (PlayerPrefs.GetInt(UNLOCK_PREFIX + "0", 0) == 0)
        {
            PlayerPrefs.SetInt(UNLOCK_PREFIX + "0", 1);
            PlayerPrefs.Save();
        }

        // 2. Load dữ liệu người dùng
        currentTempIndex = PlayerPrefs.GetInt(AVATAR_KEY, 0);
        string savedName = PlayerPrefs.GetString(NAME_KEY, "Player 1");

        if (nameInputField != null) nameInputField.text = savedName;

        ApplyAvatarToUI();
    }

    void Start()
    {
        if (editButton != null) editButton.onClick.AddListener(EnableEditing);
        ApplyAvatarToUI();
    }

    private void ApplyAvatarToUI()
    {
        if (allAvatars == null || allAvatars.Count == 0) return;
        if (currentTempIndex < 0 || currentTempIndex >= allAvatars.Count) currentTempIndex = 0;

        Sprite activeSprite = allAvatars[currentTempIndex];

        if (mainMenuBarAvatar != null) mainMenuBarAvatar.sprite = activeSprite;
        if (avatarDisplayInPanel != null) avatarDisplayInPanel.sprite = activeSprite;
    }

    public void EnableEditing()
    {
        if (nameInputField == null) return;

        nameInputField.interactable = true;
        nameInputField.ActivateInputField();

        // Xóa các listener cũ để tránh trùng lặp
        nameInputField.onEndEdit.RemoveAllListeners();

        // Thêm listener mới
        nameInputField.onEndEdit.AddListener((val) => {
            // Thực hiện lưu dữ liệu ngay lập tức
            PlayerPrefs.SetString(NAME_KEY, val);
            PlayerPrefs.Save();
            Debug.Log("Đã lưu tên mới: " + val);

            // Sử dụng một mẹo nhỏ: Tắt interactable ở frame tiếp theo để tránh lỗi Selectable
            StartCoroutine(DisableInputFieldRoutine());
        });
    }

    // Coroutine hỗ trợ tắt InputField an toàn
    private System.Collections.IEnumerator DisableInputFieldRoutine()
    {
        yield return null; // Chờ 1 frame để EventSystem hoàn tất Deselect
        if (nameInputField != null)
        {
            nameInputField.interactable = false;
        }
    }

    // --- KIỂM TRA TRẠNG THÁI UNLOCK ---
    public bool IsAvatarUnlocked(int index)
    {
        return PlayerPrefs.GetInt(UNLOCK_PREFIX + index, 0) == 1;
    }

    // --- HÀM CHỌN AVATAR (CHỈ LÀM NHIỆM VỤ THỰC THI) ---
    public void SelectAvatar(int index)
    {
        if (index < 0 || index >= allAvatars.Count) return;

        // Kiểm tra xem index này đã được mở khóa chưa
        // (Lưu ý: Việc mở khóa thực sự đã được thực hiện bởi UnlockButton trước khi gọi hàm này)
        if (IsAvatarUnlocked(index))
        {
            SetAvatar(index);
        }
        else
        {
            Debug.LogWarning("Avatar index " + index + " chưa được mở khóa. Hãy xem Ads!");
        }
    }

    private void SetAvatar(int index)
    {
        currentTempIndex = index;

        // 1. Hiệu ứng đổi hình ảnh
        if (avatarDisplayInPanel != null)
        {
            avatarDisplayInPanel.sprite = allAvatars[index];
            avatarDisplayInPanel.rectTransform.DOKill(true);
            avatarDisplayInPanel.rectTransform.localScale = Vector3.one;
            avatarDisplayInPanel.rectTransform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.3f, 5, 1);
        }

        // 2. Bắn sự kiện Firebase: Ghi nhận user chọn avatar (xx là index)
        if (FirebaseManager.Instance != null)
        {
            FirebaseManager.Instance.LogCountAvatar(index);
        }

        SaveAndRefreshUI();

        // 3. Phát lệnh cho tất cả các nút cùng Update lại (để ẩn ổ khóa nếu cần)
        OnAvatarUnlocked?.Invoke();
    }

    private void SaveAndRefreshUI()
    {
        PlayerPrefs.SetInt(AVATAR_KEY, currentTempIndex);
        PlayerPrefs.Save();

        // Cập nhật Avatar ở thanh menu chính
        if (mainMenuBarAvatar != null) mainMenuBarAvatar.sprite = allAvatars[currentTempIndex];
    }

    public void SaveAndExit()
    {
        if (nameInputField != null) PlayerPrefs.SetString(NAME_KEY, nameInputField.text);
        PlayerPrefs.SetInt(AVATAR_KEY, currentTempIndex);
        PlayerPrefs.Save();
        ApplyAvatarToUI();
    }
}