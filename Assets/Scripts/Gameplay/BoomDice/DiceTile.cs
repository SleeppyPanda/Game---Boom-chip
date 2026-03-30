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

        // Mặc định tắt Animator khi bắt đầu để tránh tiêu tốn hiệu năng
        if (anim != null) anim.enabled = false;
    }

    /// <summary>
    /// Thiết lập hiển thị cho ô (Visual)
    /// </summary>
    /// <param name="sp">Sprite của người chơi hoặc sprite quả bom</param>
    /// <param name="isBomb">Ô này có phải là bom không</param>
    public void SetVisual(Sprite sp, bool isBomb)
    {
        if (img == null) img = GetComponent<Image>();
        if (anim == null) anim = GetComponent<Animator>();

        if (isBomb)
        {
            // --- TRƯỜNG HỢP Ô BOMB ---
            isClaimed = true;

            // 1. Xóa override để Animator có thể điều khiển Sprite trong Clip nổ (nếu có)
            img.overrideSprite = null;

            // 2. Gán sprite gốc là hình quả bom để sau khi nổ xong nó hiện đúng hình bom
            img.sprite = sp;

            // 3. Kích hoạt Animator và chạy state "Explosion"
            if (anim != null)
            {
                anim.enabled = true;
                anim.Play("Explosion", 0, 0f);
            }

            // GHI CHÚ: Đã loại bỏ Destroy(gameObject) để không làm hỏng GridLayoutGroup.
            // Ô bom sẽ đứng yên tại vị trí cũ sau khi nổ xong.
        }
        else
        {
            // --- TRƯỜNG HỢP Ô THƯỜNG ---
            isClaimed = true;

            // 1. Đảm bảo Animator tắt để không chạy nhầm hiệu ứng nổ
            if (anim != null) anim.enabled = false;

            // 2. Reset Scale về 1 (phòng trường hợp Animator trước đó làm lệch scale)
            transform.localScale = Vector3.one;

            // 3. Gán hình ảnh của Player (Màu sắc/Chip)
            img.sprite = sp;
            img.overrideSprite = sp;
        }

        // Đảm bảo màu sắc hiển thị rõ ràng
        img.color = Color.white;
        img.SetAllDirty();
    }

    /// <summary>
    /// Bật/Tắt khả năng tương tác của nút
    /// </summary>
    public void SetInteractable(bool state)
    {
        if (btn != null) btn.interactable = state;
    }

    /// <summary>
    /// Xử lý khi người dùng click vào ô
    /// </summary>
    private void OnClick()
    {
        if (DiceModeManager.Instance != null && !isClaimed)
        {
            DiceModeManager.Instance.OnTileClicked(tileIndex, this);
        }
    }
}