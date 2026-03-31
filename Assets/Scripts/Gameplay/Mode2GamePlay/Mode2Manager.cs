using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Linq;

[System.Serializable]
public class BottleData
{
    public int bottleID;
    public Sprite bottleSprite;
}

public class Mode2Manager : MonoBehaviour
{
    public static Mode2Manager Instance;

    [Header("UI & Players")]
    public GameObject player1Cover;
    public GameObject player2Cover;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI turnNotificationText;
    public GameObject winPanel;
    public GameObject settingPanel;

    [Header("Audio Settings")]
    public string sfxBottleSelect = "BottleSelect";
    public string sfxBottleLand = "BottleLand";
    public string sfxCorrectMatch = "CorrectMatch";
    public string sfxPopupShow = "PopupShow";
    public string sfxWin = "Win";
    public string bgmMusic = "BackgroundMusic";

    [Header("Turn Popup Simple")]
    public GameObject turnPopupObj;     // Kéo TurnPopup vào đây
    public GameObject player1TextObj;   // Kéo Player1Text vào đây
    public GameObject player2TextObj;   // Kéo Player2Text vào đây

    [Header("Transition Strips (New)")]
    public Transform mainCanvas;
    public GameObject transitionPrefab; // Kéo Prefab Transition vào đây trong Inspector
    private GameObject currentTransitionObj;

    [Header("Tutorial System")]
    public GameObject tutorialStep1;
    public GameObject tutorialStep2;
    public GameObject tutorialWinTip;
    private bool isTutorialActive = false;

    [Header("Win Effects & Objects")]
    public GameObject player1Crown;
    public GameObject player2Crown;
    public ParticleSystem fireworkEffect;
    public float delayBeforeWinPanel = 2.0f;

    [Header("Dữ liệu chai")]
    public List<BottleData> masterBottleList;
    public Sprite grayBottleSprite;

    [Header("Containers")]
    public Transform topShelf;
    public Transform bottomShelf;

    [Header("Prefabs")]
    public GameObject bottleTopPrefab;
    public GameObject bottleBottomPrefab;

    [Header("Shuffle Settings")]
    public RectTransform curtainRect;
    public float widthPerBottle = 160f;
    public float paddingWidth = 100f;
    public float curtainMoveDuration = 0.6f;

    [Header("Visual Shelves (Decor)")]
    public RectTransform topShelfVisual;
    public RectTransform bottomShelfVisual;

    private int currentTotalScore = 0;
    private int currentTurn = 1;
    private int currentLevelCount = 3;

    private List<GameObject> bottomBottles = new List<GameObject>();
    private List<GameObject> topBottles = new List<GameObject>();
    private List<bool> isPosCorrect = new List<bool>();
    private GameObject firstSelected;
    private bool isGameOver = false;
    private bool isProcessingTurn = false;
    private Color numberColor = Color.black;
    private bool isFirstStart = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(bgmMusic);
        }

        UpdateTurnUI();
        isTutorialActive = (PlayerPrefs.GetInt("Mode2TutorialDone", 0) == 0);
        currentLevelCount = isTutorialActive ? 3 : Random.Range(4, 10);

        StartNewRound();
    }

    public void StartNewRound()
    {
        // 1. Dọn dẹp các Coroutine và trạng thái cũ
        StopAllCoroutines();
        isGameOver = false;
        isProcessingTurn = true;
        winPanel.SetActive(false);
        player1Crown.SetActive(false);
        player2Crown.SetActive(false);

        if (fireworkEffect != null) fireworkEffect.Stop();

        // 2. Reset điểm số và hình ảnh
        currentTotalScore = 0;
        numberColor = Color.black;
        UpdateScoreText();

        // 3. Xóa các chai cũ trên kệ
        foreach (Transform t in topShelf) Destroy(t.gameObject);
        foreach (Transform t in bottomShelf) Destroy(t.gameObject);

        bottomBottles.Clear();
        topBottles.Clear();
        isPosCorrect.Clear();
        firstSelected = null;

        // 4. Logic Transition (Chỉ chạy lần đầu tiên vào Scene)
        if (isFirstStart && transitionPrefab != null)
        {
            currentTransitionObj = Instantiate(transitionPrefab, mainCanvas);
            currentTransitionObj.transform.SetAsLastSibling(); // Đảm bảo đè lên trên UI khác
        }
        else
        {
            currentTransitionObj = null; // Đảm bảo không có rác dữ liệu từ màn trước
        }

        // 5. Bắt đầu luồng game chính
        StartCoroutine(MainGameFlow());
    }

    #region TRANSITION LOGIC (Updated to use CoreUiTransition script)
    private IEnumerator RunStartTransition()
    {
        if (currentTransitionObj == null) yield break;

        CanvasGroup cg = currentTransitionObj.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; }

        // Prefab khi sinh ra đã tự động gọi PlayInAnimation() nhờ biến playOnStart trong CoreUiTransition.
        // Chờ 2 giây để người chơi nhìn thấy UI (Bao gồm thời gian bay vào và thời gian đứng im hiển thị)
        yield return new WaitForSeconds(2.0f);

        // Lấy script CoreUiTransition từ object để gọi lệnh bay ra
        CoreUiTransition transScript = currentTransitionObj.GetComponent<CoreUiTransition>();

        if (transScript != null)
        {
            // Gọi hàm bay ra. Khi bay xong, script kia sẽ tự động gọi Destroy(gameObject)
            transScript.PlayOutAnimation();
        }
        else
        {
            // Đề phòng trường hợp bạn quên gắn script vào prefab
            Destroy(currentTransitionObj);
        }

        // QUAN TRỌNG: Chờ cho đến khi object bị Destroy hoàn toàn thì mới cho game chạy tiếp
        yield return new WaitUntil(() => currentTransitionObj == null);

        Debug.Log("<color=green>[Mode2Manager]</color> Transition đã mở xong, bắt đầu chơi!");
    }
    #endregion

    IEnumerator MainGameFlow()
    {
        isProcessingTurn = true;
        ToggleLayouts(true);

        float targetWidth = (currentLevelCount * widthPerBottle) + paddingWidth;
        UpdateShelfSize(targetWidth);

        yield return new WaitForEndOfFrame();
        SyncTopShelfSize();

        if (isFirstStart && currentTransitionObj != null)
        {
            yield return StartCoroutine(RunStartTransition());
            isFirstStart = false; // Đánh dấu đã qua lần đầu, các lần Next Level sau sẽ bỏ qua
        }

        if (curtainRect != null)
        {
            curtainRect.gameObject.SetActive(true);

            // 1. Tính toán Scale dựa trên giới hạn 1080
            float maxCurtainWidth = 1080f;
            float curtainScale = 1f;

            if (targetWidth > maxCurtainWidth)
            {
                curtainScale = maxCurtainWidth / targetWidth;
            }

            // 2. Áp dụng kích thước và Scale
            // Chiều rộng vẫn set theo targetWidth để nội dung bên trong (nếu có) không bị bóp
            // Nhưng Scale sẽ làm nó trông nhỏ lại vừa màn hình
            curtainRect.sizeDelta = new Vector2(targetWidth, curtainRect.sizeDelta.y);
            curtainRect.localScale = new Vector3(curtainScale, curtainScale, 1f);

            // 3. Đẩy vị trí xuống đáy (Pivot của Curtain nên để là 0.5, 0.5)
            // Tính toán offset dựa trên chiều cao bị mất đi do scale
            float baseHeight = curtainRect.sizeDelta.y;
            float yOffset = (baseHeight * (1f - curtainScale)) / 2f;

            // Giữ nguyên X ở giữa (0), đẩy Y xuống dưới
            curtainRect.anchoredPosition = new Vector2(0f, -yOffset);
        }

        List<int> availableIDs = masterBottleList.Select(x => x.bottleID).ToList();
        ShuffleList(availableIDs);
        List<int> selectedIDs = availableIDs.Take(currentLevelCount).ToList();

        for (int i = 0; i < currentLevelCount; i++)
        {
            int bID = selectedIDs[i];
            var bottleData = masterBottleList.Find(x => x.bottleID == bID);
            isPosCorrect.Add(false);

            GameObject bBottom = Instantiate(bottleBottomPrefab, bottomShelf);
            bBottom.GetComponent<Image>().sprite = bottleData.bottleSprite;
            BottleItem bottomItem = bBottom.GetComponent<BottleItem>() ?? bBottom.AddComponent<BottleItem>();
            bottomItem.ID = bID;
            bottomBottles.Add(bBottom);
            SetupBottleRect(bBottom.GetComponent<RectTransform>());

            GameObject bTop = Instantiate(bottleTopPrefab, topShelf);
            bTop.GetComponent<Image>().sprite = bottleData.bottleSprite;
            BottleItem topItem = bTop.GetComponent<BottleItem>() ?? bTop.AddComponent<BottleItem>();
            topItem.ID = bID;
            topBottles.Add(bTop);
            SetupBottleRect(bTop.GetComponent<RectTransform>());

            Button btnTop = bTop.GetComponent<Button>();
            btnTop.interactable = false;
            btnTop.onClick.AddListener(() => OnBottleClick(bTop));
        }

        yield return new WaitForEndOfFrame();
        foreach (var b in topBottles) b.GetComponent<BottleController>()?.PlayUpperLand();
        yield return new WaitForSeconds(0.5f);
        foreach (var b in topBottles) b.GetComponent<BottleController>()?.InactiveUpperLand();

        ToggleLayouts(false);

        yield return StartCoroutine(FastShuffleRoutine(bottomBottles));
        foreach (var b in bottomBottles)
        {
            b.GetComponent<Image>().sprite = grayBottleSprite;
            b.GetComponent<BottleController>()?.InactiveUpperLand();
        }
        bottomBottles = bottomBottles.OrderBy(go => go.transform.localPosition.x).ToList();

        yield return StartCoroutine(FastShuffleRoutine(topBottles));
        foreach (var b in topBottles) b.GetComponent<BottleController>()?.InactiveUpperLand();
        topBottles = topBottles.OrderBy(go => go.transform.localPosition.x).ToList();

        EnsureNoMatchesAtStart();

        if (curtainRect != null)
        {
            float canvasWidth = curtainRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;
            float endX = -((canvasWidth / 2) + (curtainRect.sizeDelta.x / 2) + 200f);
            yield return curtainRect.DOAnchorPosX(endX, curtainMoveDuration).SetEase(Ease.InQuad).WaitForCompletion();
            curtainRect.gameObject.SetActive(false);
        }

        topBottles.ForEach(b => b.GetComponent<Button>().interactable = true);
        isProcessingTurn = false;

        if (isTutorialActive && tutorialStep1 != null)
        {
            tutorialStep1.SetActive(true);
            AnimateHand(tutorialStep1);
        }
    }

    IEnumerator FastShuffleRoutine(List<GameObject> list)
    {
        int swapAttempts = 5;
        float moveSpeed = 0.12f;
        float pauseSpeed = 0.15f;

        for (int s = 0; s < swapAttempts; s++)
        {
            int r1 = Random.Range(0, list.Count);
            int r2 = Random.Range(0, list.Count);
            while (r1 == r2 && list.Count > 1) r2 = Random.Range(0, list.Count);

            Transform t1 = list[r1].transform;
            Transform t2 = list[r2].transform;

            Vector3 p1 = t1.localPosition;
            Vector3 p2 = t2.localPosition;

            t1.DOLocalMove(p2, moveSpeed).SetEase(Ease.Linear);
            t2.DOLocalMove(p1, moveSpeed).SetEase(Ease.Linear);

            yield return new WaitForSeconds(moveSpeed + pauseSpeed);
        }
    }

    void CheckAllPositionsAfterSwap()
    {
        bool hasCorrected = false;
        for (int i = 0; i < topBottles.Count; i++)
        {
            int topID = topBottles[i].GetComponent<BottleItem>().ID;
            int bottomID = bottomBottles[i].GetComponent<BottleItem>().ID;

            bool currentlyCorrect = (topID == bottomID);

            if (isPosCorrect[i] != currentlyCorrect)
            {
                isPosCorrect[i] = currentlyCorrect;
                Sprite targetSprite;

                if (currentlyCorrect)
                {
                    var data = masterBottleList.Find(x => x.bottleID == topID);
                    targetSprite = data != null ? data.bottleSprite : grayBottleSprite;
                    hasCorrected = true;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxCorrectMatch);
                    PlayBounceEffect(bottomBottles[i]);
                }
                else
                {
                    targetSprite = grayBottleSprite;
                }

                bottomBottles[i].GetComponent<BottleController>()?.PlayLowerSmoke(targetSprite);
                bottomBottles[i].GetComponent<Image>().sprite = targetSprite;
            }
        }

        UpdateScoreLogic();

        if (!isPosCorrect.Contains(false))
            StartCoroutine(WinSequence());
        else
            StartCoroutine(WaitAndHandleTurn(hasCorrected));

        if (isTutorialActive && hasCorrected)
        {
            isTutorialActive = false;
            PlayerPrefs.SetInt("Mode2TutorialDone", 1);
            PlayerPrefs.Save();

            if (tutorialWinTip)
            {
                tutorialWinTip.SetActive(true);
                DOVirtual.DelayedCall(4f, () => {
                    if (tutorialWinTip != null) tutorialWinTip.SetActive(false);
                });
            }
        }
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

    void EnsureNoMatchesAtStart()
    {
        int n = topBottles.Count;
        if (n < 2) return;
        int safety = 0;
        bool hasMatch = true;
        while (hasMatch && safety < 100)
        {
            hasMatch = false; safety++;
            for (int i = 0; i < n; i++)
            {
                if (topBottles[i].GetComponent<BottleItem>().ID == bottomBottles[i].GetComponent<BottleItem>().ID)
                {
                    hasMatch = true;
                    int next = (i + 1) % n;
                    GameObject temp = topBottles[i];
                    topBottles[i] = topBottles[next];
                    topBottles[next] = temp;
                    Vector3 p = topBottles[i].transform.localPosition;
                    topBottles[i].transform.localPosition = topBottles[next].transform.localPosition;
                    topBottles[next].transform.localPosition = p;
                }
            }
        }
    }

    void OnBottleClick(GameObject clickedBottle)
    {
        if (isGameOver || isProcessingTurn) return;

        if (firstSelected == null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxBottleSelect);
            firstSelected = clickedBottle;
            firstSelected.transform.DOLocalMoveY(35f, 0.2f).SetRelative(true);

            if (isTutorialActive)
            {
                if (tutorialStep1) tutorialStep1.SetActive(false);
                if (tutorialStep2)
                {
                    tutorialStep2.SetActive(true);
                    AnimateHand(tutorialStep2);
                }
            }
        }
        else if (firstSelected == clickedBottle)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BottleSelect");
            firstSelected.transform.DOLocalMoveY(-35f, 0.2f).SetRelative(true);
            firstSelected = null;

            if (isTutorialActive)
            {
                if (tutorialStep1) tutorialStep1.SetActive(true);
                if (tutorialStep2) tutorialStep2.SetActive(false);
            }
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("BottleSelect");
            isProcessingTurn = true;
            if (isTutorialActive && tutorialStep2) tutorialStep2.SetActive(false);

            GameObject secondSelected = clickedBottle;
            secondSelected.transform.DOLocalMoveY(35f, 0.2f).SetRelative(true).OnComplete(() => {
                SwapBottles(firstSelected, secondSelected);
                firstSelected = null;
            });
        }
    }

    void SwapBottles(GameObject a, GameObject b)
    {
        int indexA = topBottles.IndexOf(a);
        int indexB = topBottles.IndexOf(b);
        float groundY = a.transform.localPosition.y - 35f;
        Vector3 posA = new Vector3(a.transform.localPosition.x, groundY, 0);
        Vector3 posB = new Vector3(b.transform.localPosition.x, groundY, 0);

        Sequence s = DOTween.Sequence();
        s.Append(a.transform.DOLocalMoveX(posB.x, 0.4f).SetEase(Ease.InOutQuad));
        s.Join(b.transform.DOLocalMoveX(posA.x, 0.4f).SetEase(Ease.InOutQuad));
        s.Append(a.transform.DOLocalMoveY(groundY, 0.15f));
        s.Join(b.transform.DOLocalMoveY(groundY, 0.15f));
        s.OnComplete(() => {
            topBottles[indexA] = b; topBottles[indexB] = a;
            StartCoroutine(FinishSwapAnimation(a, b));
        });
    }

    IEnumerator FinishSwapAnimation(GameObject a, GameObject b)
    {
        a.GetComponent<BottleController>()?.PlayUpperLand();
        b.GetComponent<BottleController>()?.PlayUpperLand();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxBottleLand);
        yield return new WaitForSeconds(0.5f);
        a.GetComponent<BottleController>()?.InactiveUpperLand();
        b.GetComponent<BottleController>()?.InactiveUpperLand();
        CheckAllPositionsAfterSwap();
    }

    void UpdateScoreLogic()
    {
        int score = isPosCorrect.Count(c => c);
        if (score > currentTotalScore) UpdateScoreVisuals(1);
        else if (score < currentTotalScore) UpdateScoreVisuals(-1);
        currentTotalScore = score;
        UpdateScoreText();
    }

    void UpdateScoreVisuals(int dir)
    {
        Color c = dir > 0 ? Color.green : Color.red;
        DOTween.To(() => numberColor, x => numberColor = x, c, 0.2f).OnUpdate(UpdateScoreText)
            .OnComplete(() => DOTween.To(() => numberColor, x => numberColor = x, Color.black, 0.8f).OnUpdate(UpdateScoreText));
    }

    void UpdateScoreText() => totalScoreText.text = $"Bottle Collected: <color=#{ColorUtility.ToHtmlStringRGB(numberColor)}>{currentTotalScore}</color>";

    IEnumerator WinSequence()
    {
        isGameOver = true;
        if (fireworkEffect != null) fireworkEffect.Play();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Win");

        yield return new WaitForSeconds(delayBeforeWinPanel);

        winPanel.SetActive(true);
        player1Crown.SetActive(currentTurn == 1);
        player2Crown.SetActive(currentTurn == 2);

        if (AdsManager.Instance != null) AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");
    }

    IEnumerator WaitAndHandleTurn(bool keep)
    {
        yield return new WaitForSeconds(0.5f);
        if (!keep)
        {
            currentTurn = currentTurn == 1 ? 2 : 1;
            UpdateTurnUI();

            // Gọi hàm show popup
            ShowTurnPopup();

            // Chờ cho popup chạy xong rồi mới cho phép người chơi bấm tiếp
            // (0.3s phóng to + 1.0s hiển thị + 0.3s thu nhỏ = 1.6s)
            yield return new WaitForSeconds(1.6f);
        }
        isProcessingTurn = false;
    }

    void UpdateTurnUI()
    {
        player1Cover.SetActive(currentTurn != 1);
        player2Cover.SetActive(currentTurn != 2);
        if (turnNotificationText) turnNotificationText.text = PlayerPrefs.GetString(currentTurn == 1 ? "PlayerName_P1" : "PlayerName_P2", currentTurn == 1 ? "P1" : "P2").ToUpper() + "'S TURN";
    }

    void ToggleLayouts(bool e)
    {
        if (topShelf.GetComponent<HorizontalLayoutGroup>()) topShelf.GetComponent<HorizontalLayoutGroup>().enabled = e;
        if (bottomShelf.GetComponent<HorizontalLayoutGroup>()) bottomShelf.GetComponent<HorizontalLayoutGroup>().enabled = e;
    }

    void SyncTopShelfSize() { if (bottomShelf is RectTransform rb && topShelf is RectTransform rt) rt.sizeDelta = rb.sizeDelta; }

    void SetupBottleRect(RectTransform rt) { rt.localScale = Vector3.one; rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition3D = Vector3.zero; }

    void ShuffleList<T>(List<T> list) { for (int i = 0; i < list.Count; i++) { int r = Random.Range(i, list.Count); T t = list[i]; list[i] = list[r]; list[r] = t; } }

    void UpdateShelfSize(float w)
    {
        float maxShelfWidth = 1080f;
        float targetWidth = Mathf.Min(w, maxShelfWidth);

        if (topShelfVisual) topShelfVisual.sizeDelta = new Vector2(targetWidth, topShelfVisual.sizeDelta.y);
        if (bottomShelfVisual) bottomShelfVisual.sizeDelta = new Vector2(targetWidth, bottomShelfVisual.sizeDelta.y);

        float requiredScale = 1f;
        if (w > maxShelfWidth) requiredScale = maxShelfWidth / w;

        topShelf.localScale = new Vector3(requiredScale, requiredScale, 1f);
        bottomShelf.localScale = new Vector3(requiredScale, requiredScale, 1f);

        if (topShelf is RectTransform rt) rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
        if (bottomShelf is RectTransform rb) rb.sizeDelta = new Vector2(w, rb.sizeDelta.y);

        float bottleBaseHeight = 160f;
        float yOffset = (bottleBaseHeight * (1f - requiredScale)) / 2f;

        topShelf.localPosition = new Vector3(topShelf.localPosition.x, -yOffset, 0f);
        bottomShelf.localPosition = new Vector3(bottomShelf.localPosition.x, -yOffset, 0f);
    }

    public void OnNextLevelClick()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_retry", () => {
                currentLevelCount = Random.Range(4, 10);
                StartNewRound();
            }, 0.3f);
        }
        else
        {
            currentLevelCount = Random.Range(4, 10);
            StartNewRound();
        }
    }

    public void OnBackToMode1Click()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_back_home", () => {
                SceneManager.LoadScene("SelectScene");
            }, 0.3f);
        }
        else SceneManager.LoadScene("SelectScene");
    }

    public void OnSettingClick() => settingPanel.SetActive(true);
    public void OnCloseSettingClick() => settingPanel.SetActive(false);

    private void PlayBounceEffect(GameObject bottle)
    {
        if (bottle == null) return;
        Sequence bounceSeq = DOTween.Sequence();
        bounceSeq.Append(bottle.transform.DOLocalMoveY(20f, 0.3f).SetRelative(true).SetEase(Ease.OutQuad));
        bounceSeq.Append(bottle.transform.DOLocalMoveY(-20f, 0.5f).SetRelative(true).SetEase(Ease.OutBounce));
        bounceSeq.Join(bottle.transform.DOScale(new Vector3(1.1f, 0.9f, 1f), 0.2f).SetLoops(2, LoopType.Yoyo));
    }

    private void ShowTurnPopup()
    {
        if (turnPopupObj == null) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxPopupShow);
        // Bật/tắt text theo đúng người chơi hiện tại
        if (player1TextObj != null) player1TextObj.SetActive(currentTurn == 1);
        if (player2TextObj != null) player2TextObj.SetActive(currentTurn == 2);

        // Reset trạng thái và ngắt các animation cũ (nếu có)
        turnPopupObj.transform.DOKill();
        turnPopupObj.SetActive(true);
        turnPopupObj.transform.localScale = Vector3.zero;

        // Chạy hiệu ứng Pop-up: Phóng to -> Đứng im -> Thu nhỏ -> Tắt
        Sequence seq = DOTween.Sequence();
        seq.Append(turnPopupObj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
        seq.AppendInterval(1.0f); // Thời gian popup hiện trên màn hình (1 giây)
        seq.Append(turnPopupObj.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
        seq.OnComplete(() => turnPopupObj.SetActive(false));
    }
}