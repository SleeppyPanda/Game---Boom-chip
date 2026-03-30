using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Quan trọng để chuyển Scene

public class Mode3Manager : MonoBehaviour
{
    public static Mode3Manager Instance;

    [Header("UI & Data")]
    public TextMeshProUGUI txtQuestion;
    public GameObject resultPanel;
    public TextMeshProUGUI txtScore;       // Text hiển thị điểm lúc đang chơi (màu vàng)
    public TextMeshProUGUI txtFinalScore;  // Text hiển thị tổng điểm ở Result Panel
    public TextMeshProUGUI txtComment;
    public List<Mode3Data> database;

    [Header("Renderers")]
    public SpriteRenderer leftCircleRenderer;
    public SpriteRenderer rightCircleRenderer;

    [Header("Gameplay Objects")]
    public Transform leftHalf;
    public Transform rightHalf;
    public Transform spawnPoint; // Điểm xuất hiện phía trên khe hở
    public GameObject itemPrefab;

    private int bounceCount;
    private Mode3Data currentData;
    private GameObject currentItem;
    private bool isDragging = false;
    private bool canPlay = true;

    [System.Serializable]
    public class Mode3Data
    {
        public string themeName;
        public string question;
        public string unit;
        public Sprite itemSprite;
        public Sprite circleSprite;
        [Range(0.5f, 15f)] public float gapSize; // Khe hở rộng tối đa 15
        public List<string> comments;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start() => SetupNewTurn();

    // Hàm thiết lập màn chơi mới
    public void SetupNewTurn()
    {
        resultPanel.SetActive(false);
        bounceCount = 0;
        canPlay = true;
        isDragging = false;

        // Cài đặt UI Score ban đầu là màu vàng
        if (txtScore != null)
        {
            txtScore.text = "Score: 0";
            txtScore.color = Color.yellow;
        }

        // Kiểm tra database
        if (database == null || database.Count == 0) return;
        currentData = database[UnityEngine.Random.Range(0, database.Count)];

        // Cập nhật câu hỏi và hình ảnh vòng tròn
        txtQuestion.text = currentData.question;
        leftCircleRenderer.sprite = currentData.circleSprite;
        rightCircleRenderer.sprite = currentData.circleSprite;

        // Chỉnh khe hở (Gap) dựa trên gapSize
        leftHalf.localPosition = new Vector3(-currentData.gapSize, leftHalf.localPosition.y, 0);
        rightHalf.localPosition = new Vector3(currentData.gapSize, rightHalf.localPosition.y, 0);

        // HIỆN SẴN ITEM KHI VÀO GAME
        if (currentItem != null) Destroy(currentItem);
        SpawnPreviewItem();
    }

    void Update()
    {
        // Nếu đã thả rơi hoặc game kết thúc thì không cho kéo
        if (!canPlay || currentItem == null) return;

        HandleDragInput();
    }

    // Xử lý kéo thả Item
    void HandleDragInput()
    {
        // Nhấn xuống bắt đầu kéo
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }

        // Cập nhật vị trí X của Item theo chuột/tay
        if (isDragging && currentItem != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Item trượt ngang theo X, giữ nguyên độ cao Y của spawnPoint
            currentItem.transform.position = new Vector3(mousePos.x, spawnPoint.position.y, 0);
        }

        // Thả tay ra để Item rơi
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            canPlay = false; // Khóa điều khiển

            Mode3Item itemScript = currentItem.GetComponent<Mode3Item>();
            if (itemScript != null)
            {
                itemScript.StartFalling();
            }
        }
    }

    // Sinh ra Item ở trạng thái chờ (Awake của Mode3Item phải tắt rb.simulated)
    void SpawnPreviewItem()
    {
        currentItem = Instantiate(itemPrefab, spawnPoint.position, Quaternion.identity);
        SpriteRenderer sr = currentItem.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = currentData.itemSprite;
    }

    // Hàm gọi khi va chạm với vòng (Tag: Wall)
    public void AddBounce()
    {
        bounceCount++;
        if (txtScore != null)
        {
            txtScore.text = "Score: " + (bounceCount * 10);
            txtScore.color = Color.yellow;
        }
        StartCoroutine(ScreenShake(0.05f, 0.03f));
    }

    // Hàm kết thúc game khi rơi xuống DeadZone
    public void FinishGame()
    {
        resultPanel.SetActive(true);

        // Chuỗi tổng hợp điểm và đơn vị
        string scoreFinalText = (bounceCount * 10).ToString() + " " + currentData.unit;

        // Hiển thị điểm cuối cùng vào bảng kết quả
        if (txtFinalScore != null)
        {
            txtFinalScore.text = scoreFinalText;
        }

        // Hiển thị lời bình hài hước
        if (currentData.comments.Count > 0)
        {
            txtComment.text = currentData.comments[UnityEngine.Random.Range(0, currentData.comments.Count)];
        }
    }

    // --- CÁC HÀM CHO NÚT BẤM (BUTTON EVENTS) ---

    // Gán hàm này vào nút "Chơi lại" (Play Again)
    public void Btn_PlayAgain()
    {
        SetupNewTurn();
    }

    // Gán hàm này vào nút "Về trang chủ" (Home)
    public void Btn_BackToHome()
    {
        // Hãy đảm bảo tên Scene chính xác là "Home" hoặc đổi lại tên bạn muốn
        SceneManager.LoadScene("Gameplay scene");
    }

    // --------------------------------------------

    // Hiệu ứng rung màn hình
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