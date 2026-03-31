using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

[System.Serializable]
public class BottleData
{
    public int bottleID;
    public Sprite bottleSprite;
}

public class BottleItem : MonoBehaviour
{
    public int ID;
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
    public GameObject bottlePrefab;

    [Header("Shuffle & Curtain Settings")]
    public RectTransform curtainRect;
    public float widthPerBottle = 160f;
    public float paddingWidth = 100f;
    public float shuffleDuration = 1.2f;
    public float curtainMoveDuration = 0.6f;

    [Header("Visual Shelves (Decor)")]
    public RectTransform topShelfVisual;
    public RectTransform bottomShelfVisual;

    private int currentTotalScore = 0;
    private int currentTurn = 1;
    private int currentLevelCount = 3;
    private List<int> targetIndexes = new List<int>();
    private List<Image> bottomImages = new List<Image>();
    private List<GameObject> topBottles = new List<GameObject>();
    private List<bool> isPosCorrect = new List<bool>();
    private GameObject firstSelected;
    private bool isGameOver = false;
    private bool isProcessingTurn = false;

    private Color numberColor = Color.black;
    private const int MODE_ID = 11;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(MODE_ID);
        UpdateTurnUI();
        StartNewRound();
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();
    }

    public void StartNewRound()
    {
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

        bottomImages.Clear();
        topBottles.Clear();
        targetIndexes.Clear();
        isPosCorrect.Clear();
        firstSelected = null;

        StartCoroutine(ShuffleSequence());
    }

    IEnumerator ShuffleSequence()
    {
        isProcessingTurn = true;
        float targetWidth = (currentLevelCount * widthPerBottle) + paddingWidth;
        float slowCurtainDuration = curtainMoveDuration * 1.5f;

        UpdateShelfSize(targetWidth);
        if (curtainRect != null)
        {
            curtainRect.sizeDelta = new Vector2(targetWidth, curtainRect.sizeDelta.y);
            curtainRect.gameObject.SetActive(true);
            float canvasWidth = curtainRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;
            float startX = (canvasWidth / 2) + (targetWidth / 2) + 100f;
            curtainRect.anchoredPosition = new Vector2(startX, curtainRect.anchoredPosition.y);
            yield return curtainRect.DOAnchorPosX(0, slowCurtainDuration).SetEase(Ease.OutQuad).WaitForCompletion();
        }

        List<int> availableIDs = new List<int>();
        masterBottleList.ForEach(x => availableIDs.Add(x.bottleID));
        ShuffleList(availableIDs);

        for (int i = 0; i < currentLevelCount; i++)
        {
            int bID = availableIDs[i];
            targetIndexes.Add(bID);
            isPosCorrect.Add(false);

            // --- TẦNG DƯỚI ---
            GameObject bBottom = Instantiate(bottlePrefab, bottomShelf);
            if (bBottom == null) { Debug.LogError("QUÊN KÉO PREFAB VÀO INSPECTOR!"); yield break; }

            BottleController ctrlBottom = bBottom.GetComponent<BottleController>();
            Transform bottleChild = bBottom.transform.Find("Bottle");

            if (bottleChild != null)
            {
                Image imgBottom = bottleChild.GetComponent<Image>();
                bottomImages.Add(imgBottom); // Nạp vào list ĐỂ TRÁNH NULL DÒNG 174
                if (ctrlBottom != null)
                    ctrlBottom.PlayLowerSmoke(masterBottleList.Find(x => x.bottleID == bID).bottleSprite);
            }
            else { Debug.LogError("PREFAB THIẾU OBJECT CON TÊN 'Bottle'!"); }

            // --- TẦNG TRÊN ---
            GameObject bTop = Instantiate(bottlePrefab, topShelf);
            Transform bottleTopChild = bTop.transform.Find("Bottle");
            if (bottleTopChild != null)
                bottleTopChild.GetComponent<Image>().sprite = masterBottleList.Find(x => x.bottleID == bID).bottleSprite;

            bTop.AddComponent<BottleItem>().ID = bID;
            bTop.GetComponent<Button>().interactable = false;
            bTop.GetComponent<Button>().onClick.AddListener(() => OnBottleClick(bTop));
            topBottles.Add(bTop);
        }

        yield return new WaitForEndOfFrame();

        // Tắt Layout Group
        var lgTop = topShelf.GetComponent<HorizontalOrVerticalLayoutGroup>();
        var lgBottom = bottomShelf.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (lgTop) lgTop.enabled = false;
        if (lgBottom) lgBottom.enabled = false;

        // --- FIX LỖI DÒNG 174: Đảm bảo List có đủ số lượng trước khi truy cập ---
        Vector3[] topLocals = new Vector3[topBottles.Count];
        Vector3[] bottomLocals = new Vector3[bottomImages.Count];

        for (int i = 0; i < currentLevelCount; i++)
        {
            // Kiểm tra từng phần tử trước khi lấy tọa độ
            if (i < topBottles.Count && topBottles[i] != null)
                topLocals[i] = topBottles[i].transform.localPosition;

            if (i < bottomImages.Count && bottomImages[i] != null)
                bottomLocals[i] = bottomImages[i].transform.parent.localPosition; // Lấy vị trí của Prefab (cha)
        }

        // XÁO TẦNG DƯỚI (Chỉ chạy nếu đủ số lượng)
        if (bottomImages.Count >= currentLevelCount)
        {
            Sequence bottomSeq = DOTween.Sequence();
            List<int> currentPosIdx = new List<int>();
            for (int i = 0; i < currentLevelCount; i++) currentPosIdx.Add(i);

            for (int s = 0; s < 8; s++)
            {
                int r1 = Random.Range(0, currentLevelCount);
                int r2 = Random.Range(0, currentLevelCount);
                while (r1 == r2) r2 = Random.Range(0, currentLevelCount);

                int tempIdx = currentPosIdx[r1];
                currentPosIdx[r1] = currentPosIdx[r2];
                currentPosIdx[r2] = tempIdx;

                bottomSeq.Append(bottomImages[r1].transform.parent.DOLocalMove(bottomLocals[currentPosIdx[r1]], 0.2f).SetEase(Ease.InOutQuad));
                bottomSeq.Join(bottomImages[r2].transform.parent.DOLocalMove(bottomLocals[currentPosIdx[r2]], 0.2f).SetEase(Ease.InOutQuad));
            }

            for (int i = 0; i < bottomImages.Count; i++)
                bottomSeq.Join(bottomImages[i].transform.parent.DOLocalMove(bottomLocals[i], 0.3f));

            bottomSeq.OnComplete(() => {
                foreach (var img in bottomImages)
                    img.GetComponentInParent<BottleController>()?.PlayLowerSmoke(grayBottleSprite);
            });
            yield return bottomSeq.WaitForCompletion();
        }

        // --- XÁO TẦNG TRÊN ---
        List<int> newIndices = new List<int>();
        for (int i = 0; i < currentLevelCount; i++) newIndices.Add(i);
        EnsureDerangement(newIndices);

        Sequence topSeq = DOTween.Sequence();
        for (int i = 0; i < topBottles.Count; i++)
        {
            int idx = i;
            topSeq.Join(topBottles[idx].transform.DOLocalMove(topLocals[newIndices[idx]], 0.8f).SetEase(Ease.InCubic)
                .OnComplete(() => topBottles[idx].GetComponent<BottleController>()?.PlayUpperLand()));
        }
        yield return topSeq.WaitForCompletion();

        // Sắp xếp lại list sau xáo trộn
        GameObject[] reordered = new GameObject[topBottles.Count];
        for (int i = 0; i < topBottles.Count; i++) reordered[newIndices[i]] = topBottles[i];
        topBottles = new List<GameObject>(reordered);

        // Mở rèm
        if (curtainRect != null)
        {
            float canvasWidth = curtainRect.GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;
            float endX = -((canvasWidth / 2) + (curtainRect.sizeDelta.x / 2) + 100f);
            yield return curtainRect.DOAnchorPosX(endX, slowCurtainDuration).SetEase(Ease.InQuad).WaitForCompletion();
            curtainRect.gameObject.SetActive(false);
        }

        topBottles.ForEach(b => b.GetComponent<Button>().interactable = true);
        isProcessingTurn = false;
    }

    void UpdateShelfSize(float targetWidth)
    {
        if (topShelfVisual != null) topShelfVisual.sizeDelta = new Vector2(targetWidth, topShelfVisual.sizeDelta.y);
        if (bottomShelfVisual != null) bottomShelfVisual.sizeDelta = new Vector2(targetWidth, bottomShelfVisual.sizeDelta.y);
        RectTransform rtTop = topShelf as RectTransform;
        RectTransform rtBottom = bottomShelf as RectTransform;
        if (rtTop != null) rtTop.sizeDelta = new Vector2(targetWidth, rtTop.sizeDelta.y);
        if (rtBottom != null) rtBottom.sizeDelta = new Vector2(targetWidth, rtBottom.sizeDelta.y);
    }

    void EnsureDerangement(List<int> indices)
    {
        if (indices.Count <= 1) return;
        int safety = 0;
        while (safety < 100)
        {
            ShuffleList(indices);
            bool hasMatch = false;
            for (int j = 0; j < indices.Count; j++) if (indices[j] == j) { hasMatch = true; break; }
            if (!hasMatch) break;
            safety++;
        }
    }

    void UpdateScoreText()
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(numberColor);
        totalScoreText.text = $"Bottle Collected: <color=#{hexColor}>{currentTotalScore}</color>";
    }

    void OnBottleClick(GameObject clickedBottle)
    {
        if (isGameOver || isProcessingTurn) return;
        if (firstSelected == null)
        {
            firstSelected = clickedBottle;
            firstSelected.transform.DOLocalMoveY(30f, 0.2f).SetRelative(true);
        }
        else if (firstSelected == clickedBottle)
        {
            firstSelected.transform.DOLocalMoveY(-30f, 0.2f).SetRelative(true);
            firstSelected = null;
        }
        else { SwapBottles(firstSelected, clickedBottle); }
    }

    void SwapBottles(GameObject a, GameObject b)
    {
        isProcessingTurn = true;
        int indexA = topBottles.IndexOf(a);
        int indexB = topBottles.IndexOf(b);
        a.GetComponent<Button>().interactable = false;
        b.GetComponent<Button>().interactable = false;

        Sequence swapSeq = DOTween.Sequence();
        swapSeq.Join(a.transform.DOMove(b.transform.position, 0.4f).SetEase(Ease.InOutQuad));
        swapSeq.Join(b.transform.DOMove(a.transform.position, 0.4f).SetEase(Ease.InOutQuad));

        swapSeq.OnComplete(() => {
            a.transform.SetSiblingIndex(indexB);
            b.transform.SetSiblingIndex(indexA);
            topBottles[indexA] = b;
            topBottles[indexB] = a;
            CheckAllPositionsAfterSwap();
            firstSelected = null;
        });
    }

    void CheckAllPositionsAfterSwap()
    {
        int scoreChange = 0;
        bool hasNewlyCorrectBottle = false;

        for (int i = 0; i < topBottles.Count; i++)
        {
            int currentTopID = topBottles[i].GetComponent<BottleItem>().ID;
            int correctID = targetIndexes[i];

            if (isPosCorrect[i] && currentTopID != correctID)
            {
                isPosCorrect[i] = false;
                bottomImages[i].GetComponentInParent<BottleController>()?.PlayLowerSmoke(grayBottleSprite);
                currentTotalScore--;
                scoreChange--;
            }
            else if (!isPosCorrect[i] && currentTopID == correctID)
            {
                isPosCorrect[i] = true;
                Sprite correctSprite = masterBottleList.Find(x => x.bottleID == correctID).bottleSprite;
                bottomImages[i].GetComponentInParent<BottleController>()?.PlayLowerSmoke(correctSprite);
                currentTotalScore++;
                scoreChange++;
                hasNewlyCorrectBottle = true;
            }
        }

        if (scoreChange > 0)
        {
            DOTween.To(() => numberColor, x => numberColor = x, Color.green, 0.2f).OnUpdate(UpdateScoreText);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Correct");
        }
        else if (scoreChange < 0)
        {
            DOTween.To(() => numberColor, x => numberColor = x, Color.red, 0.2f).OnUpdate(UpdateScoreText)
                .OnComplete(() => DOTween.To(() => numberColor, x => numberColor = x, Color.black, 0.8f).OnUpdate(UpdateScoreText));
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Wrong");
        }

        if (!isPosCorrect.Contains(false)) StartCoroutine(WinSequence());
        else StartCoroutine(WaitAndHandleTurn(hasNewlyCorrectBottle));
    }

    IEnumerator WinSequence()
    {
        isGameOver = true;
        isProcessingTurn = true;
        if (fireworkEffect != null) fireworkEffect.Play();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Win");
        yield return new WaitForSeconds(delayBeforeWinPanel);
        winPanel.SetActive(true);
        player1Crown.SetActive(currentTurn == 1);
        player2Crown.SetActive(currentTurn == 2);
    }

    public void OnNextLevelClick()
    {
        currentLevelCount = Mathf.Min(currentLevelCount + 1, 7);
        StartNewRound();
    }

    public void OnBackToMode1Click()
    {
        SceneManager.LoadScene("SelectScene");
    }

    void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    IEnumerator WaitAndHandleTurn(bool keepTurn)
    {
        yield return new WaitForSeconds(0.5f);
        if (!keepTurn)
        {
            currentTurn = (currentTurn == 1) ? 2 : 1;
            UpdateTurnUI();
        }
        isProcessingTurn = false;
    }

    void UpdateTurnUI()
    {
        player1Cover.SetActive(currentTurn != 1);
        player2Cover.SetActive(currentTurn != 2);
        if (turnNotificationText != null)
        {
            string pName = PlayerPrefs.GetString(currentTurn == 1 ? "PlayerName_P1" : "PlayerName_P2", currentTurn == 1 ? "P1" : "P2");
            turnNotificationText.text = pName.ToUpper() + "'S TURN";
        }
    }

    public void OnSettingClick() { settingPanel.SetActive(true); }
    public void OnCloseSettingClick() { settingPanel.SetActive(false); }
}