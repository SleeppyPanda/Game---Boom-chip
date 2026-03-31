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

    [Header("Tutorial System")]
    public GameObject tutorialStep1;
    public GameObject tutorialStep2;
    public GameObject tutorialWinTip;
    private bool isTutorialActive = false;

    [Header("Win Effects & Objects")]
    public GameObject player1Crown;
    public GameObject player2Crown;
    public ParticleSystem fireworkEffect;
    public float delayBeforeWinPanel = 1.2f;

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

    void Awake() => Instance = this;

    void Start()
    {
        UpdateTurnUI();
        isTutorialActive = (PlayerPrefs.GetInt("Mode2TutorialDone", 0) == 0);

        // Nếu đã xong tutorial, random số lượng chai ngay từ đầu
        if (!isTutorialActive)
        {
            currentLevelCount = Random.Range(4, 10); // Random từ 3 đến 6 chai
        }
        else
        {
            currentLevelCount = 3; // Mặc định 3 chai cho tutorial
        }

        StartNewRound();
    }

    public void StartNewRound()
    {
        StopAllCoroutines();
        DOTween.KillAll();
        isGameOver = false;
        isProcessingTurn = true;
        winPanel.SetActive(false);
        player1Crown.SetActive(false);
        player2Crown.SetActive(false);
        if (fireworkEffect != null) fireworkEffect.Stop();

        currentTotalScore = 0;
        numberColor = Color.black;
        UpdateScoreText();

        foreach (Transform t in topShelf) Destroy(t.gameObject);
        foreach (Transform t in bottomShelf) Destroy(t.gameObject);

        bottomBottles.Clear();
        topBottles.Clear();
        isPosCorrect.Clear();
        firstSelected = null;

        StartCoroutine(MainGameFlow());
    }

    IEnumerator MainGameFlow()
    {
        isProcessingTurn = true;
        ToggleLayouts(true);

        float targetWidth = (currentLevelCount * widthPerBottle) + paddingWidth;
        UpdateShelfSize(targetWidth);

        yield return new WaitForEndOfFrame();
        SyncTopShelfSize();

        if (curtainRect != null)
        {
            curtainRect.sizeDelta = new Vector2(targetWidth, curtainRect.sizeDelta.y);
            curtainRect.gameObject.SetActive(true);
            curtainRect.anchoredPosition = new Vector2(0f, curtainRect.anchoredPosition.y);
        }

        List<int> availableIDs = masterBottleList.Select(x => x.bottleID).ToList();
        ShuffleList(availableIDs);
        List<int> selectedIDs = availableIDs.Take(currentLevelCount).ToList();

        // Bước 1: Khởi tạo - Cả trên và dưới đều hiện ảnh có màu để người chơi ghi nhớ
        for (int i = 0; i < currentLevelCount; i++)
        {
            int bID = selectedIDs[i];
            var bottleData = masterBottleList.Find(x => x.bottleID == bID);

            isPosCorrect.Add(false);

            // Khởi tạo chai dưới (Ảnh màu)
            GameObject bBottom = Instantiate(bottleBottomPrefab, bottomShelf);
            bBottom.GetComponent<Image>().sprite = bottleData.bottleSprite;
            BottleItem bottomItem = bBottom.GetComponent<BottleItem>() ?? bBottom.AddComponent<BottleItem>();
            bottomItem.ID = bID;
            bottomBottles.Add(bBottom);
            SetupBottleRect(bBottom.GetComponent<RectTransform>());

            // Khởi tạo chai trên (Ảnh màu)
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

        // Bước 2: Shuffle kệ dưới và CHUYỂN SANG ẢNH XÁM
        yield return StartCoroutine(FastShuffleRoutine(bottomBottles));
        foreach (var b in bottomBottles)
        {
            b.GetComponent<Image>().sprite = grayBottleSprite;
            b.GetComponent<BottleController>()?.InactiveUpperLand();
        }
        bottomBottles = bottomBottles.OrderBy(go => go.transform.localPosition.x).ToList();

        // Bước 3: Shuffle kệ trên
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

        if (curtainRect != null)
        {
            float canvasWidth = curtainRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;
            float endX = -((canvasWidth / 2) + (curtainRect.sizeDelta.x / 2) + 200f);
            yield return curtainRect.DOAnchorPosX(endX, curtainMoveDuration).SetEase(Ease.InQuad).WaitForCompletion();
            curtainRect.gameObject.SetActive(false);
        }

        topBottles.ForEach(b => b.GetComponent<Button>().interactable = true);
        isProcessingTurn = false;

        // --- THÊM LOGIC TUTORIAL VÀO ĐÂY ---
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

            // Logic thay đổi ảnh: Đúng hiện màu, sai hiện xám
            if (isPosCorrect[i] != currentlyCorrect)
            {
                isPosCorrect[i] = currentlyCorrect;
                Sprite targetSprite;

                if (currentlyCorrect)
                {
                    // Lấy lại ảnh màu từ master data dựa trên ID
                    var data = masterBottleList.Find(x => x.bottleID == topID);
                    targetSprite = data != null ? data.bottleSprite : grayBottleSprite;
                    hasCorrected = true;
                    PlayBounceEffect(bottomBottles[i]);
                }
                else
                {
                    targetSprite = grayBottleSprite;
                }

                // Cập nhật hiệu ứng và sprite
                bottomBottles[i].GetComponent<BottleController>()?.PlayLowerSmoke(targetSprite);
                bottomBottles[i].GetComponent<Image>().sprite = targetSprite;
            }
        }

        UpdateScoreLogic();

        if (!isPosCorrect.Contains(false))
            StartCoroutine(WinSequence());
        else
            StartCoroutine(WaitAndHandleTurn(hasCorrected));

        if (isTutorialActive)
        {
            isTutorialActive = false; // Tắt trạng thái tutorial
            PlayerPrefs.SetInt("Mode2TutorialDone", 1); // Lưu lại đã hoàn thành
            PlayerPrefs.Save();

            if (tutorialWinTip)
            {
                tutorialWinTip.SetActive(true);
                // Thông báo này sẽ tự ẩn sau 4 giây
                DOVirtual.DelayedCall(4f, () => {
                    if (tutorialWinTip != null) tutorialWinTip.SetActive(false);
                });
            }
        }

        if (!isPosCorrect.Contains(false))
            StartCoroutine(WinSequence());
        else
            StartCoroutine(WaitAndHandleTurn(hasCorrected));
    }

    // Hàm hiệu ứng bàn tay nhấp nhô
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
            firstSelected = clickedBottle;
            firstSelected.transform.DOLocalMoveY(35f, 0.2f).SetRelative(true);

            // Chuyển sang bước 2
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
            firstSelected.transform.DOLocalMoveY(-35f, 0.2f).SetRelative(true);
            firstSelected = null;

            // Nếu bỏ chọn, quay lại bước 1
            if (isTutorialActive)
            {
                if (tutorialStep1) tutorialStep1.SetActive(true);
                if (tutorialStep2) tutorialStep2.SetActive(false);
            }
        }
        else
        {
            isProcessingTurn = true;
            // Tắt Step 2 khi bắt đầu swap
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
        yield return new WaitForSeconds(delayBeforeWinPanel);
        winPanel.SetActive(true);
        player1Crown.SetActive(currentTurn == 1);
        player2Crown.SetActive(currentTurn == 2);
    }

    IEnumerator WaitAndHandleTurn(bool keep)
    {
        yield return new WaitForSeconds(0.5f);
        if (!keep) { currentTurn = currentTurn == 1 ? 2 : 1; UpdateTurnUI(); }
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
        // 1. Cố định chiều rộng tối đa theo yêu cầu của bạn
        float maxShelfWidth = 1080f;

        // 2. Chiều rộng thực tế của kệ gỗ (Visual) không được vượt quá 1080
        float targetWidth = Mathf.Min(w, maxShelfWidth);

        // 3. Cập nhật kích thước khung kệ gỗ
        if (topShelfVisual) topShelfVisual.sizeDelta = new Vector2(targetWidth, topShelfVisual.sizeDelta.y);
        if (bottomShelfVisual) bottomShelfVisual.sizeDelta = new Vector2(targetWidth, bottomShelfVisual.sizeDelta.y);

        // 4. Tính toán tỉ lệ thu nhỏ nếu tổng độ rộng chai vượt quá 1080
        float requiredScale = 1f;
        if (w > maxShelfWidth)
        {
            requiredScale = maxShelfWidth / w;
        }

        // 5. Áp dụng Scale cho Container
        topShelf.localScale = new Vector3(requiredScale, requiredScale, 1f);
        bottomShelf.localScale = new Vector3(requiredScale, requiredScale, 1f);

        // Cập nhật SizeDelta của Container để HorizontalLayoutGroup căn giữa đúng
        if (topShelf is RectTransform rt) rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
        if (bottomShelf is RectTransform rb) rb.sizeDelta = new Vector2(w, rb.sizeDelta.y);

        // 6. ĐIỀU CHỈNH VỊ TRÍ Y ĐỂ CHAI CHẠM MẶT KỆ
        // Giả sử chiều cao Prefab chai của bạn là khoảng 150-200 đơn vị.
        // Khi scale nhỏ lại, chúng ta cần hạ thấp trục Y xuống.

        float bottleBaseHeight = 160f; // Thay số này bằng chiều cao thực tế của Prefab chai
        float yOffset = (bottleBaseHeight * (1f - requiredScale)) / 2f;

        // Áp dụng vị trí Y cho Container để kéo toàn bộ hàng chai xuống
        // Nếu Container của bạn đang ở Pos Y = 0, thì trừ đi yOffset
        topShelf.localPosition = new Vector3(topShelf.localPosition.x, -yOffset, 0f);
        bottomShelf.localPosition = new Vector3(bottomShelf.localPosition.x, -yOffset, 0f);
    }

    public void OnNextLevelClick()
    {
        // Random số lượng chai từ 3 đến 6 (hoặc 7 tùy bạn)
        currentLevelCount = Random.Range(4, 10);
        StartNewRound();
    }
    public void OnBackToMode1Click() => SceneManager.LoadScene("SelectScene");
    public void OnSettingClick() => settingPanel.SetActive(true);
    public void OnCloseSettingClick() => settingPanel.SetActive(false);
    // Thêm hàm này vào trong class Mode2Manager
    private void PlayBounceEffect(GameObject bottle)
    {
        if (bottle == null) return;

        // Hiệu ứng nẩy lên: Di chuyển lên 20 đơn vị rồi rơi về vị trí cũ với độ đàn hồi (Elastic/Back)
        // Sau đó co giãn nhẹ (Scale) để tạo cảm giác mềm mại
        Sequence bounceSeq = DOTween.Sequence();

        // Nẩy lên và rơi xuống
        bounceSeq.Append(bottle.transform.DOLocalMoveY(20f, 0.3f).SetRelative(true).SetEase(Ease.OutQuad));
        bounceSeq.Append(bottle.transform.DOLocalMoveY(-20f, 0.5f).SetRelative(true).SetEase(Ease.OutBounce));

        // Co giãn nhẹ
        bounceSeq.Join(bottle.transform.DOScale(new Vector3(1.1f, 0.9f, 1f), 0.2f).SetLoops(2, LoopType.Yoyo));
    }
}