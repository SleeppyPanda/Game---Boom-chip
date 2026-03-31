using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    // Singleton để các Script khác gọi nhanh qua MenuManager.Instance
    public static MenuManager Instance;

    [Header("Danh sách các Panel (Cần gắn CanvasGroup)")]
    public CanvasGroup panelMode1;
    public CanvasGroup panelMode2;
    public CanvasGroup panelMode3;
    public CanvasGroup panelAccount;
    public CanvasGroup panelSetting;

    [Header("Danh sách các Transform của Button (Bottom Bar)")]
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
    private bool isTransitioning = false;

    [Header("Tên Scene Gameplay")]
    public string sceneNameMode2 = "GameplayMode2";
    public string sceneNameMode3 = "GameplayMode3";

    [Header("Transition Panel (Logic Animation)")]
    public GameObject panelTransition;
    public RectTransform leftStrip;
    public RectTransform rightStrip;
    public RectTransform leftObj;
    public RectTransform rightObj;
    public CanvasGroup centerLogo;

    private Vector2 _leftStripOrig, _rightStripOrig, _leftObjOrig, _rightObjOrig;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        DOTween.KillAll();

        // Khởi tạo vị trí gốc cho hiệu ứng transition
        if (leftStrip != null) _leftStripOrig = leftStrip.anchoredPosition;
        if (rightStrip != null) _rightStripOrig = rightStrip.anchoredPosition;
        if (leftObj != null) _leftObjOrig = leftObj.anchoredPosition;
        if (rightObj != null) _rightObjOrig = rightObj.anchoredPosition;

        if (panelTransition != null)
        {
            panelTransition.SetActive(false);
            if (centerLogo != null) { centerLogo.DOKill(); centerLogo.transform.localScale = Vector3.zero; centerLogo.alpha = 0; }
        }

        // Âm thanh và Ads
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic("background_01");
        if (AdsManager.Instance != null) AdsManager.Instance.ShowBanner();

        // Khởi tạo mảng UI Bottom Bar
        buttonImages = new Image[menuButtons.Length];
        buttonTexts = new TextMeshProUGUI[menuButtons.Length];
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            buttonImages[i] = menuButtons[i].GetComponent<Image>();
            buttonTexts[i] = menuButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        // Reset tất cả các panel về trạng thái ẩn
        InitPanel(panelMode1);
        InitPanel(panelMode2);
        InitPanel(panelMode3);
        InitPanel(panelAccount);
        InitPanel(panelSetting);

        isTransitioning = false;

        // Luôn bắt đầu ở Panel chính (Mode 1)
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

    #region PANEL NAVIGATION

    public void ShowPanel1()
    {
        if (currentPanel == panelAccount || currentPanel == panelSetting)
            CloseExtraPanel();
        else if (currentPanel != panelMode1)
            SwitchToMode(panelMode1, 0, false);
    }

    /// <summary>
    /// Chuyển hướng hiển thị UI Mode (Dành cho việc hiện Panel sau khi mở khóa)
    /// </summary>
    public void DirectSwitchToMode(int index)
    {
        if (index == 0) SwitchToMode(panelMode1, 0, false);
        else if (index == 1) SwitchToMode(panelMode2, 1, true);
        else if (index == 2) SwitchToMode(panelMode3, 2, true);
    }

    private void SwitchToMode(CanvasGroup target, int index, bool isPopup)
    {
        if (target == null) return;
        HandleButtonAnimationOnly(index);
        HandleTransition(target, !isPopup);
        currentPanel = target;
    }

    public void ClosePanelMode2() => CloseSpecificPopup(panelMode2);
    public void ClosePanelMode3() => CloseSpecificPopup(panelMode3);

    private void CloseSpecificPopup(CanvasGroup popup)
    {
        if (popup == null) return;
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        popup.DOKill();
        popup.interactable = false;
        popup.blocksRaycasts = false;
        popup.DOFade(0, fadeDuration);
        popup.transform.DOScale(0.8f, fadeDuration).OnComplete(() => {
            popup.gameObject.SetActive(false);

            // Reset về Mode 1
            currentPanel = panelMode1;
            panelMode1.gameObject.SetActive(true);
            panelMode1.alpha = 1;
            panelMode1.interactable = true;
            panelMode1.blocksRaycasts = true;
            HandleButtonAnimationOnly(0);
        });
    }
    #endregion

    #region SCENE LOADING & TRANSITION (PUBLIC)

    // Các hàm này giờ có thể gọi từ bên ngoài (LevelButton/UnlockButton)
    public void StartGameMode2() => StartAnyScene(sceneNameMode2, 2);
    public void StartGameMode3() => StartAnyScene(sceneNameMode3, 3);

    /// <summary>
    /// Chạy hiệu ứng Transition Strips và load Scene bất kỳ
    /// </summary>
    public void StartAnyScene(string sceneName, int modeIndex)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // Cập nhật UI Bottom Bar để người dùng thấy phản hồi ngay lập tức
        HandleButtonAnimationOnly(modeIndex - 1);

        // Lưu mode đang chọn vào bộ nhớ
        PlayerPrefs.SetInt("SelectedMode", modeIndex);
        PlayerPrefs.Save();

        StartCoroutine(TransitionAndLoad(sceneName));
    }

    private IEnumerator TransitionAndLoad(string sceneName)
    {
        // Khóa tương tác toàn bộ UI tránh người dùng bấm lung tung
        if (currentPanel != null)
        {
            currentPanel.interactable = false;
            currentPanel.blocksRaycasts = false;
        }

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideBanner();
            AdsManager.Instance.HideMREC();
        }

        // Kích hoạt dải màu và logo trung tâm (Strips Transition)
        if (panelTransition != null)
        {
            panelTransition.SetActive(true);
            SetStripsPos(0);
            SetObjectsPos(0);

            if (centerLogo != null)
            {
                centerLogo.DOKill();
                centerLogo.transform.localScale = Vector3.zero;
                centerLogo.alpha = 0;
                centerLogo.DOFade(1, 0.3f);
                centerLogo.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }

            StartCoroutine(AnimateStrips(0, 1, 0.6f));
            StartCoroutine(AnimateSideElements(0, 1, 0.6f));
        }

        // Đợi 1.2s cho hiệu ứng dải màu che kín màn hình rồi mới load
        yield return new WaitForSecondsRealtime(1.2f);

        DOTween.KillAll();
        SceneManager.LoadScene(sceneName);
    }

    private void SetStripsPos(float t)
    {
        float offset = 2500f;
        if (leftStrip) leftStrip.anchoredPosition = Vector2.Lerp(_leftStripOrig + new Vector2(-offset, 0), _leftStripOrig, t);
        if (rightStrip) rightStrip.anchoredPosition = Vector2.Lerp(_rightStripOrig + new Vector2(offset, 0), _rightStripOrig, t);
    }

    private void SetObjectsPos(float t)
    {
        float offset = 1800f;
        if (leftObj) leftObj.anchoredPosition = Vector2.Lerp(_leftObjOrig + new Vector2(-offset, 0), _leftObjOrig, t);
        if (rightObj) rightObj.anchoredPosition = Vector2.Lerp(_rightObjOrig + new Vector2(offset, 0), _rightObjOrig, t);
    }

    private IEnumerator AnimateStrips(float start, float end, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentT = Mathf.Lerp(start, end, Mathf.SmoothStep(0, 1, elapsed / duration));
            SetStripsPos(currentT);
            yield return null;
        }
        SetStripsPos(end);
    }

    private IEnumerator AnimateSideElements(float start, float end, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentT = Mathf.Lerp(start, end, Mathf.SmoothStep(0, 1, elapsed / duration));
            SetObjectsPos(currentT);
            yield return null;
        }
        SetObjectsPos(end);
    }
    #endregion

    #region UI BOTTOM BAR & EXTRA PANELS

    public void HandleButtonAnimationOnly(int index)
    {
        // Hiệu ứng dịch chuyển nhẹ vị trí X cho Bottom Bar
        if (menuButtons.Length > 0 && menuButtons[0] != null)
        {
            menuButtons[0].DOKill();
            if (index == 1) menuButtons[0].DOAnchorPosX(29f, 0.25f);
            else if (index == 2) menuButtons[0].DOAnchorPosX(-29f, 0.25f);
            else if (index == 0) menuButtons[0].DOAnchorPosX(0f, 0.25f);
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            menuButtons[i].DOKill();

            if (i == index) // Trạng thái đang chọn (Highlight)
            {
                if (buttonImages[i] != null) buttonImages[i].sprite = frameSelected;
                if (buttonTexts[i] != null) buttonTexts[i].color = Color.white;
                menuButtons[i].DOAnchorPosY(200f, 0.25f).SetEase(Ease.OutBack);
                menuButtons[i].DOScale(1.37f, 0.25f).SetEase(Ease.OutBack);
            }
            else // Trạng thái không chọn
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

    public void ShowAccount() => SwitchToExtra(panelAccount, btnAccount, 1.6f);
    public void ShowSetting() => SwitchToExtra(panelSetting, btnSetting, 1.0f);

    private void SwitchToExtra(CanvasGroup target, RectTransform btn, float scale)
    {
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
            AccountManager.Instance.SaveAndExit();

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
            panelMode1.alpha = 1;
            panelMode1.interactable = true;
            panelMode1.blocksRaycasts = true;
            panelMode1.transform.localScale = Vector3.one;

            if (AdsManager.Instance != null) AdsManager.Instance.ShowBanner();
        });
    }

    private void AnimateButtonScale(RectTransform btn, float targetScale)
    {
        if (btn == null) return;
        btn.DOKill(true);
        btn.DOScale(Vector3.one * targetScale, 0.2f);
    }
    #endregion
}