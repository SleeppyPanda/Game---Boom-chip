using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIBinder : MonoBehaviour
{
    public enum Target { Player1, Player2, Winner }

    [Header("Thiết lập hiển thị")]
    public Target targetType;

    [Header("Liên kết Components")]
    public TextMeshProUGUI nameText;
    public Image avatarImage;

    void OnEnable() => BindData();
    void Start() => BindData();

    public void BindData()
    {
        if (AccountManager.Instance == null) return;

        int id = 1;
        switch (targetType)
        {
            case Target.Player1: id = 1; break;
            case Target.Player2: id = 2; break;
            case Target.Winner: id = AccountManager.LastWinnerID; break;
        }

        if (nameText != null)
            nameText.text = AccountManager.Instance.GetPlayerName(id);

        if (avatarImage != null)
            avatarImage.sprite = AccountManager.Instance.GetAvatarSprite(id);
    }
}