using UnityEngine;
using UnityEngine.UI;

public partial class TileButton : MonoBehaviour
{
    public int tileIndex;
    [Header("1 cho bảng P1, 2 cho bảng P2")]
    public int ownerID; // ID để xác định ô này thuộc về bàn cờ của ai (PlayerID)

    private Button button;
    private Image buttonImage;
    private BoomChipManager manager;

    // Lưu lại hình ảnh gốc (ví dụ: hình ô gạch chưa lật hoặc hình mặc định)
    private Sprite originalSprite;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // Lưu lại hình ảnh mặc định trong Editor/Prefab ngay khi Awake
        if (buttonImage != null)
        {
            originalSprite = buttonImage.sprite;
        }

        // Tìm Manager trong Scene
        manager = FindFirstObjectByType<BoomChipManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
    }

    void Start()
    {
        ApplyInitialSkin();
    }

    private void ApplyInitialSkin()
    {
        // Đảm bảo lúc bắt đầu ô hiện hình ảnh mặc định (originalSprite)
        if (buttonImage != null && originalSprite != null)
        {
            buttonImage.sprite = originalSprite;
        }
    }

    void OnClicked()
    {
        if (manager == null) return;

        // --- GIAI ĐOẠN ĐẶT BOM (Phase 1 & 2) ---
        if (manager.currentPhase == GamePhase.Phase1 || manager.currentPhase == GamePhase.Phase2)
        {
            // Chỉ cho phép click vào bảng của chính mình để đặt bom
            int currentPlayerSettingBombs = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;

            if (ownerID == currentPlayerSettingBombs)
            {
                // Gọi manager để xử lý việc chọn/bỏ chọn bom
                manager.HandleTileClick(tileIndex, this);
            }
        }
        // --- GIAI ĐOẠN CHIẾN ĐẤU (Phase 3) ---
        else if (manager.currentPhase == GamePhase.Phase3)
        {
            // Logic tách biệt: 
            // ownerID đóng vai trò xác định bảng của người chơi bị tác động vật lý.
            // manager.ExecuteTurn sẽ kiểm tra lượt (isP1Turn) và so khớp với ownerID này.
            manager.ExecuteTurn(tileIndex, this, ownerID);
        }
    }

    /// <summary>
    /// Thay đổi hình ảnh hiển thị (Skin nhân vật khi trúng bom hoặc hình Miss). 
    /// Nếu truyền vào null, ô sẽ tự quay về hình ảnh gốc ban đầu.
    /// </summary>
    public void SetVisual(Sprite newSprite)
    {
        if (buttonImage == null) return;

        if (newSprite == null)
        {
            buttonImage.sprite = originalSprite;
        }
        else
        {
            buttonImage.sprite = newSprite;
        }
    }

    /// <summary>
    /// Reset ô về trạng thái ban đầu để chơi ván mới hoặc reset phase
    /// </summary>
    public void ResetTile(Sprite defaultSprite = null)
    {
        // Nếu defaultSprite truyền vào là null, SetVisual sẽ tự lấy originalSprite
        SetVisual(defaultSprite);
        SetInteractable(true);
    }

    public void SetInteractable(bool state)
    {
        if (button != null) button.interactable = state;
    }
}