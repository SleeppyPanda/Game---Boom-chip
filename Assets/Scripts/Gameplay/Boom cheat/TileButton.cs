using UnityEngine;
using UnityEngine.UI;

public partial class TileButton : MonoBehaviour
{
    public int tileIndex;
    [Header("1 cho bảng P1, 2 cho bảng P2")]
    public int ownerID; // ID để xác định ô này thuộc về bàn cờ của ai

    private Button button;
    private Image buttonImage;
    private BoomChipManager manager;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // Sử dụng FindFirstObjectByType để tìm Manager trong Scene
        manager = FindFirstObjectByType<BoomChipManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
    }

    void OnClicked()
    {
        if (manager == null) return;

        // Giữ logic tách biệt theo yêu cầu của bạn
        if (manager.currentPhase == GamePhase.Phase1 || manager.currentPhase == GamePhase.Phase2)
        {
            // Xử lý chọn vị trí bom (Giai đoạn chuẩn bị)
            manager.HandleTileClick(tileIndex, this);
        }
        else if (manager.currentPhase == GamePhase.Phase3)
        {
            // Xử lý bắn bom (Giai đoạn chiến đấu) - Cần truyền thêm ownerID
            manager.ExecuteTurn(tileIndex, this, ownerID);
        }
    }

    public void SetVisual(Sprite newSprite)
    {
        if (newSprite != null && buttonImage != null)
        {
            buttonImage.sprite = newSprite;
        }
    }

    public void ResetTile(Sprite defaultSprite)
    {
        if (buttonImage != null) buttonImage.sprite = defaultSprite;
        if (button != null) button.interactable = true;
    }
    public void SetInteractable(bool state)
    {
        if (button != null) button.interactable = state;
    }
}