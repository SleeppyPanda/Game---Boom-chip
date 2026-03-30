using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    public Renderer bgRenderer;
    public float speedX; // Tốc độ theo chiều ngang
    public float speedY; // Tốc độ theo chiều dọc

    void Update()
    {
        // Tạo vector di chuyển chéo bằng cách kết hợp X và Y
        Vector2 offset = new Vector2(Time.time * speedX, Time.time * speedY);

        // Gán offset vào material
        bgRenderer.material.mainTextureOffset = offset;
    }
}