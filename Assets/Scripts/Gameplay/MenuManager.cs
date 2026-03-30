using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    public RectTransform[] menuButtons; // Index 0: Mode 1, 1: Mode 2, 2: Mode 3
    public RectTransform btnAccount;
    public RectTransform btnSetting;

    [Header("Cấu hình Sprite Khung (Bottom Bar)")]
    public Sprite frameSelected;
    public Sprite frameUnselected;
    private Image[] buttonImages;
    private TextMeshProUGUI[] buttonTexts;

    [Header("Cấu hình hiệu ứng")]
    public float fadeDuration = 0.3f;
    private CanvasGroup currentPanel;
    private CanvasGroup previousModePanel;

    void Start()
    {
        buttonImages = new Image[menuButtons.Length];
        buttonTexts = new TextMeshProUGUI[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            buttonImages[i] = menuButtons[i].GetComponent<Image>();
            buttonTexts[i] = menuButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        InitPanel(panelMode1);
        InitPanel(panelMode2);
        InitPanel(panelMode3);
        InitPanel(panelAccount);
        InitPanel(panelSetting);

        ShowPanel1();
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

    private void SwitchToMode(CanvasGroup target, int index)
    {
        // --- LOGIC DỊCH CHUYỂN POS X CHO MODE 1 ---
        // Giả sử menuButtons[0] luôn là Mode 1
        if (menuButtons[0] != null)
        {
            if (index == 1) // Nếu chọn Mode 2
            {
                menuButtons[0].DOAnchorPosX(29f, 0.25f);
            }
            else if (index == 2) // Nếu chọn Mode 3
            {
                menuButtons[0].DOAnchorPosX(-29f, 0.25f);
            }
            else if (index == 0) // Nếu chọn chính Mode 1 (về lại vị trí mặc định của bạn)
            {
                // Bạn có thể để 0 hoặc giá trị pos X mặc định mà bạn đã thiết lập ban đầu
                menuButtons[0].DOAnchorPosX(0f, 0.25f);
            }
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            // Không DOKill toàn bộ để tránh ngắt quãng DOAnchorPosX của Mode 1 ở trên
            menuButtons[i].DOScale(menuButtons[i].localScale, 0);

            if (i == index)
            {
                if (buttonImages[i] != null) buttonImages[i].sprite = frameSelected;
                if (buttonTexts[i] != null) buttonTexts[i].color = Color.white;

                menuButtons[i].DOAnchorPosY(69f, 0.25f).SetEase(Ease.OutBack);
                menuButtons[i].DOScale(1.374428f, 0.25f).SetEase(Ease.OutBack);
            }
            else
            {
                if (buttonImages[i] != null) buttonImages[i].sprite = frameUnselected;

                if (buttonTexts[i] != null)
                {
                    Color unselectedColor;
                    ColorUtility.TryParseHtmlString("#CCABFC", out unselectedColor);
                    buttonTexts[i].color = unselectedColor;
                }

                menuButtons[i].DOAnchorPosY(22f, 0.25f);
                menuButtons[i].DOScale(1.1591f, 0.25f);
            }
        }

        if (target == null || currentPanel == target) return;
        previousModePanel = target;
        HandleTransition(target);
    }

    // Các hàm còn lại giữ nguyên...
    public void ShowAccount() { SwitchToExtra(panelAccount, btnAccount, 1.6187f); }
    public void ShowSetting() { SwitchToExtra(panelSetting, btnSetting, 1.0f); }

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
        btn.DOScale(Vector3.one * targetScale, 0.2f);
    }

    // Thêm hàm này vào MenuManager.cs
    public void HandleButtonAnimationOnly(int index)
    {
        // Copy logic xử lý visual từ SwitchToMode sang đây
        // (Bao gồm DOAnchorPosX cho Mode 1, Scale, Sprite, Text Color...)

        // Logic dịch chuyển Mode 1
        if (menuButtons[0] != null)
        {
            if (index == 1) menuButtons[0].DOAnchorPosX(29f, 0.25f);
            else if (index == 2) menuButtons[0].DOAnchorPosX(-29f, 0.25f);
            else if (index == 0) menuButtons[0].DOAnchorPosX(0f, 0.25f);
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            if (i == index)
            {
                if (buttonImages[i] != null) buttonImages[i].sprite = frameSelected;
                if (buttonTexts[i] != null) buttonTexts[i].color = Color.white;
                menuButtons[i].DOAnchorPosY(69f, 0.25f).SetEase(DG.Tweening.Ease.OutBack);
                menuButtons[i].DOScale(1.374428f, 0.25f).SetEase(DG.Tweening.Ease.OutBack);
            }
            else
            {
                if (buttonImages[i] != null) buttonImages[i].sprite = frameUnselected;
                if (buttonTexts[i] != null)
                {
                    Color unselectedColor;
                    ColorUtility.TryParseHtmlString("#CCABFC", out unselectedColor);
                    buttonTexts[i].color = unselectedColor;
                }
                menuButtons[i].DOAnchorPosY(22f, 0.25f);
                menuButtons[i].DOScale(1.1591f, 0.25f);
            }
        }
    }
}