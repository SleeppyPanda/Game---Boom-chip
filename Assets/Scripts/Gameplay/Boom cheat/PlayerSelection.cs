using UnityEngine;
using System.Collections.Generic;

public class PlayerSelection : MonoBehaviour
{
    public List<int> p1SelectedTiles = new List<int>();
    public List<int> p2SelectedTiles = new List<int>();

    [Header("Cấu hình hiển thị")]
    public Sprite selectedSprite;

    // Tách riêng 2 Sprite mặc định để phù hợp với màu sắc từng bảng
    public Sprite defaultSpritePhase1;
    public Sprite defaultSpritePhase2;

    private BoomChipManager manager;

    void Awake()
    {
        manager = GetComponent<BoomChipManager>();

        // Ưu tiên dùng Sprite tùy chỉnh từ Settings nếu người chơi đã cài đặt
        if (BoomChipSettings.customHitSprite != null)
        {
            selectedSprite = BoomChipSettings.customHitSprite;
        }
    }

    public void HandleTileClick(int tileIndex, TileButton tile)
    {
        // Chỉ cho phép chọn trong Phase 1 hoặc Phase 2
        if (manager.currentPhase != GamePhase.Phase1 && manager.currentPhase != GamePhase.Phase2) return;

        // Xác định Player và Sprite mặc định tương ứng
        int currentPlayerID = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;
        List<int> targetList = (currentPlayerID == 1) ? p1SelectedTiles : p2SelectedTiles;
        Sprite currentDefault = (currentPlayerID == 1) ? defaultSpritePhase1 : defaultSpritePhase2;

        if (targetList.Contains(tileIndex))
        {
            // --- HÀNH ĐỘNG: BỎ CHỌN (TOGGLE OFF) ---
            targetList.Remove(tileIndex);

            // Trả về đúng màu sắc mặc định của bảng đó
            tile.SetVisual(currentDefault);
        }
        else
        {
            // --- HÀNH ĐỘNG: CHỌN MỚI (TOGGLE ON) ---
            // Kiểm tra giới hạn tối đa (thường là 3)
            if (targetList.Count < GameConstants.MAX_SELECTIONS)
            {
                targetList.Add(tileIndex);
                tile.SetVisual(selectedSprite);
            }
        }

        // Cập nhật trạng thái hiển thị nút "Next" trên UI của Manager
        manager.UpdateButtonNextVisibility();
    }

    public bool IsSelectionComplete(int playerID)
    {
        // Kiểm tra xem Player đã chọn đủ số lượng yêu cầu chưa
        List<int> targetList = (playerID == 1) ? p1SelectedTiles : p2SelectedTiles;
        return targetList.Count >= GameConstants.MAX_SELECTIONS;
    }
}