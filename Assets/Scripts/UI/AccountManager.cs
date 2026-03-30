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
    public Image mainMenuBarAvatar; // Thường hiển thị Avatar của P1 hoặc người chơi hiện tại
    public List<Sprite> allAvatars;

    [Header("Cấu hình 2 Người Chơi")]
    private int currentPlayerID = 1; // Mặc định là Player 1
    private int currentTempIndex;

    // Các Key cơ sở (sẽ được cộng thêm đuôi _P1 hoặc _P2)
    private const string NAME_BASE_KEY = "PlayerName";
    private const string AVATAR_BASE_KEY = "SelectedAvatarIndex";
    private const string UNLOCK_PREFIX = "Unlock_Avatar_"; // Unlock dùng chung cho cả 2

    // Hàm tiện ích để lấy Key động theo Player hiện tại
    private string GetCurrentNameKey() => NAME_BASE_KEY + "_P" + currentPlayerID;
    private string GetCurrentAvatarKey() => AVATAR_BASE_KEY + "_P" + currentPlayerID;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        // Tự động mở khóa Avatar đầu tiên (index 0) - Dùng chung
        if (PlayerPrefs.GetInt(UNLOCK_PREFIX + "0", 0) == 0)
        {
            PlayerPrefs.SetInt(UNLOCK_PREFIX + "0", 1);
            PlayerPrefs.Save();
        }

        // Load dữ liệu mặc định ban đầu (Player 1)
        LoadPlayerData(1);
    }

    void Start()
    {
        if (editButton != null) editButton.onClick.AddListener(EnableEditing);
    }

    void OnDestroy()
    {
        OnAvatarUnlocked = null;
    }

    // --- LOGIC MỚI: CHUYỂN ĐỔI NGƯỜI CHƠI ---

    /// <summary>
    /// Được gọi từ ProfileTabManager khi nhấn nút Tab P1 hoặc P2
    /// </summary>
    public void SwitchCurrentPlayer(int playerID)
    {
        // Lưu dữ liệu của người cũ trước khi chuyển
        SaveAndExit();

        // Chuyển sang người mới
        currentPlayerID = playerID;
        LoadPlayerData(playerID);

        Debug.Log($"Đã chuyển sang chỉnh sửa: Player {playerID}");
    }

    private void LoadPlayerData(int playerID)
    {
        currentTempIndex = PlayerPrefs.GetInt(GetCurrentAvatarKey(), (playerID == 1) ? 0 : 1);
        string savedName = PlayerPrefs.GetString(GetCurrentNameKey(), "Player " + playerID);

        if (nameInputField != null) nameInputField.text = savedName;

        ApplyAvatarToUI();
    }

    // --- LOGIC REMOTE CONFIG CHO AVATAR (GIỮ NGUYÊN) ---

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

    public void SelectAvatar(int index)
    {
        if (index < 0 || index >= allAvatars.Count) return;

        if (IsAvatarUnlocked(index) || !DoesAvatarRequireAd(index))
        {
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
        }
    }

    public void SetAvatar(int index)
    {
        currentTempIndex = index;

        if (avatarDisplayInPanel != null)
        {
            avatarDisplayInPanel.sprite = allAvatars[index];
            avatarDisplayInPanel.rectTransform.DOKill(true);

            float targetScale = 0.8f;
            avatarDisplayInPanel.rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
            avatarDisplayInPanel.rectTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.3f, 5, 1);
        }

        // Gắn thêm thông tin player vào tracking nếu cần
        AdEventTracker.TrackAvatarChoose(index);

        SaveAndRefreshUI();

        OnAvatarUnlocked?.Invoke();
    }

    private void SaveAndRefreshUI()
    {
        PlayerPrefs.SetInt(GetCurrentAvatarKey(), currentTempIndex);
        PlayerPrefs.Save();

        if (mainMenuBarAvatar != null) mainMenuBarAvatar.sprite = allAvatars[currentTempIndex];
    }

    // --- QUẢN LÝ TÊN ---

    public void EnableEditing()
    {
        if (nameInputField == null) return;

        nameInputField.interactable = true;
        nameInputField.ActivateInputField();
        nameInputField.caretPosition = nameInputField.text.Length;

        nameInputField.onEndEdit.RemoveAllListeners();
        nameInputField.onEndEdit.AddListener(OnEndEditName);
    }

    private void OnEndEditName(string val)
    {
        string cleanName = val.Trim();

        if (!string.IsNullOrEmpty(cleanName))
        {
            PlayerPrefs.SetString(GetCurrentNameKey(), cleanName);
            PlayerPrefs.Save();
            Debug.Log($"Đã lưu tên mới cho P{currentPlayerID}: " + cleanName);
        }
        else
        {
            nameInputField.text = PlayerPrefs.GetString(GetCurrentNameKey(), "Player " + currentPlayerID);
        }

        StartCoroutine(DisableInputFieldRoutine());
    }

    private IEnumerator DisableInputFieldRoutine()
    {
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
                PlayerPrefs.SetString(GetCurrentNameKey(), cleanName);
        }

        PlayerPrefs.SetInt(GetCurrentAvatarKey(), currentTempIndex);
        PlayerPrefs.Save();
        ApplyAvatarToUI();
    }
}