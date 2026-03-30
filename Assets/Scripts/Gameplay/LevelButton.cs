using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public string sceneToLoad;

    [Header("Cấu hình Sprite")]
    public bool setWinByHittingThree = true;
    public Sprite hitSprite;
    public Sprite missSprite;

    [Header("Cấu hình Âm thanh (Nhập tên file)")]
    public string hitSFX;  // Ví dụ: SFX_Bomb_Explosion
    public string missSFX; // Ví dụ: SFX_Water_Splash

    public void OnClickSelect()
    {
        Time.timeScale = 1f;

        // Lưu dữ liệu vào class trung gian
        BoomChipSettings.winByHittingThree = setWinByHittingThree;
        BoomChipSettings.customHitSprite = hitSprite;
        BoomChipSettings.customMissSprite = missSprite;

        // --- TRUYỀN TÊN SOUND ---
        BoomChipSettings.hitSFXName = hitSFX;
        BoomChipSettings.missSFXName = missSFX;

        // Phát tiếng click khi chọn màn (nếu có AudioManager)
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}