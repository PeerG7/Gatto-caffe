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
            if (queue[i] != null)
            {
                // บรรทัดนี้จะทำงานได้เมื่อเพิ่มฟังก์ชันใน NPCController แล้ว
                queue[i].SetQueueTarget(queuePoints[i]);
            }
        }
    }
    public bool IsStoreEmpty()
    {
        // เช็คว่าไม่มี NPC ใน Queue และไม่มี NPC ที่กำลังนั่งอยู่ที่โต๊ะ (เช็คจาก CustomerTable)
        return queue.Count == 0 && FindObjectsOfType<NPCController>().Length == 0;
    }

    public NPCController GetFrontNPC()
    {
        return queue.Count > 0 ? queue[0] : null;
    }
}