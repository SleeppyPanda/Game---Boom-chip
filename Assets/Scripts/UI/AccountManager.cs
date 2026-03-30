using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using System;
using System.Collections;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance;
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
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Tự động mở khóa Avatar đầu tiên (index 0)
        if (PlayerPrefs.GetInt(UNLOCK_PREFIX + "0", 0) == 0)
        {
            PlayerPrefs.SetInt(UNLOCK_PREFIX + "0", 1);
            PlayerPrefs.Save();
        }

        currentTempIndex = PlayerPrefs.GetInt(AVATAR_KEY, 0);
        string savedName = PlayerPrefs.GetString(NAME_KEY, "Player 1");

        if (nameInputField != null) nameInputField.text = savedName;

        ApplyAvatarToUI();
    }

    void Start()
    {
        if (editButton != null) editButton.onClick.AddListener(EnableEditing);
    }

    void OnDestroy()
    {
        OnAvatarUnlocked = null;
    }

    // --- LOGIC REMOTE CONFIG CHO AVATAR ---

    /// <summary>
    /// Kiểm tra xem Avatar index này có bắt người dùng xem Ads để mở không.
    /// Sử dụng hàm tiện ích từ AdEventTracker (đã xử lý chuỗi "1,2,3" từ Firebase)
    /// </summary>
    public bool DoesAvatarRequireAd(int index)
    {
        return AdEventTracker.IsAvatarInRwList(index);
    }

    // --- QUẢN LÝ AVATAR ---

    private void ApplyAvatarToUI()
    {
        if (allAvatars == null || allAvatars.Count == 0) return;
        if (currentTempIndex < 0 || currentTempIndex >= allAvatars.Count) currentTempIndex = 0;

        Sprite activeSprite = allAvatars[currentTempIndex];

        if (mainMenuBarAvatar != null) mainMenuBarAvatar.sprite = activeSprite;
        if (avatarDisplayInPanel != null) avatarDisplayInPanel.sprite = activeSprite;
    }

    public bool IsAvatarUnlocked(int index)
    {
        return PlayerPrefs.GetInt(UNLOCK_PREFIX + index, 0) == 1;
    }

    /// <summary>
    /// Được gọi từ UnlockButton hoặc UI chọn Avatar
    /// </summary>
    public void SelectAvatar(int index)
    {
        if (index < 0 || index >= allAvatars.Count) return;

        // Nếu đã mở khóa hoặc KHÔNG nằm trong danh sách bắt xem Ads của Firebase
        if (IsAvatarUnlocked(index) || !DoesAvatarRequireAd(index))
        {
            // Mở khóa luôn nếu nó không yêu cầu Ads
            if (!IsAvatarUnlocked(index))
            {
                PlayerPrefs.SetInt(UNLOCK_PREFIX + index, 1);
                PlayerPrefs.Save();
            }

            SetAvatar(index);
        }
        else
        {
            Debug.Log($"Avatar {index} yêu cầu xem quảng cáo Reward để mở khóa.");
            // Logic ShowRewardedAd thường được gọi trực tiếp tại script của Button mở khóa
        }
    }

   
    public void SetAvatar(int index)
    {
        currentTempIndex = index;

        if (avatarDisplayInPanel != null)
        {
            avatarDisplayInPanel.sprite = allAvatars[index];

            // Dừng các tween cũ để tránh xung đột
            avatarDisplayInPanel.rectTransform.DOKill(true);

            // --- THAY ĐỔI TẠI ĐÂY: Thiết lập scale mặc định là 0.7116897f ---
            float targetScale = 0.7116897f;
            avatarDisplayInPanel.rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);

            // Hiệu ứng PunchScale sẽ nẩy dựa trên scale hiện tại (0.7116897)
            // Nếu bạn muốn hiệu ứng mạnh hơn hoặc nhẹ hơn, có thể chỉnh thông số 0.1f ở dưới
            avatarDisplayInPanel.rectTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.3f, 5, 1);
        }

        // --- FIREBASE TRACKING: count_avatar_xx ---
        AdEventTracker.TrackAvatarChoose(index);

        SaveAndRefreshUI();

        // Thông báo cho các UI/UnlockButton khác cập nhật trạng thái (nếu có)
        OnAvatarUnlocked?.Invoke();
    }

    private void SaveAndRefreshUI()
    {
        PlayerPrefs.SetInt(AVATAR_KEY, currentTempIndex);
        PlayerPrefs.Save();

        if (mainMenuBarAvatar != null) mainMenuBarAvatar.sprite = allAvatars[currentTempIndex];
    }

    // --- QUẢN LÝ TÊN ---

    public void EnableEditing()
    {
        if (nameInputField == null) return;

        nameInputField.interactable = true;
        nameInputField.ActivateInputField();

        // Di chuyển con trỏ về cuối dòng
        nameInputField.caretPosition = nameInputField.text.Length;

        nameInputField.onEndEdit.RemoveAllListeners();
        nameInputField.onEndEdit.AddListener(OnEndEditName);
    }

    private void OnEndEditName(string val)
    {
        string cleanName = val.Trim();

        if (!string.IsNullOrEmpty(cleanName))
        {
            PlayerPrefs.SetString(NAME_KEY, cleanName);
            PlayerPrefs.Save();
            Debug.Log("Đã lưu tên mới: " + cleanName);
        }
        else
        {
            // Nếu rỗng, trả về tên cũ đã lưu
            nameInputField.text = PlayerPrefs.GetString(NAME_KEY, "Player 1");
        }

        StartCoroutine(DisableInputFieldRoutine());
    }

    private IEnumerator DisableInputFieldRoutine()
    {
        // Chờ 1 frame để tránh lỗi focus của Unity UI khi bấm Enter/Thoát
        yield return null;
        if (nameInputField != null)
        {
            nameInputField.interactable = false;
        }
    }

    public void SaveAndExit()
    {
        if (nameInputField != null)
        {
            string cleanName = nameInputField.text.Trim();
            if (!string.IsNullOrEmpty(cleanName))
                PlayerPrefs.SetString(NAME_KEY, cleanName);
        }

        PlayerPrefs.SetInt(AVATAR_KEY, currentTempIndex);
        PlayerPrefs.Save();
        ApplyAvatarToUI();
    }
}