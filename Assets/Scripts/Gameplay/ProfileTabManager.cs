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

    [Header("Tham chiếu UI Handler")]
    public AccountUIHandler uiHandler;

    void Start()
    {
        if (uiHandler == null) uiHandler = GetComponentInParent<AccountUIHandler>();
        SelectPlayer(1);
        btnP1.onClick.AddListener(() => SelectPlayer(1));
        btnP2.onClick.AddListener(() => SelectPlayer(2));
    }

    public void SelectPlayer(int playerID)
    {
        if (AccountManager.Instance == null)
        {
            Debug.LogError("Không tìm thấy AccountManager Instance trong scene!");
            return;
        }
        AccountManager.Instance.SwitchEditingPlayer(playerID);
        if (uiHandler != null)
        {
            uiHandler.RefreshUI();
        }
        UpdateTabVisuals(playerID);
    }

    private void UpdateTabVisuals(int playerID)
    {
        btnP1.transform.localScale = Vector3.one;
        btnP2.transform.localScale = Vector3.one;

        if (playerID == 1)
        {
            btnP1.image.sprite = p1Active;
            btnP2.image.sprite = p2Inactive;

            btnP1.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 1);
        }
        else
        {
            btnP1.image.sprite = p1Inactive;
            btnP2.image.sprite = p2Active;

            btnP2.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 1);
        }
    }
}