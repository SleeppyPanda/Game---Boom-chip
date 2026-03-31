using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems; // DÒNG QUAN TRỌNG: Sửa lỗi CS0103
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PolygonCollider2D))]
public class Mode3Item : MonoBehaviour
{
    [Header("Cấu hình di chuyển")]
    public float moveSpeed = 5f;
    public float moveRange = 2.5f;

    [Header("Vận tốc rơi (Đã chỉnh chậm)")]
    public float gravityScale = 1.2f;
    public float maxVelocity = 10f;

    [Header("Cài đặt va chạm")]
    public float bounceCooldown = 0.15f;

    [Header("Animation Khói")]
    public Animator anim;
    public string deadAnimName = "Dead"; // Tên State chứa clip khói trong Animator

    private bool isDropped = false;
    private bool isDead = false;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PolygonCollider2D polygonCollider;
    private float lastBounceTime;
    private float startX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        if (anim == null) anim = GetComponent<Animator>();

        // 1. Vô hiệu hóa Animator ngay từ đầu để khói không chạy
        if (anim != null) anim.enabled = false;

        if (rb != null)
        {
            rb.simulated = false;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale = gravityScale;
        }
        startX = transform.position.x;
    }

    void Start()
    {
        UpdatePolygonCollider();
    }

    void Update()
    {
        // 2. Chỉ di chuyển ngang khi chưa rơi
        if (!isDropped && !isDead)
        {
            HandlePingPongMovement();
        }
    }

    private void HandlePingPongMovement()
    {
        float newX = startX + Mathf.Sin(Time.time * moveSpeed) * moveRange;
        transform.position = new Vector3(newX, transform.position.y, 0);
    }

    private void OnMouseDown()
    {
        // Kiểm tra không click xuyên qua UI
        if (!isDropped && !isDead)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            StartFalling();
        }
    }

    public void StartFalling()
    {
        isDropped = true;
        rb.simulated = true;
        rb.AddTorque(Random.Range(-0.5f, 0.5f), ForceMode2D.Impulse);
    }

    void FixedUpdate()
    {
        if (isDead || rb == null || !rb.simulated) return;

        if (rb.velocity.magnitude > maxVelocity)
            rb.velocity = rb.velocity.normalized * maxVelocity;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // --- THÊM ÂM THANH KHI CHẠM BÁNH XE (WALL/HALF) ---
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.name.Contains("Half"))
        {
            if (Time.time - lastBounceTime > bounceCooldown)
            {
                lastBounceTime = Time.time;

                // GỌI ÂM THANH NẢY (Bounce)
                if (AudioManager.Instance != null && Mode3Manager.Instance != null)
                    AudioManager.Instance.PlaySFX(Mode3Manager.Instance.sfxBounce);

                if (Mode3Manager.Instance != null)
                    Mode3Manager.Instance.AddBounce(collision.transform);

                rb.AddForce(new Vector2(Random.Range(-0.3f, 0.3f), 0.4f), ForceMode2D.Impulse);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 3. CHỈ KÍCH HOẠT KHI CHẠM DEADZONE
        if (other.CompareTag("DeadZone") && !isDead)
        {
            // --- THÊM ÂM THANH KHI CHẠM VÀO VÙNG THUA (DeadZone) ---
            if (AudioManager.Instance != null && Mode3Manager.Instance != null)
                AudioManager.Instance.PlaySFX(Mode3Manager.Instance.sfxDead);

            HandleDeadSequence();
        }
    }

    private void HandleDeadSequence()
    {
        isDead = true;
        rb.simulated = false; // Dừng vật lý để vật thể đứng yên

        if (Mode3Manager.Instance != null)
            Mode3Manager.Instance.FinishGame();

        // 1. ẨN ITEM NGAY LẬP TỨC
        if (sr != null)
        {
            // Cách 1: Chỉnh Alpha về 0 (Trong suốt hoàn toàn)
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;

            // Cách 2 (Triệt để hơn): Tắt luôn hiển thị của Sprite
            // sr.enabled = false; 
        }

        // 2. CHẠY ANIMATION KHÓI THẲNG ĐỨNG
        if (anim != null)
        {
            anim.enabled = true;

            // Ép xoay về 0 để khói luôn bay lên trên, bất kể Item cha đang xoay thế nào
            anim.transform.rotation = Quaternion.identity;

            anim.Play(deadAnimName, 0, 0f);
        }

        // 3. XÓA OBJECT SAU KHI KHÓI CHẠY XONG (Ví dụ 0.6 giây)
        Destroy(gameObject, 0.6f);
    }

    public void UpdatePolygonCollider()
    {
        if (sr != null && sr.sprite != null)
        {
            polygonCollider.pathCount = 0;
            int shapeCount = sr.sprite.GetPhysicsShapeCount();
            for (int i = 0; i < shapeCount; i++)
            {
                List<Vector2> path = new List<Vector2>();
                sr.sprite.GetPhysicsShape(i, path);
                polygonCollider.pathCount++;
                polygonCollider.SetPath(polygonCollider.pathCount - 1, path.ToArray());
            }
        }
    }
}