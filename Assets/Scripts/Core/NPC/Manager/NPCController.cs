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

    // ── ใหม่: เพิ่ม GoingToInteractionZone / AtInteractionZone แยกจาก Sitting โดยเด็ดขาด
    //    เพื่อให้ patience/QTE logic เดิมที่ผูกกับ Sitting หยุดทำงานอัตโนมัติทันทีที่
    //    ออกจาก Sitting (ผ่าน guard `if (currentState != NPCState.Sitting) yield break;`
    //    ที่มีอยู่แล้วใน SitRoutine/AbsoluteTimeoutRoutine) โดยไม่ต้องใช้ flag แยกอีกต่อไป
    public enum NPCState { InQueue, GoingToSeat, Sitting, GoingToInteractionZone, AtInteractionZone, GoingToDamage, Leaving }
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

    // ── ใหม่: Interaction Zone (แยกจากที่นั่งกินข้าวโดยเด็ดขาด — ไม่มีการเดินกลับมานั่งอีก) ──
    [HideInInspector] public InteractionZone currentZone;
    private System.Action onArrivedAtInteractionZone;

    [Header("Interaction Timeout")]
    [Tooltip("ถ้าผู้เล่นไม่มากด Interact (E) ภายในเวลานี้ (วินาที) หลังไปถึงโซน จะยกเลิกแล้วออกจากร้านไปเลย")]
    public float waitForPlayerTimeout = 15f;
    [Tooltip("หลังผู้เล่นกด Interact แล้ว ถ้าไม่เลือกประเภท QTE ภายในเวลานี้ (วินาที) จะยกเลิกแล้วออกจากร้านไปเลย")]
    public float interactionChoiceTimeout = 5f;
    private Coroutine interactionTimeoutCoroutine;

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

        // ✅ Fix: Canvas ที่ยังอยู่บน NPC (qteCanvasInPrefab, orderCanvas)
        //    ถ้าเป็น World Space ต้องเซ็ต worldCamera เอง ไม่งั้นกดปุ่มไม่ติดเลย
        //    (ของเดิมสมัยอยู่บน CustomerTable มี SetWorldSpaceCamera() ทำให้อยู่แล้ว
        //     แต่ NPCController ไม่เคยมี — เพิ่มให้ตรงนี้แทน)
        SetWorldSpaceCamera();
    }

    void Start()
    {
        // เผื่อ Camera.main ยังไม่พร้อมตอน Awake (เช่น NPC ถูก Spawn ก่อนกล้องตั้งค่าเสร็จ)
        SetWorldSpaceCamera();
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

        if (currentState == NPCState.GoingToSeat)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                ArriveAtSeat();
        }

        // ── ใหม่: เดินไป Interaction Zone (ทางเดียว ไม่มีเดินกลับ) ──────────
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
    }

    // ------------------- ANIMATION -------------------

    void UpdateMovementAnimation()
    {
        // Sitting และ AtInteractionZone มีอนิเมชันนิ่ง/QTE ของตัวเอง ไม่ต้องเล่น walk anim ทับ
        if (currentState == NPCState.Sitting || currentState == NPCState.AtInteractionZone)
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

    /// <summary>
    /// เรียกจาก CustomerTable.TryServeFood() ทันทีหลังเสิร์ฟสำเร็จ
    /// ✅ ปลดโต๊ะให้ว่างทันที ไม่ว่าแมวจะไป Interaction Zone ต่อหรือออกจากร้านเลยก็ตาม
    ///    ทำให้แมวตัวอื่นเดินเข้ามานั่งแทนได้ทันที ไม่ต้องรอแมวตัวนี้ทำอะไรเสร็จก่อน
    /// </summary>
    public void FinishServingAndProceed(bool willInteract)
    {
        isInQTE = false;
        isSittingAngryAnim = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);
        if (patienceBarRoot != null) patienceBarRoot.SetActive(false);

        // ✅ ปลดโต๊ะทันที — ไม่รอจนกว่า Interaction ที่ Zone จะจบ
        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        foreach (var table in allTables)
        {
            if (table.sittingNPC == this)
            {
                table.ResetTable();
                break;
            }
        }

        if (willInteract)
            GoToInteractionZone(ArriveAtInteractionZone);
        else
            GoExit();
    }

    // ── ใหม่: เดินไปโซน Interaction (ขอจากส่วนกลางผ่าน InteractionZoneManager) ──
    // ทางเดียว ไม่มีเดินกลับมาที่โต๊ะอีก — จบที่โซนแล้วออกจากร้านเลย
    void GoToInteractionZone(System.Action onArrived)
    {
        if (agent == null)
        {
            onArrived?.Invoke();
            return;
        }

        if (InteractionZoneManager.Instance == null)
        {
            // ไม่มี Manager ในซีน — fallback: ออกจากร้านเลยกันเกมค้าง
            Debug.LogWarning("[NPCController] ไม่พบ InteractionZoneManager.Instance — ข้ามการเดินไปโซน");
            GoExit();
            return;
        }

        // ✅ เช็คโซนว่างแบบทันที ไม่เข้าคิวรออีกต่อไป
        //    เดิมใช้ RequestZone() ซึ่งจะพาแมวเข้าคิวรอจนกว่าจะมีโซนว่าง
        //    ทำให้แมวค้างนั่งรอที่โต๊ะเมื่อโซนเต็ม — ตอนนี้ถ้าเต็มให้ออกจากร้านไปเลย
        InteractionZone zone = InteractionZoneManager.Instance.TryOccupyZoneImmediate(this);
        if (zone == null)
        {
            Debug.Log($"[{gameObject.name}] Interaction Zone เต็ม — ออกจากร้านเลย ไม่รอคิว");
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
        agent.isStopped = true;
        SetAnimState(AnimState.Sit);

        var cb = onArrivedAtInteractionZone;
        onArrivedAtInteractionZone = null;
        cb?.Invoke();

        // ✅ แค่ยืนรอที่โซนเฉยๆ — ไม่เปิดเมนูอัตโนมัติ ต้องรอผู้เล่นกด E เข้ามาก่อน (ดู RequestInteractionChoice())
        interactionTimeoutCoroutine = StartCoroutine(WaitForPlayerRoutine());
    }

    /// <summary>รอผู้เล่นเดินมากด E ที่โซน — ถ้าเกิน waitForPlayerTimeout ไม่มีใครมา ให้ออกจากร้านไปเลย</summary>
    IEnumerator WaitForPlayerRoutine()
    {
        float elapsed = 0f;
        while (elapsed < waitForPlayerTimeout)
        {
            if (currentState != NPCState.AtInteractionZone) yield break; // ถูกเรียกไปทางอื่นแล้ว (เช่น เริ่ม QTE ไปแล้ว)
            if (!GameIsPaused) elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState == NPCState.AtInteractionZone)
            FinishInteractionAtZone();
    }

    /// <summary>เรียกจาก PlayerInteract2D ตอนผู้เล่นกด E ใกล้แมวที่กำลังยืนรออยู่ที่โซน — เปิดเมนูเลือก QTE</summary>
    public bool CanRequestInteractionChoice()
    {
        return currentState == NPCState.AtInteractionZone;
    }

    public void RequestInteractionChoice()
    {
        if (currentState != NPCState.AtInteractionZone) return;

        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }

        CatSystemManager.Instance?.ShowInteractionChoice(this);
        interactionTimeoutCoroutine = StartCoroutine(InteractionChoiceTimeoutRoutine());
    }

    IEnumerator InteractionChoiceTimeoutRoutine()
    {
        float elapsed = 0f;
        while (elapsed < interactionChoiceTimeout)
        {
            if (currentState != NPCState.AtInteractionZone) yield break; // เริ่มเล่น QTE ไปแล้ว หรือถูกบังคับออกไปแล้ว
            if (!GameIsPaused) elapsed += Time.deltaTime;
            yield return null;
        }

        // ผู้เล่นไม่เลือกประเภท QTE ทันเวลา — ยกเลิกแล้วให้ออกจากร้านไปเลย
        if (currentState == NPCState.AtInteractionZone)
            FinishInteractionAtZone();
    }

    /// <summary>เปิด QTE Canvas จริง — เรียกจาก CatSystemManager ตอนผู้เล่นเลือกประเภทแล้ว</summary>
    public void OpenQTECanvas()
    {
        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }

        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(true);
    }

    /// <summary>จบ Interaction ที่โซน — ปล่อยโซนคืนแล้วออกจากร้าน (ไม่เดินกลับโต๊ะ)</summary>
    public void FinishInteractionAtZone()
    {
        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }

        CatSystemManager.Instance?.HideInteractionChoice();
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        if (currentZone != null)
        {
            currentZone.Release();
            currentZone = null;
        }

        GoExit();
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
                    SetAnimState(AnimState.SitAngry);
                }
                else if (isSittingAngryAnim && ratio > angryPatienceRatioThreshold)
                {
                    // patience recovered in time (e.g. served just before the cutoff)
                    isSittingAngryAnim = false;
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

        // ── เคลียร์สถานะ Interaction Zone ให้หมด ไม่ว่าจะกำลังเดินอยู่/ใช้งานอยู่/
        //    หรือรอคิวอยู่ — กันโซนค้าง "ถูกจอง" ตลอดไปถ้า NPC โดนบังคับออกกลางทาง
        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }
        onArrivedAtInteractionZone = null;
        if (currentZone != null)
        {
            currentZone.Release();
            currentZone = null;
        }
        if (InteractionZoneManager.Instance != null)
            InteractionZoneManager.Instance.CancelRequest(this);
        CatSystemManager.Instance?.HideInteractionChoice();

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