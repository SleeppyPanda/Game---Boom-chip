using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class BottleData
{
    public int bottleID;
    public Sprite bottleSprite;
}

public class Mode2Manager : MonoBehaviour
{
    [Header("UI & Players")]
    public GameObject player1Cover;
    public GameObject player2Cover;
    public TextMeshProUGUI totalScoreText; // Text hiển thị dạng "Score: X"
    public GameObject winPanel;

    [Header("Win Effects & Objects")]
    public GameObject player1Crown;
    public GameObject player2Crown;
    public ParticleSystem fireworkEffect;
    public float delayBeforeWinPanel = 1.5f;

    [Header("Dữ liệu chai")]
    public List<BottleData> masterBottleList;
    public Sprite grayBottleSprite;

    [Header("Containers")]
    public Transform topShelf;
    public Transform bottomShelf;
    public GameObject bottlePrefab;

    private int currentTotalScore = 0;
    private int currentTurn = 1;
    private int currentLevelCount = 3;
    private List<int> targetIndexes = new List<int>();
    private List<Image> bottomImages = new List<Image>();
    private List<GameObject> topBottles = new List<GameObject>();
    private List<bool> isPosCorrect = new List<bool>();
    private GameObject firstSelected;
    private bool isGameOver = false;

    // Quản lý màu sắc con số
    private Color numberColor = Color.black;

    void Start() { UpdateTurnUI(); StartNewRound(); }

    public void StartNewRound()
    {
        isGameOver = false;
        winPanel.SetActive(false);
        player1Crown.SetActive(false);
        player2Crown.SetActive(false);
        if (fireworkEffect != null) fireworkEffect.Stop();

        currentTotalScore = 0;
        numberColor = Color.black;
        UpdateScoreText(); // Cập nhật text ban đầu

        foreach (Transform t in topShelf) Destroy(t.gameObject);
        foreach (Transform t in bottomShelf) Destroy(t.gameObject);
        bottomImages.Clear(); topBottles.Clear(); targetIndexes.Clear(); isPosCorrect.Clear(); firstSelected = null;

        // ... (Logic khởi tạo giữ nguyên như cũ)
        List<int> availableIDs = new List<int>();
        for (int i = 0; i < masterBottleList.Count; i++) availableIDs.Add(masterBottleList[i].bottleID);
        ShuffleList(availableIDs);
        List<int> roundIDs = new List<int>();
        for (int i = 0; i < currentLevelCount; i++) roundIDs.Add(availableIDs[i]);
        for (int i = 0; i < currentLevelCount; i++)
        {
            targetIndexes.Add(roundIDs[i]);
            isPosCorrect.Add(false);
            GameObject bBottom = Instantiate(bottlePrefab, bottomShelf);
            bBottom.GetComponent<Image>().sprite = grayBottleSprite;
            bottomImages.Add(bBottom.GetComponent<Image>());
        }
        List<int> topIDs = new List<int>(roundIDs);
        EnsureNoMatch(topIDs, roundIDs);
        for (int i = 0; i < currentLevelCount; i++)
        {
            GameObject bTop = Instantiate(bottlePrefab, topShelf);
            int currentID = topIDs[i];
            bTop.GetComponent<Image>().sprite = masterBottleList.Find(x => x.bottleID == currentID).bottleSprite;
            bTop.name = currentID.ToString();
            topBottles.Add(bTop);
            bTop.GetComponent<Button>().onClick.AddListener(() => OnBottleClick(bTop));
        }
    }

    // Hàm cập nhật Text sử dụng Rich Text để chỉ đổi màu con số
    void UpdateScoreText()
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(numberColor);
        totalScoreText.text = $"Score: <color=#{hexColor}>{currentTotalScore}</color>";
    }

    void OnBottleClick(GameObject clickedBottle)
    {
        if (isGameOver) return;
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
        int indexA = topBottles.IndexOf(a);
        int indexB = topBottles.IndexOf(b);
        a.GetComponent<Button>().interactable = false;
        b.GetComponent<Button>().interactable = false;

        a.transform.DOMove(b.transform.position, 0.4f);
        b.transform.DOMove(a.transform.position, 0.4f).OnComplete(() => {
            a.transform.SetSiblingIndex(indexB);
            b.transform.SetSiblingIndex(indexA);
            topBottles[indexA] = b;
            topBottles[indexB] = a;
            a.transform.localPosition = Vector3.zero;
            b.transform.localPosition = Vector3.zero;
            a.GetComponent<Button>().interactable = true;
            b.GetComponent<Button>().interactable = true;
            CheckAllPositionsAfterSwap();
            firstSelected = null;
        });
    }

    void CheckAllPositionsAfterSwap()
    {
        int scoreChange = 0;
        bool changed = false;

        for (int i = 0; i < topBottles.Count; i++)
        {
            int currentTopID = int.Parse(topBottles[i].name);
            int correctID = targetIndexes[i];

            if (isPosCorrect[i] && currentTopID != correctID)
            {
                isPosCorrect[i] = false;
                bottomImages[i].sprite = grayBottleSprite;
                bottomImages[i].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                currentTotalScore--;
                scoreChange--;
                changed = true;
            }
            else if (!isPosCorrect[i] && currentTopID == correctID)
            {
                isPosCorrect[i] = true;
                bottomImages[i].sprite = masterBottleList.Find(x => x.bottleID == correctID).bottleSprite;
                bottomImages[i].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                currentTotalScore++;
                scoreChange++;
                changed = true;
            }
        }

        // Xử lý hiệu ứng màu sắc cho con số
        if (scoreChange > 0)
        {
            // Tăng điểm: Chuyển sang Xanh lục và GIỮ NGUYÊN
            DOTween.To(() => numberColor, x => numberColor = x, Color.green, 0.2f)
                .OnUpdate(UpdateScoreText);
        }
        else if (scoreChange < 0)
        {
            // Giảm điểm: Chuyển sang Đỏ rồi mới về Đen dần
            DOTween.To(() => numberColor, x => numberColor = x, Color.red, 0.2f)
                .OnUpdate(UpdateScoreText)
                .OnComplete(() => {
                    DOTween.To(() => numberColor, x => numberColor = x, Color.black, 0.8f)
                        .OnUpdate(UpdateScoreText);
                });
        }
        else
        {
            UpdateScoreText();
        }

        if (!isPosCorrect.Contains(false)) { StartCoroutine(WinSequence()); }
        else if (!changed) { StartCoroutine(WaitAndSwitch()); }
    }

    // Các hàm phụ giữ nguyên...
    IEnumerator WinSequence()
    {
        isGameOver = true;
        if (fireworkEffect != null) fireworkEffect.Play();
        yield return new WaitForSeconds(delayBeforeWinPanel);
        winPanel.SetActive(true);
        if (currentTurn == 1) { player1Crown.SetActive(true); player2Crown.SetActive(false); }
        else { player1Crown.SetActive(false); player2Crown.SetActive(true); }
    }

    public void OnNextLevelClick() { currentLevelCount = Mathf.Min(currentLevelCount + 1, 7); StartNewRound(); }
    public void OnBackToMode1Click() { UnityEngine.SceneManagement.SceneManager.LoadScene("Mode1SceneName"); }

    void EnsureNoMatch(List<int> list, List<int> reference)
    {
        int safety = 0;
        while (safety < 100)
        {
            ShuffleList(list);
            bool hasMatch = false;
            for (int i = 0; i < list.Count; i++) if (list[i] == reference[i]) hasMatch = true;
            if (!hasMatch) break;
            safety++;
        }
    }

    void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i]; int rnd = Random.Range(i, list.Count);
            list[i] = list[rnd]; list[rnd] = temp;
        }
    }

    IEnumerator WaitAndSwitch()
    {
        yield return new WaitForSeconds(0.2f);
        currentTurn = (currentTurn == 1) ? 2 : 1;
        UpdateTurnUI();
    }

    void UpdateTurnUI()
    {
        player1Cover.SetActive(currentTurn != 1);
        player2Cover.SetActive(currentTurn != 2);
    }
}