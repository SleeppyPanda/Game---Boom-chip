using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TileButton : MonoBehaviour
{
    [Header("Cấu hình ô")]
    public int tileIndex;
    public int ownerID;
    public int boardID;

    private Button button;
    private Image buttonImage;
    private BoomChipManager manager;
    private Sprite originalSprite;
    private Vector3 initialScale;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        initialScale = transform.localScale;

        if (buttonImage != null)
        {
            originalSprite = buttonImage.sprite;
        }

        manager = FindFirstObjectByType<BoomChipManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
    }

    void Start()
    {
        ApplyInitialSkin();
    }

    private void ApplyInitialSkin()
    {
        if (buttonImage != null && originalSprite != null)
        {
            buttonImage.sprite = originalSprite;
        }
    }

    void OnClicked()
    {
        if (manager == null) return;

        if (manager.currentPhase == GamePhase.Phase1 || manager.currentPhase == GamePhase.Phase2)
        {
            int currentPlayerSettingBombs = (manager.currentPhase == GamePhase.Phase1) ? 1 : 2;
            if (ownerID == currentPlayerSettingBombs)
            {
                manager.HandleTileClick(tileIndex, this);
            }
        }
        else if (manager.currentPhase == GamePhase.Phase3)
        {
            manager.ExecuteTurn(tileIndex, this, ownerID, boardID);
        }
    }

    public void SetVisual(Sprite newSprite)
    {
        if (buttonImage == null) return;

        // Xác định Sprite mục tiêu: Nếu newSprite null thì lấy lại hình gốc (originalSprite)
        Sprite targetSprite = (newSprite == null) ? originalSprite : newSprite;

        transform.DOKill();
        Sequence flipSeq = DOTween.Sequence();

        // 1. Lật vào (Scale X về 0)
        flipSeq.Append(transform.DOScaleX(0f, 0.12f).SetEase(Ease.InQuad));

        // 2. Đổi hình ảnh ở giữa quá trình lật
        flipSeq.AppendCallback(() =>
        {
            buttonImage.sprite = targetSprite;
        });

        // 3. Lật ra (Scale X về giá trị ban đầu của board đó)
        flipSeq.Append(transform.DOScaleX(initialScale.x, 0.2f).SetEase(Ease.OutQuad));

        // 4. Hiệu ứng nảy (Punch) tạo cảm giác vật lý
        Vector3 punchAmount = new Vector3(0.12f * initialScale.x, 0.12f * initialScale.y, 0.12f);
        flipSeq.Append(transform.DOPunchScale(punchAmount, 0.25f, 6, 0.5f));

        // 5. Đảm bảo Scale chuẩn sau khi kết thúc
        flipSeq.OnComplete(() =>
        {
            transform.localScale = initialScale;
        });
    }

    public void ResetTile(Sprite defaultSprite = null)
    {
        transform.DOKill();
        transform.localScale = initialScale;
        buttonImage.sprite = (defaultSprite == null) ? originalSprite : defaultSprite;
        SetInteractable(true);
    }

    public void SetInteractable(bool state)
    {
        if (button != null) button.interactable = state;
    }
}