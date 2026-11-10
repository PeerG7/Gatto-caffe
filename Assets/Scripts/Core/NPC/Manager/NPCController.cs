using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI;

public class NPCController : MonoBehaviour
{
    private NavMeshAgent agent;
    public DamageableObject targetObject;
    public Transform exitPoint;

    [Header("Guide Event Settings (โอกาสแมวไม่รู้ตำแหน่งโต๊ะและต้องให้ Player จูง)")]
    [Range(0, 100)] public int guideEventChance = 20; // โอกาส 20%
    [HideInInspector] public bool needsGuidance = false;
    public GameObject guidanceIconObject; // Icon แจ้งเตือนบนหัวว่าต้องการให้นำทาง
    [HideInInspector] public Transform playerTransformToFollow;
    [Tooltip("ระยะที่แมวหยุดเดินตาม Player (หน่วย Unity)")]
    public float followStopDistance = 1.2f;
    [Tooltip("เวลารอให้ Player มาจูงสูงสุด (วินาที) ก่อนแมวหงุดหงิดแล้วเดินออก — นับเฉพาะตอนเกมไม่ Pause")]
    public float guidanceWaitTimeout = 30f;
    private float guidanceWaitTimer = 0f;
    private bool hasRolledGuidance = false;

    [Header("Carry Event Settings (โอกาสแมวต้องการให้อุ้ม)")]
    [Range(0, 100)] public int carryEventChance = 20; // โอกาส 20%
    [HideInInspector] public bool needsToBeCarried = false;
    public GameObject carryIconObject; // ลาก GameObject ของ Icon บนหัวแมวมาใส่ที่นี่

    [Header("Tutorial / Quest Settings")]
    [HideInInspector] public bool ignorePauseForWalking = false; // 🌟 อนุญาตให้เดินได้แม้เกมจะ Pause

    [Header("Group & Table References")]
    [HideInInspector] public CustomerTable assignedTable;
    [HideInInspector] public bool hasBeenServed = false;
    [HideInInspector] public string groupID = "";
    [HideInInspector] public bool hasFinishedEating = false;

    [Header("Original System References")]
    public Transform seatPoint;
    public bool isOccupied = false;
    public string wantedItem;

    [Header("QTE References")]
    public GameObject qteCanvasInPrefab;
    public Image qteProgressBarFill;

    [Header("Patience Settings")]
    [Tooltip("เวลารอพื้นฐานเมื่อ Relationship = 0")]
    public float maxWaitTime = 20f;
    [Tooltip("เวลาที่จะบวกเพิ่มสูงสุดเมื่อ Relationship เต็ม 100%")]
    public float maxBonusWaitTime = 15f;

    private float actualWaitTime = 20f;
    private float waitTimer = 0f;
    private bool isAngry = false;

    [Header("Queue Patience Settings (หน้าร้าน)")]
    [Tooltip("เวลารอสูงสุดเมื่อยืนรอคิวหน้าร้าน (วินาที)")]
    public float maxQueueWaitTime = 25f;
    private float queueWaitTimer = 0f;
    private bool hasReachedQueueOnce = false;
    public GameObject queuePatienceBarRoot;
    public Image queuePatienceBarFill;

    [Header("Patience Bar UI")]
    public GameObject patienceBarRoot;
    public UnityEngine.UI.Image patienceBarFill;

    [Header("Patience UI Images (2 ภาพใน Panel)")]
    public Image patienceImage1;
    public Image patienceImage2;

    [Header("Sprites (ปกติ vs เต็ม 100%)")]
    public Sprite normalSprite1;
    public Sprite maxRepSprite1;
    public Sprite normalSprite2;
    public Sprite maxRepSprite2;

    [Header("Safety Timeout (ป้องกัน NPC ค้างโต๊ะ)")]
    public float absoluteMaxSitTime = 120f;

    [Header("VIP Settings")]
    public bool isVIP = false;
    public float vipMoneyMultiplier = 2f;

    private bool hasArrivedAtDamageTarget = false;

    [HideInInspector] public bool isInQTE = false;

    // 🟢 Flag พิเศษควบคุมสิทธิ์การเปิด Interaction Canvas โดยเฉพาะ (Architectural Guard)
    [HideInInspector] public bool isInteractionAllowed = false;

    // 🟢 2-PHASE ARCHITECTURE ENUM & PROPERTY
    public enum CatPhase { DiningPhase, PlayingPhase }
    public CatPhase CurrentPhase
    {
        get
        {
            if (currentState == NPCState.AtInteractionZone || currentState == NPCState.Performing)
                return CatPhase.PlayingPhase;
            return CatPhase.DiningPhase;
        }
    }

    private bool isChoiceUIVisible = false; // ตัวแปรเช็คว่ากำลังเปิดหน้าจอเลือกช้อยส์อยู่หรือไม่ เพื่อหยุดเวลา timeout

    public enum NPCState { InQueue, GoingToSeat, Sitting, Eating, GoingToInteractionZone, AtInteractionZone, GoingToDamage, Leaving, Performing, WaitingForGuidance, FollowingPlayer }
    public NPCState currentState = NPCState.InQueue;

    [Header("Order System")]
    public GameObject orderCanvas;
    public SpriteRenderer orderIcon;
    public RecipeSO requestedRecipe;
    public List<RecipeSO> allRecipes;

    [Header("Menu Chat Bubble Reference")]
    public CatMenuChatBubble menuChatBubble;

    [Header("Animation Settings")]
    public Animator animator;
    public SpriteRenderer bodySpriteRenderer;
    public float idleBeforeSitDuration = 0.4f;
    [Range(0f, 1f)]
    public float angryPatienceRatioThreshold = 0.25f;
    public float moveAnimThreshold = 0.05f;

    [Header("Queue Arrival (กันแมวเดินวนไม่หยุดที่จุดคิว)")]
    [Tooltip("ใกล้จุดคิวเท่านี้ถือว่าถึงแล้ว (กันแมวชนกันจนไม่ถึงจุดเป๊ะๆ)")]
    public float queueArriveTolerance = 0.25f;
    private bool queueHold = false;

    [Tooltip("ใกล้จุดคิวเท่านี้ + แทบหยุดนิ่ง = ถือว่าถึงแล้ว (กรณี NavMesh Agent ไม่ยอมเข้าใกล้จุดเป๊ะๆ)")]
    public float queueSettleRadius = 0.6f;
    [Tooltip("ต้องช้า/นิ่งต่อเนื่องเท่านี้ (วินาที) ถึงจะนับว่าถึงจุดคิว")]
    public float queueSettleTime = 0.25f;
    [Tooltip("ถึงจุดคิวแล้ว จะหลุดสถานะ 'ถึงแล้ว' ก็ต่อเมื่อถูกดันออกไกลเกินรัศมีนี้ (กันกระพริบเดิน/ยืนสลับกัน)")]
    public float queueLeaveRadius = 0.9f;

    private Vector3 queueTargetPos;
    private bool hasQueueTarget = false;
    private float queueStillTimer = 0f;

    [Header("Seat Arrival (กันแมวค้างไม่ยอมนั่ง)")]
    [Tooltip("ใกล้ที่นั่งเท่านี้ถือว่าถึงแล้ว")]
    public float seatArriveDistance = 0.4f;
    [Tooltip("ยืนนิ่งนานเท่านี้ (วินาที) และอยู่ใกล้ที่นั่ง = บังคับให้นั่ง")]
    public float seatStuckTimeout = 1.5f;
    public float seatForceArriveDistance = 1.5f;
    private float seatStuckTimer = 0f;

    [Header("Perform Animation Settings")]
    public float fallbackPerformDuration = 5.0f;
    [Tooltip("ระยะเวลาหน่วงให้กล้องเริ่มถอย Zoom Out ก่อนปลดล็อคให้แมวเดินออก")]
    public float postPerformZoomDelay = 0.8f;

    [Header("Random Sit/Idle Flavor Animations (AnimState = 8)")]
    public bool enableRandomSitAnims = true;
    public float minSitAnimInterval = 4f;
    public float maxSitAnimInterval = 9f;
    public float fallbackFlavorDuration = 5.07f;

    private float queueFlavorTimer = 0f;
    private float nextQueueFlavorInterval = 5f;
    private float interactionFlavorTimer = 0f;
    private float nextInteractionFlavorInterval = 5f;

    [HideInInspector] public InteractionZone currentZone;
    private System.Action onArrivedAtInteractionZone;

    [Header("Interaction Timeout")]
    public float waitForPlayerTimeout = 15f;
    public float interactionChoiceTimeout = 5f;
    private Coroutine interactionTimeoutCoroutine;

    public enum AnimState
    {
        Idle = 0,
        WalkSide = 1,
        WalkUp = 2,
        Sit = 3,
        SitAngry = 4,
        PettingAnimation = 5,
        CatWandPlay = 6,
        CatToyMouse = 7,
        RandomSitPhase = 8,
        Chatting = 9,
        ReadingMenu = 10
    }

    private AnimState currentAnim = AnimState.Idle;
    private static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    private bool isSittingAngryAnim = false;
    private bool isPlayingSittingFlavor = false;

    private bool isPerforming = false;
    public bool IsPerforming => isPerforming;

    /// <summary>
    /// 🟢 true = แมวนั่งโต๊ะ สั่งอาหารแล้ว (ป้ายออเดอร์แสดงอยู่) และยังไม่ได้รับอาหาร
    /// ใช้โดย CustomerTable.HasAnyNPCOrdered()
    /// </summary>
    public bool HasPlacedOrder =>
        currentState == NPCState.Sitting &&
        !hasBeenServed &&
        requestedRecipe != null &&
        orderCanvas != null && orderCanvas.activeSelf;

    static bool GameIsPaused =>
        DayNightManager.Instance != null && DayNightManager.Instance.isPaused;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (bodySpriteRenderer == null) bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        isInteractionAllowed = false;
        isChoiceUIVisible = false;
        if (guidanceIconObject != null) guidanceIconObject.SetActive(false);

        SetWorldSpaceCamera();
    }

    void Start()
    {
        SetWorldSpaceCamera();
        nextQueueFlavorInterval = Random.Range(minSitAnimInterval, maxSitAnimInterval);
        nextInteractionFlavorInterval = Random.Range(minSitAnimInterval, maxSitAnimInterval);
    }

    void SetWorldSpaceCamera()
    {
        if (Camera.main == null) return;
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace)
                c.worldCamera = Camera.main;
        }
    }

    void Update()
    {
        if (GameIsPaused && !ignorePauseForWalking)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }

            // 🟢 FIX: เกม Pause = Agent หยุดนิ่ง แต่ Animator ไม่ได้หยุดตาม (Pause นี้ไม่ใช่ timeScale)
            // เดิม return ทิ้งทันที → แมวที่ถึงจุดคิวตอน Pause ยืนนิ่งแต่ท่า Walk เล่นอยู่ที่เดิม
            FreezeWalkAnimWhilePaused();
            return;
        }
        else if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (currentState != NPCState.Performing && currentState != NPCState.Sitting && currentState != NPCState.AtInteractionZone && currentState != NPCState.Eating && currentState != NPCState.WaitingForGuidance)
            {
                agent.isStopped = false;
            }
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        UpdateMovementAnimation();
        UpdateWaitingFlavorTimers();

        if (currentState == NPCState.InQueue)
        {
            if (currentState != NPCState.InQueue) return;

            // 🟢 FIX: ตัดสินว่าถึงจุดคิวจาก "ตำแหน่งจริง" เทียบกับจุดเป้าหมายที่เก็บไว้เอง
            // ไม่พึ่ง remainingDistance อย่างเดียว (ค่านี้เพี้ยนได้เมื่อปรับ Stopping Distance / Avoidance / Base Offset)
            Vector2 selfPos = transform.position;
            Vector2 targetPos = hasQueueTarget ? (Vector2)queueTargetPos : (Vector2)agent.destination;
            float distToTarget = Vector2.Distance(selfPos, targetPos);
            float arriveTol = Mathf.Max(agent.stoppingDistance, queueArriveTolerance);

            bool isAtCurrentQueuePoint;
            if (queueHold)
            {
                // ถึงแล้ว → หลุดสถานะก็ต่อเมื่อถูกดันออกไกลจริงๆ (Hysteresis)
                isAtCurrentQueuePoint = distToTarget <= queueLeaveRadius;
            }
            else
            {
                isAtCurrentQueuePoint = distToTarget <= arriveTol ||
                    (!agent.pathPending && agent.hasPath && agent.remainingDistance <= arriveTol);

                // ใกล้จุดแล้วแต่ Agent ไม่ยอมเข้าไปอีก (ชนแมวตัวอื่น/Avoidance) → ถ้านิ่งพอก็นับว่าถึง
                if (!isAtCurrentQueuePoint &&
                    !agent.pathPending &&
                    distToTarget <= queueSettleRadius &&
                    agent.velocity.sqrMagnitude < 0.15f * 0.15f)
                {
                    queueStillTimer += Time.deltaTime;
                    if (queueStillTimer >= queueSettleTime) isAtCurrentQueuePoint = true;
                }
                else if (!isAtCurrentQueuePoint)
                {
                    queueStillTimer = 0f;
                }
            }

            queueHold = isAtCurrentQueuePoint;
            if (isAtCurrentQueuePoint) queueStillTimer = 0f;

            if (isAtCurrentQueuePoint)
            {
                if (!hasReachedQueueOnce)
                {
                    hasReachedQueueOnce = true;
                    ignorePauseForWalking = false;

                    // 🟢 FIX: ถึงจุดคิวแล้ว → Idle ทันที (ถ้าเกม Pause อยู่ เฟรมถัดไป Update จะไม่ได้มาถึงส่วนอนิเมชัน)
                    if (!isPlayingSittingFlavor) SetAnimState(AnimState.Idle);
                }

                // 🟢 FIX: หยุด Agent ค้างไว้ทุกเฟรมที่อยู่ที่จุดคิว (ต้นฟังก์ชันตั้ง isStopped=false ทุกเฟรม จึงต้องสั่งซ้ำ)
                // เดิมสั่งหยุดแค่ครั้งแรกครั้งเดียว → Avoidance/แมวตัวอื่นดันจนเกิดความเร็วเล็กๆ แล้วอนิเมชันเดินเล่นวนไม่หยุด
                if (!agent.isStopped)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
            }
            else
            {
                if (agent.isStopped) agent.isStopped = false;
            }

            if (hasReachedQueueOnce)
            {
                queueWaitTimer += Time.deltaTime;
                float ratio = 1f - (queueWaitTimer / maxQueueWaitTime);

                if (queuePatienceBarFill != null)
                    queuePatienceBarFill.fillAmount = Mathf.Clamp01(ratio);

                if (queueWaitTimer >= maxQueueWaitTime)
                {
                    OnQueueWaitTimeout();
                    return;
                }
            }
        }

        if (currentState == NPCState.Leaving || currentState == NPCState.Performing) return;

        if (currentState == NPCState.GoingToSeat)
        {
            if (HasReachedSeat()) ArriveAtSeat();
        }

        if (currentState == NPCState.GoingToInteractionZone)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                ArriveAtInteractionZone();
        }

        if (currentState == NPCState.GoingToDamage && !hasArrivedAtDamageTarget)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                ArriveAtDamageTarget();
        }

        if (currentState == NPCState.WaitingForGuidance)
        {
            if (!GameIsPaused) guidanceWaitTimer += Time.deltaTime;
            if (guidanceWaitTimer >= guidanceWaitTimeout)
            {
                OnGuidanceTimeout();
            }
            return;
        }

        if (currentState == NPCState.FollowingPlayer)
        {
            if (playerTransformToFollow != null)
            {
                float distToPlayer = Vector2.Distance(transform.position, playerTransformToFollow.position);
                if (distToPlayer > followStopDistance)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTransformToFollow.position);
                }
                else
                {
                    agent.isStopped = true;
                }
                UpdateMovementAnimation();
            }
            return;
        }
    }

    private void OnQueueWaitTimeout()
    {
        if (currentState != NPCState.InQueue) return;

        if (queuePatienceBarRoot != null) queuePatienceBarRoot.SetActive(false);

        if (QueueManager.Instance != null)
        {
            QueueManager.Instance.RemoveFromQueue(this);
        }

        NPCInteract interact = GetComponent<NPCInteract>();
        if (interact != null) interact.PlayAngry();

        GoExit();
    }

    void UpdateWaitingFlavorTimers()
    {
        if (!enableRandomSitAnims || isPlayingSittingFlavor || isInQTE) return;

        if (currentState == NPCState.InQueue)
        {
            if (agent != null && agent.velocity.sqrMagnitude < moveAnimThreshold * moveAnimThreshold)
            {
                queueFlavorTimer += Time.deltaTime;
                if (queueFlavorTimer >= nextQueueFlavorInterval)
                {
                    queueFlavorTimer = 0f;
                    nextQueueFlavorInterval = Random.Range(minSitAnimInterval, maxSitAnimInterval);
                    StartCoroutine(PlayRandomSitFlavorRoutine(AnimState.Idle));
                }
            }
            else
            {
                queueFlavorTimer = 0f;
            }
        }
        else if (currentState == NPCState.AtInteractionZone)
        {
            interactionFlavorTimer += Time.deltaTime;
            if (interactionFlavorTimer >= nextInteractionFlavorInterval)
            {
                interactionFlavorTimer = 0f;
                nextInteractionFlavorInterval = Random.Range(minSitAnimInterval, maxSitAnimInterval);
                StartCoroutine(PlayRandomSitFlavorRoutine(AnimState.Sit));
            }
        }
    }

    // ตอนเกม Pause แล้วแมวหยุดเดิน → เปลี่ยนท่าเดินเป็น Idle (ไม่ให้เดินอยู่กับที่)
    void FreezeWalkAnimWhilePaused()
    {
        if (currentAnim == AnimState.WalkSide || currentAnim == AnimState.WalkUp)
            SetAnimState(AnimState.Idle);
    }

    void UpdateMovementAnimation()
    {
        if (currentState == NPCState.Sitting || currentState == NPCState.Eating || currentState == NPCState.AtInteractionZone || currentState == NPCState.Performing)
            return;

        // 🟢 ยืนอยู่ที่จุดคิวแล้ว → Idle เสมอ ไม่ว่า velocity จะแกว่งแค่ไหน
        if (currentState == NPCState.InQueue && queueHold)
        {
            if (!isPlayingSittingFlavor) SetAnimState(AnimState.Idle);
            return;
        }

        Vector3 vel = agent.velocity;

        if (isPlayingSittingFlavor && vel.sqrMagnitude < moveAnimThreshold * moveAnimThreshold)
            return;

        if (vel.sqrMagnitude < moveAnimThreshold * moveAnimThreshold)
        {
            if (currentState != NPCState.Leaving)
            {
                SetAnimState(AnimState.Idle);
            }
            return;
        }

        if (isPlayingSittingFlavor)
        {
            isPlayingSittingFlavor = false;
        }

        if (vel.y > 0.1f && Mathf.Abs(vel.y) >= Mathf.Abs(vel.x))
        {
            SetAnimState(AnimState.WalkUp);
        }
        else
        {
            SetAnimState(AnimState.WalkSide);
            if (bodySpriteRenderer != null && Mathf.Abs(vel.x) > 0.01f)
                bodySpriteRenderer.flipX = vel.x < 0f;
        }
    }

    public void SetAnimState(AnimState state, bool force = false)
    {
        if (!force && currentAnim == state) return;
        currentAnim = state;

        if (animator != null)
        {
            animator.SetInteger(AnimStateHash, (int)state);

            string stateName = state switch
            {
                AnimState.Idle => "Idle",
                AnimState.WalkSide => "Walk",
                AnimState.WalkUp => "WalkUp",
                AnimState.Sit => "Siting",
                AnimState.SitAngry => "SitingAngry",
                AnimState.PettingAnimation => "Petting animation",
                AnimState.CatWandPlay => "catwandplay",
                AnimState.CatToyMouse => "Cat_Toy_Mouse",
                AnimState.RandomSitPhase => "Cat_Random_Sitting_Phase",
                AnimState.Chatting => "Chatting",
                AnimState.ReadingMenu => "ReadingMenu",
                _ => state.ToString()
            };

            int stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
            }
        }
    }

    public void SetQueueTarget(Transform target)
    {
        isInteractionAllowed = false;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            queueHold = false; // เดินไปจุดใหม่ → เล่นอนิเมชันเดินได้อีกครั้ง
            queueStillTimer = 0f;
            queueTargetPos = target.position;
            hasQueueTarget = true;
            agent.SetDestination(target.position);
            currentState = NPCState.InQueue;

            if (queuePatienceBarRoot != null) queuePatienceBarRoot.SetActive(true);
        }
    }

    public void SetQueueTargetCustom(Vector3 targetPosition)
    {
        isInteractionAllowed = false;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            queueHold = false;
            queueStillTimer = 0f;
            queueTargetPos = targetPosition;
            hasQueueTarget = true;
            agent.SetDestination(targetPosition);
            currentState = NPCState.InQueue;

            if (queuePatienceBarRoot != null) queuePatienceBarRoot.SetActive(true);
        }
    }

    private bool TryStartGuidanceEvent()
    {
        if (hasRolledGuidance) return false;

        if (!HasAvailableTableForGroup(GetGroupSize())) return false;

        bool needGuide = Random.Range(0, 100) < guideEventChance;

        List<NPCController> members = new List<NPCController>();
        if (!string.IsNullOrEmpty(groupID))
        {
            members = FindObjectsOfType<NPCController>()
                .Where(n => n != null && n.groupID == groupID &&
                            (n == this || n.currentState == NPCState.InQueue))
                .ToList();
        }
        if (!members.Contains(this)) members.Add(this);

        foreach (var m in members) m.hasRolledGuidance = true;

        if (!needGuide) return false;

        foreach (var m in members) m.EnterWaitingForGuidance();
        return true;
    }

    private void EnterWaitingForGuidance()
    {
        if (QueueManager.Instance != null) QueueManager.Instance.RemoveFromQueue(this);
        if (queuePatienceBarRoot != null) queuePatienceBarRoot.SetActive(false);
        hasReachedQueueOnce = false;
        queueWaitTimer = 0f;

        needsGuidance = true;
        guidanceWaitTimer = 0f;
        currentState = NPCState.WaitingForGuidance;
        AllowWalkingWhilePausedIfTutorial();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

        SetAnimState(AnimState.Idle);
        if (guidanceIconObject != null) guidanceIconObject.SetActive(true);
    }

    private void OnGuidanceTimeout()
    {
        var waiting = FindObjectsOfType<NPCController>()
            .Where(n => n != null && n.currentState == NPCState.WaitingForGuidance &&
                        (n == this || (!string.IsNullOrEmpty(groupID) && n.groupID == groupID)))
            .ToList();

        foreach (var m in waiting)
        {
            m.needsGuidance = false;
            if (m.guidanceIconObject != null) m.guidanceIconObject.SetActive(false);
            if (GuidanceZoneManager.Instance != null) GuidanceZoneManager.Instance.ReleasePoint(m);

            NPCInteract inter = m.GetComponent<NPCInteract>();
            if (inter != null) inter.PlayAngry();

            m.GoExit();
        }
    }

    private void PrepareOrderForTable(CustomerTable targetTable)
    {
        if (targetTable == null) return;

        if (string.IsNullOrEmpty(targetTable.wantedItem))
        {
            GenerateOrderData();
            if (requestedRecipe != null)
            {
                targetTable.wantedItem = requestedRecipe.recipeName;
                if (targetTable.tableItemRenderer != null)
                {
                    targetTable.tableItemRenderer.sprite = requestedRecipe.finalDishSprite;
                    targetTable.tableItemRenderer.enabled = true;
                }
            }
        }
        targetTable.SetVIPVisual(isVIP);
    }

    private void AllowWalkingWhilePausedIfTutorial()
    {
        bool tutorialActive = MainQuestManager.Instance != null &&
                              MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;
        if (tutorialActive || GameIsPaused)
            ignorePauseForWalking = true;
    }

    public void GoToTableDirectly()
    {
        isInteractionAllowed = false;

        if (currentState == NPCState.WaitingForGuidance || currentState == NPCState.FollowingPlayer) return;

        if (TryStartGuidanceEvent()) return;

        int groupSize = GetGroupSize();
        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();

        var availableTables = allTables.Where(t =>
        {
            FurnitureObject furniture = t.GetComponent<FurnitureObject>();
            if (furniture == null) furniture = t.GetComponentInChildren<FurnitureObject>();

            bool isUnlocked = (furniture == null || furniture.isUnlocked);

            bool isSameGroup = t.sittingNPCs.Count > 0 &&
                               !string.IsNullOrEmpty(groupID) &&
                               t.sittingNPCs.Exists(n => n != null && n.groupID == groupID);

            return isUnlocked && (!t.isOccupied || isSameGroup);
        }).ToList();

        CustomerTable targetTable = null;

        if (groupSize == 1)
        {
            targetTable = availableTables
                .Where(t => t.seatPoints.Count >= 1)
                .OrderBy(t => t.seatPoints.Count == 1 ? 0 : 1)
                .ThenBy(t => Vector3.Distance(transform.position, t.transform.position))
                .FirstOrDefault();
        }
        else
        {
            targetTable = availableTables
                .Where(t => t.seatPoints.Count >= groupSize)
                .OrderBy(t => t.seatPoints.Count)
                .ThenBy(t => Vector3.Distance(transform.position, t.transform.position))
                .FirstOrDefault();
        }

        if (targetTable == null) return;

        if (QueueManager.Instance != null) QueueManager.Instance.RemoveFromQueue(this);
        if (queuePatienceBarRoot != null) queuePatienceBarRoot.SetActive(false);
        hasReachedQueueOnce = false;
        AllowWalkingWhilePausedIfTutorial();

        if (agent != null)
        {
            agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(transform.position);
            agent.ResetPath();
            agent.isStopped = false;
        }

        Transform seat = targetTable.GetAvailableSeat();
        if (seat != null)
        {
            seatPoint = seat;
            assignedTable = targetTable;

            PrepareOrderForTable(targetTable);

            targetTable.AssignNPCToTable(this);
            currentState = NPCState.GoingToSeat;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(seatPoint.position);
            }
        }
    }

    void GenerateOrderData()
    {
        if (allRecipes == null || allRecipes.Count == 0) return;
        int randomIndex = Random.Range(0, allRecipes.Count);
        requestedRecipe = allRecipes[randomIndex];

        if (requestedRecipe != null && orderIcon != null)
            orderIcon.sprite = requestedRecipe.finalDishSprite;
    }

    public void SetupOrderUI()
    {
        if (requestedRecipe == null)
        {
            GenerateOrderData();
        }

        if (orderCanvas != null)
        {
            orderCanvas.SetActive(true);
        }

        if (orderIcon != null && requestedRecipe != null)
        {
            orderIcon.sprite = requestedRecipe.finalDishSprite;
        }
    }

    public void ForceSitAtTable(CustomerTable table, Transform seat)
    {
        isInteractionAllowed = false;

        if (queuePatienceBarRoot != null) queuePatienceBarRoot.SetActive(false);

        if (QueueManager.Instance != null) QueueManager.Instance.RemoveFromQueue(this);

        assignedTable = table;
        seatPoint = seat;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.Warp(seat.position);
        }
        else
        {
            transform.position = seat.position;
        }

        currentState = NPCState.Sitting;
        isSittingAngryAnim = false;
        SetAnimState(AnimState.Sit);

        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);
        CatSystemManager.Instance?.HideInteractionChoice();
        PlayerController2D.SetLock(false);

        SetupOrderUI();

        if (patienceBarRoot != null) patienceBarRoot.SetActive(true);
        if (patienceBarFill != null) patienceBarFill.fillAmount = 1f;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySitDown();

        StopAllCoroutines();
        StartCoroutine(SitRoutine());
        StartCoroutine(AbsoluteTimeoutRoutine());
    }

    // 🟢 ถึงที่นั่งหรือยัง (ผ่อนเงื่อนไข + กันค้างเมื่อชนแมวตัวอื่น/โต๊ะ)
    bool HasReachedSeat()
    {
        if (seatPoint == null) return false;

        float dist = Vector2.Distance(transform.position, seatPoint.position);

        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.05f)) return true;
        if (dist <= seatArriveDistance) return true;

        // ติดค้าง: ใกล้ที่นั่งแล้วแต่ไม่ขยับ
        if (!agent.pathPending && agent.velocity.sqrMagnitude < 0.01f)
        {
            seatStuckTimer += Time.deltaTime;
            if (seatStuckTimer >= seatStuckTimeout && dist <= seatForceArriveDistance) return true;
        }
        else
        {
            seatStuckTimer = 0f;
        }

        return false;
    }

    // 🟢 วางแมวลงตำแหน่งที่นั่งจริง (เดิมแมวนั่งตรงจุดที่ Agent หยุด ซึ่งอาจเหลื่อมกับแมวอีกตัว)
    void SnapToSeat()
    {
        if (seatPoint == null) return;
        Vector3 target = seatPoint.position;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (!agent.Warp(target)) transform.position = new Vector3(target.x, target.y, transform.position.z);
        }
        else
        {
            transform.position = new Vector3(target.x, target.y, transform.position.z);
        }
    }

    void ArriveAtSeat()
    {
        isInteractionAllowed = false;
        if (currentState == NPCState.Sitting) return;
        currentState = NPCState.Sitting;
        seatStuckTimer = 0f;
        SnapToSeat();
        if (agent != null && agent.enabled) agent.isStopped = true;

        if (MainQuestManager.Instance != null && MainQuestManager.Instance.currentQuest == MainQuestManager.QuestStep.GuideCat2ToTable)
        {
            MainQuestManager.Instance.AdvanceQuest();
        }

        StartCoroutine(EnterSeatRoutine());
    }

    IEnumerator EnterSeatRoutine()
    {
        SetAnimState(AnimState.Idle);
        if (idleBeforeSitDuration > 0f)
            yield return new WaitForSeconds(idleBeforeSitDuration);

        if (currentState != NPCState.Sitting) yield break;

        isSittingAngryAnim = false;

        bool originalFlipX = bodySpriteRenderer != null && bodySpriteRenderer.flipX;

        bool willReadMenu = Random.value < 0.5f;
        if (willReadMenu)
        {
            if (assignedTable != null && bodySpriteRenderer != null)
            {
                if (transform.position.x > assignedTable.transform.position.x)
                {
                    bodySpriteRenderer.flipX = true;
                }
                else
                {
                    bodySpriteRenderer.flipX = false;
                }
            }

            SetAnimState(AnimState.ReadingMenu);

            // 🌟 IMPLEMENTED: Trigger food menu chat bubble here
            if (menuChatBubble != null)
            {
                bool faceRight = (bodySpriteRenderer != null) ? !bodySpriteRenderer.flipX : true;
                menuChatBubble.ShowFoodMenuBubble(faceRight, 2.0f, allRecipes);
            }

            yield return new WaitForSeconds(2.0f);

            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.flipX = originalFlipX;
            }
        }

        SetAnimState(AnimState.Sit);

        yield return new WaitForSeconds(1.5f);

        if (currentState != NPCState.Sitting) yield break;

        bool isTwoSeaterOrMore = assignedTable != null && assignedTable.seatPoints.Count >= 2;
        bool hasFriendWithThem = assignedTable != null && assignedTable.sittingNPCs.Count >= 2;
        bool willChat = isTwoSeaterOrMore && hasFriendWithThem && (Random.Range(0, 100) < assignedTable.chatTriggerChance);

        if (willChat)
        {
            if (assignedTable != null)
            {
                assignedTable.TriggerTableChat();
            }

            NPCController friend = assignedTable.sittingNPCs.Find(n => n != null && n != this);

            if (friend != null && bodySpriteRenderer != null)
            {
                if (transform.position.x > friend.transform.position.x)
                {
                    bodySpriteRenderer.flipX = true;
                }
                else
                {
                    bodySpriteRenderer.flipX = false;
                }
            }

            SetAnimState(AnimState.Chatting);

            float chatTimer = 0f;
            while (chatTimer < 5f)
            {
                if (currentState != NPCState.Sitting) yield break;
                if (!GameIsPaused) chatTimer += Time.deltaTime;
                yield return null;
            }

            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.flipX = originalFlipX;
            }

            SetAnimState(AnimState.Sit);
        }

        SetupOrderUI();

        if (patienceBarRoot != null) patienceBarRoot.SetActive(true);
        if (patienceBarFill != null) patienceBarFill.fillAmount = 1f;

        float currentRelRatio = GetCurrentCatRelationshipRatio();
        bool isMaxRep = currentRelRatio >= 0.999f;

        if (patienceImage1 != null)
        {
            Sprite target1 = isMaxRep ? maxRepSprite1 : normalSprite1;
            if (target1 != null) patienceImage1.sprite = target1;
        }

        if (patienceImage2 != null)
        {
            Sprite target2 = isMaxRep ? maxRepSprite2 : normalSprite2;
            if (target2 != null) patienceImage2.sprite = target2;
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySitDown();

        NPCInteract interactWantsFood = GetComponent<NPCInteract>();
        if (interactWantsFood != null)
            interactWantsFood.PlayWantsFood();
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayWantsFood();

        float postOrderWait = 1.5f;
        float postTimer = 0f;
        while (postTimer < postOrderWait)
        {
            if (currentState != NPCState.Sitting) yield break;
            if (!GameIsPaused) postTimer += Time.deltaTime;
            yield return null;
        }

        StartCoroutine(SitRoutine());
        StartCoroutine(AbsoluteTimeoutRoutine());
    }

    public void StartEatingProcess()
    {
        isInteractionAllowed = false;
        StopAllCoroutines();
        if (patienceBarRoot != null) patienceBarRoot.SetActive(false);
        if (orderCanvas != null) orderCanvas.SetActive(false);

        currentState = NPCState.Eating;
        StartCoroutine(EatingRoutine());
    }

    IEnumerator EatingRoutine()
    {
        float eatTimer = 0f;
        float eatDuration = 5f;

        while (eatTimer < eatDuration)
        {
            bool tutorialActive = MainQuestManager.Instance != null &&
                                  MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;

            if (!GameIsPaused || tutorialActive)
            {
                eatTimer += Time.deltaTime;
            }
            yield return null;
        }

        if (assignedTable != null)
        {
            int reward = assignedTable.dishReward;
            assignedTable.LeaveMoneyOnTable(reward, isVIP);

            hasFinishedEating = true;

            int roll = Random.Range(0, 100);
            if (roll < carryEventChance)
            {
                needsToBeCarried = true;
                currentState = NPCState.Sitting;

                if (carryIconObject != null) carryIconObject.SetActive(true);
                yield break;
            }

            CatIdentity identity = GetComponent<CatIdentity>();
            bool isDataCat1 = identity != null && identity.IsDataCat1();

            if (isDataCat1)
            {
                currentState = NPCState.Sitting;
                yield break;
            }
            else
            {
                assignedTable.RemoveNPCFromTable(this);
                bool willInteract = Random.Range(0, 101) <= (assignedTable != null ? assignedTable.interactionChance : 70);
                FinishEatingAndProceed(willInteract);
            }
        }
    }

    public void FinishEatingAndProceed(bool willInteract)
    {
        isInQTE = false;
        isSittingAngryAnim = false;
        isPlayingSittingFlavor = false;

        if (willInteract)
            GoToInteractionZone(ArriveAtInteractionZone);
        else
            GoExit();
    }

    void GoToInteractionZone(System.Action onArrived)
    {
        isInteractionAllowed = false;
        if (agent == null || !agent.enabled)
        {
            onArrived?.Invoke();
            return;
        }

        if (InteractionZoneManager.Instance == null)
        {
            GoExit();
            return;
        }

        InteractionZone zone = InteractionZoneManager.Instance.TryOccupyZoneImmediate(this);
        if (zone == null)
        {
            GoExit();
            return;
        }

        currentZone = zone;
        onArrivedAtInteractionZone = onArrived;
        currentState = NPCState.GoingToInteractionZone;
        agent.isStopped = false;
        agent.SetDestination(zone.point.position);
    }

    void ArriveAtInteractionZone()
    {
        if (currentState == NPCState.AtInteractionZone) return;
        currentState = NPCState.AtInteractionZone;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        SetAnimState(AnimState.Sit);

        isInteractionAllowed = true;
        isChoiceUIVisible = false;

        var cb = onArrivedAtInteractionZone;
        onArrivedAtInteractionZone = null;
        cb?.Invoke();

        interactionTimeoutCoroutine = StartCoroutine(WaitForPlayerRoutine());
    }

    public void InitAtInteractionZone(InteractionZone zone)
    {
        currentZone = zone;
        zone.Occupy(this);
        currentState = NPCState.AtInteractionZone;

        needsToBeCarried = false;
        if (carryIconObject != null) carryIconObject.SetActive(false);

        isInteractionAllowed = true;
        isChoiceUIVisible = false;

        if (agent != null)
        {
            agent.enabled = false;
            transform.position = zone.point.position;
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.Warp(zone.point.position);
            }
        }
        else
        {
            transform.position = zone.point.position;
        }

        SetAnimState(AnimState.Sit, true);
        StopAllCoroutines();
        interactionTimeoutCoroutine = StartCoroutine(WaitForPlayerRoutine());
    }

    IEnumerator WaitForPlayerRoutine()
    {
        float elapsed = 0f;
        while (elapsed < waitForPlayerTimeout)
        {
            if (currentState != NPCState.AtInteractionZone) yield break;

            if (!isChoiceUIVisible && !GameIsPaused)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        if (currentState == NPCState.AtInteractionZone)
            FinishInteractionAtZone();
    }

    public bool CanRequestInteractionChoice()
    {
        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (var npc in allNPCs)
        {
            if (npc != null && npc != this && npc.IsPerforming)
            {
                return false;
            }
        }

        return CurrentPhase == CatPhase.PlayingPhase && isInteractionAllowed && currentState == NPCState.AtInteractionZone && assignedTable == null;
    }

    public void RequestInteractionChoice()
    {
        if (!CanRequestInteractionChoice())
        {
            // ❗ ไม่ปลดล็อกที่นี่ — การล็อก/ปลดล็อกเป็นหน้าที่ของ CatSystemManager
            return;
        }

        StopInteractionTimeout();
        isChoiceUIVisible = true;

        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        QTEZoomController.Instance?.ZoomInToQTE(transform, true);
        CatSystemManager.Instance?.ShowInteractionChoice(this);
    }

    // เลือกแมวใน Scene view ตอนรันเกม → เห็นรัศมี "ถึงจุดคิว" (เขียว) และรัศมีผ่อนปรน (เหลือง)
    void OnDrawGizmosSelected()
    {
        if (!hasQueueTarget) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(queueTargetPos, queueArriveTolerance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(queueTargetPos, queueSettleRadius);
    }

    void OnDestroy()
    {
        // กำลัง Perform อยู่แล้วโดนลบ → Coroutine ตายก่อนถึง SetLock(false)
        if (isPerforming)
        {
            isPerforming = false;
            PlayerController2D.SetLock(false);

            if (QTEZoomController.Instance != null)
            {
                QTEZoomController.Instance.UnlockQTEZoom();
                QTEZoomController.Instance.ZoomOutToGameplay(true);
            }
        }

        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.NotifyNPCRemoved(this);

        if (currentZone != null)
        {
            currentZone.Release();
            currentZone = null;
        }
    }

    public void StopInteractionTimeout()
    {
        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }
    }

    public void OpenQTECanvas()
    {
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        QTEZoomController.Instance?.ZoomInToQTE(transform, true);
    }

    public void PlayPerformAndExit(int performIndex = 0, float customDuration = -1f)
    {
        StartCoroutine(PerformAnimationRoutine(performIndex, customDuration));
    }

    private IEnumerator PerformAnimationRoutine(int performIndex, float durationOverride)
    {
        if (isPerforming) yield break;
        isPerforming = true;

        StopInteractionTimeout();

        currentState = NPCState.Performing;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        if (currentZone != null && currentZone.point != null)
        {
            transform.position = currentZone.point.position;
        }

        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);
        CatSystemManager.Instance?.HideInteractionChoice();

        AnimState targetState = performIndex switch
        {
            1 => AnimState.CatWandPlay,
            2 => AnimState.CatToyMouse,
            3 => AnimState.RandomSitPhase,
            _ => AnimState.PettingAnimation
        };

        string targetStateName = targetState switch
        {
            AnimState.CatWandPlay => "catwandplay",
            AnimState.CatToyMouse => "Cat_Toy_Mouse",
            AnimState.RandomSitPhase => "Cat_Random_Sitting_Phase",
            _ => "Petting animation"
        };

        SetAnimState(targetState, true);

        float transitionWait = 1.0f;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(targetStateName) && transitionWait > 0f)
        {
            bool tutorialActive = MainQuestManager.Instance != null &&
                                  MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;
            if (!GameIsPaused || tutorialActive) transitionWait -= Time.deltaTime;
            yield return null;
        }

        float clipDuration = durationOverride > 0f ? durationOverride : fallbackPerformDuration;

        if (durationOverride <= 0f && animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(targetStateName) && info.length > 0.1f)
            {
                clipDuration = info.length;
            }
        }

        float safePlayDuration = clipDuration + 0.4f;
        float timer = 0f;
        while (timer < safePlayDuration)
        {
            bool tutorialActive = MainQuestManager.Instance != null &&
                                  MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;

            if (!GameIsPaused || tutorialActive)
            {
                timer += Time.deltaTime;
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
            }
            yield return null;
        }

        if (postPerformZoomDelay > 0f)
        {
            float delayTimer = 0f;
            while (delayTimer < postPerformZoomDelay)
            {
                bool tutorialActive = MainQuestManager.Instance != null &&
                                      MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;
                if (!GameIsPaused || tutorialActive) delayTimer += Time.deltaTime;
                yield return null;
            }
        }

        isPerforming = false;
        currentAnim = (AnimState)(-1);

        PlayerController2D.SetLock(false);

        FinishInteractionAtZone();
    }

    public void FinishInteractionAtZone()
    {
        if (isPerforming) return;

        isInteractionAllowed = false;
        isChoiceUIVisible = false;

        StopInteractionTimeout();

        CatSystemManager.Instance?.HideInteractionChoice();
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        QTEZoomController.Instance?.UnlockQTEZoom();
        QTEZoomController.Instance?.ZoomOutToGameplay(true);

        if (currentZone != null)
        {
            currentZone.Release();
            currentZone = null;
        }

        if (MainQuestManager.Instance != null && MainQuestManager.Instance.currentQuest == MainQuestManager.QuestStep.CarryCat2ToZone)
        {
            MainQuestManager.Instance.AdvanceQuest();
        }

        GoExit();
    }

    public void LeaveSeat()
    {
        isInteractionAllowed = false;
        isChoiceUIVisible = false;
        isInQTE = false;
        isSittingAngryAnim = false;
        isPlayingSittingFlavor = false;
        needsToBeCarried = false;
        if (carryIconObject != null) carryIconObject.SetActive(false);

        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);
        if (patienceBarRoot != null) patienceBarRoot.SetActive(false);

        if (assignedTable != null)
        {
            assignedTable.RemoveNPCFromTable(this);
        }

        GoExit();
    }

    IEnumerator SitRoutine()
    {
        waitTimer = 0f;

        float currentRelRatio = GetCurrentCatRelationshipRatio();
        actualWaitTime = maxWaitTime + (maxBonusWaitTime * currentRelRatio);

        float flavorTimer = 0f;
        float nextFlavorInterval = Random.Range(minSitAnimInterval, maxSitAnimInterval);

        CatIdentity identity = GetComponent<CatIdentity>();
        bool isDataCat1 = identity != null && identity.IsDataCat1();

        while (waitTimer < actualWaitTime)
        {
            if (currentState != NPCState.Sitting) yield break;

            if (isDataCat1)
            {
                if (patienceBarRoot != null) patienceBarRoot.SetActive(false);
                yield return null;
                continue;
            }

            bool shouldPause = isInQTE || GameIsPaused;
            if (!shouldPause)
            {
                waitTimer += Time.deltaTime;

                float ratio = 1f - (waitTimer / actualWaitTime);
                if (patienceBarFill != null)
                {
                    patienceBarFill.fillAmount = ratio;
                    patienceBarFill.color = Color.Lerp(Color.red, Color.green, ratio);
                }

                if (enableRandomSitAnims && !isPlayingSittingFlavor)
                {
                    flavorTimer += Time.deltaTime;
                    if (flavorTimer >= nextFlavorInterval)
                    {
                        StartCoroutine(PlayRandomSitFlavorRoutine(ratio <= angryPatienceRatioThreshold ? AnimState.SitAngry : AnimState.Sit));
                        flavorTimer = 0f;
                        nextFlavorInterval = Random.Range(minSitAnimInterval, maxSitAnimInterval);
                    }
                }

                if (!isPlayingSittingFlavor)
                {
                    if (!isSittingAngryAnim && ratio <= angryPatienceRatioThreshold)
                    {
                        isSittingAngryAnim = true;
                        SetAnimState(AnimState.SitAngry);
                    }
                    else if (isSittingAngryAnim && ratio > angryPatienceRatioThreshold)
                    {
                        isSittingAngryAnim = false;
                        SetAnimState(AnimState.Sit);
                    }
                }
            }
            yield return null;
        }

        if (currentState == NPCState.Sitting && !isInQTE && !isDataCat1)
            BecomeAngry();
    }

    IEnumerator PlayRandomSitFlavorRoutine(AnimState returnState)
    {
        isPlayingSittingFlavor = true;

        SetAnimState(AnimState.RandomSitPhase, true);

        float transitionWait = 0.5f;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Cat_Random_Sitting_Phase") && transitionWait > 0f)
        {
            transitionWait -= Time.deltaTime;
            yield return null;
        }

        float clipLength = fallbackFlavorDuration;
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Cat_Random_Sitting_Phase") && info.length > 0.1f)
            {
                clipLength = info.length;
            }
        }

        float elapsed = 0f;
        while (elapsed < clipLength)
        {
            if (currentState == NPCState.InQueue && agent != null && agent.enabled && agent.velocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold)
            {
                break;
            }

            if (currentState == NPCState.Leaving || isInQTE) break;
            if (!GameIsPaused) elapsed += Time.deltaTime;
            yield return null;
        }

        isPlayingSittingFlavor = false;

        if (currentState == NPCState.Sitting || currentState == NPCState.InQueue || currentState == NPCState.AtInteractionZone)
        {
            SetAnimState(returnState, true);
        }
    }

    public void AddExtraPatience(float bonusTime)
    {
        if (currentState != NPCState.Sitting) return;

        waitTimer = Mathf.Max(0f, waitTimer - bonusTime);

        if (patienceBarFill != null && actualWaitTime > 0f)
        {
            float ratio = 1f - (waitTimer / actualWaitTime);
            patienceBarFill.fillAmount = ratio;
            patienceBarFill.color = Color.Lerp(Color.red, Color.green, ratio);

            if (isSittingAngryAnim && ratio > angryPatienceRatioThreshold)
            {
                isSittingAngryAnim = false;
                SetAnimState(AnimState.Sit);
            }
        }
    }

    private float GetCurrentCatRelationshipRatio()
    {
        if (RelationshipManager.Instance == null) return 0f;

        string catID = "";
        CatIdentity identity = GetComponent<CatIdentity>();
        if (identity != null)
        {
            catID = identity.CatID;
        }
        else
        {
            NPCRelationship npcRel = GetComponent<NPCRelationship>();
            if (npcRel != null) catID = npcRel.npcName;
        }

        if (string.IsNullOrEmpty(catID)) return 0f;

        CatRelationshipData data = RelationshipManager.Instance.allCats.Find(c => c.catID == catID);
        float maxRel = data != null ? data.maxRelationship : 100f;
        float currentRel = RelationshipManager.Instance.GetRelationship(catID);

        return Mathf.Clamp01(currentRel / maxRel);
    }

    IEnumerator AbsoluteTimeoutRoutine()
    {
        CatIdentity identity = GetComponent<CatIdentity>();
        if (identity != null && identity.IsDataCat1()) yield break;

        float elapsed = 0f;
        while (elapsed < absoluteMaxSitTime)
        {
            if (currentState != NPCState.Sitting) yield break;
            if (!GameIsPaused) elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState == NPCState.Sitting)
        {
            isInQTE = false;
            LeaveSeat();
        }
    }

    void BecomeAngry()
    {
        if (isAngry) return;
        isAngry = true;
        isInteractionAllowed = false;
        isChoiceUIVisible = false;
        isPlayingSittingFlavor = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        NPCInteract interact = GetComponent<NPCInteract>();
        bool playSad = Random.Range(0, 100) < 50;

        if (interact != null)
        {
            if (playSad) interact.PlaySad();
            else interact.PlayAngry();
        }
        else if (AudioManager.instance != null)
        {
            if (playSad) AudioManager.instance.Catsad();
            else AudioManager.instance.PlayAngry();
        }

        if (assignedTable != null)
        {
            assignedTable.RemoveNPCFromTable(this);
        }

        if (Random.Range(0, 100) < 25)
            ChooseRandomTarget();
        else
            GoExit();
    }

    void ChooseRandomTarget()
    {
        isInteractionAllowed = false;
        isChoiceUIVisible = false;
        var available = DamageableObject.allObjects
            .Where(obj => obj.IsAvailable())
            .ToList();

        if (available.Count == 0) { GoExit(); return; }

        targetObject = available[Random.Range(0, available.Count)];
        targetObject.isTargeted = true;

        currentState = NPCState.GoingToDamage;
        hasArrivedAtDamageTarget = false;
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(targetObject.transform.position);
        }
    }

    void ArriveAtDamageTarget()
    {
        hasArrivedAtDamageTarget = true;
        if (targetObject == null) { GoExit(); return; }

        if (agent != null && agent.enabled) agent.isStopped = true;
        targetObject.StartDamage();
        StartCoroutine(WaitThenExit(targetObject.damageTime));
    }

    IEnumerator WaitThenExit(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if (targetObject != null)
            targetObject.isTargeted = false;
        GoExit();
    }

    public void GoExit()
    {
        if (isPerforming) return;

        isInteractionAllowed = false;
        isChoiceUIVisible = false;

        if (exitPoint == null)
        {
            GameObject exitObj = GameObject.Find("ExitPoint");

            if (exitObj != null)
            {
                exitPoint = exitObj.transform;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        if (queuePatienceBarRoot != null) queuePatienceBarRoot.SetActive(false);
        needsGuidance = false;
        if (guidanceIconObject != null) guidanceIconObject.SetActive(false);

        StopInteractionTimeout();
        onArrivedAtInteractionZone = null;
        if (currentZone != null)
        {
            currentZone.Release();
            currentZone = null;
        }

        if (InteractionZoneManager.Instance != null)
            InteractionZoneManager.Instance.CancelRequest(this);

        CatSystemManager.Instance?.HideInteractionChoice();

        QTEZoomController.Instance?.UnlockQTEZoom();
        QTEZoomController.Instance?.ZoomOutToGameplay(true);

        currentState = NPCState.Leaving;
        isSittingAngryAnim = false;
        isPlayingSittingFlavor = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPoint.position);
        }

        Vector3 dir = (exitPoint.position - transform.position).normalized;
        if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x) && dir.y > 0.1f)
        {
            SetAnimState(AnimState.WalkUp, true);
        }
        else
        {
            SetAnimState(AnimState.WalkSide, true);
            if (bodySpriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
                bodySpriteRenderer.flipX = dir.x < 0f;
        }

        StartCoroutine(DestroyWhenArrive());
    }

    IEnumerator DestroyWhenArrive()
    {
        yield return new WaitForSeconds(0.5f);
        while (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.5f) break;
            yield return null;
        }
        Destroy(gameObject);
    }

    public int GetGroupSize()
    {
        if (string.IsNullOrEmpty(groupID)) return 1;
        int count = FindObjectsOfType<NPCController>().Count(n => n != null && n.groupID == groupID);
        return count > 0 ? count : 1;
    }

    public static bool HasAvailableTableForGroup(int groupSize)
    {
        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        foreach (var table in allTables)
        {
            FurnitureObject furn = table.GetComponent<FurnitureObject>();
            bool isUnlocked = (furn == null || furn.isUnlocked);
            if (isUnlocked && !table.isOccupied && table.seatPoints.Count >= groupSize)
            {
                return true;
            }
        }
        return false;
    }

    public float GetPatienceRatio()
    {
        if (actualWaitTime <= 0f) return 1f;
        float ratio = 1f - Mathf.Clamp01(waitTimer / actualWaitTime);
        return ratio;
    }

    public void StartFollowingPlayer(Transform player)
    {
        if (!needsGuidance) return;

        if (!string.IsNullOrEmpty(groupID))
        {
            NPCController[] allNPCs = FindObjectsOfType<NPCController>();
            foreach (var npc in allNPCs)
            {
                if (npc != null && npc != this && npc.groupID == groupID && npc.needsGuidance && npc.currentState == NPCState.WaitingForGuidance)
                {
                    npc.StartFollowingPlayerWithoutGroupCheck(player);
                }
            }
        }

        StartFollowingPlayerWithoutGroupCheck(player);
    }

    public void StartFollowingPlayerWithoutGroupCheck(Transform player)
    {
        if (GuidanceZoneManager.Instance != null)
        {
            GuidanceZoneManager.Instance.ReleasePoint(this);
        }

        playerTransformToFollow = player;
        currentState = NPCState.FollowingPlayer;
        AllowWalkingWhilePausedIfTutorial();
        if (guidanceIconObject != null) guidanceIconObject.SetActive(false);
        if (agent != null && agent.enabled)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    public bool TrySitAtTable(CustomerTable targetTable)
    {
        if (currentState != NPCState.FollowingPlayer || targetTable == null) return false;

        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        List<NPCController> groupMembers = new List<NPCController>();

        foreach (var npc in allNPCs)
        {
            if (npc != null && !string.IsNullOrEmpty(groupID) && npc.groupID == groupID && npc.currentState == NPCState.FollowingPlayer)
            {
                groupMembers.Add(npc);
            }
        }

        if (groupMembers.Count == 0) groupMembers.Add(this);

        if (targetTable.seatPoints.Count - targetTable.sittingNPCs.Count < groupMembers.Count)
        {
            return false;
        }

        foreach (var member in groupMembers)
        {
            Transform seat = targetTable.GetAvailableSeat();
            if (seat != null)
            {
                member.assignedTable = targetTable;
                member.seatPoint = seat;
                member.PrepareOrderForTable(targetTable);
                targetTable.AssignNPCToTable(member);

                member.needsGuidance = false;
                member.playerTransformToFollow = null;

                member.AllowWalkingWhilePausedIfTutorial();
                member.currentState = NPCState.GoingToSeat;
                if (member.agent != null && member.agent.enabled && member.agent.isOnNavMesh)
                {
                    member.agent.isStopped = false;
                    member.agent.SetDestination(member.seatPoint.position);
                }
            }
        }

        return true;
    }
}