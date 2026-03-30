using UnityEngine;
using DG.Tweening; //

public class MenuManager : MonoBehaviour
{
    [Header("Danh sách các Panel (Cần gắn CanvasGroup)")]
    public CanvasGroup panelMode1;
    public CanvasGroup panelMode2;
    public CanvasGroup panelMode3;

    [Header("Danh sách các Transform của Button")]
    public RectTransform[] menuButtons; // Kéo 3 Button vào đây

    [Header("Cấu hình hiệu ứng")]
    public float fadeDuration = 0.3f;
    private CanvasGroup currentPanel;

    void Start()
    {
        // Ẩn hết ban đầu
        InitPanel(panelMode1);
        InitPanel(panelMode2);
        InitPanel(panelMode3);

        ShowPanel1();
    }

    private void InitPanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0;
        cg.gameObject.SetActive(false);
    }

    public void ShowPanel1() { SwitchTo(panelMode1, 0); }
    public void ShowPanel2() { SwitchTo(panelMode2, 1); }
    public void ShowPanel3() { SwitchTo(panelMode3, 2); }

    private void SwitchTo(CanvasGroup target, int btnIndex)
    {
        // 1. LUÔN LUÔN chạy hiệu ứng nút (đã sửa để không bị thu nhỏ luôn)
        AnimateButton(btnIndex);

        // 2. Kiểm tra nếu đang ở đúng trang đó thì không chạy animation Panel nữa
        if (target == null || currentPanel == target) return;

        // 3. Chuyển trang Panel
        FadeOut(panelMode1);
        FadeOut(panelMode2);
        FadeOut(panelMode3);

        target.gameObject.SetActive(true);
        target.DOFade(1, fadeDuration);
        target.transform.localScale = Vector3.one * 0.8f;
        target.transform.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);

        currentPanel = target;
    }

    private void FadeOut(CanvasGroup cg)
    {
        if (cg != null && cg.gameObject.activeSelf)
        {
            cg.DOFade(0, fadeDuration).OnComplete(() => cg.gameObject.SetActive(false));
        }
    }

    private void AnimateButton(int index)
    {
        if (index >= 0 && index < menuButtons.Length && menuButtons[index] != null)
        {
            // 1. Dừng mọi hiệu ứng đang chạy để tránh cộng dồn
            menuButtons[index].DOKill(true);

            // 2. ÉP RESET về đúng scale 1.35 thay vì 1
            Vector3 defaultScale = new Vector3(1.35f, 1.35f, 1.35f);
            menuButtons[index].localScale = defaultScale;

            // 3. Chạy hiệu ứng nảy từ mốc 1.35
            // Vector3(0.2f, 0.2f, 0) là độ lớn cú nảy cộng thêm vào 1.35
            menuButtons[index].DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.25f, 10, 1);
        }
    }
}