using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    public static QueueManager Instance;

    public List<NPCController> queue = new List<NPCController>();
    public Transform[] queuePoints;

    void Awake()
    {
        Instance = this;
    }

    public void AddToQueue(NPCController npc)
    {
        queue.Add(npc);
        UpdateQueuePositions();
    }

    public void RemoveFromQueue(NPCController npc)
    {
        if (queue.Contains(npc))
        {
            queue.Remove(npc);
            UpdateQueuePositions();
        }
    }

    void UpdateQueuePositions()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            queue[i].SetQueueTarget(queuePoints[i]);
        }
    }

    public NPCController GetFrontNPC()
    {
        if (queue.Count > 0)
            return queue[0];

        return null;
    }
}