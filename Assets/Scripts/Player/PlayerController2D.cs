using UnityEngine;

// =====================================================================
// PlayerController2D — Integrated Cat Holding & Movement System (Safe Hide/Unhide Fixed)
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

    // ขยาย AnimState รองรับท่าทางแยก Idle และ Walk สำหรับแมวแต่ละ Variant
    public enum AnimState
    {
        Idle = 0,
        Walk = 1,
        WalkUp = 2,
        IdleHolding = 3,
        WalkHolding = 4,
        WalkUpHolding = 5,

        // Cat Variant A (เช่น AIKow)
        HoldCatA_Idle = 6,
        HoldCatA_Walk = 9,

        // Cat Variant B (เช่น AIDum)
        HoldCatB_Idle = 7,
        HoldCatB_Walk = 10,

        // Cat Variant C (เช่น AISom)
        HoldCatC_Idle = 8,
        HoldCatC_Walk = 11
    }

    private AnimState currentAnim = (AnimState)(-1);
    private static readonly int AnimStateHash = Animator.StringToHash("animState");

    [Header("Holding System")]
    public bool isHoldingItem = false;
    public bool isHoldingCat = false;
    private CatRelationshipData heldCatData = null;
    private float idleSideInitialLocalX = 0.2f;

    [Header("Cat Tracking System")]
    private GameObject heldCatObject = null; // เก็บ Reference ตัวจริงของแมวที่กำลังอุ้มอยู่

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
        if (playerInventory != null && !isHoldingCat)
        {
            isHoldingItem = playerInventory.HasItem();
        }

        // 🟢 แก้ไขตรงนี้: เอา || IsGamePaused ออก ให้ล็อกเฉพาะตอน IsLocked เป็นจริงเท่านั้น
        if (IsLocked)
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

    // 🌟 อุ้มแมวโดยปิดการทำงานและซ่อน Sprite ของตัวจริงไว้ชั่วคราว (ปลอดภัยจาก Prefab Asset Corruption)
    public void PickUpCat(CatRelationshipData catData, Sprite catSprite, GameObject catGameObject)
    {
        isHoldingCat = true;
        heldCatData = catData;
        heldCatObject = catGameObject;

        if (heldCatObject != null)
        {
            var agent = heldCatObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            var rbCat = heldCatObject.GetComponent<Rigidbody2D>();
            if (rbCat != null) rbCat.simulated = false;

            var colCat = heldCatObject.GetComponent<Collider2D>();
            if (colCat != null) colCat.enabled = false;

            foreach (var renderer in heldCatObject.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.enabled = false;
            }
        }

        if (playerInventory != null)
        {
            playerInventory.ClearItem();
        }

        UpdateAnimation();
    }

    // 🌟 ปล่อยแมวลงที่ InteractionZone โดยเปิดระบบฟิสิกส์/AI และเปิด Sprite กลับมาแสดงผล
    public GameObject ReleaseHeldCat(Vector3 targetPosition)
    {
        isHoldingCat = false;
        GameObject releasedCat = heldCatObject;

        if (releasedCat != null)
        {
            foreach (var renderer in releasedCat.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.enabled = true;
            }

            releasedCat.transform.position = targetPosition;

            var colCat = releasedCat.GetComponent<Collider2D>();
            if (colCat != null) colCat.enabled = true;

            var rbCat = releasedCat.GetComponent<Rigidbody2D>();
            if (rbCat != null) rbCat.simulated = true;

            var agent = releasedCat.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh) agent.Warp(targetPosition);
            }
        }

        heldCatObject = null;
        heldCatData = null;
        UpdateAnimation();

        return releasedCat;
    }

    public void DropCat()
    {
        if (heldCatObject != null)
        {
            ReleaseHeldCat(transform.position + Vector3.down * 1f);
        }
        else
        {
            isHoldingCat = false;
            heldCatData = null;
            UpdateAnimation();
        }
    }

    public CatRelationshipData GetHeldCatData() => heldCatData;

    public string GetHeldCatName()
    {
        if (heldCatData != null)
        {
            return heldCatData.catID;
        }
        return "";
    }

    void UpdateAnimation()
    {
        AnimState targetState = AnimState.Idle;
        bool isMoving = movement.sqrMagnitude > 0.01f;
        bool isWalkUpDirection = false;

        if (isHoldingCat)
        {
            string variantID = heldCatData != null ? heldCatData.catID : "";

            bool isVariantA = variantID.Contains("Kow") || variantID.Equals("AIKow");
            bool isVariantB = variantID.Contains("Dum") || variantID.Equals("AIDum");
            bool isVariantC = variantID.Contains("Som") || variantID.Equals("AISom");

            if (!isMoving)
            {
                // --- IDLE HOLDING VARIANTS ---
                if (isVariantA) targetState = AnimState.HoldCatA_Idle;
                else if (isVariantB) targetState = AnimState.HoldCatB_Idle;
                else if (isVariantC) targetState = AnimState.HoldCatC_Idle;
                else targetState = AnimState.HoldCatA_Idle;
            }
            else
            {
                // --- WALK HOLDING VARIANTS ---
                if (isVariantA) targetState = AnimState.HoldCatA_Walk;
                else if (isVariantB) targetState = AnimState.HoldCatB_Walk;
                else if (isVariantC) targetState = AnimState.HoldCatC_Walk;
                else targetState = AnimState.HoldCatA_Walk;

                if (spriteRenderer != null && movement.x != 0)
                {
                    spriteRenderer.flipX = movement.x > 0;
                }
            }
        }
        else if (!isMoving)
        {
            targetState = isHoldingItem ? AnimState.IdleHolding : AnimState.Idle;
            isWalkUpDirection = false;
        }
        else
        {
            if (movement.y > 0.1f && Mathf.Abs(movement.y) >= Mathf.Abs(movement.x))
            {
                targetState = isHoldingItem ? AnimState.WalkUpHolding : AnimState.WalkUp;
                isWalkUpDirection = true;

                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = false;
                }
            }
            else
            {
                targetState = isHoldingItem ? AnimState.WalkHolding : AnimState.Walk;
                isWalkUpDirection = false;

                if (spriteRenderer != null && movement.x != 0)
                {
                    spriteRenderer.flipX = movement.x > 0;
                }
            }
        }

        if (playerInventory != null && !isHoldingCat)
        {
            playerInventory.UpdateHolderVisibility(isHoldingItem, isWalkUpDirection);

            if (isHoldingItem)
            {
                if (!isWalkUpDirection && playerInventory.idleSideRenderer != null)
                {
                    Transform holderTransform = playerInventory.idleSideRenderer.transform;
                    Vector3 localPos = holderTransform.localPosition;
                    localPos.x = spriteRenderer.flipX ? -idleSideInitialLocalX : idleSideInitialLocalX;
                    holderTransform.localPosition = localPos;
                }
            }
        }
        else if (playerInventory != null && isHoldingCat)
        {
            playerInventory.UpdateHolderVisibility(false, false);
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
        // 🟢 แก้ไขตรงนี้เช่นกัน: เอา || IsGamePaused ออก
        if (IsLocked)
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