using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCController : MonoBehaviour
{
    private NavMeshAgent agent;

    public Transform exitPoint;
    Seat targetSeat;
    bool isSitting = false;

    public enum NPCState
    {
        InQueue,
        GoingToSeat,
        Sitting,
        Leaving
    }

    public NPCState currentState = NPCState.InQueue;
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("❌ NavMeshAgent NOT FOUND on " + gameObject.name);
            enabled = false; // ปิด script กัน error spam
            return;
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        if (targetSeat != null && !agent.pathPending && !isSitting)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Sit();
            }
        }
    }
    void Sit()
    {
        isSitting = true;
        currentState = NPCState.Sitting;

        Debug.Log("NPC Sitting");

        agent.isStopped = true;

        StartCoroutine(SitRoutine());

        // TODO: เล่น animation นั่ง
    }
    IEnumerator SitRoutine()
    {
        yield return new WaitForSeconds(10f); // นั่ง 10 วิ
        LeaveSeat();
    }
    public void LeaveSeat()
    {
        if (targetSeat != null)
        {
            targetSeat.Leave();
            targetSeat = null;
        }

        QueueManager.Instance.RemoveFromQueue(this);

        GoExit();
    }


    public void MoveTo(Transform target)
    {
        if (agent == null) return;

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Agent not on NavMesh!");
            return;
        }

        agent.SetDestination(target.position);
    }
    public void GoToSeat()
    {
        // 👇 สำคัญมาก
        QueueManager.Instance.RemoveFromQueue(this);

        targetSeat = SeatManager.Instance.GetAvailableSeat();

        if (targetSeat != null)
        {
            currentState = NPCState.GoingToSeat;

            targetSeat.Occupy(this);
            MoveTo(targetSeat.transform);
        }
        else
        {
            GoExit();
        }
    }
    public void SetQueueTarget(Transform target)
    {
        agent.isStopped = false; 
        MoveTo(target);
    }

    public void GoExit()
    {
        currentState = NPCState.Leaving;

        agent.isStopped = false;
        agent.SetDestination(exitPoint.position);

        StartCoroutine(DestroyWhenArrive());
    }

    System.Collections.IEnumerator DestroyWhenArrive()
    {
        while (agent.pathPending || agent.remainingDistance > 0.2f)
        {
            yield return null;
        }

        Destroy(gameObject);
    }
}