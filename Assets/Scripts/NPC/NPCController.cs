using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCController : MonoBehaviour
{
    private NavMeshAgent agent;

    public Transform exitPoint;
    Seat targetSeat;

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
        if (agent == null || !agent.isOnNavMesh)
            return;

        if (currentState == NPCState.Sitting || currentState == NPCState.Leaving)
            return;

        if (targetSeat != null && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Sit();
            }
        }
    }
    void Sit()
    {
        if (currentState == NPCState.Sitting || currentState == NPCState.Leaving)
            return;

        if (currentState == NPCState.Sitting) return;

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

        //QueueManager.Instance.RemoveFromQueue(this);

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
        if (currentState == NPCState.Leaving) return; // 👈 กันซ้ำ

        currentState = NPCState.Leaving;

        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning("Agent not ready");
            return;
        }

        agent.isStopped = false; // 👈 กันติดค้าง

        agent.SetDestination(exitPoint.position);
        StartCoroutine(DestroyWhenArrive());
    }

    IEnumerator DestroyWhenArrive()
    {
        while (true)
        {
            if (agent == null || !agent.isOnNavMesh)
            {
                yield break; // ❌ หยุดเลย กัน error
            }

            if (!agent.pathPending && agent.remainingDistance <= 0.2f)
            {
                break;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}