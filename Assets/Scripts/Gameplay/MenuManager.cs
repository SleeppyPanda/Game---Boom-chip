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

    [Header("Tên Scene Gameplay")]
    public string sceneNameMode2 = "GameplayMode2";
    public string sceneNameMode3 = "GameplayMode3";

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic("background_01");
        }

        buttonImages = new Image[menuButtons.Length];
        buttonTexts = new TextMeshProUGUI[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
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

    // --- HÀM SHOW PANEL (CÓ KIỂM TRA UNLOCK & REMOTE CONFIG) ---
    public void ShowPanel1()
    {
        if (currentPanel == panelMode2) ClosePanelMode2();
        else if (currentPanel == panelMode3) ClosePanelMode3();

        SwitchToMode(panelMode1, 0, false);

        // Firebase: Tracking vào màn hình chính (Mode 1) sử dụng API mới LogModeEnter
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(1);
    }

    public void ShowPanel2WithAvatar(Sprite avatar)
    {
        bool isConfigOpen = AdsManager.Instance != null ? AdsManager.Instance.IsShowRwChallenge : true;

        if (!isConfigOpen)
        {
            SwitchToMode(panelMode2, 1, true);
        }
        else
        {
            CheckUnlockAndShow("Mode2", panelMode2, 1, avatar, "Challenge");
        }
    }

    public void ShowPanel3WithAvatar(Sprite avatar)
    {
        bool isConfigOpen = AdsManager.Instance != null ? AdsManager.Instance.IsShowRwPrediction : true;

        if (!isConfigOpen)
        {
            SwitchToMode(panelMode3, 2, true);
        }
        else
        {
            CheckUnlockAndShow("Mode3", panelMode3, 2, avatar, "Prediction");
        }
    }

    private void CheckUnlockAndShow(string itemID, CanvasGroup targetPanel, int index, Sprite avatarSprite, string adType)
    {
        bool isUnlocked = UnlockSystemManager.Instance != null && UnlockSystemManager.Instance.IsItemUnlocked(itemID);

        if (isUnlocked)
        {
            SwitchToMode(targetPanel, index, true);
        }
        else
        {
            if (UnlockSystemManager.Instance != null)
            {
                UnlockSystemManager.Instance.OpenUnlockPopup(itemID, avatarSprite, () => {
                    SwitchToMode(targetPanel, index, true);
                });
            }
            else
            {
                SwitchToMode(targetPanel, index, true);
            }
        }
    }

    private void SwitchToMode(CanvasGroup target, int index, bool isPopup)
    {
        HandleButtonAnimationOnly(index);

        if (target == null || currentPanel == target) return;

        HandleTransition(target, !isPopup);
        currentPanel = target;

        // Firebase: Tracking count_mode_xx (xx là 01, 02, 03...) sử dụng API mới LogModeEnter
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(index + 1);
    }

    // --- HÀM ĐÓNG POPUP ---
    public void ClosePanelMode2() { CloseSpecificPopup(panelMode2); }
    public void ClosePanelMode3() { CloseSpecificPopup(panelMode3); }

    private void CloseSpecificPopup(CanvasGroup popup)
    {
        if (popup == null) return;
        popup.DOKill();
        popup.DOFade(0, fadeDuration);
        popup.transform.DOScale(0.8f, fadeDuration).OnComplete(() => {
            popup.gameObject.SetActive(false);
            currentPanel = panelMode1;
            HandleButtonAnimationOnly(0);
        });
    }

    // --- HÀM CHUYỂN SCENE ---
    public void StartGameMode2() { LoadGameplay(2, sceneNameMode2); }
    public void StartGameMode3() { LoadGameplay(3, sceneNameMode3); }

    private void LoadGameplay(int mode, string sceneName)
    {
        PlayerPrefs.SetInt("SelectedMode", mode);
        PlayerPrefs.SetInt("CurrentLevel", 1);
        PlayerPrefs.Save();

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowInterstitial("is_show_inter_p1_choose", () => {
                DOTween.KillAll();
                SceneManager.LoadScene(sceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // --- LOGIC ANIMATION BOTTOM BAR ---
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

                menuButtons[i].DOAnchorPosY(150f, 0.25f);
                menuButtons[i].DOScale(1.1591f, 0.25f);
            }
        }
    }

    // --- ACCOUNT & SETTING ---
    public void ShowAccount() { SwitchToExtra(panelAccount, btnAccount, 1.6187f); }
    public void ShowSetting() { SwitchToExtra(panelSetting, btnSetting, 1.0f); }

    private void SwitchToExtra(CanvasGroup target, RectTransform btn, float scale)
    {
        AnimateButtonScale(btn, scale);
        if (target == null || currentPanel == target) return;
        HandleTransition(target, true);
    }

    private void HandleTransition(CanvasGroup target, bool hideOld)
    {
        if (hideOld && currentPanel != null)
        {
            CanvasGroup old = currentPanel;
            old.DOKill();
            old.DOFade(0, fadeDuration);
            old.transform.DOScale(0.8f, fadeDuration).OnComplete(() => old.gameObject.SetActive(false));
        }

        target.gameObject.SetActive(true);
        target.DOKill();
        target.DOFade(1, fadeDuration);
        target.transform.localScale = Vector3.one * 0.8f;
        target.transform.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack);
        currentPanel = target;
    }

    public void CloseExtraPanel()
    {
        if (currentPanel == null) return;
        CanvasGroup closingPanel = currentPanel;
        closingPanel.DOKill();
        closingPanel.DOFade(0, fadeDuration).OnComplete(() => {
            closingPanel.gameObject.SetActive(false);
            currentPanel = panelMode1;
            panelMode1.gameObject.SetActive(true);
            panelMode1.DOFade(1, fadeDuration);
        });
    }

    private void AnimateButtonScale(RectTransform btn, float targetScale)
    {
        if (btn == null) return;
        btn.DOKill(true);
        btn.DOScale(Vector3.one * targetScale, 0.2f);
    }
}