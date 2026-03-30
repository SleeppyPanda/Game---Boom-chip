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

    [Header("Transition Elements")]
    public RectTransform leftStrip;
    public RectTransform rightStrip;
    public RectTransform leftObj;
    public RectTransform rightObj;
    public CanvasGroup centerLogo;

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
    public bool isP1Turn;

    [Header("Win Panel Custom UI")]
    public GameObject p1Crown;
    public GameObject p2Crown;
    public TextMeshProUGUI p1HitCounterText;
    public TextMeshProUGUI p2HitCounterText;
    // Cập nhật: Thêm Image để hiển thị đúng loại bom người chơi đã chọn
    public Image p1WinBombImage;
    public Image p2WinBombImage;

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

        // --- CẬP NHẬT: Nhận dữ liệu truyền từ Selection Scene ---
        if (BoomChipSettings.customHitSprite != null)
        {
            hitSprite = BoomChipSettings.customHitSprite;

            // Đồng bộ sprite quả bom vào logic lựa chọn (PlayerSelection)
            if (selectionLogic != null)
            {
                selectionLogic.selectedSprite = hitSprite;
            }

            // Gán sprite vào các icon trên màn hình Win và giữ tỉ lệ
            if (p1WinBombImage != null) SetupWinImage(p1WinBombImage, hitSprite);
            if (p2WinBombImage != null) SetupWinImage(p2WinBombImage, hitSprite);
        }

        if (BoomChipSettings.customMissSprite != null)
            missSprite = BoomChipSettings.customMissSprite;

        winByHittingThree = BoomChipSettings.winByHittingThree;
        // -------------------------------------------------------

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

    // Hàm bổ trợ để thiết lập Image giữ đúng tỷ lệ
    private void SetupWinImage(Image img, Sprite sp)
    {
        img.sprite = sp;
        img.preserveAspect = true; // Giữ đúng tỷ lệ định dạng cho mọi chỗ được gán
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

        panelTransition.SetActive(true);

        Vector2 targetLeftStrip = leftStrip.anchoredPosition;
        Vector2 targetRightStrip = rightStrip.anchoredPosition;
        Vector2 targetLeftObj = leftObj.anchoredPosition;
        Vector2 targetRightObj = rightObj.anchoredPosition;

        Vector2 startLeftStrip = new Vector2(-2000f, targetLeftStrip.y);
        Vector2 startRightStrip = new Vector2(2000f, targetRightStrip.y);
        Vector2 startLeftObj = new Vector2(-2000f, targetLeftObj.y);
        Vector2 startRightObj = new Vector2(2000f, targetRightObj.y);

        leftStrip.anchoredPosition = startLeftStrip;
        rightStrip.anchoredPosition = startRightStrip;
        leftObj.anchoredPosition = startLeftObj;
        rightObj.anchoredPosition = startRightObj;
        centerLogo.alpha = 0;
        centerLogo.transform.localScale = Vector3.zero;

        float elapsed = 0;
        float stripInDuration = 0.4f;
        while (elapsed < stripInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / stripInDuration);
            leftStrip.anchoredPosition = Vector2.Lerp(startLeftStrip, targetLeftStrip, t);
            rightStrip.anchoredPosition = Vector2.Lerp(startRightStrip, targetRightStrip, t);
            yield return null;
        }

        elapsed = 0;
        float objInDuration = 0.5f;
        while (elapsed < objInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / objInDuration);
            leftObj.anchoredPosition = Vector2.Lerp(startLeftObj, targetLeftObj, t);
            rightObj.anchoredPosition = Vector2.Lerp(startRightObj, targetRightObj, t);
            centerLogo.alpha = t;
            centerLogo.transform.localScale = Vector3.one * t;
            yield return null;
        }

        panelPhase2.SetActive(false);
        panelPhase3.SetActive(true);
        panelAnimation.SetActive(true);

        yield return new WaitForSeconds(0.8f);

        elapsed = 0;
        float objOutDuration = 0.5f;
        while (elapsed < objOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / objOutDuration);
            leftObj.anchoredPosition = Vector2.Lerp(targetLeftObj, startLeftObj, t);
            rightObj.anchoredPosition = Vector2.Lerp(targetRightObj, startRightObj, t);
            centerLogo.alpha = 1 - t;
            centerLogo.transform.localScale = Vector3.one * (1 - t);
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);

        elapsed = 0;
        float stripOutDuration = 1.0f;
        while (elapsed < stripOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / stripOutDuration);
            leftStrip.anchoredPosition = Vector2.Lerp(targetLeftStrip, startLeftStrip, t);
            rightStrip.anchoredPosition = Vector2.Lerp(targetRightStrip, startRightStrip, t);
            yield return null;
        }

        panelTransition.SetActive(false);
        currentPhase = GamePhase.Animation;

        CoinFlipController flip = FindFirstObjectByType<CoinFlipController>();
        if (flip != null)
        {
            flip.transform.localRotation = Quaternion.identity;
            flip.StartCoinFlip();
        }
    }

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
        panelAnimation.transform.localRotation = Quaternion.identity;
        string winnerName = (winnerID == 0) ? "PLAYER 1" : "PLAYER 2";
        TextMeshProUGUI animText = panelAnimation.GetComponentInChildren<TextMeshProUGUI>();
        if (animText != null) animText.text = "<color=yellow>" + winnerName + "</color> GO FIRST!";
        yield return new WaitForSeconds(1.5f);
        panelAnimation.SetActive(false);
        UpdateBoardVisuals();
    }

    public void ExecuteTurn(int tileIndex, TileButton tile, int boardOwnerID)
    {
        if (currentPhase != GamePhase.Phase3) return;
        if (isP1Turn && boardOwnerID != 1) return;
        if (!isP1Turn && boardOwnerID != 2) return;
        List<int> targetList = (isP1Turn) ? p1TargetBombs : p2TargetBombs;
        if (targetList.Contains(tileIndex))
        {
            tile.SetVisual(hitSprite);
            if (isP1Turn) p1HitCount++; else p2HitCount++;
            UpdateHeartsUI(isP1Turn ? 1 : 2);
        }
        else tile.SetVisual(missSprite);
        tile.SetInteractable(false);
        CheckWinCondition();
        if (!panelWin.activeSelf) { isP1Turn = !isP1Turn; UpdateBoardVisuals(); }
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
            foreach (TileButton tile in tilesInP2) tile.transform.localScale = new Vector3(-1, -1, 1);
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