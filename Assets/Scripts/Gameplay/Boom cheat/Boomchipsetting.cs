using UnityEngine;

public static class BoomChipSettings
{
    public static bool winByHittingThree = true;
    public static bool isBombModeActive = false;

    // --- SPRITE RIÊNG BIỆT ---
    public static Sprite player1Sprite; // Hình chiếm ô của P1
    public static Sprite player2Sprite; // Hình chiếm ô của P2

    // --- ÂM THANH TRÚNG/NỔ RIÊNG BIỆT ---
    public static string player1HitSFX;
    public static string player2HitSFX;

    // --- DÙNG CHUNG ---
    public static Sprite customHitSprite; // Có thể dùng làm hình quả bom chung hoặc bỏ nếu dùng skin riêng
    public static Sprite customMissSprite;

    public static string hitSFXName;
    public static string missSFXName; // Âm thanh hụt (Dùng chung)

    public static void ResetSettings()
    {
        winByHittingThree = true;
        isBombModeActive = false;

        player1Sprite = null;
        player2Sprite = null;

        player1HitSFX = string.Empty;
        player2HitSFX = string.Empty;

        customHitSprite = null;
        customMissSprite = null;
        missSFXName = string.Empty;
    }
}