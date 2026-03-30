using UnityEngine;

public class Mode3DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Khi item rơi vào vùng này
        if (other.CompareTag("Player") || other.GetComponent<Mode3Item>() != null)
        {
            Mode3Manager.Instance.FinishGame();
        }
    }
}