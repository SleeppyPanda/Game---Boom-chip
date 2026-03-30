using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerUIBinder : MonoBehaviour
{
    public enum PlayerSide { Player1, Player2 }

    [Header("Thiết lập")]
    public PlayerSide targetPlayer;

    [Header("Liên kết UI (Không bắt buộc, có cái nào gán cái đó)")]
    public TextMeshProUGUI nameText;
    public Image avatarImage;

    [Header("Dữ liệu Avatar")]
    public List<Sprite> allAvatars; // Kéo bộ avatar giống bên Profile vào đây

    void Start()
    {
        BindData();
    }

    // Gọi hàm này nếu bạn muốn cập nhật UI ngay lập tức khi có thay đổi
    public void BindData()
    {
        string suffix = (targetPlayer == PlayerSide.Player1) ? "_P1" : "_P2";
        string defaultName = (targetPlayer == PlayerSide.Player1) ? "Player 1" : "Player 2";
        int defaultAvatar = (targetPlayer == PlayerSide.Player1) ? 0 : 1;

        // Lấy dữ liệu từ PlayerPrefs (giống key bên ProfileTabManager)
        string savedName = PlayerPrefs.GetString("PlayerName" + suffix, defaultName);
        int savedAvatarIndex = PlayerPrefs.GetInt("SelectedAvatarIndex" + suffix, defaultAvatar);

        // Đổ dữ liệu vào Text
        if (nameText != null)
            nameText.text = savedName;

        // Đổ dữ liệu vào Image
        if (avatarImage != null && allAvatars != null && savedAvatarIndex < allAvatars.Count)
        {
            avatarImage.sprite = allAvatars[savedAvatarIndex];
        }
    }
}