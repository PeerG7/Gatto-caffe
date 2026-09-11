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

    [Header("Original System References")]
    public Transform seatPoint;
    public NPCController sittingNPC;
    public bool isOccupied = false;
    public string wantedItem;

    [Header("QTE References")]
    public GameObject qteCanvasInPrefab;
    public Image qteProgressBarFill;

    [Header("Patience")]
    public float maxWaitTime = 20f;
    private float waitTimer = 0f;
    private bool isAngry = false;

    [Header("Patience Bar UI")]
    public GameObject patienceBarRoot;
    public UnityEngine.UI.Image patienceBarFill; // Image Type = Filled, Fill Method = Radial 360

    [Header("Safety Timeout (ป้องกัน NPC ค้างโต๊ะ)")]
    public float absoluteMaxSitTime = 120f;

    [Header("VIP Settings")]
    [Tooltip("ติ๊กไว้บน Prefab แมว VIP (สีพิเศษ) — หรือให้ NPCSpawner เซ็ตให้อัตโนมัติตอน Spawn")]
    public bool isVIP = false;
    [Tooltip("ตัวคูณเงินรางวัลเมื่อ Serve แมว VIP สำเร็จ เช่น 2 = ได้เงิน 2 เท่า")]
    public float vipMoneyMultiplier = 2f;

    private bool hasArrivedAtDamageTarget = false;

    [HideInInspector] public bool isInQTE = false;

    public enum NPCState { InQueue, GoingToSeat, Sitting, GoingToDamage, Leaving }
    public NPCState currentState = NPCState.InQueue;

    [Header("Order System")]
    public GameObject orderCanvas;
    public SpriteRenderer orderIcon;
    public RecipeSO requestedRecipe;
    public List<RecipeSO> allRecipes;

    [Header("Animation")]
    public Animator animator;
    public SpriteRenderer bodySpriteRenderer; // sprite that gets flipped for left/right walk
    [Tooltip("How long the NPC idles after arriving at the seat, before the sitting animation kicks in.")]
    public float idleBeforeSitDuration = 0.4f;
    [Range(0f, 1f)]
    [Tooltip("When remaining patience ratio drops to/below this, switch to the sitting-angry animation (visual warning) even though the NPC hasn't actually left yet.")]
    public float angryPatienceRatioThreshold = 0.25f;
    [Tooltip("Below this speed, the NPC is considered stopped and plays Idle.")]
    public float moveAnimThreshold = 0.05f;

    // ── ใหม่: Interaction Zone (แยกจากที่นั่งกินข้าว) ──────────────
    [HideInInspector] public InteractionZone currentZone;
    private bool isMovingToInteraction = false;
    private bool isReturningFromInteraction = false;
    private System.Action onArrivedAtInteractionZone;
    private System.Action onReturnedToSeat;

    // Idle=0, WalkSide=1, WalkUp=2, Sit=3, SitAngry=4 — set up your Animator Controller
    // with an int parameter "AnimState" and Any State -> State transitions on these values.
    private enum AnimState { Idle, WalkSide, WalkUp, Sit, SitAngry }
    private AnimState currentAnim = AnimState.Idle;
    private static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    private bool isSittingAngryAnim = false;

    // ✅ Helper — อ่าน isPaused จาก DayNightManager แทน TimeManager
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
    }

    void Update()
    {
        if (GameIsPaused)
        {
            if (agent != null) agent.isStopped = true;
            return;
        }
        else if (agent != null)
        {
            agent.isStopped = false;
        }

        if (agent == null || !agent.isOnNavMesh) return;

        UpdateMovementAnimation();

        if (currentState == NPCState.Leaving) return;

        // ── ใหม่: กำลังเดินไป/กลับจาก Interaction Zone ──────────────
        // เช็คก่อน GoingToSeat/GoingToDamage เพราะ currentState ยังเป็น Sitting อยู่
        // ระหว่างเดินไป/กลับ (ไม่เปลี่ยน state เพื่อไม่ให้ patience/QTE logic เดิมพัง)
        if (isMovingToInteraction || isReturningFromInteraction)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.isStopped = true;

                if (isMovingToInteraction)
                {
                    isMovingToInteraction = false;
                    var cb = onArrivedAtInteractionZone;
                    onArrivedAtInteractionZone = null;
                    cb?.Invoke();
                }
                else
                {
                    isReturningFromInteraction = false;
                    if (currentState == NPCState.Sitting)
                        SetAnimState(isSittingAngryAnim ? AnimState.SitAngry : AnimState.Sit);

                    var cb = onReturnedToSeat;
                    onReturnedToSeat = null;
                    cb?.Invoke();
                }
            }
            return;
        }

        if (currentState == NPCState.GoingToSeat)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                ArriveAtSeat();
        }

        if (currentState == NPCState.GoingToDamage && !hasArrivedAtDamageTarget)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                ArriveAtDamageTarget();
        }
    }

    // ------------------- ANIMATION -------------------

    void UpdateMovementAnimation()
    {
        // Sitting has its own dedicated animation handling (see EnterSeatRoutine / SitRoutine),
        // ยกเว้นตอนกำลังเดินไป/กลับจาก Interaction Zone ซึ่งต้องเล่นอนิเมชันเดินตามปกติ
        if (currentState == NPCState.Sitting && !isMovingToInteraction && !isReturningFromInteraction)
            return;

        Vector3 vel = agent.velocity;

        if (vel.sqrMagnitude < moveAnimThreshold * moveAnimThreshold)
        {
            SetAnimState(AnimState.Idle);
            return;
        }

        // Assumes a top-down layout where +Y on the NavMesh plane is "up" on screen
        // (matches updateUpAxis = false above). Flip this check if your world differs.
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

    void SetAnimState(AnimState state)
    {
        if (currentAnim == state) return;
        currentAnim = state;
        if (animator != null)
            animator.SetInteger(AnimStateHash, (int)state);
    }

    // ---------------------------------------------------

    public void SetQueueTarget(Transform target)
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            currentState = NPCState.InQueue;
        }
    }

    public void GoToTableDirectly()
    {
        if (currentState != NPCState.InQueue) return;

        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        foreach (var table in allTables)
        {
            FurnitureObject furniture = table.GetComponent<FurnitureObject>();
            bool isUnlocked = (furniture == null || furniture.isUnlocked);

            if (!table.isOccupied && isUnlocked)
            {
                table.isOccupied = true;
                table.sittingNPC = this;
                currentState = NPCState.GoingToSeat;
                if (QueueManager.Instance != null) QueueManager.Instance.RemoveFromQueue(this);
                agent.isStopped = false;
                agent.SetDestination(table.seatPoint.position);

                GenerateOrderData();

                if (requestedRecipe != null)
                {
                    table.wantedItem = requestedRecipe.recipeName;

                    // ✅ เซ็ต sprite ให้ tableItemRenderer ด้วย
                    if (table.tableItemRenderer != null)
                    {
                        table.tableItemRenderer.sprite = requestedRecipe.finalDishSprite;
                        table.tableItemRenderer.enabled = true;
                    }
                }

                // ✅ แสดง Badge บอกว่าโต๊ะนี้มีลูกค้า VIP
                table.SetVIPVisual(isVIP);
                return;
            }
        }
    }

    void GenerateOrderData()
    {
        if (allRecipes == null || allRecipes.Count == 0) return;
        int randomIndex = Random.Range(0, allRecipes.Count);
        requestedRecipe = allRecipes[randomIndex];

        // orderIcon บน NPC (ถ้ามี)
        if (requestedRecipe != null && orderIcon != null)
            orderIcon.sprite = requestedRecipe.finalDishSprite;
    }

    void ArriveAtSeat()
    {
        if (currentState == NPCState.Sitting) return;
        currentState = NPCState.Sitting;
        agent.isStopped = true;

        StartCoroutine(EnterSeatRoutine());
    }

    IEnumerator EnterSeatRoutine()
    {
        // brief idle beat between arriving and actually sitting down
        SetAnimState(AnimState.Idle);
        if (idleBeforeSitDuration > 0f)
            yield return new WaitForSeconds(idleBeforeSitDuration);

        if (currentState != NPCState.Sitting) yield break; // may have been forced off already

        isSittingAngryAnim = false;
        SetAnimState(AnimState.Sit);

        if (orderCanvas != null) orderCanvas.SetActive(true);

        // ✅ เปิด patience bar
        if (patienceBarRoot != null) patienceBarRoot.SetActive(true);
        if (patienceBarFill != null) patienceBarFill.fillAmount = 1f;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySitDown();

        StartCoroutine(SitRoutine());
        StartCoroutine(AbsoluteTimeoutRoutine());
    }

    // ── ใหม่: เดินไปโซน Interaction (ขอจากส่วนกลางผ่าน InteractionZoneManager) ──
    // onArrived จะถูกเรียกตอนแมวเดินไปถึงโซนแล้วเท่านั้น (ไม่ใช่ตอนขอจองสำเร็จ)
    public void GoToInteractionZone(System.Action onArrived)
    {
        // ✅ Fix: ซ่อน patience bar (วงกลมนับเวลาใต้ตัวแมว) ตอนเริ่มเดินไปโซน Interaction
        //    เพราะ ณ จุดนี้ Serve อาหารเสร็จแล้วเสมอ (ไม่ต้องรอ order อีกต่อไป)
        //    เดิม patienceBarRoot ถูกปิดแค่ตอน LeaveSeat() เท่านั้น ทำให้วงกลมค้างโชว์
        //    อยู่ตลอดช่วงเดินไปโซน/ทำ QTE แม้ waitTimer จะหยุดนับถูกต้องแล้วก็ตาม
        if (patienceBarRoot != null) patienceBarRoot.SetActive(false);

        if (agent == null)
        {
            onArrived?.Invoke();
            return;
        }

        if (InteractionZoneManager.Instance == null)
        {
            // ไม่มี Manager ในซีน — fallback: ทำ Interaction ที่จุดเดิมเลยกันเกมค้าง
            Debug.LogWarning("[NPCController] ไม่พบ InteractionZoneManager.Instance — ข้ามการเดินไปโซน");
            onArrived?.Invoke();
            return;
        }

        InteractionZoneManager.Instance.RequestZone(this, zone =>
        {
            currentZone = zone;
            onArrivedAtInteractionZone = onArrived;
            isMovingToInteraction = true;
            agent.isStopped = false;
            agent.SetDestination(zone.point.position);
        });
    }

    public void LeaveSeat()
    {
        isInQTE = false;
        isSittingAngryAnim = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        // ✅ ซ่อน patience bar
        if (patienceBarRoot != null) patienceBarRoot.SetActive(false);

        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        foreach (var table in allTables)
        {
            if (table.sittingNPC == this)
            {
                table.ResetTable();
                break;
            }
        }

        GoExit();
    }

    IEnumerator SitRoutine()
    {
        waitTimer = 0f;
        while (waitTimer < maxWaitTime)
        {
            if (currentState != NPCState.Sitting) yield break;
            bool shouldPause = isInQTE || GameIsPaused;
            if (!shouldPause)
            {
                waitTimer += Time.deltaTime;

                // ✅ อัปเดตวงกลม (1 = เต็ม, 0 = หมด) + เปลี่ยนสี เขียว → แดง
                float ratio = 1f - (waitTimer / maxWaitTime);
                if (patienceBarFill != null)
                {
                    patienceBarFill.fillAmount = ratio;
                    patienceBarFill.color = Color.Lerp(Color.red, Color.green, ratio);
                }

                if (!isSittingAngryAnim && ratio <= angryPatienceRatioThreshold)
                {
                    isSittingAngryAnim = true;
                    if (!isMovingToInteraction && !isReturningFromInteraction)
                        SetAnimState(AnimState.SitAngry);
                }
                else if (isSittingAngryAnim && ratio > angryPatienceRatioThreshold)
                {
                    // patience recovered in time (e.g. served just before the cutoff)
                    isSittingAngryAnim = false;
                    if (!isMovingToInteraction && !isReturningFromInteraction)
                        SetAnimState(AnimState.Sit);
                }
            }
            yield return null;
        }

        if (currentState == NPCState.Sitting && !isInQTE)
            BecomeAngry();
    }

    IEnumerator AbsoluteTimeoutRoutine()
    {
        float elapsed = 0f;
        while (elapsed < absoluteMaxSitTime)
        {
            if (currentState != NPCState.Sitting) yield break;
            if (!GameIsPaused) elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState == NPCState.Sitting)
        {
            Debug.LogWarning($"⚠️ [{gameObject.name}] ค้างโต๊ะเกิน {absoluteMaxSitTime}s — บังคับออก (isInQTE={isInQTE})");
            isInQTE = false;
            LeaveSeat();
        }
    }

    void BecomeAngry()
    {
        if (isAngry) return;
        isAngry = true;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        NPCInteract interact = GetComponent<NPCInteract>();
        if (interact != null)
            interact.PlayAngry();
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayAngry();

        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        foreach (var table in allTables)
        {
            if (table.sittingNPC == this)
            {
                table.ResetTable();
                break;
            }
        }

        if (Random.Range(0, 100) < 25)
            ChooseRandomTarget();
        else
            GoExit();
    }

    void ChooseRandomTarget()
    {
        var available = DamageableObject.allObjects
            .Where(obj => obj.IsAvailable())
            .ToList();

        if (available.Count == 0) { GoExit(); return; }

        targetObject = available[Random.Range(0, available.Count)];
        targetObject.isTargeted = true;

        currentState = NPCState.GoingToDamage;
        hasArrivedAtDamageTarget = false;
        agent.isStopped = false;
        agent.SetDestination(targetObject.transform.position);
    }

    void ArriveAtDamageTarget()
    {
        hasArrivedAtDamageTarget = true;
        if (targetObject == null) { GoExit(); return; }

        agent.isStopped = true;
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
        if (exitPoint == null) return;

        // ── ใหม่: เคลียร์สถานะ Interaction Zone ให้หมด ไม่ว่าจะกำลังเดินอยู่/
        //    ใช้งานอยู่/หรือรอคิวอยู่ — กันโซนค้าง "ถูกจอง" ตลอดไปถ้า NPC โดนบังคับออก
        isMovingToInteraction = false;
        isReturningFromInteraction = false;
        onArrivedAtInteractionZone = null;
        onReturnedToSeat = null;
        if (currentZone != null)
        {
            currentZone.Release();
            currentZone = null;
        }
        if (InteractionZoneManager.Instance != null)
            InteractionZoneManager.Instance.CancelRequest(this);

        currentState = NPCState.Leaving;
        isSittingAngryAnim = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);
        agent.isStopped = false;
        agent.SetDestination(exitPoint.position);
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
}