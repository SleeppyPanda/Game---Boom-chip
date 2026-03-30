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
    }

    public void HandleTileClick(int tileIndex, TileButton tile)
    {
        int currentPlayerID = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;
        List<int> targetList = (currentPlayerID == 1) ? p1SelectedTiles : p2SelectedTiles;

        if (targetList.Contains(tileIndex))
        {
            targetList.Remove(tileIndex);
            tile.SetVisual(defaultSprite);
        }
        else
        {
            // Kiểm tra giới hạn MAX_SELECTIONS = 3
            if (targetList.Count < GameConstants.MAX_SELECTIONS)
            {
                targetList.Add(tileIndex);
                tile.SetVisual(selectedSprite);
            }
        }

        // Gọi Manager kiểm tra điều kiện để hiện nút Next
        manager.UpdateButtonNextVisibility();
    }

    public bool IsSelectionComplete(int playerID)
    {
        return (playerID == 1 ? p1SelectedTiles : p2SelectedTiles).Count >= GameConstants.MAX_SELECTIONS;
    }
}