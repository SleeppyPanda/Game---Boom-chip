using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BottleController : MonoBehaviour
{
    public GameObject bottomSmoke; // Kéo BottomAnimation vào đây
    public GameObject topSmoke;    // Kéo TopAnimation vào đây
    public Image bottleImage;      // Kéo GameObject 'Bottle' vào đây

    // Tầng dưới: Hô biến khói trắng và đổi màu
    public void PlayLowerSmoke(Sprite newSprite)
    {
        if (bottomSmoke != null)
        {
            bottomSmoke.SetActive(false);
            bottomSmoke.SetActive(true); // Animator tự chạy vì là Default State
        }

        if (newSprite != null)
        {
            // Đợi 0.15s (lúc khói bùng to nhất che chai) rồi mới đổi Sprite
            StartCoroutine(DelayedChange(newSprite));
        }
    }

    // Tầng trên: Khói từ bàn khi đặt xuống
    public void PlayUpperLand()
    {
        if (topSmoke != null)
        {
            topSmoke.SetActive(false);
            topSmoke.SetActive(true);
        }
    }

    IEnumerator DelayedChange(Sprite s)
    {
        yield return new WaitForSeconds(0.15f);
        if (bottleImage != null) bottleImage.sprite = s;
    }
}