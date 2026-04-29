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

    public enum NPCState { InQueue, GoingToSeat, Sitting, GoingToDamage, Leaving }
    public NPCState currentState = NPCState.InQueue;

    [Header("Order System")]
    public GameObject orderCanvas;
    public SpriteRenderer orderIcon;
    public RecipeSO requestedRecipe;
    public List<RecipeSO> allRecipes;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
        if (orderCanvas != null) orderCanvas.SetActive(false);
    }

    void Update()
    {
        // 🔥 FIX: ป้องกัน NullReference จาก TimeManager
        if (TimeManager.Instance != null && TimeManager.Instance.isPaused)
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
            {
                ArriveAtSeat();
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
        if (requestedRecipe != null && orderIcon != null) orderIcon.sprite = requestedRecipe.finalDishSprite;
    }

    void ArriveAtSeat()
    {
        if (currentState == NPCState.Sitting) return;
        currentState = NPCState.Sitting;
        agent.isStopped = true;
        if (orderCanvas != null) orderCanvas.SetActive(true);
        StartCoroutine(SitRoutine());
    }

    public void LeaveSeat()
    {
        if (orderCanvas != null) orderCanvas.SetActive(false);
        GoExit();
    }

    IEnumerator SitRoutine()
    {
        waitTimer = 0f;
        while (waitTimer < maxWaitTime)
        {
            // 🔥 FIX: เช็ค Null ก่อนเข้าถึงค่า isPaused
            if (TimeManager.Instance != null && !TimeManager.Instance.isPaused) waitTimer += Time.deltaTime;
            yield return null;
        }
        BecomeAngry();
    }

    void BecomeAngry()
    {
        if (isAngry) return;
        isAngry = true;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        ChooseRandomTarget();
    }

    void ChooseRandomTarget()
    {
        var available = DamageableObject.allObjects.Where(obj => !obj.IsBroken()).ToList();
        if (available.Count == 0) { GoExit(); return; }
        targetObject = available[Random.Range(0, available.Count)];
        currentState = NPCState.GoingToDamage;
        agent.isStopped = false;
        agent.SetDestination(targetObject.transform.position);
    }

    public void GoExit()
    {
        currentState = NPCState.Leaving;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        agent.isStopped = false;
        agent.SetDestination(exitPoint.position);
        StartCoroutine(DestroyWhenArrive());
    }

    IEnumerator DestroyWhenArrive()
    {
        yield return new WaitForSeconds(0.5f);
        // รอจนกว่าจะเดินถึงจุด Exit Point
        while (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.5f) break;
            yield return null;
        }
        Destroy(gameObject); // 🔥 สำคัญมาก: ต้องถูกทำลายเพื่อให้ระบบ Summary ทำงานต่อได้
    }
}