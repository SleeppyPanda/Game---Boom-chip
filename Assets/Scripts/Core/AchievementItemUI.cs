using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AchievementItemUI : MonoBehaviour
{
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtDescription;
    public Image imgIcon; // Kéo thành phần UI Image của ô danh hiệu vào đây

    public void Setup(string name, string desc, Sprite icon, bool isUnlocked)
    {
        txtName.text = name;
        txtDescription.text = desc;

        if (imgIcon != null && icon != null)
        {
            imgIcon.sprite = icon;

            // Nếu chưa mở khóa: Làm mờ ảnh và đổi màu chữ sang xám
            if (!isUnlocked)
            {
                Color c = imgIcon.color;
                c.a = 0.2f; // Độ mờ khi chưa đạt được
                imgIcon.color = c;

                txtName.color = Color.gray;
                txtDescription.color = Color.gray;
            }
            else
            {
                // Nếu đã mở khóa: Để ảnh rõ nét
                Color c = imgIcon.color;
                c.a = 1.0f;
                imgIcon.color = c;

                txtName.color = Color.white;
                txtDescription.color = Color.white;
            }
        }
    }
}