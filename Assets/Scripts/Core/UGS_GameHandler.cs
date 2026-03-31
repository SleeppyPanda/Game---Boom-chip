using UnityEngine;
using Unity.Services.Leaderboards;
using System.Threading.Tasks;

public class UGS_GameHandler : MonoBehaviour
{
    public static UGS_GameHandler Instance;

    [Header("Configuration")]
    public string leaderboardId = "Prediction_HighScore";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Hàm gửi điểm lên bảng xếp hạng - Gọi từ Mode3Manager
    public async void SubmitScore(int score)
    {
        // Kiểm tra xem Id có trống không để tránh lỗi runtime
        if (string.IsNullOrEmpty(leaderboardId)) return;

        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            Debug.Log($"<color=green>[UGS]</color> Đã gửi {score} điểm lên Leaderboard.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Không gửi được điểm: " + e.Message);
        }
    }
}