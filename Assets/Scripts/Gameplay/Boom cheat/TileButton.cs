using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TileButton : MonoBehaviour
{
    [Header("Cấu hình ô")]
    public int tileIndex;

    [Header("1 cho bảng P1, 2 cho bảng P2")]
    public int ownerID; // Xác định ô này thuộc về "người chơi" nào (Chủ nhà)

    [Header("ID bảng (Thường giống ownerID)")]
    public int boardID; // Xác định ô này nằm trên "bàn cờ" vật lý nào (1 hoặc 2)

    private Button button;
    private Image buttonImage;
    private BoomChipManager manager;

    // Lưu lại hình ảnh gốc (ô gạch chưa lật)
    private Sprite originalSprite;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // Lưu lại hình ảnh mặc định ngay khi Awake
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
        // Đảm bảo lúc bắt đầu ô hiện hình ảnh mặc định
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
            // Truyền đủ 4 tham số sang Manager để xử lý logic "Sân ai hiện skin người đó"
            // tileIndex: vị trí ô
            // this: chính component này để đổi sprite
            // ownerID: để lấy Skin của chủ nhà
            // boardID: để biết kiểm tra túi bom của ai (đã đảo)
            manager.ExecuteTurn(tileIndex, this, ownerID, boardID);
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
    /// Reset ô về trạng thái ban đầu để chơi ván mới
    /// </summary>
    public void ResetTile(Sprite defaultSprite = null)
    {
        SetVisual(defaultSprite);
        SetInteractable(true);
    }

    public void SetInteractable(bool state)
    {
        if (button != null) button.interactable = state;
    }
}