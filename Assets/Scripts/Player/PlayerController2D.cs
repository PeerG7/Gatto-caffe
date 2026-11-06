using UnityEngine;
using System.Collections.Generic;

// =====================================================================
// PlayerController2D — Inspector-Driven Cat Animation Mapping System
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
        WalkUpHolding = 5,

        HoldCatA_Idle = 6,
        HoldCatB_Idle = 7,
        HoldCatC_Idle = 8,
        HoldCatA_Walk = 9,
        HoldCatB_Walk = 10,
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
    private GameObject heldCatObject = null;

    [System.Serializable]
    public struct CatAnimationMapping
    {
        [Tooltip("พิมพ์ Variant ID เช่น AIKow, AIDum, AISom")]
        public string variantID;
        public AnimState idleAnimState;
        public AnimState walkAnimState;
    }

    [Header("Inspector-Driven Cat Variant Animation Mappings")]
    public List<CatAnimationMapping> catAnimationMappings = new List<CatAnimationMapping>();

    public static void SetLock(bool value)
    {
        IsLocked = value;
    }

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

            // ค้นหาค่า Mapping จาก Inspector List
            CatAnimationMapping matchedMapping = catAnimationMappings.Find(m => !string.IsNullOrEmpty(variantID) && variantID.Contains(m.variantID));

            if (string.IsNullOrEmpty(matchedMapping.variantID) && catAnimationMappings.Count > 0)
            {
                matchedMapping = catAnimationMappings[0]; // Fallback ตัวแรก
            }

            if (!isMoving)
            {
                targetState = matchedMapping.idleAnimState;
            }
            else
            {
                targetState = matchedMapping.walkAnimState;

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