using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    private NavMeshAgent agent;
    public DamageableObject targetObject;
    public Transform exitPoint;
    Seat targetSeat;

    [Header("Patience")]
    public float maxWaitTime = 20f;
    private float waitTimer = 0f;
    private bool isAngry = false;

    public enum NPCState { InQueue, GoingToSeat, Sitting, GoingToDamage, Leaving }
    public NPCState currentState = NPCState.InQueue;

    [Header("Order System")]
    public GameObject orderCanvas; // Canvas (World Space) เหนือหัวแมว
    public SpriteRenderer orderIcon; // แสดงรูปอาหารที่สุ่มได้
    public RecipeSO requestedRecipe;
    public List<RecipeSO> availableRecipes; // ลาก Recipe.asset, Recipe1.asset, Recipe2.asset มาใส่ที่นี่

    [Header("Recipe Data")]
    public List<RecipeSO> allRecipes;

    public void SetQueueTarget(Transform target)
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        agent.isStopped = false;
        agent.SetDestination(target.position);
    }
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    void Update()
    {
        if (TimeManager.Instance.isPaused)
        {
            if (agent != null) agent.isStopped = true;
            return;
        }
        else
        {
            if (agent != null) agent.isStopped = false;
        }

        if (agent == null || !agent.isOnNavMesh) return;
        if (currentState == NPCState.Leaving) return;

        // เช็คว่าเดินถึงที่นั่งหรือยัง
        if (currentState == NPCState.GoingToSeat && targetSeat != null)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                ArriveAtSeat();
            }
        }
    }
    public void LeaveSeat()
    {
        if (targetSeat != null)
        {
            targetSeat.Leave();
            targetSeat = null;
        }
        GoExit();
    }
    void ArriveAtSeat()
    {
        if (currentState == NPCState.Sitting) return;

        currentState = NPCState.Sitting;
        agent.isStopped = true;

        // 🔥 สุ่มออเดอร์ทันทีที่นั่งลง
        GenerateOrder(allRecipes);

        StartCoroutine(SitRoutine());
    }

    public void GenerateOrder(List<RecipeSO> availableRecipes)
    {
        if (availableRecipes == null || availableRecipes.Count == 0) return;

        int randomIndex = Random.Range(0, availableRecipes.Count);
        requestedRecipe = availableRecipes[randomIndex];

        // ตรวจสอบว่ามีข้อมูล Recipe จริง
        if (requestedRecipe != null)
        {
            // แสดง UI บนหัวแมว
            if (orderIcon != null) orderIcon.sprite = requestedRecipe.finalDishSprite;
            if (orderCanvas != null) orderCanvas.SetActive(true);

            Debug.Log("แมวสุ่มอยากกิน: " + requestedRecipe.recipeName);
        }
    }

    IEnumerator SitRoutine()
    {
        waitTimer = 0f;
        while (waitTimer < maxWaitTime)
        {
            if (!TimeManager.Instance.isPaused) waitTimer += Time.deltaTime;
            yield return null;
        }
        BecomeAngry();
    }

    public void GoToTableDirectly()
    {
        // 🚩 ป้องกันแมวเดินวน: ถ้าไม่ได้อยู่ในคิว (เช่น กำลังเดินหรือนั่งแล้ว) ให้หยุดทำงานทันที
        if (currentState != NPCState.InQueue) return;

        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        foreach (var table in allTables)
        {
            if (!table.isOccupied)
            {
                // ล็อคโต๊ะตัวนี้ทันที
                table.isOccupied = true;
                table.sittingNPC = this;

                currentState = NPCState.GoingToSeat;
                QueueManager.Instance.RemoveFromQueue(this);

                agent.isStopped = false;
                agent.SetDestination(table.seatPoint.position);

                // สุ่มออเดอร์
                GenerateOrder(allRecipes);
                if (requestedRecipe != null)
                {
                    table.wantedItem = requestedRecipe.recipeName;
                    Debug.Log($"แมวจองโต๊ะ {table.name} และอยากกิน {table.wantedItem}");
                }
                return; // เจอโต๊ะแล้วออกลูปทันที
            }
        }
    }

    void BecomeAngry()
    {
        if (isAngry) return;
        isAngry = true;
        if (orderCanvas != null) orderCanvas.SetActive(false);
        if (targetSeat != null) { targetSeat.Leave(); targetSeat = null; }
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
        while (true)
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.2f) break;
            yield return null;
        }
        Destroy(gameObject);
    }
}