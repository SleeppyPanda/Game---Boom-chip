using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DiceTile : MonoBehaviour
{
    public int tileIndex;
    public bool isClaimed = false;
    private Image img;
    private Button btn;
    private Animator anim;

    void Awake()
    {
        img = GetComponent<Image>();
        btn = GetComponent<Button>();
        anim = GetComponent<Animator>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }

        if (anim != null) anim.enabled = false;
    }

    public void SetVisual(Sprite sp, bool isBomb)
    {
        if (img == null) img = GetComponent<Image>();
        if (anim == null) anim = GetComponent<Animator>();

        isClaimed = true;
        SetInteractable(false);

        // Sử dụng Sequence để quản lý các chuyển động không bị chồng chéo
        Sequence flipSeq = DOTween.Sequence();

        // 1. Lật vào giữa (Scale X về 0)
        flipSeq.Append(transform.DOScaleX(0f, 0.15f).SetEase(Ease.InQuad));

        // 2. Đổi nội dung
        flipSeq.AppendCallback(() => UpdateTileContent(sp, isBomb));

        // 3. Lật ra lại (Scale X về 1)
        flipSeq.Append(transform.DOScaleX(1f, 0.25f).SetEase(Ease.OutQuad));

        // 4. Hiệu ứng nảy (Punch) - Chỉ chạy SAU KHI đã lật xong hoàn toàn để tránh lỗi Scale
        flipSeq.Append(transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f, 5, 0.5f));

        // 5. Chốt chặn cuối cùng: Đảm bảo Scale luôn là 1 khi kết thúc mọi thứ
        flipSeq.OnComplete(() => transform.localScale = Vector3.one);
    }

    private void UpdateTileContent(Sprite sp, bool isBomb)
    {
        if (isBomb)
        {
            img.overrideSprite = null;
            img.sprite = sp;

            if (anim != null)
            {
                anim.enabled = true;
                anim.Play("Explosion", 0, 0f);
            }
        }
        else
        {
            if (anim != null) anim.enabled = false;
            img.sprite = sp;
            img.overrideSprite = sp;
        }

        img.color = Color.white;
        img.SetAllDirty();
    }

    public void SetInteractable(bool state)
    {
        if (btn != null) btn.interactable = state;
    }

    private void OnClick()
    {
        if (DiceModeManager.Instance != null && !isClaimed)
        {
            SetInteractable(false);
            DiceModeManager.Instance.OnTileClicked(tileIndex, this);
        }
    }
}