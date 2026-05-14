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

    [Header("Safety Timeout (ป้องกัน NPC ค้างโต๊ะ)")]
    public float absoluteMaxSitTime = 120f;

    private bool hasArrivedAtDamageTarget = false;

    [HideInInspector] public bool isInQTE = false;

    public enum NPCState { InQueue, GoingToSeat, Sitting, GoingToDamage, Leaving }
    public NPCState currentState = NPCState.InQueue;

    [Header("Order System")]
    public GameObject orderCanvas;
    public SpriteRenderer orderIcon;
    public RecipeSO requestedRecipe;
    public List<RecipeSO> allRecipes;

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
        if (currentState == NPCState.Leaving) return;

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
                if (requestedRecipe != null) table.wantedItem = requestedRecipe.recipeName;
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
        if (orderCanvas != null) orderCanvas.SetActive(true);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySitDown();

        StartCoroutine(SitRoutine());
        StartCoroutine(AbsoluteTimeoutRoutine());
    }

    public void LeaveSeat()
    {
        isInQTE = false;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (qteCanvasInPrefab != null) qteCanvasInPrefab.SetActive(false);

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
            if (!shouldPause) waitTimer += Time.deltaTime;
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
        currentState = NPCState.Leaving;
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