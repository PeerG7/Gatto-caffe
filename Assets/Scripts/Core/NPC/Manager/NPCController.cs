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
    [Tooltip("Fallback duration if Animator component is missing or clip length cannot be read.")]
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

    // Safety lock during performance
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

        // Check if Animator component is currently stuck playing a performance state
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

            // 1. Try exact enum name
            string enumName = state.ToString();
            int primaryHash = Animator.StringToHash(enumName);

            if (animator.HasState(0, primaryHash))
            {
                animator.Play(primaryHash, 0, 0f);
            }
            else
            {
                // 2. Try alternate common naming formats (e.g. Walk_Side, Perform_1)
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
                    // 3. Fallback for walking state
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

                    if (table.tableItemRenderer != null)
                    {
                        table.tableItemRenderer.sprite = requestedRecipe.finalDishSprite;
                        table.tableItemRenderer.enabled = true;
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

        if (orderCanvas != null) orderCanvas.SetActive(true);

        if (patienceBarRoot != null) patienceBarRoot.SetActive(true);
        if (patienceBarFill != null) patienceBarFill.fillAmount = 1f;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySitDown();

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
        if (interactionTimeoutCoroutine != null)
        {
            StopCoroutine(interactionTimeoutCoroutine);
            interactionTimeoutCoroutine = null;
        }

        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(true);
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

        // Freeze physical position completely
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

            // Wait until Animator enters the perform state
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

        // Wait stationary for exact animation duration (2.26s)
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

        // Release perform lock and reset cached animation state
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

        currentState = NPCState.Leaving;
        isSittingAngryAnim = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPoint.position);
        }

        // Set direction and force transition into Walk state
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