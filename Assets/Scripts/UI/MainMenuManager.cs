using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Tên của Scene chọn game (nhớ đặt tên đúng với trong Unity)
    private string selectSceneName = "SelectScene";

    public void GoToSelectScene()
    {
        Debug.Log("Đang chuyển sang màn hình Chọn Game...");
        SceneManager.LoadScene(selectSceneName);
    }

    public void OpenSettings()
    {
        Debug.Log("Mở bảng Cài đặt (Bật/tắt âm thanh, rung...)");
        // Gọi UI Panel Settings ra (sẽ code thêm nếu cần)
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game!");
        Application.Quit();
    }
}