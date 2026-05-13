using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Vector2 movement;

    public static bool IsLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
    }

    void Update()
    {
        if (IsLocked)
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
        if (IsLocked)
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