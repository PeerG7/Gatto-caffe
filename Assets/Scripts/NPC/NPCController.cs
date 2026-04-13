using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

public class NPCController : MonoBehaviour
{
    private NavMeshAgent agent;

    public DamageableObject targetObject;
    public Transform exitPoint;
    Seat targetSeat;

    [Header("Patience")]
    public float maxWaitTime = 10f;
    private float waitTimer = 0f;
    private bool isAngry = false;

    public enum NPCState
    {
        InQueue,
        GoingToSeat,
        Sitting,
        GoingToDamage,
        Leaving
    }

    public NPCState currentState = NPCState.InQueue;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("No NavMeshAgent");
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        if (TimeManager.Instance.isPaused)
        {
            if (agent != null)
                agent.isStopped = true;

            return;
        }
        else
        {
            if (agent != null)
                agent.isStopped = false;
        }
        if (agent == null || !agent.isOnNavMesh) return;

        if (currentState == NPCState.GoingToSeat)
        {
            // ถ้าเดินถึงระยะที่กำหนด (ใกล้โต๊ะ)
            if (Vector2.Distance(transform.position, targetSeat.position) < 0.2f)
            {
                currentState = NPCState.Sitting;
                CustomerTable table = targetSeat.GetComponentInParent<CustomerTable>();
                if (table != null)
                {
                    table.OnCustomerSeated(this); // ตรงนี้ถูกต้องแล้ว คือการส่งตัวเองไปให้โต๊ะ
                }
            }
        }

        // ❌ ไม่ทำอะไรตอนออก
        if (currentState == NPCState.Leaving) return;

        // 🧱 ไปทำลาย
        if (currentState == NPCState.GoingToDamage && targetObject != null)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                StartCoroutine(DamageRoutine());
            }
            return;
        }

        // 🪑 ไปนั่ง
        if (currentState == NPCState.GoingToSeat && targetSeat != null)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                Sit();
            }
        }
    }


    // =====================
    // 🪑 นั่ง
    // =====================
    void Sit()
    {
        if (currentState == NPCState.Sitting) return;

        currentState = NPCState.Sitting;
        agent.isStopped = true;

        StartCoroutine(SitRoutine());
    }

    IEnumerator SitRoutine()
    {
        waitTimer = 0f;

        while (waitTimer < maxWaitTime)
        {
            if (!TimeManager.Instance.isPaused)
            {
                waitTimer += Time.deltaTime;
            }

            yield return null;
        }

        BecomeAngry();
    }

    // =====================
    // 😡 โมโห
    // =====================
    void BecomeAngry()
    {
        if (isAngry) return;

        isAngry = true;

        Debug.Log("NPC Angry!");

        // 🔓 ลุกจากที่นั่ง
        if (targetSeat != null)
        {
            targetSeat.Leave();
            targetSeat = null;
        }

        ChooseRandomTarget();
    }

    // =====================
    // 🧱 เลือกของไปพัง
    // =====================
    void ChooseRandomTarget()
    {
        if (DamageableObject.allObjects.Count == 0)
        {
            GoExit();
            return;
        }

        var available = DamageableObject.allObjects
            .Where(obj => !obj.IsBroken())
            .ToList();

        if (available.Count == 0)
        {
            GoExit();
            return;
        }

        int index = Random.Range(0, available.Count);
        targetObject = available[index];

        currentState = NPCState.GoingToDamage;

        agent.isStopped = false;
        agent.SetDestination(targetObject.transform.position);
    }

    IEnumerator DamageRoutine()
    {
        agent.isStopped = true;

        float timer = 0f;

        while (timer < 1f)
        {
            if (!TimeManager.Instance.isPaused)
            {
                timer += Time.deltaTime;
            }

            yield return null;
        }

        if (targetObject != null)
        {
            targetObject.StartDamage();
        }

        GoExit();
    }

    // =====================
    // 🚶 ไปนั่ง (เรียกจาก QueueManager)
    // =====================
    public void GoToSeat()
    {
        QueueManager.Instance.RemoveFromQueue(this);

        targetSeat = SeatManager.Instance.GetAvailableSeat();

        if (targetSeat != null)
        {
            currentState = NPCState.GoingToSeat;

            targetSeat.Occupy(this);
            agent.isStopped = false;
            agent.SetDestination(targetSeat.transform.position);
        }
        else
        {
            GoExit();
        }
    }

    public void SetQueueTarget(Transform target)
    {
        agent.isStopped = false;
        agent.SetDestination(target.position);
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

    // =====================
    // 🚪 ออก
    // =====================
    public void GoExit()
    {
        if (currentState == NPCState.Leaving) return;

        currentState = NPCState.Leaving;

        agent.isStopped = false;
        agent.SetDestination(exitPoint.position);

        StartCoroutine(DestroyWhenArrive());
    }

    IEnumerator DestroyWhenArrive()
    {
        while (true)
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.2f)
                break;

            yield return null;
        }

        Destroy(gameObject);
    }
}