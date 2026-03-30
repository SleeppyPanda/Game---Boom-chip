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

    private int currentTotalScore = 0;
    private int currentTurn = 1;
    private int currentLevelCount = 3;
    private List<int> targetIndexes = new List<int>();
    private List<Image> bottomImages = new List<Image>();
    private List<GameObject> topBottles = new List<GameObject>();
    private List<bool> isPosCorrect = new List<bool>();
    private GameObject firstSelected;
    private bool isGameOver = false;
    private bool isProcessingTurn = false; // Chặn click khi đang đổi lượt hoặc chạy animation

    private Color numberColor = Color.black;

    // ID cố định cho Mode 2 là 11
    private const int MODE_ID = 11;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Debug kiểm tra việc bắn Analytics khi bắt đầu
        Debug.Log($"<color=cyan>[Analytics]</color> Calling LogModeEnter for Mode: {MODE_ID}");
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(MODE_ID);

        UpdateTurnUI();
        StartNewRound();

        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();
    }

    public void StartNewRound()
    {
        isGameOver = false;
        isProcessingTurn = false;
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

    void UpdateScoreText()
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(numberColor);
        totalScoreText.text = $"Bottle Collected: <color=#{hexColor}>{currentTotalScore}</color>";
    }

    #region BOTTLE LOGIC
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
        isProcessingTurn = true; // Khóa tương tác khi đang swap
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
        bool changedCorrectness = false;

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
                changedCorrectness = true;
            }
            else if (!isPosCorrect[i] && currentTopID == correctID)
            {
                isPosCorrect[i] = true;
                bottomImages[i].sprite = masterBottleList.Find(x => x.bottleID == correctID).bottleSprite;
                bottomImages[i].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
                currentTotalScore++;
                scoreChange++;
                changedCorrectness = true;
            }
        }

        if (scoreChange > 0)
        {
            DOTween.To(() => numberColor, x => numberColor = x, Color.green, 0.2f).OnUpdate(UpdateScoreText);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Correct");
        }
        else if (scoreChange < 0)
        {
            DOTween.To(() => numberColor, x => numberColor = x, Color.red, 0.2f)
                .OnUpdate(UpdateScoreText)
                .OnComplete(() => {
                    DOTween.To(() => numberColor, x => numberColor = x, Color.black, 0.8f).OnUpdate(UpdateScoreText);
                });
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Wrong");
        }
        else { UpdateScoreText(); }

        if (!isPosCorrect.Contains(false))
        {
            StartCoroutine(WinSequence());
        }
        else
        {
            // Luôn chuyển lượt sau mỗi lần swap (dù đúng thêm hay sai đi) để tránh kẹt lượt người 1
            StartCoroutine(WaitAndSwitch());
        }
    }
    #endregion

    #region WIN & ADS
    IEnumerator WinSequence()
    {
        isGameOver = true;
        isProcessingTurn = true;
        if (fireworkEffect != null) fireworkEffect.Play();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("SFX_Win");

        // FIREBASE & ACCOUNT: Cập nhật kết quả và Log
        Debug.Log($"<color=green>[Analytics]</color> Calling LogModeComplete for Mode: {MODE_ID}");
        if (AccountManager.Instance != null) AccountManager.SetWinResult(currentTurn);
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeComplete(MODE_ID);

        yield return new WaitForSeconds(delayBeforeWinPanel);

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_p1_choose", () => {
                ShowWinPanel();
            }, 0.2f);
        }
        else ShowWinPanel();
    }

    private void ShowWinPanel()
    {
        winPanel.SetActive(true);
        if (AdsManager.Instance != null) AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");

        player1Crown.SetActive(currentTurn == 1);
        player2Crown.SetActive(currentTurn == 2);
    }

    public void OnNextLevelClick()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_retry", () => {
                currentLevelCount = Mathf.Min(currentLevelCount + 1, 7);
                StartNewRound();
            }, 0.3f);
        }
        else
        {
            currentLevelCount = Mathf.Min(currentLevelCount + 1, 7);
            StartNewRound();
        }
    }

    public void OnBackToMode1Click()
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
    #endregion

    #region UTILS
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
            int rnd = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    IEnumerator WaitAndSwitch()
    {
        yield return new WaitForSeconds(0.5f);
        currentTurn = (currentTurn == 1) ? 2 : 1;
        UpdateTurnUI();
        isProcessingTurn = false; // Mở khóa cho người chơi tiếp theo thao tác
    }

    void UpdateTurnUI()
    {
        player1Cover.SetActive(currentTurn != 1);
        player2Cover.SetActive(currentTurn != 2);

        if (turnNotificationText != null)
        {
            string pName = "PLAYER";
            if (AccountManager.Instance != null)
            {
                pName = AccountManager.Instance.GetPlayerName(currentTurn);
            }
            else
            {
                string key = (currentTurn == 1) ? "PlayerName_P1" : "PlayerName_P2";
                string defaultName = (currentTurn == 1) ? "PLAYER 1" : "PLAYER 2";
                pName = PlayerPrefs.GetString(key, defaultName);
            }

            turnNotificationText.text = pName.ToUpper() + "'S TURN";
            turnNotificationText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }
    }

    public void OnSettingClick()
    {
        if (settingPanel != null)
        {
            if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();
            settingPanel.SetActive(true);
            settingPanel.transform.localScale = Vector3.zero;
            settingPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    public void OnCloseSettingClick()
    {
        if (settingPanel != null)
        {
            settingPanel.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => {
                settingPanel.SetActive(false);
                if (isGameOver && winPanel.activeSelf && AdsManager.Instance != null)
                {
                    AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");
                }
            });
        }
    }
    #endregion
}