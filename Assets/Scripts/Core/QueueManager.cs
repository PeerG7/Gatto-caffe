using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    public static QueueManager Instance;
    public List<Transform> queuePoints = new List<Transform>();
    private List<NPCController> queue = new List<NPCController>();

    void Awake() { Instance = this; }

    public void AddToQueue(NPCController npc)
    {
        if (queue.Count >= queuePoints.Count)
        {
            npc.GoExit();
            return;
        }
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
            // เรียกฟังก์ชันใน NPCController
            if (queue[i] != null)
            {
                queue[i].SetQueueTarget(queuePoints[i]);
            }
        }
    }

    public NPCController GetFrontNPC()
    {
        return queue.Count > 0 ? queue[0] : null;
    }
}