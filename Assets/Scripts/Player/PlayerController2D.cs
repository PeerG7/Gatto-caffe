using UnityEngine;

// =====================================================================
// PlayerController2D — v4 (Clean Fix)
//
// animState Values:
// 0 = Idle
// 1 = Walk (Horizontal / Downward)
// 2 = Walk Up (Upward)
// =====================================================================
public class PlayerController2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;

    public static bool IsLocked = false;

    // fail-safe — ล็อก Player อัตโนมัติทุกครั้งที่เกมถูก Pause
    private static bool IsGamePaused =>
        DayNightManager.Instance != null && DayNightManager.Instance.isPaused;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        // Look in child objects (e.g., Player > ChildSprite)
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (IsLocked || IsGamePaused)
        {
            movement = Vector2.zero;
            UpdateAnimation();
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        int animState = 0; // Default: Idle (0)

        if (movement.sqrMagnitude > 0.01f)
        {
            // Walk Up takes precedence when moving upwards
            if (movement.y > 0.1f)
            {
                animState = 2; // Walk Up
            }
            else
            {
                animState = 1; // Walk (Side / Down)
            }

            // Flip sprite horizontally when moving left
            if (spriteRenderer != null && movement.x != 0)
            {
                spriteRenderer.flipX = movement.x < 0;
            }
        }

        anim.SetInteger("animState", animState);
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

        Vector2 toWall = (hit.point - (rb.position + col.offset)).normalized;
        if (Vector2.Dot(direction.normalized, toWall) <= 0f) return false;

        return true;
    }
}