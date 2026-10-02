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

    [Header("Group & Table References")]
    [HideInInspector] public CustomerTable assignedTable;
    [HideInInspector] public bool hasBeenServed = false;
    [HideInInspector] public string groupID = "";

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

    [Header("Patience Bar UI")]
    public GameObject patienceBarRoot;
    public UnityEngine.UI.Image patienceBarFill;

    [Header("Patience UI Images (2 ภาพใน Panel)")]
    public Image patienceImage1;        // UI Image อันที่ 1 ใน Panel
    public Image patienceImage2;        // UI Image อันที่ 2 ใน Panel

    [Header("Sprites (ปกติ vs เต็ม 100%)")]
    public Sprite normalSprite1;        // ภาพปกติ Image 1
    public Sprite maxRepSprite1;        // ภาพเต็ม 100% Image 1
    public Sprite normalSprite2;        // ภาพปกติ Image 2
    public Sprite maxRepSprite2;        // ภาพเต็ม 100% Image 2

    [Header("Safety Timeout (ป้องกัน NPC ค้างโต๊ะ)")]
    public float absoluteMaxSitTime = 120f;

    [Header("VIP Settings")]
    public bool isVIP = false;
    public float vipMoneyMultiplier = 2f;

    private bool hasArrivedAtDamageTarget = false;

    [HideInInspector] public bool isInQTE = false;

    public enum NPCState { InQueue, GoingToSeat, Sitting, GoingToInteractionZone, AtInteractionZone, GoingToDamage, Leaving, Performing }
    public NPCState currentState = NPCState.InQueue;

    [Header("Order System")]
    public GameObject orderCanvas;
    public SpriteRenderer orderIcon;
    public RecipeSO requestedRecipe;
    public List<RecipeSO> allRecipes;

    [Header("Animation")]
    public Animator animator;
    public SpriteRenderer bodySpriteRenderer;
    public float idleBeforeSitDuration = 0.4f;
    [Range(0f, 1f)]
    public float angryPatienceRatioThreshold = 0.25f;
    public float moveAnimThreshold = 0.05f;

    [Header("Perform Animation Settings")]
    public float fallbackPerformDuration = 2.26f;

    [HideInInspector] public InteractionZone currentZone;
    private System.Action onArrivedAtInteractionZone;

    [Header("Interaction Timeout")]
    public float waitForPlayerTimeout = 15f;
    public float interactionChoiceTimeout = 5f;
    private Coroutine interactionTimeoutCoroutine;

    private enum AnimState { Idle, WalkSide, WalkUp, Sit, SitAngry, Perform1, Perform2, Perform3 }
    private AnimState currentAnim = AnimState.Idle;
    private static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    private bool isSittingAngryAnim = false;

    private bool isPerforming = false;

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

        SetWorldSpaceCamera();
    }

    void Start()
    {
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
            if (currentState != NPCState.Performing && currentState != NPCState.Sitting && currentState != NPCState.AtInteractionZone)
            {
                agent.isStopped = false;
            }
        }

        if (agent == null || !agent.isOnNavMesh) return;

        UpdateMovementAnimation();

        if (currentState == NPCState.Leaving || currentState == NPCState.Performing) return;

        if (currentState == NPCState.GoingToSeat)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                ArriveAtSeat();
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
    }

    void UpdateMovementAnimation()
    {
        if (currentState == NPCState.Sitting || currentState == NPCState.AtInteractionZone || currentState == NPCState.Performing)
            return;

        bool isStuckInPerform = false;
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Perform1") || info.IsName("Perform2") || info.IsName("Perform3") ||
                info.IsName("Perform_1") || info.IsName("Perform_2") || info.IsName("Perform_3"))
            {
                isStuckInPerform = true;
            }
        }

        Vector3 vel = agent.velocity;

        if (vel.sqrMagnitude < moveAnimThreshold * moveAnimThreshold)
        {
            if (currentState != NPCState.Leaving)
            {
                SetAnimState(AnimState.Idle, isStuckInPerform);
            }
            return;
        }

        if (vel.y > 0.1f && Mathf.Abs(vel.y) >= Mathf.Abs(vel.x))
        {
            SetAnimState(AnimState.WalkUp, isStuckInPerform);
        }
        else
        {
            SetAnimState(AnimState.WalkSide, isStuckInPerform);
            if (bodySpriteRenderer != null && Mathf.Abs(vel.x) > 0.01f)
                bodySpriteRenderer.flipX = vel.x < 0f;
        }
    }

    void SetAnimState(AnimState state, bool force = false)
    {
        if (!force && currentAnim == state) return;
        currentAnim = state;

        if (animator != null)
        {
            animator.SetInteger(AnimStateHash, (int)state);

            string enumName = state.ToString();
            int primaryHash = Animator.StringToHash(enumName);

            if (animator.HasState(0, primaryHash))
            {
                animator.Play(primaryHash, 0, 0f);
            }
            else
            {
                string altName = enumName switch
                {
                    "WalkSide" => "Walk_Side",
                    "WalkUp" => "Walk_Up",
                    "SitAngry" => "Sit_Angry",
                    "Perform1" => "Perform_1",
                    "Perform2" => "Perform_2",
                    "Perform3" => "Perform_3",
                    _ => enumName
                };

                int altHash = Animator.StringToHash(altName);
                if (animator.HasState(0, altHash))
                {
                    animator.Play(altHash, 0, 0f);
                }
                else if (state == AnimState.WalkSide || state == AnimState.WalkUp)
                {
                    int genericWalkHash = Animator.StringToHash("Walk");
                    if (animator.HasState(0, genericWalkHash))
                    {
                        animator.Play(genericWalkHash, 0, 0f);
                    }
                }
            }
        }
    }

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

            Transform seat = table.GetAvailableSeat();

            if (seat != null && isUnlocked)
            {
                table.AssignNPCToTable(this);
                seatPoint = seat;
                currentState = NPCState.GoingToSeat;

                if (QueueManager.Instance != null) QueueManager.Instance.RemoveFromQueue(this);

                agent.isStopped = false;
                agent.SetDestination(seatPoint.position);

                if (string.IsNullOrEmpty(table.wantedItem))
                {
                    GenerateOrderData();

                    if (requestedRecipe != null)
                    {
                        table.wantedItem = requestedRecipe.recipeName;

                        if (table.tableItemRenderer != null)
                        {
                            table.tableItemRenderer.sprite = requestedRecipe.finalDishSprite;
                            table.tableItemRenderer.enabled = true;
                        }
                    }
                }

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

        if (requestedRecipe != null && orderIcon != null)
            orderIcon.sprite = requestedRecipe.finalDishSprite;
    }

    // 🟢 ฟังก์ชันสำหรับเปิด UI อาหารแบบสมบูรณ์ (เรียกใช้ได้จากทุก Script)
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

    // 🟢 ฟังก์ชันรองรับการถูกจับ/ลากมาวางที่โต๊ะโดยตรง
    public void ForceSitAtTable(CustomerTable table, Transform seat)
    {
        if (QueueManager.Instance != null) QueueManager.Instance.RemoveFromQueue(this);

        assignedTable = table;
        seatPoint = seat;

        if (agent != null && agent.isOnNavMesh)
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

        SetupOrderUI();

        if (patienceBarRoot != null) patienceBarRoot.SetActive(true);
        if (patienceBarFill != null) patienceBarFill.fillAmount = 1f;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySitDown();

        StopAllCoroutines();
        StartCoroutine(SitRoutine());
        StartCoroutine(AbsoluteTimeoutRoutine());
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
        SetAnimState(AnimState.Idle);
        if (idleBeforeSitDuration > 0f)
            yield return new WaitForSeconds(idleBeforeSitDuration);

        if (currentState != NPCState.Sitting) yield break;

        isSittingAngryAnim = false;
        SetAnimState(AnimState.Sit);

        // เรียกเปิด UI อาหาร
        SetupOrderUI();

        if (patienceBarRoot != null) patienceBarRoot.SetActive(true);
        if (patienceBarFill != null) patienceBarFill.fillAmount = 1f;

        // ── สลับภาพ UI Image ทั้ง 2 อันตามระดับ Relationship (100%) ──
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

        StartCoroutine(SitRoutine());
        StartCoroutine(AbsoluteTimeoutRoutine());
    }

    public void FinishServingAndProceed(bool willInteract)
    {
        isInQTE = false;
        isSittingAngryAnim = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);
        if (patienceBarRoot != null) patienceBarRoot.SetActive(false);

        if (willInteract)
            GoToInteractionZone(ArriveAtInteractionZone);
        else
            GoExit();
    }

    void GoToInteractionZone(System.Action onArrived)
    {
        if (agent == null)
        {
            onArrived?.Invoke();
            return;
        }

        if (InteractionZoneManager.Instance == null)
        {
            Debug.LogWarning("[NPCController] ไม่พบ InteractionZoneManager.Instance — ข้ามการเดินไปโซน");
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
        agent.isStopped = true;
        SetAnimState(AnimState.Sit);

        var cb = onArrivedAtInteractionZone;
        onArrivedAtInteractionZone = null;
        cb?.Invoke();

        interactionTimeoutCoroutine = StartCoroutine(WaitForPlayerRoutine());
    }

    IEnumerator WaitForPlayerRoutine()
    {
        float elapsed = 0f;
        while (elapsed < waitForPlayerTimeout)
        {
            if (currentState != NPCState.AtInteractionZone) yield break;
            if (!GameIsPaused) elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState == NPCState.AtInteractionZone)
            FinishInteractionAtZone();
    }

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
            if (currentState != NPCState.AtInteractionZone) yield break;
            if (!GameIsPaused) elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState == NPCState.AtInteractionZone)
            FinishInteractionAtZone();
    }

    public void OpenQTECanvas()
    {
        if (currentState != NPCState.AtInteractionZone) return;

        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }

        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(true);

        QTEZoomController.Instance?.ZoomInToQTE(transform);
    }

    public void PlayPerformAndExit(int performIndex = 0, float customDuration = -1f)
    {
        StartCoroutine(PerformAnimationRoutine(performIndex, customDuration));
    }

    private IEnumerator PerformAnimationRoutine(int performIndex, float durationOverride)
    {
        if (isPerforming) yield break;
        isPerforming = true;

        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }

        currentState = NPCState.Performing;

        if (agent != null && agent.isOnNavMesh)
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
            1 => AnimState.Perform2,
            2 => AnimState.Perform3,
            _ => AnimState.Perform1
        };

        SetAnimState(targetState, true);

        float clipDuration = durationOverride > 0f ? durationOverride : fallbackPerformDuration;

        if (animator != null)
        {
            string targetStateName = targetState.ToString();

            float safetyWait = 0f;
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName(targetStateName) && safetyWait < 0.5f)
            {
                safetyWait += Time.deltaTime;
                yield return null;
            }

            if (durationOverride <= 0f && animator.GetCurrentAnimatorStateInfo(0).IsName(targetStateName))
            {
                float detectedLen = animator.GetCurrentAnimatorStateInfo(0).length;
                if (detectedLen > 0.1f) clipDuration = detectedLen;
            }
        }

        float timer = 0f;
        while (timer < clipDuration)
        {
            if (!GameIsPaused)
            {
                timer += Time.deltaTime;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
            }
            yield return null;
        }

        isPerforming = false;
        currentAnim = (AnimState)(-1);

        FinishInteractionAtZone();
    }

    public void FinishInteractionAtZone()
    {
        if (isPerforming) return;

        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }

        CatSystemManager.Instance?.HideInteractionChoice();
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        QTEZoomController.Instance?.ZoomOutToGameplay();

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

        while (waitTimer < actualWaitTime)
        {
            if (currentState != NPCState.Sitting) yield break;
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
            yield return null;
        }

        if (currentState == NPCState.Sitting && !isInQTE)
            BecomeAngry();
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
        if (isPerforming) return;
        if (exitPoint == null) return;

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

        QTEZoomController.Instance?.ZoomOutToGameplay();

        currentState = NPCState.Leaving;
        isSittingAngryAnim = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPoint.position);
        }

        Vector3 dir = (exitPoint.position - transform.position).normalized;
        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x) && dir.y > 0.1f)
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
}