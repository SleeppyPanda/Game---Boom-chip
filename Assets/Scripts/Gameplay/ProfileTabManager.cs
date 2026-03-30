using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ProfileTabManager : MonoBehaviour
{
    [Header("Nút Tab")]
    public Button btnP1;
    public Button btnP2;

    [Header("Sprites Trạng Thái")]
    public Sprite p1Active;   // Hình ảnh khi P1 được chọn
    public Sprite p1Inactive; // Hình ảnh khi P1 không được chọn
    public Sprite p2Active;   // Hình ảnh khi P2 được chọn
    public Sprite p2Inactive; // Hình ảnh khi P2 không được chọn

    [Header("Tham chiếu")]
    public AccountManager accountManager;

    void Start()
    {
        // Mặc định load Player 1 khi mở game
        SelectPlayer(1);

        btnP1.onClick.AddListener(() => SelectPlayer(1));
        btnP2.onClick.AddListener(() => SelectPlayer(2));
    }

    public void SelectPlayer(int playerID)
    {
        if (accountManager == null) return;

        // 1. Cập nhật ID người chơi hiện tại vào bộ nhớ tạm của AccountManager
        accountManager.SwitchCurrentPlayer(playerID);

        // 2. Thay đổi Sprite cho 2 nút Tab để người dùng biết đang ở Tab nào
        if (playerID == 1)
        {
            btnP1.image.sprite = p1Active;
            btnP2.image.sprite = p2Inactive;
            // Hiệu ứng nẩy nhẹ cho Tab đang chọn
            btnP1.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
        else
        {
            btnP1.image.sprite = p1Inactive;
            btnP2.image.sprite = p2Active;
            btnP2.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
    }
}