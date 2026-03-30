using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelButton : MonoBehaviour
{
    public TextMeshProUGUI gameNameText; // Kéo Text tên game vào đây
    public string sceneToLoad;           // Nhập tên Scene chính xác vào Inspector

    public void OnClickSelect()
    {
        Time.timeScale = 1f; // Đảm bảo thời gian chạy bình thường
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}