using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro; // Thêm thư viện TextMeshPro

public class CoinFlipController : MonoBehaviour
{
    private Animator animator;
    private Image coinImage;
    private BoomChipManager manager;

    [Header("UI Elements")]
    public TextMeshProUGUI flipStatusText; // Kéo Text trong Panel Animation vào đây

    [Header("Sprites Kết Quả")]
    public Sprite p1ResultSprite;
    public Sprite p2ResultSprite;

    void Awake()
    {
        animator = GetComponent<Animator>();
        coinImage = GetComponent<Image>();
    }

    public void StartCoinFlip()
    {
        gameObject.SetActive(true);

        // 1. Hiển thị dòng chữ đang quay ngay lập tức
        if (flipStatusText != null)
        {
            flipStatusText.text = "FLIPPING COIN...";
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetTrigger("StartFlip"); // Kích hoạt animation quay vòng lặp
        }

        StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        // 1. Cập nhật Text ngay khi bắt đầu (như đã thảo luận)
        if (flipStatusText != null) flipStatusText.text = "FLIPPING COIN...";

        // 2. Cho phép quay trong 2 giây
        yield return new WaitForSeconds(2.0f);

        int winnerID = Random.Range(0, 2);

        if (animator != null)
        {
            animator.SetTrigger("StopFlip");

            // QUAN TRỌNG: Vì bật "Has Exit Time", bạn cần chờ một khoảng thời gian 
            // đủ để Animator chạy nốt vòng quay cuối và chuyển về trạng thái nghỉ.
            // Khoảng 0.5s đến 0.8s thường là con số an toàn.
            yield return new WaitForSeconds(0.6f);

            animator.enabled = false;
        }

        // 3. HIỂN THỊ KẾT QUẢ MẶT TĨNH
        if (coinImage != null)
        {
            coinImage.sprite = (winnerID == 0) ? p1ResultSprite : p2ResultSprite;
            coinImage.color = Color.white;
        }

        // 4. Giữ mặt tĩnh lại một chút để người chơi nhận diện P1 hay P2 thắng tung xu
        yield return new WaitForSeconds(0.5f);

        if (manager == null) manager = FindFirstObjectByType<BoomChipManager>();
        if (manager != null) manager.OnCoinFlipFinished(winnerID);
    }
}