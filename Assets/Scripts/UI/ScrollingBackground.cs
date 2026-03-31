using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    public Renderer bgRenderer;
    public float speedX; // Tốc độ theo chiều ngang
    public float speedY; // Tốc độ theo chiều dọc

    void Update()
    {
        // Sử dụng toán tử % 1.0f để giữ giá trị luôn trong khoảng 0-1
        // Điều này giúp tránh lỗi sai số dấu phẩy động (Floating Point Precision Error)
        float offsetX = (Time.time * speedX) % 1.0f;
        float offsetY = (Time.time * speedY) % 1.0f;

        Vector2 offset = new Vector2(offsetX, offsetY);

        // Gán offset vào material
        bgRenderer.material.mainTextureOffset = offset;
    }
}