using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// PlayerInteract2D — v2 (Keyboard + Gamepad Added)
//
// ของเดิม: mouse click / KeyCode.E ยังทำงานได้ปกติ
// เพิ่มใหม่: รองรับ Unity New Input System
//   - ถ้า project ใช้ New Input System → ผูก PlayerInput component
//     บน Player แล้ว assign interactAction ใน Inspector
//   - ถ้ายังใช้ Legacy Input → interactAction ว่างไว้ได้ ระบบเดิมทำงาน
//
// Setup (New Input System):
//   1. Player prefab → Add Component → PlayerInput
//   2. สร้าง InputActionAsset: Actions > Gameplay > Interact
//      Binding: Keyboard/E, Gamepad/Button South (A/Cross)
//   3. ลาก Action Reference มาใส่ช่อง "Interact Action" ใน Inspector
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

    // ── Canvas navigation support ──────────────────────────────────
    // script อื่นสามารถ register/unregister canvas ที่กำลัง active อยู่
    // เพื่อให้ PlayerInteract2D หยุดส่ง interact ผ่าน world space
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

        // ── Legacy KeyCode (เดิม) ──────────────────────────────────
        foreach (KeyCode key in interactKeys)
        {
            if (Input.GetKeyDown(key)) { pressed = true; break; }
        }

#if ENABLE_INPUT_SYSTEM
        // ── New Input System (ใหม่) ────────────────────────────────
        // รับ gamepad South button (A/Cross) หรือ keyboard ที่ bind ไว้
        if (!pressed && interactAction != null)
        {
            if (interactAction.action.WasPressedThisFrame())
                pressed = true;
        }
#endif

        if (!pressed) return;

        // ถ้ามี canvas ที่ active อยู่ ส่ง confirm ไปที่ canvas แทน
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

        // 1. ปลดล็อกเฟอร์นิเจอร์ (ยังเป็นเงาอยู่) — priority สูงสุดเสมอ
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

        // 3. เสิร์ฟอาหารที่โต๊ะ
        //    (เดิมมีข้อ "กด interact แทนการคลิก Heart Icon" อยู่ก่อนหน้านี้ — เอาออกแล้ว
        //     เพราะ Heart Icon ถูกลบออกจากระบบทั้งหมด แมวเดินไป Interaction Zone
        //     ทันทีหลัง Serve เสร็จโดยอัตโนมัติ ไม่ต้องรอผู้เล่นกด E ที่โต๊ะอีกต่อไป)
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.sittingNPC != null &&
                table.sittingNPC.currentState == NPCController.NPCState.Sitting)
            { table.TryServeFood(); return; }
        }

        // 4. เปิด Relationship Book
        foreach (var hit in hits)
        {
            BookStation book = hit.GetComponent<BookStation>();
            if (book != null) { book.OpenBook(); return; }
        }

        // 5. เปิด cooking / drink station
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

        // 8. อัปเกรดเฟอร์นิเจอร์ (ไม้ -> หินอ่อน)
        //    ย้ายมาไว้ก่อน NPC interact เพื่อกันไม่ให้กด E ใกล้แมวที่นั่งอยู่
        //    ไปโดน RelationShip() (คุยกับแมว) โดยไม่ตั้งใจตอนแค่จะ Upgrade โต๊ะ
        //    ซึ่งเป็นสาเหตุของบั๊กเกมค้างที่ RelationShip() ไปเปิด Canvas ที่มีปัญหา
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
        // ✅ ให้ความสำคัญกับแมวที่ยืนรออยู่ที่โซนก่อนเสมอ (ไม่สนว่าใครใกล้กว่า)
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

        // ถ้าไม่มีแมวรอที่โซน ค่อยมาดูแมวตัวที่ใกล้ที่สุด
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