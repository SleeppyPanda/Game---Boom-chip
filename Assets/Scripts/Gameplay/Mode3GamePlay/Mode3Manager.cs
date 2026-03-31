using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class Mode3Manager : MonoBehaviour
{
    public static Mode3Manager Instance;

    [Header("UI & Data")]
    public GameObject resultPanel;
    public TextMeshProUGUI txtQuestion;
    public TextMeshProUGUI txtScore;
    public TextMeshProUGUI txtFinalScore;
    public TextMeshProUGUI txtComment;
    public List<Mode3Data> database;

    [Header("Audio Settings")]
    public string sfxBounce = "ItemBounce";
    public string sfxDead = "ItemDead";
    public string bgmMusic = "Mode3_BGM";
    public string sfxWin = "Win";

    [Header("Transition Prefab (New)")]
    public Transform mainCanvas;
    public GameObject transitionPrefab;
    private GameObject currentTransitionObj;
    private bool isTransitioning = false; // Biến chặn thao tác người chơi khi đang chạy transition

    [Header("Renderers")]
    public SpriteRenderer leftCircleRenderer;
    public SpriteRenderer rightCircleRenderer;

    [Header("Gameplay Objects")]
    public Transform leftHalf;
    public Transform rightHalf;
    public Transform spawnPoint;
    public GameObject itemPrefab;

    [Header("Quick Tutorial")]
    public GameObject tutorialGroup;
    public GameObject handPointer;

    [Header("Animation Settings")]
    public float rotateSpeed = 60f;

    [Header("Win Effects (Panel-based)")]
    public GameObject fireworkPanel;
    public float delayBeforeWinPanel = 2.0f;

    private int bounceCount;
    private Mode3Data currentData;
    private GameObject currentItem;
    private bool canPlay = true;
    private bool isGameOver = false;
    private bool isShowingTutorial = false;
    private bool isFirstStart = true; // Thêm biến này

    private const int MODE_ID = 12;

    [System.Serializable]
    public class Mode3Data
    {
        public string themeName;
        public string question;
        public string unit;
        public Sprite itemSprite;
        public Sprite circleSprite;
        [Range(0.5f, 15f)] public float gapSize;
        public List<string> comments;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(bgmMusic);
        Debug.Log($"<color=cyan>[ANALYTIC]</color> Enter Mode: {MODE_ID}");
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(MODE_ID);
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        StartCoroutine(StartGameFlow());
    }

    private IEnumerator StartGameFlow()
    {
        // 1. Setup bàn chơi ngầm phía dưới
        SetupNewTurn();

        // 2. Kiểm tra nếu là lần đầu mới chạy Transition
        if (isFirstStart && transitionPrefab != null && mainCanvas != null)
        {
            isTransitioning = true; // Khóa game lại

            currentTransitionObj = Instantiate(transitionPrefab, mainCanvas);
            currentTransitionObj.transform.SetAsLastSibling();

            // Đợi Transition chạy xong
            yield return StartCoroutine(RunStartTransition());

            isFirstStart = false; // Sau khi chạy xong lần đầu, đánh dấu là false
            isTransitioning = false; // Mở khóa game
        }
        else
        {
            // Nếu không phải lần đầu, đảm bảo biến chặn được reset
            isTransitioning = false;
        }

        // 3. Check Tutorial (Chỉ hiện nếu chưa từng xem)
        CheckTutorial();
    }

    #region TRANSITION LOGIC (Copied from Mode 2)
    private IEnumerator RunStartTransition()
    {
        if (currentTransitionObj == null) yield break;

        CanvasGroup cg = currentTransitionObj.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; }

        // Chờ 2 giây để người chơi nhìn thấy Transition
        yield return new WaitForSeconds(2.0f);

        CoreUiTransition transScript = currentTransitionObj.GetComponent<CoreUiTransition>();

        if (transScript != null)
        {
            transScript.PlayOutAnimation();
        }
        else
        {
            Destroy(currentTransitionObj);
        }

        // Chờ đến khi object bị HỦY hoàn toàn
        yield return new WaitUntil(() => currentTransitionObj == null);

        Debug.Log("<color=green>[Mode3Manager]</color> Transition đã mở xong!");
    }
    #endregion

    void CheckTutorial()
    {
        if (PlayerPrefs.GetInt("FirstTime_Mode3", 0) == 0)
        {
            ShowTutorial();
        }
        else
        {
            if (tutorialGroup != null) tutorialGroup.SetActive(false);
            isShowingTutorial = false;
        }
    }

    public void ShowTutorial()
    {
        if (tutorialGroup == null) return;
        tutorialGroup.SetActive(true);
        isShowingTutorial = true;
        if (handPointer != null)
        {
            handPointer.transform.DOKill();
            StartCoroutine(StartHandTweenWithDelay());
        }
    }

    private IEnumerator StartHandTweenWithDelay()
    {
        yield return new WaitForEndOfFrame();
        if (handPointer != null && isShowingTutorial)
        {
            handPointer.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    public void SetupNewTurn()
    {
        isGameOver = false;
        resultPanel.SetActive(false);
        if (fireworkPanel != null) fireworkPanel.SetActive(false);

        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        bounceCount = 0;
        canPlay = true;

        // Reset UI điểm số
        if (txtScore != null)
        {
            txtScore.text = "Score: 0";
            txtScore.color = Color.yellow;
        }

        // Chọn dữ liệu mới ngẫu nhiên
        if (database == null || database.Count == 0) return;
        currentData = database[UnityEngine.Random.Range(0, database.Count)];

        txtQuestion.text = currentData.question;
        leftCircleRenderer.sprite = currentData.circleSprite;
        rightCircleRenderer.sprite = currentData.circleSprite;

        // Cập nhật khoảng cách 2 nửa vòng tròn
        leftHalf.localPosition = new Vector3(-currentData.gapSize, leftHalf.localPosition.y, 0);
        rightHalf.localPosition = new Vector3(currentData.gapSize, rightHalf.localPosition.y, 0);

        // Xóa vật thể cũ nếu còn sót lại và sinh vật thể mới
        if (currentItem != null) Destroy(currentItem);
        SpawnPreviewItem();
    }

    void Update()
    {
        if (leftHalf != null) leftHalf.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        if (rightHalf != null) rightHalf.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        // NẾU ĐANG CHẠY TRANSITION THÌ KHÔNG CHO NGƯỜI CHƠI BẤM THẢ ĐỒ VẬT
        if (isTransitioning || currentItem == null || isGameOver) return;

        HandleDragInput();
    }

    void HandleDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI())
            {
                if (isShowingTutorial)
                {
                    isShowingTutorial = false;
                    tutorialGroup.SetActive(false);
                    PlayerPrefs.SetInt("FirstTime_Mode3", 1);
                    PlayerPrefs.Save();
                    DropItem();
                }
                else if (canPlay)
                {
                    DropItem();
                }
            }
        }
    }

    void DropItem()
    {
        if (currentItem != null)
        {
            canPlay = false;
            Mode3Item itemScript = currentItem.GetComponent<Mode3Item>();
            if (itemScript != null) itemScript.StartFalling();
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId)) return true;
        }
        return false;
    }

    void SpawnPreviewItem()
    {
        currentItem = Instantiate(itemPrefab, spawnPoint.position, Quaternion.identity);
        SpriteRenderer sr = currentItem.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = currentData.itemSprite;
    }

    public void AddBounce(Transform wheelHit)
    {
        if (isGameOver) return;
        bounceCount++;
        if (txtScore != null)
        {
            txtScore.text = "Score: " + (bounceCount * 1);
            txtScore.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f);
        }
        if (wheelHit != null)
        {
            wheelHit.DOPunchPosition(new Vector3(0, -0.2f, 0), 0.2f, 10, 1f);
        }
    }

    public void FinishGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(HandleFinishSequence());
    }

    private IEnumerator HandleFinishSequence()
    {
        if (fireworkPanel != null) fireworkPanel.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Win");

        yield return new WaitForSeconds(delayBeforeWinPanel);

        ShowResultUI();
    }

    private void ShowResultUI()
    {
        if (resultPanel) resultPanel.SetActive(true);
        if (txtFinalScore != null) txtFinalScore.text = (bounceCount * 1).ToString() + " " + currentData.unit;
        if (currentData.comments.Count > 0) txtComment.text = currentData.comments[UnityEngine.Random.Range(0, currentData.comments.Count)];

        if (AdsManager.Instance != null) AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");
    }

    public void Btn_PlayAgain()
    {
        if (fireworkPanel) fireworkPanel.SetActive(false);
        if (resultPanel) resultPanel.SetActive(false);

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_retry", () => {
                StartCoroutine(StartGameFlow()); // Đã sửa lại để gọi luôn StartGameFlow khi Play Again
            }, 0.3f);
        }
        else StartCoroutine(StartGameFlow()); // Đã sửa lại
    }

    public void Btn_BackToHome()
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
}