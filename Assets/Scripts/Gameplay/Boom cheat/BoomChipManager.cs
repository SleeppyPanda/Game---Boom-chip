using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class BoomChipManager : MonoBehaviour
{
    public GamePhase currentPhase = GamePhase.Phase1;
    private PlayerSelection selectionLogic;

    [Header("UI Panels")]
    public GameObject panelPhase1;
    public GameObject panelPhase2;
    public GameObject panelPhase3;
    public GameObject panelAnimation;
    public GameObject panelWin;
    public GameObject panelTransition;

    [Header("Transition Elements (RectTransforms)")]
    public RectTransform leftStrip;
    public RectTransform rightStrip;
    public RectTransform leftObj;
    public RectTransform rightObj;
    public CanvasGroup centerLogo;

    [Header("Scrolling Gradient Settings")]
    public float scrollSpeed = 0.5f;
    private RawImage leftRawImg;
    private RawImage rightRawImg;
    private bool isTransitioning = false;

    [Header("Phase Next Buttons")]
    public GameObject nextButtonPhase1;
    public GameObject nextButtonPhase2;

    [Header("Hearts System (Phase 3)")]
    public Image[] p1Hearts;
    public Image[] p2Hearts;
    public Sprite heartFullSprite;
    public Sprite heartEmptySprite;

    [Header("Gameplay Phase 3 (Canvas Groups)")]
    public CanvasGroup p1BoardArea; // Bảng Player 1 nhìn thấy để bấm (chứa bom P2)
    public CanvasGroup p2BoardArea; // Bảng Player 2 nhìn thấy để bấm (chứa bom P1)
    public bool isP1Turn;

    [Header("Win Panel Custom UI")]
    public GameObject p1Crown;
    public GameObject p2Crown;
    public TextMeshProUGUI p1HitCounterText;
    public TextMeshProUGUI p2HitCounterText;

    [Header("Settings & Sprites")]
    public Sprite hitSprite;
    public Sprite missSprite;
    public bool winByHittingThree = true;

    private List<int> p1TargetBombs = new List<int>();
    private List<int> p2TargetBombs = new List<int>();
    private int p1HitCount = 0;
    private int p2HitCount = 0;

    void Awake()
    {
        selectionLogic = GetComponent<PlayerSelection>();

        if (leftStrip) leftRawImg = leftStrip.GetComponent<RawImage>();
        if (rightStrip) rightRawImg = rightStrip.GetComponent<RawImage>();

        panelPhase1.SetActive(true);
        panelPhase2.SetActive(false);
        panelPhase3.SetActive(false);
        panelAnimation.SetActive(false);
        panelWin.SetActive(false);
        if (panelTransition != null) panelTransition.SetActive(false);

        if (nextButtonPhase1 != null) nextButtonPhase1.SetActive(false);
        if (nextButtonPhase2 != null) nextButtonPhase2.SetActive(false);
        if (p1Crown != null) p1Crown.SetActive(false);
        if (p2Crown != null) p2Crown.SetActive(false);

        InitializeHearts();
        RotatePlayer2TileVisuals();
    }

    void Update()
    {
        if (isTransitioning) ApplyGradientScroll();
    }

    private void ApplyGradientScroll()
    {
        if (leftRawImg != null)
        {
            Rect r = leftRawImg.uvRect;
            r.y += Time.deltaTime * scrollSpeed;
            leftRawImg.uvRect = r;
        }
        if (rightRawImg != null)
        {
            Rect r = rightRawImg.uvRect;
            r.y -= Time.deltaTime * scrollSpeed;
            rightRawImg.uvRect = r;
        }
    }

    public void NextPhase()
    {
        if (currentPhase == GamePhase.Phase1)
        {
            currentPhase = GamePhase.Phase2;
            panelPhase1.SetActive(false);
            panelPhase2.SetActive(true);
        }
        else if (currentPhase == GamePhase.Phase2)
        {
            StartCoroutine(TransitionSequence());
        }
    }

    private IEnumerator TransitionSequence()
    {
        if (panelTransition == null)
        {
            panelPhase2.SetActive(false);
            panelPhase3.SetActive(true);
            panelAnimation.SetActive(true);
            yield break;
        }

        isTransitioning = true;
        panelTransition.SetActive(true);

        Vector2 centerLeftStrip = leftStrip.anchoredPosition;
        Vector2 centerRightStrip = rightStrip.anchoredPosition;
        Vector2 centerLeftObj = leftObj.anchoredPosition;
        Vector2 centerRightObj = rightObj.anchoredPosition;

        Vector2 startLeftStrip = new Vector2(centerLeftStrip.x, 5000f);
        Vector2 startRightStrip = new Vector2(centerRightStrip.x, -5000f);
        Vector2 startLeftObj = new Vector2(-2000f, centerLeftObj.y);
        Vector2 startRightObj = new Vector2(2000f, centerRightObj.y);

        Vector2 endLeftStrip = new Vector2(centerLeftStrip.x, -5000f);
        Vector2 endRightStrip = new Vector2(centerRightStrip.x, 5000f);
        Vector2 endLeftObj = new Vector2(-2000f, centerLeftObj.y);
        Vector2 endRightObj = new Vector2(2000f, centerRightObj.y);

        leftStrip.anchoredPosition = startLeftStrip;
        rightStrip.anchoredPosition = startRightStrip;
        leftObj.anchoredPosition = startLeftObj;
        rightObj.anchoredPosition = startRightObj;
        centerLogo.alpha = 0;
        centerLogo.transform.localScale = Vector3.zero;

        float elapsed = 0;
        float inDuration = 0.7f;

        while (elapsed < inDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / inDuration);
            leftStrip.anchoredPosition = Vector2.Lerp(startLeftStrip, centerLeftStrip, t);
            rightStrip.anchoredPosition = Vector2.Lerp(startRightStrip, centerRightStrip, t);
            leftObj.anchoredPosition = Vector2.Lerp(startLeftObj, centerLeftObj, t);
            rightObj.anchoredPosition = Vector2.Lerp(startRightObj, centerRightObj, t);
            centerLogo.alpha = t;
            centerLogo.transform.localScale = Vector3.one * t;
            yield return null;
        }

        panelPhase2.SetActive(false);
        panelPhase3.SetActive(true);
        panelAnimation.SetActive(true);

        yield return new WaitForSeconds(1.0f);

        elapsed = 0;
        float outDuration = 2.5f; // Chỉnh lên 2.5s theo yêu cầu

        while (elapsed < outDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / outDuration);
            leftStrip.anchoredPosition = Vector2.Lerp(centerLeftStrip, endLeftStrip, t);
            rightStrip.anchoredPosition = Vector2.Lerp(centerRightStrip, endRightStrip, t);
            leftObj.anchoredPosition = Vector2.Lerp(centerLeftObj, endLeftObj, t);
            rightObj.anchoredPosition = Vector2.Lerp(centerRightObj, endRightObj, t);
            centerLogo.alpha = 1 - t;
            centerLogo.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }

        isTransitioning = false;
        panelTransition.SetActive(false);

        currentPhase = GamePhase.Animation;
        CoinFlipController flip = FindFirstObjectByType<CoinFlipController>();
        if (flip != null) flip.StartCoinFlip();
    }

    public void OnCoinFlipFinished(int winnerID)
    {
        panelAnimation.SetActive(false);
        currentPhase = GamePhase.Phase3;
        isP1Turn = (winnerID == 0);

        // P1 bắn tìm bom trong danh sách bom P2 đã đặt
        p1TargetBombs = new List<int>(selectionLogic.p2SelectedTiles);
        p2TargetBombs = new List<int>(selectionLogic.p1SelectedTiles);

        StartCoroutine(ShowTurnAndStartGame(winnerID));
    }

    private IEnumerator ShowTurnAndStartGame(int winnerID)
    {
        panelAnimation.SetActive(true);
        string winnerName = (winnerID == 0) ? "PLAYER 1" : "PLAYER 2";
        TextMeshProUGUI animText = panelAnimation.GetComponentInChildren<TextMeshProUGUI>();
        if (animText != null) animText.text = "<color=yellow>" + winnerName + "</color> GO FIRST!";

        yield return new WaitForSeconds(1.5f);

        panelAnimation.SetActive(false);
        panelPhase3.SetActive(true);
        UpdateBoardVisuals();
    }

    public void ExecuteTurn(int tileIndex, TileButton tile, int boardOwnerID)
    {
        if (currentPhase != GamePhase.Phase3) return;

        // ĐÃ SỬA: Lượt P1 (isP1Turn = true) thì bảng tương tác phải là bảng P1 sáng (boardOwnerID 1)
        if (isP1Turn && boardOwnerID != 1) return;
        if (!isP1Turn && boardOwnerID != 2) return;

        // P1 bắn trúng danh sách bom của P2
        List<int> targetList = (isP1Turn) ? p1TargetBombs : p2TargetBombs;

        if (targetList.Contains(tileIndex))
        {
            tile.SetVisual(hitSprite);
            if (isP1Turn) p1HitCount++; else p2HitCount++;
            UpdateHeartsUI(isP1Turn ? 1 : 2);
        }
        else
        {
            tile.SetVisual(missSprite);
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
        // ĐÃ SỬA: Lượt ai bảng đó sáng rõ (1.0) và nhấn được (interactable)
        if (p1BoardArea != null)
        {
            p1BoardArea.alpha = isP1Turn ? 1.0f : 0.6f;
            p1BoardArea.interactable = isP1Turn;
            p1BoardArea.blocksRaycasts = isP1Turn;
        }
        if (p2BoardArea != null)
        {
            p2BoardArea.alpha = isP1Turn ? 0.6f : 1.0f;
            p2BoardArea.interactable = !isP1Turn;
            p2BoardArea.blocksRaycasts = !isP1Turn;
        }
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
        if (p1Crown != null) p1Crown.SetActive(winnerID == 1);
        if (p2Crown != null) p2Crown.SetActive(winnerID == 2);
        if (p1HitCounterText != null) p1HitCounterText.text = "x " + p1HitCount;
        if (p2HitCounterText != null) p2HitCounterText.text = "x " + p2HitCount;
    }

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
            foreach (TileButton tile in tilesInP2)
            {
                tile.transform.localScale = new Vector3(-1, -1, 1);
            }
        }
    }

    public void HandleTileClick(int tileIndex, TileButton tile)
    {
        if (selectionLogic != null) selectionLogic.HandleTileClick(tileIndex, tile);
    }

    public void UpdateButtonNextVisibility()
    {
        if (selectionLogic == null) return;
        if (currentPhase == GamePhase.Phase1 && nextButtonPhase1 != null)
            nextButtonPhase1.SetActive(selectionLogic.IsSelectionComplete(1));
        else if (currentPhase == GamePhase.Phase2 && nextButtonPhase2 != null)
            nextButtonPhase2.SetActive(selectionLogic.IsSelectionComplete(2));
    }

    public void Rematch() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void GoToSelection() => SceneManager.LoadScene("SelectScene");
}