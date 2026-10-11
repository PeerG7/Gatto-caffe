using UnityEngine;

// =====================================================================
// PlayerController2D — v6 (Any State Setup with Holding Support)
// =====================================================================
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 movement;

    public static bool IsLocked = false;

    // --- Animation State Enum (ตรงกับค่าตัวเลข Index 0 ถึง 5) ---
    public enum AnimState
    {
        Idle = 0,               // ยืนนิ่ง (มือเปล่า)
        Walk = 1,               // เดิน ซ้าย/ขวา/ลง (มือเปล่า)
        WalkUp = 2,             // เดินขึ้น (มือเปล่า)
        IdleHolding = 3,        // ยืนนิ่ง (ถือของ)
        WalkHolding = 4,        // เดิน ซ้าย/ขวา/ลง (ถือของ)
        WalkUpHolding = 5       // เดินขึ้น (ถือของ)
    }

    private AnimState currentAnim = (AnimState)(-1);
    private static readonly int AnimStateHash = Animator.StringToHash("animState");

    [Header("Holding System")]
    [Tooltip("เปิดใช้งานเมื่อผู้เล่นกำลังถือไอเทมหรืออาหารอยู่")]
    public bool isHoldingItem = false;

    public static void SetLock(bool value)
    {
        IsLocked = value;
    }

    private static bool IsGamePaused =>
        DayNightManager.Instance != null && DayNightManager.Instance.isPaused;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        UpdateAnimation();
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
        AnimState targetState = AnimState.Idle;
        bool isMoving = movement.sqrMagnitude > 0.01f;

        if (!isMoving)
        {
            // --- เช็คตอนยืนนิ่ง (Idle) ---
            targetState = isHoldingItem ? AnimState.IdleHolding : AnimState.Idle;
        }
        else
        {
            // --- เช็คตอนกำลังเดิน (Moving) ---
            if (movement.y > 0.1f && Mathf.Abs(movement.y) >= Mathf.Abs(movement.x))
            {
                // เดินขึ้นบน
                targetState = isHoldingItem ? AnimState.WalkUpHolding : AnimState.WalkUp;
            }
            else
            {
                // เดิน ซ้าย, ขวา, หรือลง
                targetState = isHoldingItem ? AnimState.WalkHolding : AnimState.Walk;

                // จัดการทิศทางหันซ้าย-ขวาของ Sprite
                if (spriteRenderer != null && movement.x != 0)
                {
                    spriteRenderer.flipX = movement.x > 0; // ปรับเป็น movement.x < 0 ได้ตามภาพต้นฉบับ
                }
            }
        }

        SetAnimState(targetState);
    }

    void SetAnimState(AnimState state)
    {
        if (currentAnim == state) return;
        currentAnim = state;

        if (animator != null)
        {
            animator.SetInteger(AnimStateHash, (int)state);
        }
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