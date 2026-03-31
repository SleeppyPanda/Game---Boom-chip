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

    [Header("Animation Settings")]
    public float rotateSpeed = 60f; // ĐÃ GIẢM: Từ 90 xuống 60

    private int bounceCount;
    private Mode3Data currentData;
    private GameObject currentItem;
    private bool canPlay = true;
    private bool isGameOver = false;

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
        // Xoay bánh xe (Giảm tốc độ theo yêu cầu)
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
                if (currentItem != null && canPlay)
                {
                    canPlay = false;
                    Mode3Item itemScript = currentItem.GetComponent<Mode3Item>();
                    if (itemScript != null) itemScript.StartFalling();
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

    // THAY ĐỔI: Nhận Transform để chỉ rung bánh xe đó
    public void AddBounce(Transform wheelHit)
    {
        if (isGameOver) return;
        bounceCount++;
        if (txtScore != null)
        {
            txtScore.text = "Score: " + (bounceCount * 1);
            txtScore.color = Color.yellow;
            txtScore.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f);
        }

        // CHỈ RUNG BÁNH XE: Dùng DoTween rung nhẹ object bị chạm
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
        Debug.Log($"<color=green>[ANALYTIC]</color> Mode Complete: {MODE_ID}");
        if (AccountManager.Instance != null) AccountManager.SetWinResult(1);
        if (FirebaseManager.Instance != null) FirebaseManager.Instance.LogModeComplete(MODE_ID);

        yield return new WaitForSeconds(1.2f);

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.ShowInterstitialWithDelay("is_show_inter_complete_game", () => {
                ShowResultUI();
            }, 0.2f);
        }
        else
        {
            ShowResultUI();
        }
    }

    private void ShowResultUI()
    {
        resultPanel.SetActive(true);
        if (AdsManager.Instance != null) AdsManager.Instance.ShowMREC("is_show_mrec_complete_game");

        string scoreFinalText = (bounceCount * 1).ToString() + " " + currentData.unit;
        if (txtFinalScore != null)
        {
            txtFinalScore.text = scoreFinalText;
            txtFinalScore.color = Color.yellow;
            txtFinalScore.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f);
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
}