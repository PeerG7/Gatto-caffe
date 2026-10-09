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

    [Header("Animation Settings")]
    public Animator animator;
    public SpriteRenderer bodySpriteRenderer;
    public float idleBeforeSitDuration = 0.4f;
    [Range(0f, 1f)]
    public float angryPatienceRatioThreshold = 0.25f;
    public float moveAnimThreshold = 0.05f;

    [Header("Perform Animation Settings")]
    public float fallbackPerformDuration = 2.26f;

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

    // --- Animator Controller Mapping (Indices 0 to 8) ---
    public enum AnimState
    {
        Idle = 0,
        WalkSide = 1,          // Walk
        WalkUp = 2,            // WalkUp
        Sit = 3,               // Siting
        SitAngry = 4,          // SitingAngry
        PettingAnimation = 5,  // Petting animation
        CatToyMouse = 6,       // Cat_Toy_Mouse (Wand)
        RandomSitPhase = 8     // Cat_Random_Sitting_Phase
    }

    private AnimState currentAnim = AnimState.Idle;
    private static readonly int AnimStateHash = Animator.StringToHash("AnimState");
    private bool isSittingAngryAnim = false;
    private bool isPlayingSittingFlavor = false;

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
        UpdateWaitingFlavorTimers();

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

    void UpdateWaitingFlavorTimers()
    {
        if (!enableRandomSitAnims || isPlayingSittingFlavor || isInQTE) return;

        // 1. In Queue (When stationary)
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
        // 2. At Interaction Zone
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

    void UpdateMovementAnimation()
    {
        if (currentState == NPCState.Sitting || currentState == NPCState.AtInteractionZone || currentState == NPCState.Performing)
            return;

        Vector3 vel = agent.velocity;

        // Prevent movement update from interrupting flavor animation while standing still
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

        // Cancel flavor animation if NPC starts walking
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

    void SetAnimState(AnimState state, bool force = false)
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
                AnimState.CatToyMouse => "Cat_Toy_Mouse",
                AnimState.RandomSitPhase => "Cat_Random_Sitting_Phase",
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

        StartCoroutine(SitRoutine());
        StartCoroutine(AbsoluteTimeoutRoutine());
    }

    public void FinishServingAndProceed(bool willInteract)
    {
        isInQTE = false;
        isSittingAngryAnim = false;
        isPlayingSittingFlavor = false;
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
            1 => AnimState.CatToyMouse,      // Wand / Toy Mouse Animation (Index 6)
            2 => AnimState.RandomSitPhase,   // Index 8
            _ => AnimState.PettingAnimation  // Index 5
        };

        string targetStateName = targetState switch
        {
            AnimState.CatToyMouse => "Cat_Toy_Mouse",
            AnimState.RandomSitPhase => "Cat_Random_Sitting_Phase",
            _ => "Petting animation"
        };

        SetAnimState(targetState, true);

        // --- CAMERA ZOOM-OUT FIX ---
        // Wait until Animator has fully transitioned into target performance animation before checking length
        float transitionWait = 0.5f;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName(targetStateName) && transitionWait > 0f)
        {
            transitionWait -= Time.deltaTime;
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

        // Zooms camera out ONLY AFTER animation finishes complete duration
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
        isPlayingSittingFlavor = false;
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

                // Trigger Random Sit Flavor Animation (AnimState = 8)
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

                // Regular Patience Animations
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

        if (currentState == NPCState.Sitting && !isInQTE)
            BecomeAngry();
    }

    IEnumerator PlayRandomSitFlavorRoutine(AnimState returnState)
    {
        isPlayingSittingFlavor = true;

        SetAnimState(AnimState.RandomSitPhase, true);

        // Wait for state transition
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
            if (currentState == NPCState.InQueue && agent != null && agent.velocity.sqrMagnitude > moveAnimThreshold * moveAnimThreshold)
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
        isPlayingSittingFlavor = false;
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