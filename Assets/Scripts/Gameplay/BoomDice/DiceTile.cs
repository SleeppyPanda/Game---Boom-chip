using UnityEngine;
using UnityEngine.UI;

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
    }

    public void SetVisual(Sprite sp, bool isBomb)
    {
        if (img == null) img = GetComponent<Image>();
        if (anim == null) anim = GetComponent<Animator>();

        if (isBomb)
        {
            // Ô BOM: 
            // 1. Giải phóng override để Animator chạy Sprite trong Clip Explosion
            img.overrideSprite = null;

            // 2. Kích hoạt Animator và chạy clip nổ
            if (anim != null)
            {
                anim.enabled = true;
                anim.Play("Explosion", 0, 0f);
            }

            // 3. Tự hủy sau 1 khoảng thời gian (ví dụ 1 giây để kịp chạy xong animation)
            Destroy(gameObject, 1.0f);
        }
        else
        {
            // Ô THƯỜNG:
            // 1. Tắt Animator để nó không chạy linh tinh (chặn lỗi ô thường cũng nổ)
            if (anim != null) anim.enabled = false;

            // 2. Ép hình Player lên bằng override
            img.overrideSprite = sp;
            img.sprite = sp;
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
        if (DiceModeManager.Instance != null)
            DiceModeManager.Instance.OnTileClicked(tileIndex, this);
    }
}