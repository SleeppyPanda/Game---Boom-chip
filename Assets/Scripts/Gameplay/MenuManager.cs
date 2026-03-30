using UnityEngine;
using UnityEngine.UI; // Thêm thư viện UI
using DG.Tweening;

public class MenuManager : MonoBehaviour
{
    [Header("Danh sách các Panel (Cần gắn CanvasGroup)")]
    public CanvasGroup panelMode1;
    public CanvasGroup panelMode2;
    public CanvasGroup panelMode3;
    public CanvasGroup panelAccount;
    public CanvasGroup panelSetting;

    [Header("Danh sách các Transform của Button")]
    public RectTransform[] menuButtons; // Đây là Transform để Scale
    public RectTransform btnAccount;
    public RectTransform btnSetting;

    [Header("Cấu hình Sprite Khung (Bottom Bar)")]
    public Sprite frameSelected;   // Kéo Sprite khung Hồng vào đây
    public Sprite frameUnselected; // Kéo Sprite khung Tím vào đây
    private Image[] buttonImages;  // Mảng lưu Image component của 3 nút Mode

    [Header("Cấu hình hiệu ứng")]
    public float fadeDuration = 0.3f;
    private CanvasGroup currentPanel;
    private CanvasGroup previousModePanel;

    void Start()
    {
        // Khởi tạo mảng Image từ menuButtons
        buttonImages = new Image[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
        {
            buttonImages[i] = menuButtons[i].GetComponent<Image>();
        }

        InitPanel(panelMode1);
        InitPanel(panelMode2);
        InitPanel(panelMode3);
        InitPanel(panelAccount);
        InitPanel(panelSetting);

        ShowPanel1(); // Mặc định mở Mode 1
    }

    private void InitPanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0;
        cg.gameObject.SetActive(false);
    }

    public void ShowPanel1() { SwitchToMode(panelMode1, 0); }
    public void ShowPanel2() { SwitchToMode(panelMode2, 1); }
    public void ShowPanel3() { SwitchToMode(panelMode3, 2); }

    public void ShowAccount() { SwitchToExtra(panelAccount, btnAccount, 1.6187f); }
    public void ShowSetting() { SwitchToExtra(panelSetting, btnSetting, 1.0f); }

    // Logic đổi Sprite và Mode
    private void SwitchToMode(CanvasGroup target, int index)
    {
        // 1. Cập nhật Sprite khung cho 3 nút Mode
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] == null) continue;

            if (i == index)
                buttonImages[i].sprite = frameSelected; // Đổi sang hồng
            else
                buttonImages[i].sprite = frameUnselected; // Đổi về tím
        }

        // 2. Hiệu ứng Scale
        AnimateButtonScale(menuButtons[index], 1.35f);

        if (target == null || currentPanel == target) return;

        previousModePanel = target;
        HandleTransition(target);
    }

    private void SwitchToExtra(CanvasGroup target, RectTransform btn, float scale)
    {
        AnimateButtonScale(btn, scale);
        if (target == null || currentPanel == target) return;
        HandleTransition(target);
    }

    private void HandleTransition(CanvasGroup target)
    {
        if (currentPanel != null)
        {
            CanvasGroup oldPanel = currentPanel;
            oldPanel.DOFade(0, fadeDuration);
            oldPanel.transform.DOScale(0.8f, fadeDuration).OnComplete(() => oldPanel.gameObject.SetActive(false));
        }

        target.gameObject.SetActive(true);
        target.DOFade(1, fadeDuration);
        target.transform.localScale = Vector3.one * 0.8f;
        target.transform.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);

        currentPanel = target;
    }

    public void CloseExtraPanel()
    {
        if (currentPanel == null) return;
        CanvasGroup closingPanel = currentPanel;

        closingPanel.DOFade(0, fadeDuration);
        closingPanel.transform.DOScale(0.8f, fadeDuration).OnComplete(() => {
            closingPanel.gameObject.SetActive(false);
            currentPanel = null;
            if (previousModePanel != null) HandleTransition(previousModePanel);
        });
    }

    private void AnimateButtonScale(RectTransform btn, float targetScale)
    {
        if (btn == null) return;
        btn.DOKill(true);
        btn.DOScale(Vector3.one * targetScale, 0.2f); // Đổi sang DOScale mượt mà hơn
        btn.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.25f, 10, 1);
    }
}