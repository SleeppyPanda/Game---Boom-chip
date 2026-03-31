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

    [Header("Tutorial Prediction")]
    public GameObject tutorialGroupP1; // Hướng dẫn Tung xúc xắc
    public GameObject tutorialGroupP2; // Hướng dẫn Lật ô
    private bool isTutorialFinished = false; // Khi đã làm xong cả 2 bước thì tắt vĩnh viễn

    [Header("UI Text & Indicators")]
    public TextMeshProUGUI turnStatusText;
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;
    // THAY ĐỔI: Chuyển sang Image để đổi màu thay vì Alpha
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

    // Hàm bổ trợ để lấy ID mode: Bomb = 9, Thường = 10
    private int GetCurrentModeID() => isBombModeActive ? 9 : 10;

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

        // FIREBASE: Log bắt đầu game với Mode ID tương ứng
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(GetCurrentModeID());

        GenerateBoard();
        UpdateScoreUI();
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
        if (isP1Turn) p1ClaimedCells++;
        else p2ClaimedCells++;

        totalCellsClaimed++;
        currentMovesLeft--;

        UpdateScoreUI();

        UpdateTutorial();

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
        isGameOver = true;
        SetBoardInteractable(false);

        // 1. Gọi lệnh hiển thị visual bom và chạy Animation nổ trong DiceTile
        tile.SetVisual(bombSprite, true);

        // 2. Rung điện thoại và âm thanh nổ
        GlobalSettings.PlayVibrate();
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(BoomChipSettings.hitSFXName))
        {
            AudioManager.Instance.PlaySFX(BoomChipSettings.hitSFXName);
        }

        // 3. Rung toàn bộ Board (Shake Effect)
        Vector3 originalBoardPos = boardContainer.localPosition;
        float elapsed = 0f;
        float duration = 0.6f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float strength = (1 - (elapsed / duration)) * 15f; // Rung yếu dần
            boardContainer.localPosition = originalBoardPos + (Vector3)UnityEngine.Random.insideUnitCircle * strength;
            yield return null;
        }
        boardContainer.localPosition = originalBoardPos; // Trả về vị trí cũ để Grid không lệch

        // 4. Chờ 1 giây để người chơi xem xong Animation nổ
        yield return new WaitForSeconds(1.0f);

        // 5. Kết thúc game: Người bấm trúng bom là người thua
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
        if (p1ScoreText != null)
            p1ScoreText.text = $"Chips: {p1ClaimedCells}";

        if (p2ScoreText != null)
            p2ScoreText.text = $"Chips: {p2ClaimedCells}";
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
            string winnerName = "Player";
            if (AccountManager.Instance != null)
            {
                winnerName = AccountManager.Instance.GetPlayerName(winnerID == 0 ? 1 : 2);
            }
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
        UpdateTutorial();
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
            if (AccountManager.Instance != null)
            {
                currentPlayerName = AccountManager.Instance.GetPlayerName(isP1Turn ? 1 : 2);
            }
            turnStatusText.text = currentPlayerName.ToUpper() + " TURN";
        }
        UpdateTurnIndicators();
    }

    private void UpdateTurnIndicators()
    {
        // Cập nhật màu sắc thay vì Alpha
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
        UpdateTutorial();
    }
    #endregion

    #region END GAME
    void DetermineWinnerByScore()
    {
        int winner = 0;
        if (p1ClaimedCells > p2ClaimedCells) winner = 1;
        else if (p2ClaimedCells > p1ClaimedCells) winner = 2;
        else winner = 0;

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
        isGameOver = true;
        SetBoardInteractable(false);

        if (AccountManager.Instance != null)
        {
            AccountManager.SetWinResult(winnerID);
        }

        // FIREBASE: Log hoàn thành game với Mode ID tương ứng
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeComplete(GetCurrentModeID());

        if (p1IndicatorFrame) p1IndicatorFrame.color = inactiveColor;
        if (p2IndicatorFrame) p2IndicatorFrame.color = inactiveColor;
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

    public void UpdateTutorial()
    {
        // Kiểm tra xem người chơi đã từng hoàn thành tutorial chưa (Lưu trong máy)
        // 1 là đã xong, 0 là chưa xong
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 1)
        {
            if (tutorialGroupP1) tutorialGroupP1.SetActive(false);
            if (tutorialGroupP2) tutorialGroupP2.SetActive(false);
            return;
        }

        if (waitingForRoll)
        {
            if (tutorialGroupP1)
            {
                tutorialGroupP1.SetActive(true);
                AnimateHand(tutorialGroupP1);
            }
            if (tutorialGroupP2) tutorialGroupP2.SetActive(false);
        }
        else if (currentMovesLeft > 0)
        {
            if (tutorialGroupP1) tutorialGroupP1.SetActive(false);
            if (tutorialGroupP2)
            {
                tutorialGroupP2.SetActive(true);
                AnimateHand(tutorialGroupP2);
            }
        }
        else
        {
            // Khi lật xong ô đầu tiên -> Lưu lại trạng thái ĐÃ XONG vào máy
            PlayerPrefs.SetInt("TutorialDone", 1);
            PlayerPrefs.Save(); // Lưu ngay lập tức

            if (tutorialGroupP1) tutorialGroupP1.SetActive(false);
            if (tutorialGroupP2) tutorialGroupP2.SetActive(false);
        }
    }

    // Hàm bổ trợ làm tay nhấp nhô
    private void AnimateHand(GameObject group)
    {
        // Tìm object có tên là "HandPointer" bên trong Group
        Transform hand = group.transform.Find("HandPointer");
        if (hand != null)
        {
            hand.DOKill(); // Xóa các tween cũ để tránh chồng chéo
            hand.localScale = Vector3.one; // Reset về 1

            // Hiệu ứng: Phóng lên 1.2 lần, chạy liên tục (Loop), kiểu Yoyo (lên rồi về)
            hand.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    public void OnClickReplayTutorial()
    {
        // Cho phép hiện lại dù đã lưu là hoàn thành
        if (waitingForRoll)
        {
            if (tutorialGroupP1) tutorialGroupP1.SetActive(true);
            AnimateHand(tutorialGroupP1);
        }
        else
        {
            if (tutorialGroupP2) tutorialGroupP2.SetActive(true);
            AnimateHand(tutorialGroupP2);
        }
    }
}