using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NameManager : MonoBehaviour
{
    [Header("Giao diện")]
    public TMP_InputField nameInputField;
    public TextMeshProUGUI displayNameText;

    // Sử dụng chung Key với AccountManager để đồng bộ dữ liệu
    private const string NAME_KEY = "PlayerName";

    void Start()
    {
        LoadAndDisplay();

        if (nameInputField != null)
        {
            nameInputField.onEndEdit.RemoveAllListeners();
            nameInputField.onEndEdit.AddListener(SaveName);
            // Mặc định tắt để tránh hiện bàn phím sai lúc
            nameInputField.interactable = false;
        }
    }

    private void OnEnable()
    {
        // Cập nhật lại mỗi khi Panel được bật lên
        LoadAndDisplay();
    }

    private void LoadAndDisplay()
    {
        string savedName = PlayerPrefs.GetString(NAME_KEY, "Player 1");

        if (nameInputField != null) nameInputField.text = savedName;
        if (displayNameText != null) displayNameText.text = savedName;
    }

    public void SaveName(string newName)
    {
        // Loại bỏ khoảng trắng thừa
        string cleanName = newName.Trim();

        if (!string.IsNullOrEmpty(cleanName))
        {
            PlayerPrefs.SetString(NAME_KEY, cleanName);
            PlayerPrefs.Save();

            if (displayNameText != null)
            {
                displayNameText.text = cleanName;
            }

            Debug.Log("Đã lưu tên mới: " + cleanName);
        }
        else
        {
            // Nếu người dùng xóa hết tên, trả về tên cũ đã lưu
            if (nameInputField != null)
                nameInputField.text = PlayerPrefs.GetString(NAME_KEY, "Player 1");
        }

        // Khóa input sau khi chỉnh sửa xong
        if (nameInputField != null) nameInputField.interactable = false;
    }

    public void ClickRenameButton()
    {
        if (nameInputField != null)
        {
            nameInputField.interactable = true;
            nameInputField.ActivateInputField();

            // Di chuyển con trỏ về cuối dòng cho tiện chỉnh sửa
            nameInputField.caretPosition = nameInputField.text.Length;
        }
    }
}