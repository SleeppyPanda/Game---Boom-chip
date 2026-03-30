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

    // QUAN TRỌNG: Lưu lại hình ảnh gốc (hình quả bom) để quay lại khi bỏ chọn
    private Sprite originalSprite;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // Lưu lại hình ảnh quả bom bạn đã gán trong Editor/Prefab ngay khi game chạy
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
        // Giai đoạn ban đầu (Phase 1, 2) có thể không cần ApplyInitialSkin 
        // vì mặc định tất cả phải là hình quả bom che khuất.
        // Tuy nhiên, hàm này vẫn giữ để đảm bảo tính linh hoạt.
        ApplyInitialSkin();
    }

    private void ApplyInitialSkin()
    {
        // Nếu bạn muốn lúc bắt đầu game các ô hiện luôn skin (tùy logic game)
        // Hiện tại logic của chúng ta là hiện hình quả bom (originalSprite)
        if (buttonImage == null) return;
    }

    void OnClicked()
    {
        if (manager == null) return;

        // Logic xử lý click dựa trên Phase hiện tại
        if (manager.currentPhase == GamePhase.Phase1 || manager.currentPhase == GamePhase.Phase2)
        {
            // Chỉ cho phép click vào bảng của mình trong phase tương ứng để đặt bom
            int currentPlayer = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;
            if (ownerID == currentPlayer)
            {
                manager.HandleTileClick(tileIndex, this);
            }
        }
        else if (manager.currentPhase == GamePhase.Phase3)
        {
            // Giai đoạn chiến đấu: manager sẽ tự check lượt của ai được bắn bảng nào
            manager.ExecuteTurn(tileIndex, this, ownerID);
        }
    }

    /// <summary>
    /// Thay đổi hình ảnh hiển thị. 
    /// Nếu truyền vào null, ô sẽ tự quay về hình quả bom gốc.
    /// </summary>
    public void SetVisual(Sprite newSprite)
    {
        if (buttonImage == null) return;

        if (newSprite == null)
        {
            buttonImage.sprite = originalSprite; // Quay về hình quả bom mặc định
        }
        else
        {
            buttonImage.sprite = newSprite; // Hiển thị skin hoặc hình gạch chéo (miss)
        }
    }

    /// <summary>
    /// Reset ô về trạng thái ban đầu
    /// </summary>
    public void ResetTile(Sprite defaultSprite = null)
    {
        SetVisual(defaultSprite); // Sẽ dùng originalSprite nếu defaultSprite là null
        SetInteractable(true);
    }

    public void SetInteractable(bool state)
    {
        if (button != null) button.interactable = state;
    }
}