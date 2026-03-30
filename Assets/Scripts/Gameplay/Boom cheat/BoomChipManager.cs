using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class BoomChipManager : MonoBehaviour
{
    public static BoomChipManager Instance;

    public GamePhase currentPhase = GamePhase.Phase1;
    private PlayerSelection selectionLogic;

    [Header("UI Panels")]
    public GameObject panelPhase1;
    public GameObject panelPhase2;
    public GameObject panelPhase3;
    public GameObject panelAnimation;
    public GameObject panelWin;
    public GameObject panelTransition;

    [Header("Transition Elements (Strips)")]
    public RectTransform leftStrip;
    public RectTransform rightStrip;

    [Header("Transition Elements (Objects & Logo)")]
    public RectTransform leftObj;
    public RectTransform rightObj;
    public CanvasGroup centerLogo;

    private Vector2 _leftStripOrig;
    private Vector2 _rightStripOrig;
    private Vector2 _leftObjOrig;
    private Vector2 _rightObjOrig;

    [Header("Phase Next Buttons")]
    public GameObject nextButtonPhase1;
    public GameObject nextButtonPhase2;

    [Header("Hearts System (Phase 3)")]
    public Image[] p1Hearts;
    public Image[] p2Hearts;
    public Sprite heartFullSprite;
    public Sprite heartEmptySprite;

    [Header("Gameplay Phase 3 (Canvas Groups)")]
    public CanvasGroup p1BoardArea;
    public CanvasGroup p2BoardArea;

    [Header("Turn Covers")]
    public GameObject p1Cover;
    public GameObject p2Cover;
    public bool isP1Turn;

    [Header("Win Panel Custom UI")]
    public GameObject p1Crown;
    public GameObject p2Crown;
    public TextMeshProUGUI p1HitCounterText;
    public TextMeshProUGUI p2HitCounterText;
    public Image p1WinBombImage;
    public Image p2WinBombImage;

    [Header("Settings & Sprites")]
    public GameObject panelSetting;
    public Sprite hitSprite;
    public Sprite missSprite;
    public bool winByHittingThree = true;

    [Header("Audio Configuration")]
    public string gameplayMusicName = "backround_02";
    private string currentHitSound;
    private string currentMissSound;

    private List<int> p1TargetBombs = new List<int>();
    private List<int> p2TargetBombs = new List<int>();
    private int p1HitCount = 0;
    private int p2HitCount = 0;

    void Awake()
    {
        Instance = this;
        selectionLogic = GetComponent<PlayerSelection>();

        if (leftStrip != null) _leftStripOrig = leftStrip.anchoredPosition;
        if (rightStrip != null) _rightStripOrig = rightStrip.anchoredPosition;
        if (leftObj != null) _leftObjOrig = leftObj.anchoredPosition;
        if (rightObj != null) _rightObjOrig = rightObj.anchoredPosition;

        if (BoomChipSettings.customHitSprite != null)
        {
            hitSprite = BoomChipSettings.customHitSprite;
            if (p1WinBombImage != null) SetupWinImage(p1WinBombImage, hitSprite);
            if (p2WinBombImage != null) SetupWinImage(p2WinBombImage, hitSprite);
        }

        if (BoomChipSettings.customMissSprite != null)
            missSprite = BoomChipSettings.customMissSprite;

        winByHittingThree = BoomChipSettings.winByHittingThree;

        currentHitSound = !string.IsNullOrEmpty(BoomChipSettings.hitSFXName) ? BoomChipSettings.hitSFXName : "SFX_Bomb_Hit";
        currentMissSound = !string.IsNullOrEmpty(BoomChipSettings.missSFXName) ? BoomChipSettings.missSFXName : "SFX_Bomb_Miss";

        panelPhase1.SetActive(true);
        panelPhase2.SetActive(false);
        panelPhase3.SetActive(false);
        panelAnimation.SetActive(false);
        panelWin.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);
        if (panelTransition != null) panelTransition.SetActive(false);

        if (nextButtonPhase1 != null) nextButtonPhase1.SetActive(false);
        if (nextButtonPhase2 != null) nextButtonPhase2.SetActive(false);
        if (p1Crown != null) p1Crown.SetActive(false);
        if (p2Crown != null) p2Crown.SetActive(false);

        InitializeHearts();
        RotatePlayer2TileVisuals();
        InitBoardSprites();
    }

    void Start()
    {
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(9);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(gameplayMusicName);
        ShowMRECByPhase();
    }

    private void InitBoardSprites()
    {
        if (selectionLogic == null) return;
        if (panelPhase1 != null && selectionLogic.defaultSpritePhase1 != null)
        {
            TileButton[] tilesP1 = panelPhase1.GetComponentsInChildren<TileButton>(true);
            foreach (var t in tilesP1) t.SetVisual(selectionLogic.defaultSpritePhase1);
        }
        if (panelPhase2 != null && selectionLogic.defaultSpritePhase2 != null)
        {
            TileButton[] tilesP2 = panelPhase2.GetComponentsInChildren<TileButton>(true);
            foreach (var t in tilesP2) t.SetVisual(selectionLogic.defaultSpritePhase2);
        }
    }

    private void SetupWinImage(Image img, Sprite sp)
    {
        img.sprite = sp;
        img.preserveAspect = true;
    }

    #region PHASE NAVIGATION & ADS
    public void NextPhase()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");

        if (currentPhase == GamePhase.Phase1)
        {
            if (AdsManager.Instance != null)
                AdsManager.Instance.ShowInterstitial("is_show_inter_p1_choose", () => ExecutePhase2Transition());
            else ExecutePhase2Transition();
        }
        else if (currentPhase == GamePhase.Phase2)
        {
            if (AdsManager.Instance != null)
                AdsManager.Instance.ShowInterstitial("is_show_inter_p2_choose", () => StartCoroutine(TransitionSequence()));
            else StartCoroutine(TransitionSequence());
        }
    }

    private void ExecutePhase2Transition()
    {
        currentPhase = GamePhase.Phase2;
        panelPhase1.SetActive(false);
        panelPhase2.SetActive(true);
        ShowMRECByPhase();
    }

    private void ShowMRECByPhase()
    {
        if (AdsManager.Instance == null) return;
        switch (currentPhase)
        {
            case GamePhase.Phase1: AdsManager.Instance.ShowMREC("is_show_mrec_p1_choose"); break;
            case GamePhase.Phase2: AdsManager.Instance.ShowMREC("is_show_mrec_p2_choose"); break;
            case GamePhase.Phase3: AdsManager.Instance.ShowMREC("is_show_mrec_gameplay"); break;
        }
    }
    #endregion

    #region TRANSITION LOGIC (NEW TIMELINE)
    private IEnumerator TransitionSequence()
    {
        if (panelTransition == null)
        {
            SwitchToPhase3State();
            yield break;
        }

        panelTransition.SetActive(true);

        // Reset trạng thái ban đầu
        if (centerLogo != null) { centerLogo.transform.localScale = Vector3.zero; centerLogo.alpha = 0; }
        SetStripsPos(0);
        SetObjectsPos(0);

        // --- GIAI ĐOẠN 1: BAY VÀO ---
        // 1. 2 Strip bay vào (0.5s)
        yield return StartCoroutine(AnimateStrips(0, 1, 0.5f));

        // 2. 0.5s sau, 2 Obj bay vào (0.5s)
        yield return StartCoroutine(AnimateSideElements(0, 1, 0.5f));

        // 3. Hiện logo (Ngay sau khi Obj vào xong)
        if (centerLogo != null) { centerLogo.transform.localScale = Vector3.one; centerLogo.alpha = 1; }

        // Chuyển UI ngầm bên dưới transition
        SwitchToPhase3State();

        // --- GIAI ĐOẠN 2: NGHỈ ---
        // Nghỉ 1s
        yield return new WaitForSeconds(1.0f);

        // --- GIAI ĐOẠN 3: BAY RA (PHỐI HỢP THỜI GIAN) ---
        // 1. 2 Obj bắt đầu bay ra (0.5s)
        StartCoroutine(AnimateSideElements(1, 0, 0.5f));

        // 2. Logo biến mất ngay khi Obj bắt đầu bay ra (như yêu cầu: logo mất cùng lúc panel/obj bay ra)
        if (centerLogo != null) { centerLogo.transform.localScale = Vector3.zero; centerLogo.alpha = 0; }

        // 3. Đợi 0.2s rồi 2 Strip (panel) bay ra (0.5s)
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(AnimateStrips(1, 0, 0.5f));

        // Kết thúc
        panelTransition.SetActive(false);
        currentPhase = GamePhase.Animation;

        CoinFlipController flip = FindFirstObjectByType<CoinFlipController>();
        if (flip != null) flip.StartCoinFlip();
    }

    private void SwitchToPhase3State()
    {
        panelPhase2.SetActive(false);
        panelPhase3.SetActive(true);
        panelAnimation.SetActive(true);
        ShowMRECByPhase();
    }

    private void SetStripsPos(float t)
    {
        float offset = 2000f;
        if (leftStrip) leftStrip.anchoredPosition = Vector2.Lerp(_leftStripOrig + new Vector2(-offset, 0), _leftStripOrig, t);
        if (rightStrip) rightStrip.anchoredPosition = Vector2.Lerp(_rightStripOrig + new Vector2(offset, 0), _rightStripOrig, t);
    }

    private void SetObjectsPos(float t)
    {
        float offset = 1500f;
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

    #region GAMEPLAY LOGIC
    public void OnCoinFlipFinished(int winnerID)
    {
        panelAnimation.SetActive(false);
        currentPhase = GamePhase.Phase3;
        isP1Turn = (winnerID == 0);

        p1TargetBombs = new List<int>(selectionLogic.p2SelectedTiles);
        p2TargetBombs = new List<int>(selectionLogic.p1SelectedTiles);

        StartCoroutine(ShowTurnAndStartGame(winnerID));
    }

    private IEnumerator ShowTurnAndStartGame(int winnerID)
    {
        panelAnimation.SetActive(true);
        TextMeshProUGUI animText = panelAnimation.GetComponentInChildren<TextMeshProUGUI>();
        if (animText != null) animText.text = (winnerID == 0 ? "PLAYER 1" : "PLAYER 2") + " GO FIRST!";

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_StartTurn");

        yield return new WaitForSeconds(1.5f);
        panelAnimation.SetActive(false);
        UpdateBoardVisuals();
    }

    public void ExecuteTurn(int tileIndex, TileButton tile, int boardOwnerID)
    {
        if (currentPhase != GamePhase.Phase3) return;

        if (isP1Turn && boardOwnerID != 1) return;
        if (!isP1Turn && boardOwnerID != 2) return;

        List<int> targetList = isP1Turn ? p1TargetBombs : p2TargetBombs;

        if (targetList.Contains(tileIndex))
        {
            tile.SetVisual(hitSprite);
            if (isP1Turn) p1HitCount++; else p2HitCount++;
            UpdateHeartsUI(isP1Turn ? 1 : 2);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(currentHitSound);
            Handheld.Vibrate();
        }
        else
        {
            tile.SetVisual(missSprite);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(currentMissSound);
        }

        tile.SetInteractable(false);
        CheckWinCondition();

        if (!panelWin.activeSelf)
        {
            isP1Turn = !isP1Turn;
            UpdateBoardVisuals();
        }
    }

    private void UpdateHeartsUI(int playerID)
    {
        Image[] hearts = (playerID == 1) ? p1Hearts : p2Hearts;
        int hitCount = (playerID == 1) ? p1HitCount : p2HitCount;
        if (hitCount > 0 && hitCount <= hearts.Length)
        {
            if (winByHittingThree) hearts[hitCount - 1].sprite = heartFullSprite;
            else hearts[hearts.Length - hitCount].sprite = heartEmptySprite;
        }
    }

    void UpdateBoardVisuals()
    {
        if (p1Cover != null) p1Cover.SetActive(!isP1Turn);
        if (p2Cover != null) p2Cover.SetActive(isP1Turn);

        if (p1BoardArea != null) p1BoardArea.interactable = isP1Turn;
        if (p2BoardArea != null) p2BoardArea.interactable = !isP1Turn;
    }

    void CheckWinCondition()
    {
        if (winByHittingThree)
        {
            if (p1HitCount >= 3) ShowWinScreen(1);
            else if (p2HitCount >= 3) ShowWinScreen(2);
        }
        else
        {
            if (p1HitCount >= 3) ShowWinScreen(2);
            else if (p2HitCount >= 3) ShowWinScreen(1);
        }
    }

    void ShowWinScreen(int winnerID)
    {
        panelWin.SetActive(true);
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeComplete(9);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Win");
        if (AdsManager.Instance != null) AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");

        if (p1Crown != null) p1Crown.SetActive(winnerID == 1);
        if (p2Crown != null) p2Crown.SetActive(winnerID == 2);
        if (p1HitCounterText != null) p1HitCounterText.text = "x " + p1HitCount;
        if (p2HitCounterText != null) p2HitCounterText.text = "x " + p2HitCount;
    }
    #endregion

    #region UTILS & BUTTONS
    private void InitializeHearts()
    {
        Sprite startSprite = winByHittingThree ? heartEmptySprite : heartFullSprite;
        foreach (Image img in p1Hearts) if (img != null) img.sprite = startSprite;
        foreach (Image img in p2Hearts) if (img != null) img.sprite = startSprite;
    }

    private void RotatePlayer2TileVisuals()
    {
        if (p2BoardArea != null)
        {
            TileButton[] tilesInP2 = p2BoardArea.GetComponentsInChildren<TileButton>(true);
            foreach (TileButton tile in tilesInP2) tile.transform.localScale = new Vector3(-1, -1, 1);
        }
    }

    public void HandleTileClick(int tileIndex, TileButton tile)
    {
        if (currentPhase == GamePhase.Phase1 || currentPhase == GamePhase.Phase2)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");
            if (selectionLogic != null) selectionLogic.HandleTileClick(tileIndex, tile);
        }
        else if (currentPhase == GamePhase.Phase3)
        {
            int owner = tile.transform.IsChildOf(p1BoardArea.transform) ? 1 : 2;
            ExecuteTurn(tileIndex, tile, owner);
        }
    }

    public void UpdateButtonNextVisibility()
    {
        if (selectionLogic == null) return;
        if (currentPhase == GamePhase.Phase1 && nextButtonPhase1 != null)
            nextButtonPhase1.SetActive(selectionLogic.IsSelectionComplete(1));
        else if (currentPhase == GamePhase.Phase2 && nextButtonPhase2 != null)
            nextButtonPhase2.SetActive(selectionLogic.IsSelectionComplete(2));
    }

    public void Rematch()
    {
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();
        if (AdsManager.Instance != null)
            AdsManager.Instance.ShowInterstitial("is_show_inter_retry", () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToSelection()
    {
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();
        if (AdsManager.Instance != null)
            AdsManager.Instance.ShowInterstitial("is_show_inter_back_home", () => SceneManager.LoadScene("SelectScene"));
        else SceneManager.LoadScene("SelectScene");
    }

    public void OpenSetting() { if (panelSetting != null) panelSetting.SetActive(true); }
    public void CloseSetting() { if (panelSetting != null) panelSetting.SetActive(false); }
    #endregion
}