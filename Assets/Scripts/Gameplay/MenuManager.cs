using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Danh sách các Panel (Cần gắn CanvasGroup)")]
    public CanvasGroup panelMode1;
    public CanvasGroup panelMode2;
    public CanvasGroup panelMode3;
    public CanvasGroup panelAccount;
    public CanvasGroup panelSetting;

    [Header("Danh sách các Transform của Button")]
    public RectTransform[] menuButtons;
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

    [Header("Tên Scene Gameplay")]
    public string sceneNameMode2 = "GameplayMode2";
    public string sceneNameMode3 = "GameplayMode3";

    void Start()
    {
        // 1. Dọn dẹp tween cũ
        DOTween.KillAll();

        // 2. Play nhạc nền
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("background_01");
        }

        // 3. Khởi tạo Banner ngay khi vào Menu
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowBanner();
        }

        // 4. Cache UI components
        buttonImages = new Image[menuButtons.Length];
        buttonTexts = new TextMeshProUGUI[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            buttonImages[i] = menuButtons[i].GetComponent<Image>();
            buttonTexts[i] = menuButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        // 5. Khởi tạo trạng thái ẩn cho các Panel
        InitPanel(panelMode1);
        InitPanel(panelMode2);
        InitPanel(panelMode3);
        InitPanel(panelAccount);
        InitPanel(panelSetting);

        // 6. Hiển thị Panel chính
        ShowPanel1();
    }

    private void InitPanel(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.DOKill();
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.transform.localScale = Vector3.one;
        cg.gameObject.SetActive(false);
    }

    public void ShowPanel1()
    {
        // Nếu quay về từ các popup hoặc mode khác, cần dọn dẹp Ads tương ứng
        if (currentPanel == panelMode2) ClosePanelMode2();
        else if (currentPanel == panelMode3) ClosePanelMode3();
        else if (currentPanel == panelAccount || currentPanel == panelSetting) CloseExtraPanel();

        // Panel 1 là menu chính, ẩn MREC để không che nút
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        SwitchToMode(panelMode1, 0, false);
    }

    public void DirectSwitchToMode(int index)
    {
        if (index == 1) SwitchToMode(panelMode2, 1, true);
        else if (index == 2) SwitchToMode(panelMode3, 2, true);
    }

    public void ShowPanel2WithAvatar(Sprite avatar)
    {
        if (UnlockSystemManager.Instance != null)
        {
            UnlockSystemManager.Instance.HandleUnlockFlow("Mode2", "is_show_rw_challenge", avatar, () => {
                SwitchToMode(panelMode2, 1, true);

                // Hiển thị MREC khi chọn Mode 2
                if (AdsManager.Instance != null)
                    AdsManager.Instance.ShowMREC("is_show_mrec_p1_choose");
            });
        }
    }

    public void ShowPanel3WithAvatar(Sprite avatar)
    {
        if (UnlockSystemManager.Instance != null)
        {
            UnlockSystemManager.Instance.HandleUnlockFlow("Mode3", "is_show_rw_prediction", avatar, () => {
                SwitchToMode(panelMode3, 2, true);

                // Hiển thị MREC khi chọn Mode 3
                if (AdsManager.Instance != null)
                    AdsManager.Instance.ShowMREC("is_show_mrec_p1_choose");
            });
        }
    }

    private void SwitchToMode(CanvasGroup target, int index, bool isPopup)
    {
        if (target == null) return;
        HandleButtonAnimationOnly(index);
        HandleTransition(target, !isPopup);
        currentPanel = target;
    }

    public void ClosePanelMode2() { CloseSpecificPopup(panelMode2); }
    public void ClosePanelMode3() { CloseSpecificPopup(panelMode3); }

    private void CloseSpecificPopup(CanvasGroup popup)
    {
        if (popup == null) return;

        // Ẩn MREC khi đóng popup mode để về menu chính sạch sẽ
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        popup.DOKill();
        popup.interactable = false;
        popup.blocksRaycasts = false;

        popup.DOFade(0, fadeDuration);
        popup.transform.DOScale(0.8f, fadeDuration).OnComplete(() => {
            popup.gameObject.SetActive(false);
            currentPanel = panelMode1;

            panelMode1.gameObject.SetActive(true);
            panelMode1.DOKill();
            panelMode1.transform.localScale = Vector3.one;
            panelMode1.alpha = 1;
            panelMode1.interactable = true;
            panelMode1.blocksRaycasts = true;

            HandleButtonAnimationOnly(0);
        });
    }

    public void StartGameMode2() { LoadGameplay(2, sceneNameMode2); }
    public void StartGameMode3() { LoadGameplay(3, sceneNameMode3); }

    private void LoadGameplay(int mode, string sceneName)
    {
        PlayerPrefs.SetInt("SelectedMode", mode);
        PlayerPrefs.Save();

        // Loại bỏ Interstitial theo yêu cầu, chỉ dọn dẹp Ads cũ rồi load scene
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideBanner();
            AdsManager.Instance.HideMREC();
        }

        DOTween.KillAll();
        SceneManager.LoadScene(sceneName);
    }

    public void HandleButtonAnimationOnly(int index)
    {
        if (menuButtons.Length > 0 && menuButtons[0] != null)
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
                menuButtons[i].DOAnchorPosY(200f, 0.25f).SetEase(Ease.OutBack);
                menuButtons[i].DOScale(1.37f, 0.25f).SetEase(Ease.OutBack);
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
                menuButtons[i].DOAnchorPosY(150f, 0.25f);
                menuButtons[i].DOScale(1.15f, 0.25f);
            }
        }
    }

    public void ShowAccount() { SwitchToExtra(panelAccount, btnAccount, 1.6f); }
    public void ShowSetting() { SwitchToExtra(panelSetting, btnSetting, 1.0f); }

    private void SwitchToExtra(CanvasGroup target, RectTransform btn, float scale)
    {
        // Khi mở bảng Account/Setting, tạm ẩn MREC để tránh che giao diện
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        AnimateButtonScale(btn, scale);
        if (target == null || currentPanel == target) return;
        HandleTransition(target, true);
    }

    private void HandleTransition(CanvasGroup target, bool hideOld)
    {
        if (hideOld && currentPanel != null && currentPanel != target)
        {
            CanvasGroup old = currentPanel;
            old.DOKill();
            old.interactable = false;
            old.blocksRaycasts = false;
            old.DOFade(0, fadeDuration);
            old.transform.DOScale(0.8f, fadeDuration).OnComplete(() => old.gameObject.SetActive(false));
        }

        target.gameObject.SetActive(true);
        target.DOKill();
        target.alpha = 0;
        target.transform.localScale = Vector3.one * 0.8f;

        target.DOFade(1, fadeDuration);
        target.transform.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack).OnComplete(() => {
            target.interactable = true;
            target.blocksRaycasts = true;
        });

        currentPanel = target;
    }

    public void CloseExtraPanel()
    {
        if (currentPanel == null || currentPanel == panelMode1) return;

        if (currentPanel == panelAccount && AccountManager.Instance != null)
        {
            AccountManager.Instance.SaveAndExit();
        }

        CanvasGroup closingPanel = currentPanel;
        closingPanel.DOKill();
        closingPanel.interactable = false;
        closingPanel.blocksRaycasts = false;

        AnimateButtonScale(btnAccount, 1.0f);
        AnimateButtonScale(btnSetting, 1.0f);

        closingPanel.DOFade(0, fadeDuration).OnComplete(() => {
            closingPanel.gameObject.SetActive(false);

            currentPanel = panelMode1;
            panelMode1.gameObject.SetActive(true);
            panelMode1.DOKill();
            panelMode1.transform.localScale = Vector3.one;
            panelMode1.alpha = 1;
            panelMode1.interactable = true;
            panelMode1.blocksRaycasts = true;

            // Đảm bảo Banner hiện lại khi quay về menu chính
            if (AdsManager.Instance != null) AdsManager.Instance.ShowBanner();
        });
    }

    private void AnimateButtonScale(RectTransform btn, float targetScale)
    {
        if (btn == null) return;
        btn.DOKill(true);
        btn.DOScale(Vector3.one * targetScale, 0.2f);
    }
}