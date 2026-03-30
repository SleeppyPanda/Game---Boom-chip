using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class AccountUIHandler : MonoBehaviour
{
    [Header("Liên kết UI")]
    public TMP_InputField nameInputField;
    public Button editButton;
    public Image avatarDisplayInPanel;
    public Image mainMenuBarAvatar; // Avatar nhỏ ở góc màn hình Menu

    void Start()
    {
        if (editButton != null) editButton.onClick.AddListener(EnableEditing);
        RefreshUI();
    }

    // Cập nhật giao diện theo Player đang chọn (P1 hoặc P2)
    public void RefreshUI()
    {
        if (AccountManager.Instance == null) return;

        int id = AccountManager.Instance.currentPlayerID;
        nameInputField.text = AccountManager.Instance.GetPlayerName(id);

        Sprite s = AccountManager.Instance.GetAvatarSprite(id);
        if (avatarDisplayInPanel != null) avatarDisplayInPanel.sprite = s;
        if (mainMenuBarAvatar != null) mainMenuBarAvatar.sprite = s;
    }

    // Gắn vào các nút chọn Avatar trong Grid
    public void SelectAvatar(int index)
    {
        if (AccountManager.Instance == null) return;

        bool needsAd = AdEventTracker.IsAvatarInRwList(index);
        bool isUnlocked = AccountManager.Instance.IsAvatarUnlocked(index);

        if (isUnlocked || !needsAd)
        {
            if (!isUnlocked) AccountManager.Instance.UnlockAvatar(index);

            int currentID = AccountManager.Instance.currentPlayerID;
            AccountManager.Instance.SaveAvatarIndex(currentID, index);

            // Hiệu ứng Visual cho UI
            if (avatarDisplayInPanel != null)
            {
                avatarDisplayInPanel.sprite = AccountManager.Instance.allAvatars[index];
                avatarDisplayInPanel.rectTransform.DOKill(true);
                avatarDisplayInPanel.rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
            }

            AdEventTracker.TrackAvatarChoose(index);
            RefreshUI();
        }
        else
        {
            Debug.Log("Avatar này cần xem quảng cáo!");
        }
    }

    public void EnableEditing()
    {
        nameInputField.interactable = true;
        nameInputField.ActivateInputField();
        nameInputField.onEndEdit.RemoveAllListeners();
        nameInputField.onEndEdit.AddListener(val => {
            AccountManager.Instance.SavePlayerName(AccountManager.Instance.currentPlayerID, val);
            StartCoroutine(DisableInputRoutine());
            RefreshUI();
        });
    }

    private IEnumerator DisableInputRoutine()
    {
        yield return null; // Đợi 1 frame để tránh lỗi focus
        nameInputField.interactable = false;
    }
}