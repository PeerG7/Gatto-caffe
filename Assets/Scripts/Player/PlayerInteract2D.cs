using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (PlayerController2D.IsLocked) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

        // 1. ปลดล็อกเฟอร์นิเจอร์
        foreach (var hit in hits)
        {
            FurnitureObject furn = hit.GetComponent<FurnitureObject>();
            if (furn != null && !furn.isUnlocked) { furn.AttemptUnlock(); return; }
        }

        // 2. เสิร์ฟอาหารที่โต๊ะ
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.sittingNPC != null &&
                table.sittingNPC.currentState == NPCController.NPCState.Sitting)
            { table.TryServeFood(); return; }
        }

        // 3. เปิด Relationship Book
        foreach (var hit in hits)
        {
            BookStation book = hit.GetComponent<BookStation>();
            if (book != null) { book.OpenBook(); return; }
        }

        // 4. เปิด cooking / drink station
        foreach (var hit in hits)
        {
            StationInteract station = hit.GetComponent<StationInteract>();
            if (station != null)
            {
                PlayerController2D.IsLocked = true;
                station.OpenCanvas();
                return;
            }
        }

        // 5. Invite NPC
        NPCInteract closestNPC = GetClosestNPC(hits);
        if (closestNPC != null && !closestNPC.CanInteract())
            closestNPC.GoToTableDirectly();
    }

    NPCInteract GetClosestNPC(Collider2D[] hits)
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