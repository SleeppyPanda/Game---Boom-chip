using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class DiceModeManager : MonoBehaviour
{
    public static DiceModeManager Instance;

    [Header("Prefab Configuration")]
    public GameObject tilePrefab;
    public Transform boardContainer;

    [Header("Tutorial Configuration")]
    public GameObject tutorialGroupP1; // Hướng dẫn Tung xúc xắc (Dùng cho cả 2 lượt)
    public GameObject tutorialGroupP2; // Hướng dẫn Lật ô (Lượt 1)
    public GameObject tutorialGroupP3; // Hướng dẫn Lật ô (Lượt 2)
    private int tutorialStep = 0;      // 0: P1 Roll, 1: P1 Flip, 2: P2 Roll, 3: P2 Flip, 4: Done

    [Header("UI Text & Indicators")]
    public TextMeshProUGUI turnStatusText;
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;
    public Image p1IndicatorFrame;
    public Image p2IndicatorFrame;
    public Image p1Frame;
    public Image p2Frame;
    public Image p1avatar;
    public Image p2avatar;
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Game Mode Configuration")]
    public bool isBombModeActive = false;
    private int hiddenBombIndex = -1;

    [Header("UI Panels & Backgrounds")]
    public GameObject panelGameplay;
    public GameObject panelAnimation;
    public GameObject panelWin;
    public GameObject panelTransition;
    public GameObject panelSetting;

    [Header("Transition Strips")]
    public RectTransform leftStrip;
    public RectTransform rightStrip;
    public RectTransform leftObj;
    public RectTransform rightObj;
    public CanvasGroup centerLogo;
    private Vector2 _leftStripOrig, _rightStripOrig, _leftObjOrig, _rightObjOrig;

    [Header("Board Visuals")]
    public Image boardBackgroundImage;
    public Sprite p1BoardSprite;
    public Sprite p2BoardSprite;

    [Header("Dice System (Visual)")]
    public Image diceDisplayImage;
    public Sprite[] diceSprites;
    public Image diceBackground;
    public Sprite diceBgIdle;
    public Sprite diceBgRolling;

    [Header("Roll Button Control")]
    public Button rollButton;
    private Animator rollButtonAnimator;
    private int currentMovesLeft = 0;
    private bool waitingForRoll = true;

    [Header("Player Stats")]
    public bool isP1Turn;
    private int p1ClaimedCells = 0;
    private int p2ClaimedCells = 0;
    private int totalCellsClaimed = 0;

    [Header("Win UI")]
    public GameObject p1Crown;
    public GameObject p2Crown;
    public TextMeshProUGUI p1ResultScore;
    public TextMeshProUGUI p2ResultScore;
    public TextMeshProUGUI flipStatusText;

    [Header("Resources (Auto-assigned)")]
    public Sprite p1Color;
    public Sprite p2Color;
    public Sprite bombSprite;

    private bool isGameOver = false;
    private List<DiceTile> allBoardTiles = new List<DiceTile>();

    private int GetCurrentModeID() => isBombModeActive ? 9 : 10;

    void Awake()
    {
        Instance = this;

        if (leftStrip != null) _leftStripOrig = leftStrip.anchoredPosition;
        if (rightStrip != null) _rightStripOrig = rightStrip.anchoredPosition;
        if (leftObj != null) _leftObjOrig = leftObj.anchoredPosition;
        if (rightObj != null) _rightObjOrig = rightObj.anchoredPosition;

        if (BoomChipSettings.player1Sprite != null) p1Color = BoomChipSettings.player1Sprite;
        if (BoomChipSettings.player2Sprite != null) p2Color = BoomChipSettings.player2Sprite;
        isBombModeActive = BoomChipSettings.isBombModeActive;
        if (BoomChipSettings.customHitSprite != null) bombSprite = BoomChipSettings.customHitSprite;

        if (panelSetting != null) panelSetting.SetActive(false);

        if (rollButton != null)
        {
            rollButtonAnimator = rollButton.GetComponent<Animator>();
            if (rollButtonAnimator != null) rollButtonAnimator.enabled = false;
        }
    }

    void Start()
    {
        if (panelWin) panelWin.SetActive(false);
        if (panelAnimation) panelAnimation.SetActive(false);

        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(GetCurrentModeID());

        GenerateBoard();
        UpdateScoreUI();
        SetBoardInteractable(false);

        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        if (panelTransition != null)
        {
            panelTransition.SetActive(true);
            SetStripsPos(1);
            SetObjectsPos(1);
            StartCoroutine(RunStartTransition());
        }
        else StartCoinFlipSequence();

        if (isBombModeActive)
        {
            hiddenBombIndex = Random.Range(0, 25);
            Debug.Log("<color=red><b>[LOG] BOMB AT: </b></color>" + hiddenBombIndex);
        }
    }

    private void GenerateBoard()
    {
        foreach (Transform child in boardContainer) Destroy(child.gameObject);
        allBoardTiles.Clear();
        for (int i = 0; i < 25; i++)
        {
            GameObject newTileObj = Instantiate(tilePrefab, boardContainer);
            DiceTile tileScript = newTileObj.GetComponent<DiceTile>();
            tileScript.tileIndex = i;
            allBoardTiles.Add(tileScript);
        }
    }

    #region TRANSITION STRIPS LOGIC
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

    private IEnumerator RunStartTransition()
    {
        // Thời gian chờ ban đầu (bạn có thể giảm xuống 0.5f nếu muốn nhanh hơn nữa)
        yield return new WaitForSeconds(1.2f);

        StartCoroutine(AnimateSideElements(1, 0, 0.5f));
        if (centerLogo != null) { centerLogo.transform.DOScale(0, 0.3f); centerLogo.DOFade(0, 0.3f); }

        yield return new WaitForSeconds(0.2f);

        // --- SỬA Ở ĐÂY ---
        // Gọi Panel tung đồng xu sớm ngay trước khi 2 dải Strip chạy mở ra
        StartCoinFlipSequence();

        // 2 dải nền mở ra cùng lúc với lúc đồng xu đang hiện lên
        yield return StartCoroutine(AnimateStrips(1, 0, 0.5f));
        panelTransition.SetActive(false);
    }
    #endregion

    #region GAMEPLAY LOGIC
    public void OnTileClicked(int index, DiceTile tile)
    {
        if (isGameOver || waitingForRoll || currentMovesLeft <= 0) return;

        if (isBombModeActive && index == hiddenBombIndex)
        {
            StartCoroutine(HandleBombExplosionSequence(tile));
            return;
        }

        Sprite visualToSet = isP1Turn ? p1Color : p2Color;
        if (visualToSet != null) tile.SetVisual(visualToSet, false);

        tile.SetInteractable(false);
        tile.isClaimed = true;
        if (isP1Turn) p1ClaimedCells++;
        else p2ClaimedCells++;

        totalCellsClaimed++;
        currentMovesLeft--;

        UpdateScoreUI();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Chip");

        if (totalCellsClaimed >= 25)
        {
            DetermineWinnerByScore();
            return;
        }

        if (currentMovesLeft <= 0)
        {
            // Kiểm tra bước Tutorial khi người chơi dùng hết lượt lật ô
            if (tutorialStep == 1) tutorialStep = 2; // Xong Flip P1 -> Chờ Roll P2
            else if (tutorialStep == 3)
            {
                tutorialStep = 4; // Xong Flip P2 -> Xong toàn bộ Tutorial
                PlayerPrefs.SetInt("TutorialDone", 1);

                UGS_AchievementLocal.Unlock("ACH_DICE_LEARNER");

                PlayerPrefs.Save();
            }

            SetBoardInteractable(false);
            isP1Turn = !isP1Turn;
            PrepareNewTurn();
        }
        else
        {
            UpdateTutorial(); // Cập nhật để duy trì bàn tay chỉ vào ô (Group P2 hoặc P3)
        }
    }

    private IEnumerator HandleBombExplosionSequence(DiceTile tile)
    {
        int currentClaimed = isP1Turn ? p1ClaimedCells : p2ClaimedCells;
        if (currentClaimed <= 1)
        {
            UGS_AchievementLocal.Unlock("ACH_DICE_BOMB_HIT");
        }
        isGameOver = true;
        SetBoardInteractable(false);
        tile.SetVisual(bombSprite, true);
        GlobalSettings.PlayVibrate();
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(BoomChipSettings.hitSFXName))
            AudioManager.Instance.PlaySFX(BoomChipSettings.hitSFXName);

        Vector3 originalBoardPos = boardContainer.localPosition;
        float elapsed = 0f;
        float duration = 0.6f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float strength = (1 - (elapsed / duration)) * 15f;
            boardContainer.localPosition = originalBoardPos + (Vector3)UnityEngine.Random.insideUnitCircle * strength;
            yield return null;
        }
        boardContainer.localPosition = originalBoardPos;
        yield return new WaitForSeconds(1.0f);
        EndGame(isP1Turn ? 2 : 1);
    }

    private void SetBoardInteractable(bool canInteract)
    {
        foreach (var tile in allBoardTiles)
        {
            if (tile == null) continue;
            if (!canInteract) tile.SetInteractable(false);
            else if (!tile.isClaimed) tile.SetInteractable(true);
        }
    }

    private void UpdateScoreUI()
    {
        if (p1ScoreText != null) p1ScoreText.text = $"Chips: {p1ClaimedCells}";
        if (p2ScoreText != null) p2ScoreText.text = $"Chips: {p2ClaimedCells}";
    }
    #endregion

    #region COIN FLIP
    private void StartCoinFlipSequence()
    {
        if (panelAnimation)
        {
            panelAnimation.SetActive(true);
            CoinFlipController flip = Object.FindFirstObjectByType<CoinFlipController>();
            if (flip != null) flip.StartCoinFlip();
        }
    }

    public void OnCoinFlipFinished(int winnerID) => StartCoroutine(HandleCoinFlipDelay(winnerID));

    private IEnumerator HandleCoinFlipDelay(int winnerID)
    {
        if (flipStatusText != null)
        {
            string winnerName = "Player";
            if (AccountManager.Instance != null) winnerName = AccountManager.Instance.GetPlayerName(winnerID == 0 ? 1 : 2);
            flipStatusText.text = $"<color=yellow><b> {winnerName.ToUpper()}</b></color> GO FIRST!";
        }
        yield return new WaitForSeconds(1.0f);
        FinishTransitionToGameplay(winnerID);
    }

    private void FinishTransitionToGameplay(int winnerID)
    {
        if (panelAnimation) panelAnimation.SetActive(false);
        isP1Turn = (winnerID == 0);
        if (AdsManager.Instance != null) AdsManager.Instance.ShowMREC("is_show_mrec_gameplay");
        PrepareNewTurn();
    }
    #endregion

    #region DICE SYSTEM
    private void PrepareNewTurn()
    {
        if (isGameOver) return;
        waitingForRoll = true;
        rollButton.interactable = true;
        if (rollButtonAnimator != null) rollButtonAnimator.enabled = false;

        SetBoardInteractable(false);
        if (diceBackground) diceBackground.sprite = diceBgIdle;
        if (boardBackgroundImage) boardBackgroundImage.sprite = isP1Turn ? p1BoardSprite : p2BoardSprite;

        if (turnStatusText != null)
        {
            string currentPlayerName = "PLAYER";
            if (AccountManager.Instance != null) currentPlayerName = AccountManager.Instance.GetPlayerName(isP1Turn ? 1 : 2);
            turnStatusText.text = currentPlayerName.ToUpper() + " TURN";
        }
        UpdateTurnIndicators();
        UpdateTutorial(); // Hiện Tutorial Roll (P1) cho lượt mới
    }

    private void UpdateTurnIndicators()
    {
        if (p1IndicatorFrame != null) p1IndicatorFrame.color = isP1Turn ? activeColor : inactiveColor;
        if (p2IndicatorFrame != null) p2IndicatorFrame.color = isP1Turn ? inactiveColor : activeColor;
        if (p1Frame != null) p1Frame.color = isP1Turn ? activeColor : inactiveColor;
        if (p2Frame != null) p2Frame.color = isP1Turn ? inactiveColor : activeColor;
        if (p1avatar != null) p1avatar.color = isP1Turn ? activeColor : inactiveColor;
        if (p2avatar != null) p2avatar.color = isP1Turn ? inactiveColor : activeColor;
    }

    public void RollDice()
    {
        if (!waitingForRoll || isGameOver) return;
        waitingForRoll = false;

        if (diceBackground) diceBackground.sprite = diceBgRolling;
        if (rollButtonAnimator != null)
        {
            rollButtonAnimator.enabled = true;
            rollButtonAnimator.Play("Rolling", 0, 0f);
        }

        Animator diceAnim = diceDisplayImage.GetComponent<Animator>();
        if (diceAnim != null)
        {
            diceAnim.enabled = true;
            diceAnim.Play("Dice_Rolling", 0, 0f);
        }
        StartCoroutine(DiceRollAnimation());
    }

    private IEnumerator DiceRollAnimation()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Dice");
        yield return new WaitForSeconds(0.8f);

        int finalResult = Random.Range(1, 7);
        diceDisplayImage.sprite = diceSprites[finalResult - 1];

        if (finalResult == 6)
        {
            UGS_AchievementLocal.Unlock("ACH_DICE_ROLL_6");
        }

        Animator diceAnim = diceDisplayImage.GetComponent<Animator>();
        if (diceAnim != null) diceAnim.enabled = false;

        diceDisplayImage.rectTransform.localRotation = Quaternion.identity;
        diceDisplayImage.rectTransform.localScale = Vector3.one;

        if (rollButtonAnimator != null)
        {
            rollButtonAnimator.Play("Idle", 0, 0f);
            rollButtonAnimator.enabled = false;
        }

        rollButton.transform.localRotation = Quaternion.identity;
        rollButton.transform.localScale = Vector3.one;
        rollButton.interactable = false;

        if (diceBackground) diceBackground.sprite = diceBgIdle;

        // Cập nhật bước Tutorial sau khi có kết quả xúc xắc
        if (tutorialStep == 0) tutorialStep = 1;      // Sang lượt lật P1 (Group P2)
        else if (tutorialStep == 2) tutorialStep = 3; // Sang lượt lật P2 (Group P3)

        SetBoardInteractable(true);
        currentMovesLeft = Mathf.Min(finalResult, 25 - totalCellsClaimed);
        UpdateTutorial(); // Chuyển từ Group P1 sang Group P2/P3
    }
    #endregion

    #region END GAME
    void DetermineWinnerByScore()
    {
        if (isBombModeActive)
        {
            UGS_AchievementLocal.Unlock("ACH_DICE_SURVIVOR");
        }
        int winner = 0;
        if (p1ClaimedCells > p2ClaimedCells) winner = 1;
        else if (p2ClaimedCells > p1ClaimedCells) winner = 2;
        isGameOver = true;
        SetBoardInteractable(false);
        StartCoroutine(DelayEndGame(winner));
    }

    private IEnumerator DelayEndGame(int winnerID)
    {
        yield return new WaitForSeconds(1.2f);
        EndGame(winnerID);
    }

    void EndGame(int winnerID)
    {
        int winnerScore = (winnerID == 1) ? p1ClaimedCells : p2ClaimedCells;

        if (winnerScore >= 18)
        {
            UGS_AchievementLocal.Unlock("ACH_DICE_DOMINANCE");
        }
        isGameOver = true;
        SetBoardInteractable(false);
        if (AccountManager.Instance != null) AccountManager.SetWinResult(winnerID);
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeComplete(GetCurrentModeID());
        if (p1IndicatorFrame) p1IndicatorFrame.color = inactiveColor;
        if (p2IndicatorFrame) p2IndicatorFrame.color = inactiveColor;
        if (rollButton) rollButton.interactable = false;
        if (panelWin) panelWin.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Win");
        if (AdsManager.Instance != null) AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");
        if (p1ResultScore) p1ResultScore.text = "x " + p1ClaimedCells;
        if (p2ResultScore) p2ResultScore.text = "x " + p2ClaimedCells;
        if (p1Crown) p1Crown.SetActive(winnerID == 1);
        if (p2Crown) p2Crown.SetActive(winnerID == 2);
    }
    #endregion

    #region BUTTONS
    public void Restart()
    {
        if (panelWin != null) panelWin.SetActive(false);
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_retry", () => {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }, 0.3f);
        }
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Home()
    {
        if (panelWin != null) panelWin.SetActive(false);
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
            if (AdsManager.Instance == null) return;
            if (isGameOver && panelWin.activeSelf) AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");
            else if (!isGameOver) AdsManager.Instance.ShowMREC("is_show_mrec_gameplay");
        }
    }
    #endregion

    #region TUTORIAL LOGIC
    public void UpdateTutorial()
    {
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 1 || isGameOver)
        {
            HideAllTutorials();
            return;
        }

        HideAllTutorials();

        switch (tutorialStep)
        {
            case 0: // Lượt 1: Chờ Roll
            case 2: // Lượt 2: Chờ Roll
                if (waitingForRoll && tutorialGroupP1)
                {
                    tutorialGroupP1.SetActive(true);
                    AnimateHand(tutorialGroupP1);
                }
                break;

            case 1: // Lượt 1: Đang lật ô
                if (!waitingForRoll && currentMovesLeft > 0 && tutorialGroupP2)
                {
                    tutorialGroupP2.SetActive(true);
                    AnimateHand(tutorialGroupP2);
                }
                break;

            case 3: // Lượt 2: Đang lật ô
                if (!waitingForRoll && currentMovesLeft > 0 && tutorialGroupP3)
                {
                    tutorialGroupP3.SetActive(true);
                    AnimateHand(tutorialGroupP3);
                }
                break;
        }
    }

    private void HideAllTutorials()
    {
        if (tutorialGroupP1) tutorialGroupP1.SetActive(false);
        if (tutorialGroupP2) tutorialGroupP2.SetActive(false);
        if (tutorialGroupP3) tutorialGroupP3.SetActive(false);
    }

    private void AnimateHand(GameObject group)
    {
        Transform hand = group.transform.Find("HandPointer");
        if (hand != null)
        {
            hand.DOKill();
            hand.localScale = Vector3.one;
            hand.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    public void OnClickReplayTutorial()
    {
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
        {
            UpdateTutorial();
        }
    }
    #endregion
}