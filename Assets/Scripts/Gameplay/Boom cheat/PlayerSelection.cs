using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerSelection : MonoBehaviour
{
    [Header("Dữ liệu lựa chọn")]
    public List<int> p1SelectedTiles = new List<int>();
    public List<int> p2SelectedTiles = new List<int>();

    private BoomChipManager manager;

    void Awake()
    {
        manager = GetComponent<BoomChipManager>();
    }

    /// <summary>
    /// Kiểm tra xem một ô cụ thể có đang nằm trong danh sách đã chọn hay không.
    /// </summary>
    public bool IsTileSelected(int playerID, int index)
    {
        if (playerID == 1)
            return p1SelectedTiles.Contains(index);
        else
            return p2SelectedTiles.Contains(index);
    }

    /// <summary>
    /// Xử lý khi click vào một ô Tile trong giai đoạn đặt bom (Phase 1 & 2)
    /// </summary>
    public void HandleTileClick(int tileIndex, TileButton tile)
    {
        // Chỉ xử lý trong Phase đặt bom
        if (manager.currentPhase != GamePhase.Phase1 && manager.currentPhase != GamePhase.Phase2) return;

        int currentPlayerID = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;
        List<int> targetList = (currentPlayerID == 1) ? p1SelectedTiles : p2SelectedTiles;

        // Lấy Skin kẹo/bánh từ Settings dựa theo Player hiện tại
        Sprite playerSkin = (currentPlayerID == 1) ? BoomChipSettings.player1Sprite : BoomChipSettings.player2Sprite;

        if (targetList.Contains(tileIndex))
        {
            // --- HÀNH ĐỘNG: BỎ CHỌN ---
            targetList.Remove(tileIndex);

            // TRUYỀN NULL: Để TileButton tự hiển thị lại Sprite mặc định (quả bom)
            tile.SetVisual(null);
        }
        else
        {
            // --- HÀNH ĐỘNG: CHỌN MỚI ---
            if (targetList.Count < 3)
            {
                targetList.Add(tileIndex);

                // HIỂN THỊ SKIN: Đè skin kẹo/bánh lên hình quả bom
                tile.SetVisual(playerSkin);

                // Rung khi chọn đủ 3 ô
                if (targetList.Count == 3)
                {
                    VibrateDevice();
                }
            }
        }

        // Cập nhật trạng thái nút "Next" trên UI
        manager.UpdateButtonNextVisibility();
    }

    /// <summary>
    /// Kiểm tra xem người chơi đã chọn đủ số lượng bom chưa
    /// </summary>
    public bool IsSelectionComplete(int playerID)
    {
        List<int> targetList = (playerID == 1) ? p1SelectedTiles : p2SelectedTiles;
        return targetList.Count >= 3;
    }

    /// <summary>
    /// Reset dữ liệu khi bắt đầu lại game
    /// </summary>
    public void ResetSelections()
    {
        p1SelectedTiles.Clear();
        p2SelectedTiles.Clear();
    }

    private void VibrateDevice()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        try 
        {
            // Gọi hàm rung từ hệ thống GlobalSettings của bạn
            GlobalSettings.PlayVibrate();
        }
        catch (Exception e) 
        {
            Debug.LogWarning("Vibration failed: " + e.Message);
        }
#endif
    }
}