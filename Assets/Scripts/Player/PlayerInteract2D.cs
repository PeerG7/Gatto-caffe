using UnityEngine;
using System.Collections.Generic;

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    [Header("Interact Keys — เพิ่ม/ลดปุ่มได้เลยใน Inspector")]
    public KeyCode[] interactKeys = new KeyCode[] { KeyCode.E };

    private List<TraySlot> lastNearbyTrays = new List<TraySlot>();

    void Update()
    {
        if (PlayerController2D.IsLocked) return;

        UpdateTrayProximityFeedback();

        bool pressed = false;
        foreach (KeyCode key in interactKeys)
            if (Input.GetKeyDown(key)) { pressed = true; break; }

        if (!pressed) return;
        TriggerInteract();
    }

    public void TriggerInteract()
    {
        if (PlayerController2D.IsLocked) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = true;
        filter.layerMask = ~LayerMask.GetMask("Player");

        List<Collider2D> resultList = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, range, filter, resultList);
        Collider2D[] hits = resultList.ToArray();

        // 1. ปลดล็อกเฟอร์นิเจอร์
        foreach (var hit in hits)
        {
            FurnitureObject furn = hit.GetComponent<FurnitureObject>();
            if (furn != null && !furn.isUnlocked) { furn.AttemptUnlock(); return; }
        }

        // 2. ซ่อม Furniture ที่พัง
        foreach (var hit in hits)
        {
            DamageableObject dmg = hit.GetComponent<DamageableObject>();
            if (dmg != null && dmg.CanRepair()) { dmg.StartRepair(); return; }
        }

        // 3. กด interact แทนการคลิก Heart Icon
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.heartIcon != null && table.heartIcon.activeSelf)
            { table.OnHeartClicked(); return; }
        }

        // 4. เสิร์ฟอาหารที่โต๊ะ
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.sittingNPC != null &&
                table.sittingNPC.currentState == NPCController.NPCState.Sitting)
            { table.TryServeFood(); return; }
        }

        // 5. เปิด Relationship Book
        foreach (var hit in hits)
        {
            BookStation book = hit.GetComponent<BookStation>();
            if (book != null) { book.OpenBook(); return; }
        }

        // 6. เปิด cooking / drink station
        foreach (var hit in hits)
        {
            StationInteract station = hit.GetComponent<StationInteract>();
            if (station != null)
            {
                bool hasActiveTable = false;
                foreach (var h in hits)
                {
                    CustomerTable t = h.GetComponent<CustomerTable>();
                    if (t != null && t.sittingNPC != null &&
                        t.sittingNPC.currentState == NPCController.NPCState.Sitting)
                    { hasActiveTable = true; break; }
                }

                if (!hasActiveTable)
                {
                    PlayerController2D.IsLocked = true;
                    station.OpenCanvas();
                    return;
                }
            }
        }

        // 7. ✅ ทิ้งอาหารที่ Garbage Zone
        foreach (var hit in hits)
        {
            GarbageZone garbage = hit.GetComponent<GarbageZone>();
            if (garbage != null)
            {
                PlayerInventory player = GetComponent<PlayerInventory>();
                garbage.TryDiscard(player);
                return;
            }
        }

        // 8. หยิบ/วาง/swap อาหารจาก TraySlot
        foreach (var hit in hits)
        {
            TraySlot tray = hit.GetComponent<TraySlot>();
            if (tray != null)
            {
                PlayerInventory player = GetComponent<PlayerInventory>();
                tray.TryInteract(player);
                return;
            }
        }

        // 9. NPC interact
        NPCInteract closestNPC = GetClosestNPC(hits);
        if (closestNPC != null)
        {
            if (closestNPC.CanInteract())
                closestNPC.RelationShip();
            else
                closestNPC.Interact();
        }
    }

    void UpdateTrayProximityFeedback()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = true;
        filter.layerMask = ~LayerMask.GetMask("Player");

        List<Collider2D> resultList = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, range, filter, resultList);

        List<TraySlot> currentTrays = new List<TraySlot>();
        foreach (var col in resultList)
        {
            TraySlot tray = col.GetComponent<TraySlot>();
            if (tray != null) currentTrays.Add(tray);
        }

        foreach (var tray in lastNearbyTrays)
            if (!currentTrays.Contains(tray))
                tray.RefreshFeedback(isPlayerNearby: false);

        foreach (var tray in currentTrays)
            tray.RefreshFeedback(isPlayerNearby: true);

        lastNearbyTrays = currentTrays;
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