using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class DiceModeManager : MonoBehaviour
{
    public static DiceModeManager Instance;

    [Header("Prefab Configuration")]
    public GameObject tilePrefab;
    public Transform boardContainer;

    [Header("UI Text & Indicators")]
    public TextMeshProUGUI turnStatusText;
    public CanvasGroup p1CanvasGroup;
    public CanvasGroup p2CanvasGroup;

    [Header("Game Mode Configuration")]
    public bool isBombModeActive = false;
    private int hiddenBombIndex = -1;

    [Header("UI Panels & Backgrounds")]
    public GameObject panelGameplay;
    public GameObject panelAnimation;
    public GameObject panelWin;
    public GameObject panelTransition;
    public GameObject panelSetting;

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

    void Awake()
    {
        Instance = this;

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

        GenerateBoard();
        SetBoardInteractable(false);

        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        if (panelTransition != null) StartCoroutine(RunStartTransition());
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
        if (isP1Turn) p1ClaimedCells++; else p2ClaimedCells++;
        totalCellsClaimed++;
        currentMovesLeft--;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Click");

        if (totalCellsClaimed >= 25)
        {
            DetermineWinnerByScore();
            return;
        }

        if (currentMovesLeft <= 0)
        {
            SetBoardInteractable(false);
            isP1Turn = !isP1Turn;
            PrepareNewTurn();
        }
    }

    private IEnumerator HandleBombExplosionSequence(DiceTile tile)
    {
        isGameOver = true; // Chặn mọi thao tác click ngay lập tức
        SetBoardInteractable(false);
        tile.SetVisual(bombSprite, true);

        GlobalSettings.PlayVibrate();
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(BoomChipSettings.hitSFXName))
        {
            AudioManager.Instance.PlaySFX(BoomChipSettings.hitSFXName);
        }

        // Hiệu ứng rung bàn cờ
        Vector3 originalBoardPos = boardContainer.localPosition;
        float elapsed = 0f; float duration = 0.8f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float strength = (1 - (elapsed / duration)) * 15f;
            boardContainer.localPosition = originalBoardPos + (Vector3)Random.insideUnitCircle * strength;
            yield return null;
        }
        boardContainer.localPosition = originalBoardPos;

        yield return new WaitForSeconds(1.2f); // Delay 1.2s trước khi hiện Win
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
    #endregion

    #region TRANSITIONS & COIN FLIP
    private IEnumerator RunStartTransition()
    {
        panelTransition.SetActive(true);
        yield return new WaitForSeconds(1.2f);
        panelTransition.SetActive(false);
        StartCoinFlipSequence();
    }

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
            string key = (winnerID == 0) ? "PlayerName_P1" : "PlayerName_P2";
            string defaultName = (winnerID == 0) ? "Player 1" : "Player 2";
            string winnerName = PlayerPrefs.GetString(key, defaultName);

            flipStatusText.text = $"<color=yellow><b> {winnerName}</b></color> GO FIRST!";
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
            string key = isP1Turn ? "PlayerName_P1" : "PlayerName_P2";
            string defaultName = isP1Turn ? "PLAYER 1" : "PLAYER 2";
            string currentPlayerName = PlayerPrefs.GetString(key, defaultName);

            turnStatusText.text = currentPlayerName.ToUpper() + " TURN";
        }
        UpdateTurnIndicators();
    }

    private void UpdateTurnIndicators()
    {
        if (p1CanvasGroup != null) p1CanvasGroup.alpha = isP1Turn ? 1f : 0.8f;
        if (p2CanvasGroup != null) p2CanvasGroup.alpha = isP1Turn ? 0.8f : 1f;
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

        SetBoardInteractable(true);
        currentMovesLeft = Mathf.Min(finalResult, 25 - totalCellsClaimed);
    }
    #endregion

    #region END GAME
    void DetermineWinnerByScore()
    {
        int winner = 0;
        if (p1ClaimedCells > p2ClaimedCells) winner = 1;
        else if (p2ClaimedCells > p1ClaimedCells) winner = 2;
        else winner = 0;

        isGameOver = true; // Chặn click tiếp ngay lập tức
        SetBoardInteractable(false);
        StartCoroutine(DelayEndGame(winner));
    }

    private IEnumerator DelayEndGame(int winnerID)
    {
        yield return new WaitForSeconds(1.2f); // Delay 1.2s trước khi hiện bảng Win
        EndGame(winnerID);
    }

    void EndGame(int winnerID)
    {
        isGameOver = true;
        SetBoardInteractable(false);

        if (p1CanvasGroup) p1CanvasGroup.alpha = 0.6f;
        if (p2CanvasGroup) p2CanvasGroup.alpha = 0.6f;
        if (rollButton) rollButton.interactable = false;

        if (panelWin) panelWin.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Win");

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

            if (isGameOver && panelWin.activeSelf)
                AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");
            else if (!isGameOver)
                AdsManager.Instance.ShowMREC("is_show_mrec_gameplay");
        }
    }
    #endregion
}