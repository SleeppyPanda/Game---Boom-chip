using UnityEngine;
using System.Collections.Generic;

public class PlayerSelection : MonoBehaviour
{
    public List<int> p1SelectedTiles = new List<int>();
    public List<int> p2SelectedTiles = new List<int>();

    [Header("Cấu hình hiển thị")]
    public Sprite selectedSprite;
    public Sprite defaultSprite;

    private BoomChipManager manager;

    void Awake()
    {
        manager = GetComponent<BoomChipManager>();

        // --- CẬP NHẬT: Nhận Sprite từ Settings nếu có ---
        if (BoomChipSettings.customHitSprite != null)
        {
            selectedSprite = BoomChipSettings.customHitSprite;
        }
    }

    public void HandleTileClick(int tileIndex, TileButton tile)
    {
        // Xác định Player nào đang thực hiện chọn dựa trên Phase
        int currentPlayerID = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;

        // Chỉ cho phép chọn nếu đúng Phase 1 hoặc Phase 2
        if (manager.currentPhase != GamePhase.Phase1 && manager.currentPhase != GamePhase.Phase2) return;

        List<int> targetList = (currentPlayerID == 1) ? p1SelectedTiles : p2SelectedTiles;

        if (targetList.Contains(tileIndex))
        {
            // Nếu đã chọn rồi thì bỏ chọn (Toggle)
            targetList.Remove(tileIndex);
            tile.SetVisual(defaultSprite);
        }
        else
        {
            // Kiểm tra giới hạn tối đa (ví dụ: 3 quả)
            if (targetList.Count < GameConstants.MAX_SELECTIONS)
            {
                targetList.Add(tileIndex);
                tile.SetVisual(selectedSprite);
            }
        }

        // Cập nhật trạng thái hiển thị nút "Next" trên UI
        manager.UpdateButtonNextVisibility();
    }

    public bool IsSelectionComplete(int playerID)
    {
        // Kiểm tra xem Player đã chọn đủ số lượng yêu cầu chưa
        List<int> targetList = (playerID == 1) ? p1SelectedTiles : p2SelectedTiles;
        return targetList.Count >= GameConstants.MAX_SELECTIONS;
    }
}