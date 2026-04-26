using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

            // 1. เช็คปลดล็อกเฟอร์นิเจอร์ก่อน
            foreach (var hit in hits)
            {
                FurnitureObject furn = hit.GetComponent<FurnitureObject>();
                if (furn != null && !furn.isUnlocked)
                {
                    furn.AttemptUnlock();
                    return;
                }
            }

            // 2. เช็คโต๊ะเพื่อเสิร์ฟอาหาร
            foreach (var hit in hits)
            {
                CustomerTable table = hit.GetComponent<CustomerTable>();
                if (table != null && table.sittingNPC != null && table.sittingNPC.currentState == NPCController.NPCState.Sitting)
                {
                    table.TryServeFood();
                    return;
                }
            }

            // 3. เช็คสถานีทำอาหาร
            foreach (var hit in hits)
            {
                StationInteract station = hit.GetComponent<StationInteract>();
                if (station != null) { station.OpenCanvas(); return; }
            }

            // 4. เช็ค NPC เพื่อ Invite
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