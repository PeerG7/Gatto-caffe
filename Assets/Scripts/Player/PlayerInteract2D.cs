using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    void Update()
{
    if (Input.GetKeyDown(KeyCode.E))
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        // 1. เช็คโต๊ะก่อน (เพื่อเสิร์ฟ)
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.sittingNPC != null)
            {
                table.TryServeFood(); 
                return; // ถ้าเจอโต๊ะที่มีแมว ให้เสิร์ฟแล้วจบงานทันที ไม่ต้องเช็ค NPC ต่อ
            }
        }

          foreach (var hit in hits)
           {
                StationInteract station = hit.GetComponent<StationInteract>();
                if (station != null) { station.OpenCanvas(); return; }
           }

            // 2. เช็ค NPC (เพื่อ Invite) - จะทำงานก็ต่อเมื่อไม่ได้ยืนหน้าโต๊ะที่มีแมว
            NPCInteract closestNPC = GetClosestNPC(hits);
        if (closestNPC != null && !closestNPC.CanInteract())
        {
            closestNPC.GoToTableDirectly();
            return;
        }
    }
}

    private NPCInteract GetClosestNPC(Collider2D[] hits)
    {
        NPCInteract closest = null;
        float minDist = Mathf.Infinity;
        foreach (var hit in hits)
        {
            NPCInteract npc = hit.GetComponent<NPCInteract>();
            if (npc != null)
            {
                float dist = Vector2.Distance(transform.position, npc.transform.position);
                if (dist < minDist) { minDist = dist; closest = npc; }
            }
        }
        return closest;
    }
}