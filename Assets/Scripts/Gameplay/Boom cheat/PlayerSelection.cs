using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerSelection : MonoBehaviour
{
    [Header("Dữ liệu lựa chọn")]
    public List<int> p1SelectedTiles = new List<int>();
    public List<int> p2SelectedTiles = new List<int>();

    [Header("Cấu hình hiển thị")]
    public Sprite selectedSprite;
    public Sprite defaultSpritePhase1;
    public Sprite defaultSpritePhase2;

    private BoomChipManager manager;

    void Awake()
    {
        manager = GetComponent<BoomChipManager>();

        // Ưu tiên dùng Sprite tùy chỉnh từ Settings (Hệ thống Unlock đã làm trước đó)
        if (BoomChipSettings.customHitSprite != null)
        {
            selectedSprite = BoomChipSettings.customHitSprite;
        }
    }

    /// <summary>
    /// Xử lý khi click vào một ô Tile
    /// </summary>
    public void HandleTileClick(int tileIndex, TileButton tile)
    {
        // Chỉ cho phép thao tác trong Phase 1 hoặc Phase 2
        if (manager.currentPhase != GamePhase.Phase1 && manager.currentPhase != GamePhase.Phase2) return;

        int currentPlayerID = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;
        List<int> targetList = (currentPlayerID == 1) ? p1SelectedTiles : p2SelectedTiles;
        Sprite currentDefault = (currentPlayerID == 1) ? defaultSpritePhase1 : defaultSpritePhase2;

        if (targetList.Contains(tileIndex))
        {
            // --- HÀNH ĐỘNG: BỎ CHỌN ---
            targetList.Remove(tileIndex);
            tile.SetVisual(currentDefault);
        }
        else
        {
            // --- HÀNH ĐỘNG: CHỌN MỚI ---
            if (targetList.Count < GameConstants.MAX_SELECTIONS)
            {
                targetList.Add(tileIndex);
                tile.SetVisual(selectedSprite);

                // Rung nhẹ khi chọn đủ số lượng (Tăng UX trên Mobile)
                if (targetList.Count == GameConstants.MAX_SELECTIONS)
                {
                    VibrateDevice();
                }
            }
        }

        // Cập nhật nút Next trên Manager UI
        manager.UpdateButtonNextVisibility();
    }

    /// <summary>
    /// Kiểm tra trạng thái hoàn thành để mở khóa nút Next
    /// </summary>
    public bool IsSelectionComplete(int playerID)
    {
        List<int> targetList = (playerID == 1) ? p1SelectedTiles : p2SelectedTiles;
        return targetList.Count >= GameConstants.MAX_SELECTIONS;
    }

    /// <summary>
    /// Reset dữ liệu cho lượt chơi mới
    /// </summary>
    public void ResetSelections()
    {
        p1SelectedTiles.Clear();
        p2SelectedTiles.Clear();
    }

    /// <summary>
    /// Rung máy nhẹ khi hoàn thành thao tác chọn
    /// </summary>
    private void VibrateDevice()
    {
        #if UNITY_ANDROID || UNITY_IOS
        try 
        {
            Handheld.Vibrate();
        }
            catch (Exception e) 
        {
            Debug.LogWarning("Vibration not supported or failed: " + e.Message);
        }
        #endif
    }
}