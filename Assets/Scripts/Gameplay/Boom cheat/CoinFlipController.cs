using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class CoinFlipController : MonoBehaviour
{
    private Animator animator;
    private Image coinImage;
    private BoomChipManager manager;
    private RectTransform rectTransform;

    [Header("UI Elements")]
    public TextMeshProUGUI flipStatusText;

    [Header("Sprites Kết Quả")]
    public Sprite p1ResultSprite;
    public Sprite p2ResultSprite;

    void Awake()
    {
        animator = GetComponent<Animator>();
        coinImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>(); // Lấy RectTransform để xử lý rotation chính xác trong UI
    }

    public void StartCoinFlip()
    {
        gameObject.SetActive(true);

        if (flipStatusText != null)
        {
            flipStatusText.text = "FLIPPING COIN...";
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetTrigger("StartFlip");
        }

        StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        if (flipStatusText != null) flipStatusText.text = "FLIPPING COIN...";

        // 1. Cho phép quay trong 2 giây
        yield return new WaitForSeconds(2.0f);

        int winnerID = Random.Range(0, 2);

        if (animator != null)
        {
            animator.SetTrigger("StopFlip");

            // Chờ Animator hoàn tất vòng quay cuối (Exit Time)
            yield return new WaitForSeconds(0.1f);

            // Tắt Animator để quyền điều khiển transform quay về script
            animator.enabled = false;
        }

        // 2. GHI ĐÈ ROTATION: Đưa về trạng thái phẳng (0,0,0)
        // Điều này cực kỳ quan trọng để tránh đồng xu bị đứng ở góc 90 độ (không thấy hình)
        if (rectTransform != null)
        {
            rectTransform.localRotation = Quaternion.identity;
            // Quaternion.identity tương đương với Rotation(0, 0, 0)
        }

        // 3. HIỂN THỊ KẾT QUẢ MẶT TĨNH
        if (coinImage != null)
        {
            coinImage.sprite = (winnerID == 0) ? p1ResultSprite : p2ResultSprite;
            coinImage.color = Color.white;
        }

        // 4. Giữ mặt tĩnh lại một chút để người chơi nhận diện
        yield return new WaitForSeconds(0.5f);

        if (manager == null) manager = FindFirstObjectByType<BoomChipManager>();
        if (manager != null) manager.OnCoinFlipFinished(winnerID);
    }
}