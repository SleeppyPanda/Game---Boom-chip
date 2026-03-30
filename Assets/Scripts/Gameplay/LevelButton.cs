using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public string sceneToLoad;

    [Header("Firebase Tracking")]
    // Thêm dòng này để chọn Mode từ Inspector
    public AdEventTracker.GameMode gameModeID;

    [Header("Dice Mode Configuration")]
    public bool isBombMode = false;
    public bool setWinByHittingThree = true;

    [Header("Player 1 Config (Sprite & Hit Sound)")]
    public Sprite p1Selection;
    public string p1HitSFX;

    [Header("Player 2 Config (Sprite & Hit Sound)")]
    public Sprite p2Selection;
    public string p2HitSFX;

    [Header("Common Visuals (Optional)")]
    public Sprite hitSprite;
    public Sprite missSprite;

    [Header("Common Sounds")]
    public string missSFX;

    public void OnClickSelect()
    {
        Time.timeScale = 1f;

        // --- TRACKING FIREBASE ---
        // Bắn event count_mode_xx dựa trên gameModeID đã chọn ở Inspector
        AdEventTracker.TrackModeEnter(gameModeID);

        // --- GÁN DỮ LIỆU VÀO SETTINGS ---
        BoomChipSettings.player1Sprite = p1Selection;
        BoomChipSettings.player2Sprite = p2Selection;

        BoomChipSettings.player1HitSFX = p1HitSFX;
        BoomChipSettings.player2HitSFX = p2HitSFX;

        BoomChipSettings.winByHittingThree = setWinByHittingThree;
        BoomChipSettings.isBombModeActive = isBombMode;

        BoomChipSettings.customHitSprite = hitSprite;
        BoomChipSettings.customMissSprite = missSprite;
        BoomChipSettings.missSFXName = missSFX;

        // --- XỬ LÝ CHUYỂN SCENE ---
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("SFX_Click");

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("SceneToLoad đang trống trên Object: " + gameObject.name);
        }
    }
}