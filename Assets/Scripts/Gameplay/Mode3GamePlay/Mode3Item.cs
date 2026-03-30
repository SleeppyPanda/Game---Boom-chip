using UnityEngine;

public class Mode3Item : MonoBehaviour
{
    [Header("Cài đặt vận tốc")]
    public float maxVelocity = 10f;

    [Header("Cài đặt tính điểm")]
    public float bounceCooldown = 0.15f;

    private bool isDead = false;
    private Rigidbody2D rb;
    private float lastBounceTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Khi mới Instantiate, tắt vật lý để có thể kéo theo chuột/tay
        if (rb != null) rb.simulated = false;
    }

    void Start()
    {
        // Đảm bảo item hiển thị lớp trên cùng
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 10;

        // Tránh xuyên tường khi nảy nhanh
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // Hàm này sẽ được Mode3Manager gọi khi người chơi thả tay ra
    public void StartFalling()
    {
        if (rb == null) return;

        rb.simulated = true; // Bật lại vật lý
        // Thêm một chút xoay tròn ngẫu nhiên cho tự nhiên
        rb.AddTorque(Random.Range(-2f, 2f), ForceMode2D.Impulse);
    }

    void FixedUpdate()
    {
        // Chỉ giới hạn tốc độ khi item đang rơi (đã bật vật lý)
        if (isDead || rb == null || !rb.simulated) return;

        if (rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity = rb.velocity.normalized * maxVelocity;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Chỉ cộng điểm khi chạm vào Wall (2 cái vòng tròn)
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (Time.time - lastBounceTime > bounceCooldown)
            {
                lastBounceTime = Time.time;

                if (Mode3Manager.Instance != null)
                {
                    Mode3Manager.Instance.AddBounce();
                }

                // Lực đẩy ngẫu nhiên nhẹ để nảy sinh động
                float randomX = Random.Range(-1.5f, 1.5f);
                rb.AddForce(new Vector2(randomX, 0.5f), ForceMode2D.Impulse);
            }
        }

        // Nếu chạm vào Tag "Bouncer" (Trần/Tường), item vẫn nảy nhưng không gọi AddBounce
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Khi rơi vào vùng DeadZone ở dưới cùng
        if (other.CompareTag("DeadZone") && !isDead)
        {
            isDead = true;

            if (Mode3Manager.Instance != null)
            {
                Mode3Manager.Instance.FinishGame();
            }

            // Ngừng tính vật lý và biến mất sau 0.5s
            rb.simulated = false;
            Destroy(gameObject, 0.5f);
        }
    }
}