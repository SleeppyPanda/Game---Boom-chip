using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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

    [Header("Animation Settings")]
    public float rotateSpeed = 90f;

    private int bounceCount;
    private Mode3Data currentData;
    private GameObject currentItem;
    private bool isDragging = false;
    private bool canPlay = true;
    private bool isGameOver = false;

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
        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();
        SetupNewTurn();
    }

    public void SetupNewTurn()
    {
        isGameOver = false;
        resultPanel.SetActive(false);

        if (AdsManager.Instance != null) AdsManager.Instance.HideMREC();

        bounceCount = 0;
        canPlay = true;
        isDragging = false;

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
        // Luôn xoay vòng tròn kể cả khi game over (hiệu ứng nền)
        if (leftHalf != null) leftHalf.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        if (rightHalf != null) rightHalf.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        if (!canPlay || currentItem == null || isGameOver) return;

        HandleDragInput();
    }

    void HandleDragInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI())
            {
                isDragging = true;
            }
        }

        if (isDragging && currentItem != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentItem.transform.position = new Vector3(mousePos.x, spawnPoint.position.y, 0);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                isDragging = false;
                canPlay = false;

                Mode3Item itemScript = currentItem.GetComponent<Mode3Item>();
                if (itemScript != null)
                {
                    itemScript.StartFalling();
                }
            }
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

    public void AddBounce()
    {
        if (isGameOver) return;
        bounceCount++;
        if (txtScore != null)
        {
            txtScore.text = "Score: " + (bounceCount * 10);
            txtScore.color = Color.yellow;
        }
        StartCoroutine(ScreenShake(0.05f, 0.03f));
    }

    // Đã chuyển thành Coroutine để hỗ trợ Delay
    public void FinishGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(HandleFinishSequence());
    }

    private IEnumerator HandleFinishSequence()
    {
        // Đợi 1.2s sau khi vật rơi/thua để người chơi nhìn kết quả
        yield return new WaitForSeconds(1.2f);

        ShowResultUI();
    }

    private void ShowResultUI()
    {
        resultPanel.SetActive(true);

        if (AdsManager.Instance != null)
            AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");

        string scoreFinalText = (bounceCount * 10).ToString() + " " + currentData.unit;

        if (txtFinalScore != null)
        {
            txtFinalScore.text = scoreFinalText;
            txtFinalScore.color = Color.yellow;
        }

        if (currentData.comments.Count > 0)
        {
            txtComment.text = currentData.comments[UnityEngine.Random.Range(0, currentData.comments.Count)];
        }
    }

    public void Btn_PlayAgain()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_retry", () => {
                SetupNewTurn();
            }, 0.3f);
        }
        else
        {
            SetupNewTurn();
        }
    }

    public void Btn_BackToHome()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.HideMREC();
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_back_home", () => {
                SceneManager.LoadScene("SelectScene");
            }, 0.3f);
        }
        else
        {
            SceneManager.LoadScene("SelectScene");
        }
    }

    IEnumerator ScreenShake(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.localPosition = originalPos;
    }
}