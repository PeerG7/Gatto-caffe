using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCController : MonoBehaviour
{
    public enum NPCState
    {
        InQueue,
        GoingToSeat,
        Sitting,
        Leaving
    }

    public NPCState currentState = NPCState.InQueue;

    private NavMeshAgent agent;
    public Transform exitPoint;

    private Seat targetSeat;
    private bool isSitting = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("❌ NavMeshAgent NOT FOUND on " + gameObject.name);
            enabled = false;
            return;
        }

        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        if (agent == null) return;

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

        agent.isStopped = true;

        StartCoroutine(SitRoutine());
    }

    IEnumerator SitRoutine()
    {
        yield return new WaitForSeconds(10f);
        LeaveSeat();
    }

    public void GoToSeat()
    {
        // ออกจาก Queue ทันที
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

    public void LeaveSeat()
    {
        if (targetSeat != null)
        {
            targetSeat.Leave();
            targetSeat = null;
        }

        GoExit();
    }

    public void SetQueueTarget(Transform target)
    {
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    public void GoExit()
    {
        currentState = NPCState.Leaving;

        agent.isStopped = false;
        agent.SetDestination(exitPoint.position);

        StartCoroutine(DestroyWhenArrive());
    }

    IEnumerator DestroyWhenArrive()
    {
        while (agent.pathPending || agent.remainingDistance > 0.2f)
        {
            yield return null;
        }

        Destroy(gameObject);
    }
}