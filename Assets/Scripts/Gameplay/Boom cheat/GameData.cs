using UnityEngine;

// Định nghĩa các giai đoạn của trò chơi
public enum GamePhase
{
    Phase1,
    Phase2,
    Animation, // Thêm trạng thái này để khớp với logic tung xu của bạn
    Phase3,
}

// Chứa các hằng số dùng chung cho toàn bộ Project
public static class GameConstants
{
    public const int MAX_SELECTIONS = 3;
}