using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class AccountManager : MonoBehaviour
{
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

    // Awake chạy trước Start, đảm bảo dữ liệu có sẵn ngay khi Scene load xong
    void Awake()
    {
        // 1. Load dữ liệu từ máy
        currentTempIndex = PlayerPrefs.GetInt(AVATAR_KEY, 0);
        string savedName = PlayerPrefs.GetString(NAME_KEY, "Alex");

        if (nameInputField != null) nameInputField.text = savedName;

        // 2. Ép hiển thị Sprite ngay lập tức
        ApplyAvatarToUI();
    }

    void Start()
    {
        if (editButton != null) editButton.onClick.AddListener(EnableEditing);

        // Gọi lại lần nữa trong Start để phòng hờ các Script khác ghi đè
        ApplyAvatarToUI();
    }

    private void ApplyAvatarToUI()
    {
        if (allAvatars == null || allAvatars.Count == 0) return;

        // Kiểm tra an toàn Index
        if (currentTempIndex < 0 || currentTempIndex >= allAvatars.Count) currentTempIndex = 0;

        Sprite activeSprite = allAvatars[currentTempIndex];

        // Cập nhật ảnh ngoài sảnh
        if (mainMenuBarAvatar != null)
        {
            mainMenuBarAvatar.sprite = activeSprite;
            // Ép UI cập nhật trạng thái hiển thị (Graphic Update)
            mainMenuBarAvatar.SetVerticesDirty();
            mainMenuBarAvatar.SetLayoutDirty();
        }

        // Cập nhật ảnh trong Panel
        if (avatarDisplayInPanel != null)
        {
            avatarDisplayInPanel.sprite = activeSprite;
        }

        Debug.Log($"<color=yellow>[SCENE LOAD]</color> Đã nạp Sprite: <b>{activeSprite.name}</b>");
    }

    public void EnableEditing()
    {
        nameInputField.interactable = true;
        nameInputField.ActivateInputField();
        nameInputField.onEndEdit.AddListener(delegate { nameInputField.interactable = false; });
    }

    public void SelectAvatar(int index)
    {
        if (index >= 0 && index < allAvatars.Count)
        {
            currentTempIndex = index;
            if (avatarDisplayInPanel != null) avatarDisplayInPanel.sprite = allAvatars[index];

            // Hiệu ứng nảy khi chọn
            if (avatarDisplayInPanel != null)
            {
                avatarDisplayInPanel.rectTransform.DOKill(true);
                avatarDisplayInPanel.rectTransform.localScale = new Vector3(0.711f, 0.711f, 1f);
                avatarDisplayInPanel.rectTransform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f);
            }
        }
    }

    public void SaveAndExit()
    {
        PlayerPrefs.SetString(NAME_KEY, nameInputField.text);
        PlayerPrefs.SetInt(AVATAR_KEY, currentTempIndex);
        PlayerPrefs.Save();

        // Cập nhật lại ảnh ngoài sảnh khi thoát bảng
        if (mainMenuBarAvatar != null)
        {
            mainMenuBarAvatar.sprite = allAvatars[currentTempIndex];
        }

        Debug.Log($"<color=green><b>[ĐÃ LƯU]</b></color> Đã chốt Sprite Index: {currentTempIndex}");
    }
}