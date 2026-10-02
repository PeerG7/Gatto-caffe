using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// PlayerInteract2D — v2 (Keyboard + Gamepad Added)
// =====================================================================
public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    [Header("Legacy Interact Keys (ยังใช้งานได้ปกติ)")]
    public KeyCode[] interactKeys = new KeyCode[] { KeyCode.E };

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System (optional — ถ้าใช้ New Input System)")]
    [Tooltip("ลาก InputActionReference จาก InputActionAsset มาใส่ตรงนี้")]
    public UnityEngine.InputSystem.InputActionReference interactAction;
#endif

    private List<TraySlot> lastNearbyTrays = new List<TraySlot>();

    private static IGamepadNavigable activeCanvas = null;
    public static void RegisterActiveCanvas(IGamepadNavigable canvas) => activeCanvas = canvas;
    public static void UnregisterActiveCanvas(IGamepadNavigable canvas)
    { if (activeCanvas == canvas) activeCanvas = null; }

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();
    }
#endif

    void Update()
    {
        if (PlayerController2D.IsLocked) return;

        UpdateTrayProximityFeedback();

        bool pressed = false;

        foreach (KeyCode key in interactKeys)
        {
            if (Input.GetKeyDown(key)) { pressed = true; break; }
        }

#if ENABLE_INPUT_SYSTEM
        if (!pressed && interactAction != null)
        {
            if (interactAction.action.WasPressedThisFrame())
                pressed = true;
        }
#endif

        if (!pressed) return;

        if (activeCanvas != null)
        {
            activeCanvas.OnConfirm();
            return;
        }

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

        // 1. ปลดล็อกเฟอร์นิเจอร์ (ยังเป็นเงาอยู่)
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

        // 3. เสิร์ฟอาหารที่โต๊ะ (แก้จุดที่ 1 & 2)
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.HasAnyNPCWaiting())
            {
                table.TryServeFood();
                return;
            }
        }

        // 4. เปิด Relationship Book
        foreach (var hit in hits)
        {
            BookStation book = hit.GetComponent<BookStation>();
            if (book != null) { book.OpenBook(); return; }
        }

        // 5. เปิด cooking / drink station (แก้จุดที่ 3 & 4)
        foreach (var hit in hits)
        {
            StationInteract station = hit.GetComponent<StationInteract>();
            if (station != null)
            {
                bool hasActiveTable = false;
                foreach (var h in hits)
                {
                    CustomerTable t = h.GetComponent<CustomerTable>();
                    if (t != null && t.HasAnyNPCWaiting())
                    {
                        hasActiveTable = true;
                        break;
                    }
                }

                if (!hasActiveTable)
                {
                    PlayerController2D.IsLocked = true;
                    station.OpenCanvas();
                    return;
                }
            }
        }

        // 6. ทิ้งอาหารที่ Garbage Zone
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

        // 7. หยิบ/วาง/swap อาหารจาก TraySlot
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

        // 8. อัปเกรดเฟอร์นิเจอร์
        foreach (var hit in hits)
        {
            FurnitureObject furn = hit.GetComponent<FurnitureObject>();
            if (furn != null && furn.isUnlocked && !furn.isUpgraded)
            {
                furn.AttemptUpgrade();
                return;
            }
        }

        // 9. NPC interact
        NPCInteract zoneWaitingNPC = null;
        float minZoneDist = Mathf.Infinity;
        foreach (var hit in hits)
        {
            NPCInteract n = hit.GetComponent<NPCInteract>();
            if (n != null && n.CanRequestZoneInteraction())
            {
                float d = Vector2.Distance(transform.position, n.transform.position);
                if (d < minZoneDist)
                {
                    minZoneDist = d;
                    zoneWaitingNPC = n;
                }
            }
        }

        if (zoneWaitingNPC != null)
        {
            zoneWaitingNPC.RequestZoneInteraction();
            return;
        }

        NPCInteract closestNPC = GetClosestNPC(hits);
        if (closestNPC != null)
        {
            if (closestNPC.CanInteract())
            {
                PlayerInventory playerInv = GetComponent<PlayerInventory>();
                if (playerInv != null && playerInv.HasItem()) return;

                closestNPC.RelationShip();
                return;
            }
            else
            {
                closestNPC.Interact();
                return;
            }
        }

        foreach (var hit in hits)
        {
            ComputerStation comp = hit.GetComponent<ComputerStation>();
            if (comp != null) { comp.OpenComputerShop(); return; }
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