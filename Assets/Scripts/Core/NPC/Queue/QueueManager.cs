using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    public static QueueManager Instance;
    public List<Transform> queuePoints = new List<Transform>();
    private List<NPCController> queue = new List<NPCController>();

    [Header("Arrival Check (ใช้กับ Tutorial Pointer)")]
    [Tooltip("แมวห่างจากจุดคิวไม่เกินเท่านี้ ถือว่า 'ยืนรออยู่ที่คิวแล้ว'")]
    public float arriveDistance = 0.35f;

    // 🟢 เก็บตำแหน่งที่แมวแต่ละตัวต้องไปยืน เพื่อเช็กว่าไปถึงหรือยัง
    private Dictionary<NPCController, Vector3> queueTargets = new Dictionary<NPCController, Vector3>();

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
            queueTargets.Remove(npc);
            UpdateQueuePositions();
        }
    }

    public void ResetQueue()
    {
        queue.Clear();
        queueTargets.Clear();
    }

    void UpdateQueuePositions()
    {
        int currentQueueIndex = 0;

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] == null) continue;

            bool isGroupCompanion = (i > 0 && queue[i - 1] != null &&
                                     !string.IsNullOrEmpty(queue[i].groupID) &&
                                     queue[i].groupID == queue[i - 1].groupID);

            if (isGroupCompanion && currentQueueIndex > 0)
            {
                Transform basePoint = queuePoints[currentQueueIndex - 1];
                Vector3 sideBySidePos = basePoint.position + new Vector3(1f, 0f, 0f);
                queueTargets[queue[i]] = sideBySidePos;
                queue[i].SetQueueTargetCustom(sideBySidePos);
            }
            else
            {
                bool hasCompanionBehind = (i + 1 < queue.Count && queue[i + 1] != null &&
                                           !string.IsNullOrEmpty(queue[i].groupID) &&
                                           queue[i].groupID == queue[i + 1].groupID);

                if (currentQueueIndex < queuePoints.Count)
                {
                    Transform basePoint = queuePoints[currentQueueIndex];
                    if (hasCompanionBehind)
                    {
                        Vector3 splitLeftPos = basePoint.position + new Vector3(-0.6f, 0f, 0f);
                        queueTargets[queue[i]] = splitLeftPos;
                        queue[i].SetQueueTargetCustom(splitLeftPos);
                    }
                    else
                    {
                        queueTargets[queue[i]] = basePoint.position;
                        queue[i].SetQueueTarget(basePoint);
                    }
                    currentQueueIndex++;
                }
            }
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

    /// <summary>
    /// 🟢 true = มีแมวเดินไปถึงจุดคิวและยืนรออยู่แล้วอย่างน้อย 1 ตัว
    /// (TutorialPointerManager ใช้เช็กก่อนโชว์ Icon ที่หน้าร้าน)
    /// </summary>
    public bool HasNPCWaitingInQueue()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            NPCController npc = queue[i];
            if (npc == null) continue;
            if (!queueTargets.TryGetValue(npc, out Vector3 target)) continue;

            // เกม 2D → เทียบแค่แกน X,Y
            Vector2 a = npc.transform.position;
            Vector2 b = target;
            if (Vector2.Distance(a, b) <= arriveDistance) return true;
        }
        return false;
    }

    public void InviteGroup(NPCController leaderNPC)
    {
        if (leaderNPC == null) return;
        string groupID = leaderNPC.groupID;

        List<NPCController> groupToInvite = new List<NPCController>();
        groupToInvite.Add(leaderNPC);

        if (!string.IsNullOrEmpty(groupID))
        {
            for (int i = 1; i < queue.Count; i++)
            {
                if (queue[i] != null && queue[i].groupID == groupID)
                {
                    groupToInvite.Add(queue[i]);
                }
            }
        }

        foreach (var npc in groupToInvite)
        {
            if (npc != null)
            {
                NPCInteract interact = npc.GetComponent<NPCInteract>();
                if (interact != null) interact.isInStore = true;

                // 🌟 เรียกสั่งงานให้เดินเข้าโต๊ะโดยตรง
                npc.GoToTableDirectly();
            }
        }
    }
}