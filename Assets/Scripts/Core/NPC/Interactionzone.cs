using UnityEngine;

// =====================================================================
// InteractionZone — จุด (หรือโซน) ที่แมวจะเดินมายืน/นั่งเพื่อทำ Interaction
// แยกออกจากโต๊ะกินข้าว (CustomerTable.seatPoint) โดยเด็ดขาด
//
// Setup:
//   1. สร้าง Empty GameObject วางตำแหน่งในพื้นที่สีน้ำเงินตามภาพ
//      ต้องอยู่ "บน NavMesh" ที่ bake แล้ว (เดินไปถึงได้จริง)
//   2. ใส่ component นี้ (ถ้าไม่ลาก point ไว้ จะใช้ transform ตัวเองอัตโนมัติ)
//   3. ถ้าต้องการหลายจุด (รองรับแมวหลายตัวพร้อมกัน) ให้สร้างหลาย GameObject
//      แบบนี้ แล้วลากทุกตัวไปใส่ list ที่ InteractionZoneManager
// =====================================================================
public class InteractionZone : MonoBehaviour
{
    [Tooltip("จุดยืนจริงของแมว (ต้องอยู่บน NavMesh) — ถ้าไม่ใส่จะใช้ transform ของตัวเองแทน")]
    public Transform point;

    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public NPCController currentNPC;

    void Reset()
    {
        point = transform;
    }

    void Awake()
    {
        if (point == null) point = transform;
    }

    public bool IsAvailable() => !isOccupied;

    public void Occupy(NPCController npc)
    {
        isOccupied = true;
        currentNPC = npc;
    }

    public void Release()
    {
        isOccupied = false;
        currentNPC = null;
    }

    // ช่วยให้เห็นตำแหน่งจุดชัดเจนใน Scene view ตอนแก้ level
    void OnDrawGizmos()
    {
        Vector3 p = point != null ? point.position : transform.position;
        Gizmos.color = isOccupied ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawWireSphere(p, 0.3f);
        Gizmos.DrawLine(p + Vector3.up * 0.3f, p + Vector3.up * 0.6f);
    }
}