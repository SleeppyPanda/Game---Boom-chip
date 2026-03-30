using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public string sceneToLoad;

    [Header("Cấu hình truyền đi")]
    public bool setWinByHittingThree = true;
    public Sprite hitSprite;
    public Sprite missSprite;

    public void OnClickSelect()
    {
        Time.timeScale = 1f;

        // Lưu dữ liệu vào class trung gian trước khi Load Scene
        BoomChipSettings.winByHittingThree = setWinByHittingThree;
        BoomChipSettings.customHitSprite = hitSprite;
        BoomChipSettings.customMissSprite = missSprite;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}