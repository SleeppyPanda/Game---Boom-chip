using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class DiceModeManager : MonoBehaviour
{
    public static DiceModeManager Instance;

    [Header("UI Text & Indicators")]
    public TextMeshProUGUI turnStatusText; // Text hiển thị "PLAYER 1 TURN"
    public CanvasGroup p1CanvasGroup;
    public CanvasGroup p2CanvasGroup;
    // Thay đổi từ Alpha sang Color để tránh nhìn xuyên thấu
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Màu xám, Alpha vẫn là 1

    // ... (Các Header cũ giữ nguyên) ...
    [Header("Game Mode Configuration")]
    public bool isBombModeActive = false;
    private int hiddenBombIndex = -1;

    [Header("UI Panels & Backgrounds")]
    public GameObject panelGameplay;
    public GameObject panelAnimation;
    public GameObject panelWin;
    public GameObject panelTransition;

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

    [Header("Resources")]
    public Sprite p1Color;
    public Sprite p2Color;
    public Sprite bombSprite;

    [Header("Coin Flip UI Connection")]
    public TextMeshProUGUI flipStatusText;

    private bool isGameOver = false;
    private DiceTile[] allBoardTiles;

    void Awake()
    {
        Instance = this;
        isBombModeActive = PlayerPrefs.GetInt("DiceBombMode", 0) == 1;

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

        AutoAssignTileIDs();
        SetBoardInteractable(false);

        if (panelTransition != null) StartCoroutine(RunStartTransition());
        else StartCoinFlipSequence();

        if (isBombModeActive)
        {
            hiddenBombIndex = Random.Range(0, 25);
            Debug.Log("<color=red>BOMB AT: </color>" + hiddenBombIndex);
        }
    }

    private void AutoAssignTileIDs()
    {
        allBoardTiles = panelGameplay.GetComponentsInChildren<DiceTile>(true);
        for (int i = 0; i < allBoardTiles.Length; i++)
        {
            allBoardTiles[i].tileIndex = i;
        }
    }

    private void SetBoardInteractable(bool canInteract)
    {
        if (allBoardTiles == null) return;
        foreach (var tile in allBoardTiles)
        {
            if (!canInteract) tile.SetInteractable(false);
            else if (!tile.isClaimed) tile.SetInteractable(true);
        }
    }

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

    public void OnCoinFlipFinished(int winnerID)
    {
        StartCoroutine(HandleCoinFlipDelay(winnerID));
    }

    private IEnumerator HandleCoinFlipDelay(int winnerID)
    {
        if (flipStatusText != null)
            flipStatusText.text = (winnerID == 0) ? "PLAYER 1 GO FIRST!" : "PLAYER 2 GO FIRST!";

        yield return new WaitForSeconds(0.5f);
        if (panelAnimation) panelAnimation.SetActive(false);

        isP1Turn = (winnerID == 0);
        PrepareNewTurn();
    }

    private void PrepareNewTurn()
    {
        if (isGameOver) return;

        waitingForRoll = true;
        rollButton.interactable = true;
        if (rollButtonAnimator != null) rollButtonAnimator.enabled = false;

        SetBoardInteractable(false);

        if (diceBackground) diceBackground.sprite = diceBgIdle;
        if (boardBackgroundImage)
            boardBackgroundImage.sprite = isP1Turn ? p1BoardSprite : p2BoardSprite;

        // Cập nhật Text lượt chơi
        if (turnStatusText != null)
        {
            turnStatusText.text = isP1Turn ? "PLAYER 1 TURN" : "PLAYER 2 TURN";
            // Đổi màu text theo màu của Player nếu muốn
            turnStatusText.color = isP1Turn ? Color.blue : Color.red;
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

    public void OnTileClicked(int index, DiceTile tile)
    {
        if (isGameOver || waitingForRoll || currentMovesLeft <= 0) return;

        if (isBombModeActive && index == hiddenBombIndex)
        {
            tile.SetVisual(bombSprite);
            Handheld.Vibrate();
            EndGame(isP1Turn ? 2 : 1);
            return;
        }

        tile.SetVisual(isP1Turn ? p1Color : p2Color);
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

    void DetermineWinnerByScore()
    {
        if (p1ClaimedCells > p2ClaimedCells) EndGame(1);
        else if (p2ClaimedCells > p1ClaimedCells) EndGame(2);
        else EndGame(0);
    }

    void EndGame(int winnerID)
    {
        isGameOver = true;
        SetBoardInteractable(false);

        // Khi kết thúc game, cả 2 bên cùng mờ nhẹ
        if (p1CanvasGroup) p1CanvasGroup.alpha = 0.6f;
        if (p2CanvasGroup) p2CanvasGroup.alpha = 0.6f;

        if (rollButton) rollButton.interactable = false;

        if (panelWin) panelWin.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Win");

        if (p1ResultScore) p1ResultScore.text = "x " + p1ClaimedCells;
        if (p2ResultScore) p2ResultScore.text = "x " + p2ClaimedCells;
        if (p1Crown) p1Crown.SetActive(winnerID == 1);
        if (p2Crown) p2Crown.SetActive(winnerID == 2);
    }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void Home() => SceneManager.LoadScene("SelectScene");
}