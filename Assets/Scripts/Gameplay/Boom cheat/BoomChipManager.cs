using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using DG.Tweening;

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

    [Header("Multi-Phase Tutorials")]
    public GameObject tutorialP1;
    public GameObject tutorialP2;
    public GameObject tutorialP3;
    public GameObject handPointer; // Dùng chung 1 cái hoặc mỗi Group 1 cái tùy bạn


    [Header("--- THÔNG TIN NGƯỜI CHƠI (Đã đồng bộ) ---")]
    public TextMeshProUGUI nameTextP1;
    public Image avatarImageP1;
    public TextMeshProUGUI nameTextP2;
    public Image avatarImageP2;

    [Header("Transition Elements (Strips)")]
    public RectTransform leftStrip;
    public RectTransform rightStrip;
    public RectTransform leftObj;
    public RectTransform rightObj;
    public CanvasGroup centerLogo;

    private Vector2 _leftStripOrig, _rightStripOrig, _leftObjOrig, _rightObjOrig;

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
    public Sprite p1SkinSprite;
    public Sprite p2SkinSprite;
    public Sprite missSprite;
    public bool winByHittingThree = true;

    [Header("Audio Configuration")]
    public string gameplayMusicName = "backround_02";
    public string p1HitSFX;
    public string p2HitSFX;
    public string globalMissSFX;

    // Dữ liệu bom đã được đảo để phục vụ Phase 3
    private List<int> board1TargetBombs = new List<int>();
    private List<int> board2TargetBombs = new List<int>();
    private int p1HitCount = 0;
    private int p2HitCount = 0;

    private bool isShowingTutorial = false;
    private GameObject currentActiveTutorial;


    void Awake()
    {
        Instance = this;
        selectionLogic = GetComponent<PlayerSelection>();

        if (leftStrip != null) _leftStripOrig = leftStrip.anchoredPosition;
        if (rightStrip != null) _rightStripOrig = rightStrip.anchoredPosition;
        if (leftObj != null) _leftObjOrig = leftObj.anchoredPosition;
        if (rightObj != null) _rightObjOrig = rightObj.anchoredPosition;

        SyncDataFromSettings();
        UpdatePlayerInfoUI();

        if (p1WinBombImage != null) SetupWinImage(p1WinBombImage, p1SkinSprite);
        if (p2WinBombImage != null) SetupWinImage(p2WinBombImage, p2SkinSprite);

        panelPhase1.SetActive(true);
        panelPhase2.SetActive(false);
        panelPhase3.SetActive(false);
        panelAnimation.SetActive(false);
        panelWin.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);
        if (panelTransition != null)
        {
            panelTransition.SetActive(true);
            SetStripsPos(1);
            SetObjectsPos(1);
            if (centerLogo != null) { centerLogo.transform.localScale = Vector3.one; centerLogo.alpha = 1; }
        }

        InitializeHearts();
        RotatePlayer2TileVisuals();
    }

    private void UpdatePlayerInfoUI()
    {
        if (AccountManager.Instance != null)
        {
            if (nameTextP1 != null) nameTextP1.text = AccountManager.Instance.GetPlayerName(1);
            if (avatarImageP1 != null) avatarImageP1.sprite = AccountManager.Instance.GetAvatarSprite(1);

            if (nameTextP2 != null) nameTextP2.text = AccountManager.Instance.GetPlayerName(2);
            if (avatarImageP2 != null) avatarImageP2.sprite = AccountManager.Instance.GetAvatarSprite(2);
        }
        else
        {
            if (nameTextP1 != null) nameTextP1.text = PlayerPrefs.GetString("PlayerName_P1", "P1");
            if (nameTextP2 != null) nameTextP2.text = PlayerPrefs.GetString("PlayerName_P2", "P2");
        }
    }

    private void SyncDataFromSettings()
    {
        p1SkinSprite = BoomChipSettings.player1Sprite;
        p2SkinSprite = BoomChipSettings.player2Sprite;
        missSprite = BoomChipSettings.customMissSprite;
        winByHittingThree = BoomChipSettings.winByHittingThree;

        p1HitSFX = !string.IsNullOrEmpty(BoomChipSettings.player1HitSFX) ? BoomChipSettings.player1HitSFX : "SFX_Bomb_Hit";
        p2HitSFX = !string.IsNullOrEmpty(BoomChipSettings.player2HitSFX) ? BoomChipSettings.player2HitSFX : "SFX_Bomb_Hit";
        globalMissSFX = !string.IsNullOrEmpty(BoomChipSettings.missSFXName) ? BoomChipSettings.missSFXName : "SFX_Bomb_Miss";
    }

    void Start()
    {
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(9);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(gameplayMusicName);

        ShowMRECByPhase();
        StartCoroutine(InitialOpenSequence());
        CheckAndShowTutorial(1);
    }

    private void SetupWinImage(Image img, Sprite sp)
    {
        if (sp != null)
        {
            img.sprite = sp;
            img.preserveAspect = true;
        }
    }

    #region PHASE NAVIGATION & ADS
    public void NextPhase()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");

        if (currentPhase == GamePhase.Phase1)
        {
            if (AdsManager.Instance != null)
                AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_p1_choose", () => ExecutePhase2Transition(), 0.5f);
            else
                ExecutePhase2Transition();
        }
        else if (currentPhase == GamePhase.Phase2)
        {
            if (AdsManager.Instance != null)
                AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_p2_choose", () => StartCoroutine(TransitionSequence()), 0.5f);
            else
                StartCoroutine(TransitionSequence());
        }
    }

    private void ExecutePhase2Transition()
    {
        if (currentActiveTutorial != null) currentActiveTutorial.SetActive(false);
        isShowingTutorial = false;

        currentPhase = GamePhase.Phase2;
        panelPhase1.SetActive(false);
        panelPhase2.SetActive(true);
        ShowMRECByPhase();
        CheckAndShowTutorial(2);
    }

    private void ShowMRECByPhase()
    {
        if (AdsManager.Instance == null) return;

        switch (currentPhase)
        {
            case GamePhase.Phase1:
                AdsManager.Instance.ShowMREC("is_show_mrec_p1_choose");
                break;
            case GamePhase.Phase2:
                AdsManager.Instance.ShowMREC("is_show_mrec_p2_choose");
                break;
            case GamePhase.Phase3:
                AdsManager.Instance.HideMREC();
                break;
            default:
                AdsManager.Instance.HideMREC();
                break;
        }
    }
    #endregion

    #region TRANSITION LOGIC
    private IEnumerator TransitionSequence()
    {
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        if (panelTransition == null)
        {
            SwitchToPhase3State();
            yield break;
        }

        panelTransition.SetActive(true);

        // ĐẢM BẢO RESET VỀ 0 TRƯỚC KHI BAY VÀO
        SetStripsPos(0);
        SetObjectsPos(0);
        if (centerLogo != null) { centerLogo.transform.localScale = Vector3.zero; centerLogo.alpha = 0; }

        // Logic bay vào che màn hình
        yield return StartCoroutine(AnimateStrips(0, 1, 0.5f));
        yield return StartCoroutine(AnimateSideElements(0, 1, 0.5f));

        if (centerLogo != null) { centerLogo.transform.localScale = Vector3.one; centerLogo.alpha = 1; }

        SwitchToPhase3State();
        yield return new WaitForSeconds(1.0f);

        // Logic bay ra (giữ nguyên như cũ của bạn)
        StartCoroutine(AnimateSideElements(1, 0, 0.5f));
        if (centerLogo != null) { centerLogo.transform.localScale = Vector3.zero; centerLogo.alpha = 0; }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(AnimateStrips(1, 0, 0.5f));

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

        currentPhase = GamePhase.Phase3;
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

    private IEnumerator InitialOpenSequence()
    {
        // Đợi 1-2 giây tùy ý để người chơi kịp nhìn thấy logo/màn hình chờ
        yield return new WaitForSeconds(1.5f);

        // Tái sử dụng logic bay ra của bạn (từ 1 về 0)
        StartCoroutine(AnimateSideElements(1, 0, 0.5f));
        if (centerLogo != null) { centerLogo.transform.localScale = Vector3.zero; centerLogo.alpha = 0; }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(AnimateStrips(1, 0, 0.5f));

        panelTransition.SetActive(false);
    }
    #endregion

    #region GAMEPLAY LOGIC
    public void OnCoinFlipFinished(int winnerID)
    {
        panelAnimation.SetActive(false);
        currentPhase = GamePhase.Phase3;
        isP1Turn = (winnerID == 0);

        // ĐẢO BOM: Bảng 1 chứa bom P2 đặt, Bảng 2 chứa bom P1 đặt
        board1TargetBombs = new List<int>(selectionLogic.p2SelectedTiles);
        board2TargetBombs = new List<int>(selectionLogic.p1SelectedTiles);

        StartCoroutine(ShowTurnAndStartGame(winnerID));

        CheckAndShowTutorial(3);
    }

    private IEnumerator ShowTurnAndStartGame(int winnerID)
    {
        panelAnimation.SetActive(true);
        TextMeshProUGUI animText = panelAnimation.GetComponentInChildren<TextMeshProUGUI>();

        string winnerName = AccountManager.Instance != null
            ? AccountManager.Instance.GetPlayerName(winnerID == 0 ? 1 : 2)
            : (winnerID == 0 ? "PLAYER 1" : "PLAYER 2");

        if (animText != null) animText.text = winnerName.ToUpper() + " GO FIRST!";

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_StartTurn");

        yield return new WaitForSeconds(1.5f);
        panelAnimation.SetActive(false);
        UpdateBoardVisuals();
    }

    public void ExecuteTurn(int tileIndex, TileButton tile, int boardOwnerID, int boardID)
    {
        if (currentPhase != GamePhase.Phase3 || panelWin.activeSelf) return;

        // Bảng của ai người nấy mới được click (P1 click bảng P1, P2 click bảng P2)
        if (isP1Turn && boardOwnerID != 1) return;
        if (!isP1Turn && boardOwnerID != 2) return;

        // Lấy danh sách bom dựa trên boardID (vị trí bảng)
        List<int> targetList = (boardID == 1) ? board1TargetBombs : board2TargetBombs;

        if (targetList.Contains(tileIndex))
        {
            // VISUAL: Sân của ai hiện skin người đó (Chủ nhà)
            Sprite ownerSkin = (boardOwnerID == 1) ? p1SkinSprite : p2SkinSprite;
            tile.SetVisual(ownerSkin);

            // Cộng điểm cho chủ sân (vì họ "tìm thấy" bom trên sân mình)
            if (boardOwnerID == 1) p1HitCount++; else p2HitCount++;
            UpdateHeartsUI(boardOwnerID);

            string sound = (boardOwnerID == 1) ? p1HitSFX : p2HitSFX;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sound);
            GlobalSettings.PlayVibrate();
        }
        else
        {
            tile.SetVisual(missSprite);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(globalMissSFX);
        }

        tile.SetInteractable(false);
        CheckWinCondition();

        if (!panelWin.activeSelf && currentPhase == GamePhase.Phase3)
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

        if (p1BoardArea != null)
        {
            p1BoardArea.interactable = isP1Turn;
            p1BoardArea.blocksRaycasts = isP1Turn;
        }
        if (p2BoardArea != null)
        {
            p2BoardArea.interactable = !isP1Turn;
            p2BoardArea.blocksRaycasts = !isP1Turn;
        }
    }

    void CheckWinCondition()
    {
        int winner = 0;
        if (winByHittingThree)
        {
            if (p1HitCount >= 3) winner = 1;
            else if (p2HitCount >= 3) winner = 2;
        }
        else
        {
            if (p1HitCount >= 3) winner = 2;
            else if (p2HitCount >= 3) winner = 1;
        }

        if (winner != 0)
        {
            currentPhase = GamePhase.Animation;
            StartCoroutine(DelayShowWinScreen(winner));
        }
    }

    private IEnumerator DelayShowWinScreen(int winnerID)
    {
        yield return new WaitForSeconds(1.2f);
        ShowWinScreen(winnerID);
    }

    void ShowWinScreen(int winnerID)
    {
        AccountManager.SetWinResult(winnerID);

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
        if (isShowingTutorial)
        {
            CloseCurrentTutorial();
        }
        if (currentPhase == GamePhase.Phase1 || currentPhase == GamePhase.Phase2)
        {
            if (selectionLogic != null)
            {
                if (isShowingTutorial)
                {
                    CloseCurrentTutorial();
                }
                selectionLogic.HandleTileClick(tileIndex, tile);
                bool isSelected = selectionLogic.IsTileSelected(currentPhase == GamePhase.Phase1 ? 1 : 2, tileIndex);
                Sprite mySkin = (currentPhase == GamePhase.Phase1) ? p1SkinSprite : p2SkinSprite;
                tile.SetVisual(isSelected ? mySkin : null);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");
            }
        }
        else if (currentPhase == GamePhase.Phase3)
        {
            // Ở Phase 3, dùng boardOwnerID và boardID để xử lý turn
            ExecuteTurn(tileIndex, tile, tile.ownerID, tile.boardID);
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
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_retry", () => {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }, 0.3f);
        }
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToSelection()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_back_home", () => {
                SceneManager.LoadScene("SelectScene");
            }, 0.3f);
        }
        else SceneManager.LoadScene("SelectScene");
    }

    public void OpenSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(true);
            if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();
        }
    }

    public void CloseSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);
            if (currentPhase == GamePhase.Phase1 || currentPhase == GamePhase.Phase2 || panelWin.activeSelf)
            {
                ShowMRECByPhase();
                if (panelWin.activeSelf && AdsManager.Instance != null)
                    AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");
            }
        }
    }
    #endregion

    public void CheckAndShowTutorial(int phaseNumber)
    {
        string key = "FirstTime_Boom_Phase" + phaseNumber;
        Debug.Log("Checking Tutorial for Phase: " + phaseNumber + " | Status: " + PlayerPrefs.GetInt(key, 0));

        if (PlayerPrefs.GetInt(key, 0) == 1) return;

        if (tutorialP1) tutorialP1.SetActive(false);
        if (tutorialP2) tutorialP2.SetActive(false);
        if (tutorialP3) tutorialP3.SetActive(false);

        GameObject targetTutorial = null;
        if (phaseNumber == 1) targetTutorial = tutorialP1;
        else if (phaseNumber == 2) targetTutorial = tutorialP2;
        else if (phaseNumber == 3) targetTutorial = tutorialP3;

        if (targetTutorial != null)
        {
            targetTutorial.SetActive(true);
            currentActiveTutorial = targetTutorial;
            isShowingTutorial = true;

            // CHỈ CHẠY BÀN TAY NẾU KHÔNG PHẢI PHASE 3
            if (phaseNumber != 3)
            {
                Transform hand = targetTutorial.transform.Find("HandPointer");
                if (hand != null)
                {
                    hand.DOKill();
                    hand.localScale = Vector3.one;
                    hand.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                }
            }
            else
            {
                // Nếu là Phase 3, đảm bảo ẩn bàn tay đi nếu lỡ tay để trong Prefab
                Transform hand = targetTutorial.transform.Find("HandPointer");
                if (hand != null) hand.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        // Chỉ cần đang hiện tutorial và người chơi chạm màn hình là tắt ngay
        if (isShowingTutorial && Input.GetMouseButtonDown(0))
        {
            CloseCurrentTutorial();
        }
    }

    private void CloseCurrentTutorial()
    {
        if (currentActiveTutorial != null)
        {
            // Lưu lại là đã xem Phase này
            string key = "FirstTime_Boom_Phase" + (currentActiveTutorial == tutorialP1 ? 1 : (currentActiveTutorial == tutorialP2 ? 2 : 3));
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            currentActiveTutorial.SetActive(false);
            currentActiveTutorial = null;
            isShowingTutorial = false;
        }
    }

    // Hàm này để gán vào Sự kiện OnClick của nút "?" trên màn hình
    public void ReplayTutorial()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");

        int phaseToShow = 1; // Mặc định là Phase 1

        // Kiểm tra xem người chơi đang thực sự ở Phase nào
        if (currentPhase == GamePhase.Phase1) phaseToShow = 1;
        else if (currentPhase == GamePhase.Phase2) phaseToShow = 2;
        else if (currentPhase == GamePhase.Phase3) phaseToShow = 3;

        // Gọi hàm hiện Tutorial nhưng bỏ qua kiểm tra PlayerPrefs
        ForceShowTutorial(phaseToShow);
    }

    private void ForceShowTutorial(int phaseNumber)
    {
        // Tắt các cái cũ đang hiện (nếu có)
        if (tutorialP1) tutorialP1.SetActive(false);
        if (tutorialP2) tutorialP2.SetActive(false);
        if (tutorialP3) tutorialP3.SetActive(false);

        GameObject targetTutorial = null;
        if (phaseNumber == 1) targetTutorial = tutorialP1;
        else if (phaseNumber == 2) targetTutorial = tutorialP2;
        else if (phaseNumber == 3) targetTutorial = tutorialP3;

        if (targetTutorial != null)
        {
            targetTutorial.SetActive(true);
            currentActiveTutorial = targetTutorial;
            isShowingTutorial = true;

            // Chạy lại hiệu ứng bàn tay (trừ Phase 3 như bạn muốn)
            if (phaseNumber != 3)
            {
                Transform hand = targetTutorial.transform.Find("HandPointer");
                if (hand != null)
                {
                    hand.gameObject.SetActive(true);
                    hand.DOKill();
                    hand.localScale = Vector3.one;
                    hand.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                }
            }
        }
    }
}