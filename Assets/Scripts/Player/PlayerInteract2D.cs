using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 1.5f; // อ้างอิงจากระยะเดิมของคุณ
    //public static CatController currentCat; // ตัวที่เลือกอยู่

    void Update()
    {
        if (TimeManager.Instance.isPaused) return; // หยุดทุก input

        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range); //

            // 🔧 1. Repair ก่อน (จากโค้ดเดิม)
            foreach (var hit in hits)
            {
                DamageableObject obj = hit.GetComponent<DamageableObject>();

                if (obj != null && obj.IsBroken())
                {
                    Debug.Log("🔧 Repair");
                    obj.Repair();
                    return;
                }
            }

            // 🤖 2. หา NPC ที่ใกล้ที่สุด (จากโค้ดเดิม)
            NPCInteract closestNPC = null;
            float minDist = Mathf.Infinity;

            foreach (var hit in hits)
            {
                NPCInteract npc = hit.GetComponent<NPCInteract>();

                if (npc == null) continue;

                float dist = Vector2.Distance(transform.position, npc.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    closestNPC = npc;
                }
            }

            if (closestNPC != null)
            {
                if (closestNPC.CanInteract())
                {
                    Debug.Log("➡ Open QTE");
                    closestNPC.RelationShip();
                    return; // เพิ่ม return เพื่อไม่ให้ทำงานทับซ้อนกัน
                }
                else
                {
                    Debug.Log("➡ Invite NPC");
                    closestNPC.Interact();
                    return; // เพิ่ม return
                }
            }

            // 🍳 3. ค้นหา Station ทำอาหาร/เครื่องดื่ม (ส่วนที่เพิ่มใหม่)
            foreach (var hit in hits)
            {
                StationInteract station = hit.GetComponent<StationInteract>();

                if (station != null)
                {
                    Debug.Log("➡ Open Cooking/Beverage Canvas");
                    station.OpenCanvas();
                    return; // เมื่อเปิดสเตชั่นแล้วให้จบการทำงานทันที
                }
            }

            // ถ้าไม่มีอะไรเลย
            Debug.Log("❌ Nothing to interact"); //
        }
    }

    // Debug ระยะ (จากโค้ดเดิม)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}