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

    public void ResetQueue()
    {
        queue.Clear();
    }

    void UpdateQueuePositions()
    {
        int currentQueueIndex = 0;

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i] == null) continue;

            // เช็กว่าเป็นตัวที่ 2 ของกลุ่ม (คู่หู) หรือไม่
            bool isGroupCompanion = (i > 0 && queue[i - 1] != null &&
                                     !string.IsNullOrEmpty(queue[i].groupID) &&
                                     queue[i].groupID == queue[i - 1].groupID);

            if (isGroupCompanion && currentQueueIndex > 0)
            {
                // ถ้าเป็นตัวที่มาคู่กัน ให้ยืนเยื้องไปทางขวาของจุดหลักเล็กน้อย (เช่น 0.8 หน่วย)
                Transform basePoint = queuePoints[currentQueueIndex - 1];
                Vector3 sideBySidePos = basePoint.position + new Vector3(1f, 0f, 0f);
                queue[i].SetQueueTargetCustom(sideBySidePos);
            }
            else
            {
                // เช็กเพิ่มว่าถ้าตัวถัดไป (i+1) เป็นคู่หูของตัวนี้ ให้ตัวนี้ขยับซ้ายนิดนึง (จะได้ไม่ทับกันตรงกลาง)
                bool hasCompanionBehind = (i + 1 < queue.Count && queue[i + 1] != null &&
                                           !string.IsNullOrEmpty(queue[i].groupID) &&
                                           queue[i].groupID == queue[i + 1].groupID);

                if (currentQueueIndex < queuePoints.Count)
                {
                    Transform basePoint = queuePoints[currentQueueIndex];
                    if (hasCompanionBehind)
                    {
                        // ถ้ามีเพื่อนตามหลัง ให้ขยับหัวหน้ากลุ่มมาทางซ้ายนิดหน่อย (-0.4 หน่วย)
                        Vector3 splitLeftPos = basePoint.position + new Vector3(-0.6f, 0f, 0f);
                        queue[i].SetQueueTargetCustom(splitLeftPos);
                    }
                    else
                    {
                        queue[i].SetQueueTarget(queuePoints[currentQueueIndex]);
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
    /// ชวนแมวเข้ากลุ่มพร้อมกัน (ดึงตัวที่ groupID ตรงกันที่ต่อคิวอยู่ข้างหลังให้เข้าร้านพร้อมกัน)
    /// </summary>
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
                npc.GoToTableDirectly();
            }
        }
    }
}