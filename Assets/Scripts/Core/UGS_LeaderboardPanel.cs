using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Leaderboards;
using TMPro;

public class UGS_LeaderboardPanel : MonoBehaviour
{
    public string leaderboardId = "Prediction_HighScore";
    public TextMeshProUGUI txtLeaderboardContent;

    // Hàm này gán vào nút "Leaderboard" trong Setting hoặc Result
    public async void OpenLeaderboard()
    {
        gameObject.SetActive(true);
        if (txtLeaderboardContent != null) txtLeaderboardContent.text = "Loading...";

        try
        {
            var scores = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId,
                new GetScoresOptions { Limit = 10 });

            string content = "TOP 10 PLAYERS\n\n";
            foreach (var res in scores.Results)
            {
                // Lấy tên player (cắt bỏ phần ID sau dấu #)
                string playerName = res.PlayerName.Contains("#") ? res.PlayerName.Split('#')[0] : res.PlayerName;
                content += $"#{res.Rank + 1}  {playerName}: {res.Score}\n";
            }
            txtLeaderboardContent.text = content;
        }
        catch (System.Exception e)
        {
            txtLeaderboardContent.text = "Kết nối Server thất bại!";
            Debug.LogError(e.Message);
        }
    }

    public void ClosePanel() => gameObject.SetActive(false);
}