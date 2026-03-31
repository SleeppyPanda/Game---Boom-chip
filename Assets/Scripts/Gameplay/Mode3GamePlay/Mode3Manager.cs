using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;

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
    public GameObject handPointer; // Thêm biến này để làm hiệu ứng bàn tay nếu cần

    [Header("Animation Settings")]
    public float rotateSpeed = 60f;

    private int bounceCount;
    private Mode3Data currentData;
    private GameObject currentItem;
    private bool canPlay = true;
    private bool isGameOver = false;
    private bool isShowingTutorial = false; // Biến kiểm soát trạng thái đang hiện tutorial

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
        Debug.Log($"<color=cyan>[ANALYTIC]</color> Enter Mode: {MODE_ID}");
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeEnter(MODE_ID);
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        SetupNewTurn();
        CheckTutorial();
    }

    void CheckTutorial()
    {
        if (PlayerPrefs.GetInt("FirstTime_Mode3", 0) == 0)
        {
            ShowTutorial();
            // Lưu ý: Không SetInt ở đây, hãy Set ở lúc người chơi chạm tay lần đầu
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

        // --- SỬA Ở ĐÂY ---
        // Thay vì chạy code tween trực tiếp, ta gọi Coroutine để delay
        if (handPointer != null)
        {
            // Xóa các tween cũ trên object này để tránh bị chồng chéo
            handPointer.transform.DOKill();
            StartCoroutine(StartHandTweenWithDelay());
        }
    }

    // Coroutine mới để delay
    private IEnumerator StartHandTweenWithDelay()
    {
        // Đợi 1 khung hình (hoặc 0.1s) để DOTween component kịp khởi tạo
        yield return new WaitForEndOfFrame(); // Cách 1: Đợi 1 frame (Khuyên dùng)
                                              // yield return new WaitForSeconds(0.05f); // Cách 2: Đợi một chút thời gian cố định

        if (handPointer != null && isShowingTutorial) // Kiểm tra lại object vẫn tồn tại
        {
            // Chạy hiệu ứng co giãn
            handPointer.transform.DOScale(1.2f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    public void SetupNewTurn()
    {
        isGameOver = false;
        resultPanel.SetActive(false);
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        bounceCount = 0;
        canPlay = true;

        if (txtScore != null)
        {
            txtScore.text = "Score: 0";
            txtScore.color = Color.yellow;
        }

        if (database == null || database.Count == 0) return;
        currentData = database[UnityEngine.Random.Range(0, database.Count)];

        txtQuestion.text = currentData.question;
        leftCircleRenderer.sprite = currentData.circleSprite;
        rightCircleRenderer.sprite = currentData.circleSprite;

        leftHalf.localPosition = new Vector3(-currentData.gapSize, leftHalf.localPosition.y, 0);
        rightHalf.localPosition = new Vector3(currentData.gapSize, rightHalf.localPosition.y, 0);

        if (currentItem != null) Destroy(currentItem);
        SpawnPreviewItem();
    }

    void Update()
    {
        if (leftHalf != null) leftHalf.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        if (rightHalf != null) rightHalf.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        // Chỉnh sửa điều kiện: Vẫn cho phép HandleDragInput chạy khi đang hiện Tutorial
        if (currentItem == null || isGameOver) return;

        HandleDragInput();
    }

    void HandleDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI())
            {
                // TRƯỜNG HỢP 1: Đang hiện Tutorial
                if (isShowingTutorial)
                {
                    isShowingTutorial = false;
                    tutorialGroup.SetActive(false);
                    PlayerPrefs.SetInt("FirstTime_Mode3", 1);
                    PlayerPrefs.Save();

                    // Thực hiện thả bóng ngay lập tức
                    DropItem();
                }
                // TRƯỜNG HỢP 2: Đang chơi bình thường
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

    // Các hàm còn lại (IsPointerOverUI, AddBounce, FinishGame, v.v.) giữ nguyên...
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
        yield return new WaitForSeconds(1.2f);
        ShowResultUI();
    }

    private void ShowResultUI()
    {
        resultPanel.SetActive(true);
        if (txtFinalScore != null) txtFinalScore.text = (bounceCount * 1).ToString() + " " + currentData.unit;
        if (currentData.comments.Count > 0) txtComment.text = currentData.comments[UnityEngine.Random.Range(0, currentData.comments.Count)];
    }

    public void Btn_PlayAgain() { SetupNewTurn(); }
    public void Btn_BackToHome() { SceneManager.LoadScene("SelectScene"); }
}