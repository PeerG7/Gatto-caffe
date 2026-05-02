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
            Debug.Log("Queue full! NPC going exit");
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

    // ✅ เพิ่มฟังก์ชัน Reset Queue สำหรับวันใหม่
    public void ResetQueue()
    {
        queue.Clear();
    }

    void UpdateQueuePositions()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] != null)
                queue[i].SetQueueTarget(queuePoints[i]);
        }
    }

    public bool IsStoreEmpty()
    {
        return queue.Count == 0 && FindObjectsOfType<NPCController>().Length == 0;
    }

    public NPCController GetFrontNPC()
    {
        return queue.Count > 0 ? queue[0] : null;
    }
}