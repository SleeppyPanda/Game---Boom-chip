using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public string sceneToLoad;

    [Header("Firebase Tracking")]
    public AdEventTracker.GameMode gameModeID;

    [Header("Dice Mode Configuration")]
    public bool isBombMode = false;
    public bool setWinByHittingThree = true;

    [Header("Player 1 Config")]
    public Sprite p1Selection;
    public string p1HitSFX;

    [Header("Player 2 Config")]
    public Sprite p2Selection;
    public string p2HitSFX;

    [Header("Common Visuals & Sounds")]
    public Sprite hitSprite;
    public Sprite missSprite;
    public string missSFX;

    private Button btnComponent;

    void Awake()
    {
        btnComponent = GetComponent<Button>();
    }

    public void OnClickSelect()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning($"<color=red>[LevelButton]</color> SceneToLoad trống trên: {gameObject.name}");
            return;
        }

        // 1. Khóa tương tác tránh spam click
        if (btnComponent != null) btnComponent.interactable = false;

        // 2. Đảm bảo thời gian chạy bình thường (phòng trường hợp game đang pause)
        Time.timeScale = 1f;

        // 3. Âm thanh và Tracking
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");

        // Tracking chế độ chơi
        AdEventTracker.TrackModeEnter(gameModeID);

        // 4. Đổ dữ liệu vào Settings tĩnh (Để Scene Gameplay đọc được)
        BoomChipSettings.player1Sprite = p1Selection;
        BoomChipSettings.player2Sprite = p2Selection;
        BoomChipSettings.player1HitSFX = p1HitSFX;
        BoomChipSettings.player2HitSFX = p2HitSFX;
        BoomChipSettings.winByHittingThree = setWinByHittingThree;
        BoomChipSettings.isBombModeActive = isBombMode;
        BoomChipSettings.customHitSprite = hitSprite;
        BoomChipSettings.customMissSprite = missSprite;
        BoomChipSettings.missSFXName = missSFX;

        // 5. Xác định Index của Mode để MenuManager cập nhật UI Bottom Bar
        // Giả định: Mode Challenge là index 2, Mode Prediction là index 3
        int modeIndexForUI = 1; // Mặc định
        if (gameModeID == AdEventTracker.GameMode.Challenge) modeIndexForUI = 2;
        else if (gameModeID == AdEventTracker.GameMode.Prediction) modeIndexForUI = 3;

        // 6. Gọi MenuManager để chạy hiệu ứng Transition và Load Scene
        // Ưu tiên dùng Instance nếu MenuManager có Singleton, nếu không dùng Find
        MenuManager menu = MenuManager.Instance != null ? MenuManager.Instance : Object.FindFirstObjectByType<MenuManager>();

        if (menu != null)
        {
            // LƯU Ý: Phải đảm bảo hàm StartAnyScene trong MenuManager là PUBLIC
            menu.StartAnyScene(sceneToLoad, modeIndexForUI);
        }
        else
        {
            // Fallback nếu không tìm thấy MenuManager (Load trực tiếp không hiệu ứng)
            Debug.LogWarning("[LevelButton] Không tìm thấy MenuManager, đang load scene trực tiếp.");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}