using UnityEngine;
using TMPro; // Bắt buộc phải có để điều khiển TextMeshPro
using UnityEngine.UI;

public class NameManager : MonoBehaviour
{
    [Header("Giao diện")]
    public TMP_InputField nameInputField; // Kéo InputField vào đây
    public TextMeshProUGUI displayNameText; // (Tùy chọn) Nếu bạn muốn hiển thị tên ở các màn hình khác

    private const string NAME_KEY = "PlayerName";

    void Start()
    {
        // Load tên đã lưu khi vừa vào game, nếu chưa có thì mặc định là "Alex"
        string savedName = PlayerPrefs.GetString(NAME_KEY, "Alex");
        nameInputField.text = savedName;

        if (displayNameText != null)
            displayNameText.text = savedName;

        // Lắng nghe sự kiện khi người dùng nhập xong và nhấn Enter hoặc thoát Focus
        nameInputField.onEndEdit.AddListener(SaveName);
    }

    public void SaveName(string newName)
    {
        // Kiểm tra nếu tên không trống mới lưu
        if (!string.IsNullOrEmpty(newName))
        {
            PlayerPrefs.SetString(NAME_KEY, newName);
            PlayerPrefs.Save();

            if (displayNameText != null)
                displayNameText.text = newName;

            Debug.Log("Đã lưu tên mới: " + newName);
        }
    }

    // Hàm gọi khi bấm vào nút cái bút để tập trung vào ô nhập (Focus)
    public void ClickRenameButton()
    {
        nameInputField.ActivateInputField();
    }
}