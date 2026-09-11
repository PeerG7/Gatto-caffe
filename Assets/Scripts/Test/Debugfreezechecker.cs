using UnityEngine;
using UnityEngine.AI;

// =====================================================================
// DebugFreezeChecker — สคริปต์ชั่วคราวสำหรับ debug บั๊ก "ขยับไม่ได้"
//
// วิธีใช้:
//   1. สร้าง Empty GameObject ชื่ออะไรก็ได้ในซีน
//   2. ใส่สคริปต์นี้เข้าไป
//   3. กด Play แล้วเล่นจนแมวค้าง/Player ค้าง — จะเห็นค่าจริงบนหน้าจอมุมซ้ายบน
//   4. ถ่ายรูปหรืออ่านค่าแล้วส่งกลับมาบอกผมได้เลย
//   5. เสร็จแล้วลบสคริปต์นี้ออก / ลบ GameObject ทิ้ง (ไม่ใช่โค้ดที่ใช้จริงในเกม)
// =====================================================================
public class DebugFreezeChecker : MonoBehaviour
{
    void OnGUI()
    {
        GUIStyle style = new GUIStyle { fontSize = 22, normal = { textColor = Color.yellow } };
        int y = 10;

        GUI.Label(new Rect(10, y, 800, 30),
            $"PlayerController2D.IsLocked = {PlayerController2D.IsLocked}", style);
        y += 30;

        bool paused = DayNightManager.Instance != null && DayNightManager.Instance.isPaused;
        GUI.Label(new Rect(10, y, 800, 30),
            $"DayNightManager.isPaused = {paused}", style);
        y += 30;

        NPCController[] npcs = FindObjectsOfType<NPCController>();
        foreach (var npc in npcs)
        {
            NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
            string agentInfo = "no agent";
            if (agent != null)
            {
                agentInfo = $"onNavMesh={agent.isOnNavMesh} pathPending={agent.pathPending} " +
                            $"pathStatus={agent.pathStatus} remainDist={agent.remainingDistance:F2} " +
                            $"stopped={agent.isStopped} dest={agent.destination}";
            }

            GUI.Label(new Rect(10, y, 1400, 30),
                $"[{npc.gameObject.name}] state={npc.currentState} isInQTE={npc.isInQTE} | {agentInfo}", style);
            y += 26;
        }
    }
}