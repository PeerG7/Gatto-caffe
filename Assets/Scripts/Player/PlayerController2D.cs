using UnityEngine;

// =====================================================================
// PlayerController2D — v12 (Fixed Walk-Up Flip & Holder Position)
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
    private PlayerInventory playerInventory;
    private Vector2 movement;


    public static bool IsLocked = false;

    public enum AnimState
    {
        Idle = 0,
        Walk = 1,
        WalkUp = 2,
        IdleHolding = 3,
        WalkHolding = 4,
        WalkUpHolding = 5
    }

    private AnimState currentAnim = (AnimState)(-1);
    private static readonly int AnimStateHash = Animator.StringToHash("animState");

    [Header("Holding System")]
    public bool isHoldingItem = false;
    private float idleSideInitialLocalX = 0.2f;

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
        playerInventory = GetComponent<PlayerInventory>();

        if (playerInventory != null && playerInventory.idleSideRenderer != null)
        {
            idleSideInitialLocalX = Mathf.Abs(playerInventory.idleSideRenderer.transform.localPosition.x);
        }
    }

    void Start()
    {
        UpdateAnimation();
    }

    void Update()
    {
        if (playerInventory != null)
        {
            isHoldingItem = playerInventory.HasItem();
        }

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
        bool isWalkUpDirection = false;

        if (!isMoving)
        {
            targetState = isHoldingItem ? AnimState.IdleHolding : AnimState.Idle;
            isWalkUpDirection = false;
        }
        else
        {
            if (movement.y > 0.1f && Mathf.Abs(movement.y) >= Mathf.Abs(movement.x))
            {
                // 📌 ท่าเดินขึ้น (Walk Up): ล็อกไม่ให้ตัวละครพลิกซ้าย-ขวา เพื่อื่ให้มือที่ยกขึ้นอยู่ฝั่งเดิมเสมอ
                targetState = isHoldingItem ? AnimState.WalkUpHolding : AnimState.WalkUp;
                isWalkUpDirection = true;

                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                }
            }
            else
            {
                // ท่าเดิน ซ้าย / ขวา / ลง
                targetState = isHoldingItem ? AnimState.WalkHolding : AnimState.Walk;
                isWalkUpDirection = false;

                if (spriteRenderer != null && movement.x != 0)
                {
                    spriteRenderer.flipX = movement.x > 0; // ปรับเป็น < 0 ได้ถ้าภาพหันกลับด้าน
                }
            }
        }

        // อัปเดตการเปิด/ปิด Holder ตามท่าทาง
        if (playerInventory != null)
        {
            playerInventory.UpdateHolderVisibility(isHoldingItem, isWalkUpDirection);

            if (isHoldingItem)
            {
                if (!isWalkUpDirection && playerInventory.idleSideRenderer != null)
                {
                    // พลิกตำแหน่ง X ของ Holder ด้านข้างตามการหันหน้า
                    Transform holderTransform = playerInventory.idleSideRenderer.transform;
                    Vector3 localPos = holderTransform.localPosition;
                    localPos.x = spriteRenderer.flipX ? -idleSideInitialLocalX : idleSideInitialLocalX;
                    holderTransform.localPosition = localPos;
                }
                // *หมายเหตุ: สำหรับ Walk Up ให้ปล่อยตำแหน่งของ Holder_WalkUp นิ่งๆ ตามที่เราจัดไว้ใน Prefab ได้เลย ไม่ต้องคำนวณ FlipX เพิ่ม*
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

        Vector2 moveX = new Vector2(movement.x, 0) * moveSpeed * escortSpeedMultiplier * Time.fixedDeltaTime;
        if (!IsBlocked(moveX))
            newPos += moveX;

        Vector2 moveY = new Vector2(0, movement.y) * moveSpeed * escortSpeedMultiplier * Time.fixedDeltaTime;
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
    private float escortSpeedMultiplier = 1f;

    public void SetEscortSpeedMultiplier(float multiplier)
    {
        escortSpeedMultiplier = multiplier;
    }
}