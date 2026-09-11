using UnityEngine;

// =====================================================================
// PlayerController2D — v2 (Pause Fail-safe)
//
// เพิ่มใหม่: นอกจากเช็ค IsLocked (static bool ที่สคริปต์อื่นต้อง set เอง)
//   ตอนนี้เช็ค DayNightManager.Instance.isPaused ตรงๆ เพิ่มอีกชั้นด้วย
//   เพื่อป้องกันบั๊ก "Player เดินได้ตอนเกม Pause" ในอนาคต เผื่อมีจุดใหม่ๆ
//   ที่ Pause เกมแล้วลืม set IsLocked = true
// =====================================================================
public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Vector2 movement;

    public static bool IsLocked = false;

    // ✅ ใหม่: fail-safe — ล็อก Player อัตโนมัติทุกครั้งที่เกมถูก Pause จริงๆ
    //    ไม่ว่าจุดที่สั่ง Pause จะลืม set IsLocked หรือไม่ก็ตาม
    private static bool IsGamePaused =>
        DayNightManager.Instance != null && DayNightManager.Instance.isPaused;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
    }

    void Update()
    {
        if (IsLocked || IsGamePaused)
        {
            movement = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;
    }

    void FixedUpdate()
    {
        if (IsLocked || IsGamePaused)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            return;
        }

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        Vector2 newPos = rb.position;

        // เช็ค X และ Y แยกกัน
        Vector2 moveX = new Vector2(movement.x, 0) * moveSpeed * Time.fixedDeltaTime;
        if (!IsBlocked(moveX))
            newPos += moveX;

        Vector2 moveY = new Vector2(0, movement.y) * moveSpeed * Time.fixedDeltaTime;
        if (!IsBlocked(moveY))
            newPos += moveY;

        rb.MovePosition(newPos);
    }

    bool IsBlocked(Vector2 direction)
    {
        if (col == null || direction == Vector2.zero) return false;

        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position + col.offset,
            col.radius * 0.9f,
            direction.normalized,
            direction.magnitude + 0.05f,
            wallLayer
        );

        if (hit.collider == null) return false;

        // ✅ Fix: เช็คว่า wall อยู่ในทิศเดียวกับที่เดินจริงๆ ไหม
        // ถ้า dot product <= 0 แปลว่า wall อยู่ตรงข้าม → ไม่ต้อง block
        Vector2 toWall = (hit.point - (rb.position + col.offset)).normalized;
        if (Vector2.Dot(direction.normalized, toWall) <= 0f) return false;

        return true;
    }
}