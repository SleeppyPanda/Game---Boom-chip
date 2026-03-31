using UnityEngine;
public class FireworkAutoDisable : MonoBehaviour
{
    public float duration = 2.5f;

    void OnEnable()
    {
        // Tự động tắt chính nó sau một khoảng thời gian
        Invoke("DisablePanel", duration);
    }

    void DisablePanel()
    {
        gameObject.SetActive(false);
    }
}