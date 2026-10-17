using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    [Header("DATACAT 1 Prefab")]
    public GameObject dataCat1Prefab;

    [Header("Legacy Interact Keys")]
    public KeyCode[] interactKeys = new KeyCode[] { KeyCode.E };

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System")]
    public UnityEngine.InputSystem.InputActionReference interactAction;
#endif

    [Header("Anti-Spam / Cooldown Settings")]
    public float interactCooldown = 0.25f;
    private float lastInteractTime = 0f;

    private List<TraySlot> lastNearbyTrays = new List<TraySlot>();

    private static IGamepadNavigable activeCanvas = null;
    public static void RegisterActiveCanvas(IGamepadNavigable canvas) => activeCanvas = canvas;
    public static void UnregisterActiveCanvas(IGamepadNavigable canvas)
    { if (activeCanvas == canvas) activeCanvas = null; }

#if ENABLE_INPUT_SYSTEM
    void OnEnable() { if (interactAction != null) interactAction.action.Enable(); }
    void OnDisable() { if (interactAction != null) interactAction.action.Disable(); }
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
        if (!pressed && interactAction != null && interactAction.action.WasPressedThisFrame())
            pressed = true;
#endif

        if (pressed)
        {
            if (Time.time - lastInteractTime < interactCooldown) return;
            lastInteractTime = Time.time;

            if (activeCanvas != null) { activeCanvas.OnConfirm(); return; }

            TriggerInteract();
        }

        // กดปุ่ม F เพื่อสั่งให้แมวที่กำลังถูกพาเดินตาม (Escort) เข้าไปนั่งโต๊ะที่ใกล้ที่สุด
        if (Input.GetKeyDown(KeyCode.F))
        {
            TrySeatEscortedCat();
        }
    }

    public void TriggerInteract()
    {
        if (PlayerController2D.IsLocked) return;

        PlayerInventory playerInv = GetComponent<PlayerInventory>();

        // 0.1 วาง DATACAT1 ลง InteractionZone
        if (playerInv != null && playerInv.GetCurrentItemName() == "DATACAT1")
        {
            InteractionZone closestZone = null;
            float minZoneDist = range;

            foreach (var zone in FindObjectsOfType<InteractionZone>())
            {
                if (zone != null && zone.IsAvailable())
                {
                    float dist = Vector2.Distance(transform.position, zone.transform.position);
                    if (dist <= minZoneDist)
                    {
                        minZoneDist = dist;
                        closestZone = zone;
                    }
                }
            }

            if (closestZone != null)
            {
                SpawnAndSetupDataCat1AtZone(closestZone, playerInv.GetCurrentItemSprite());
                playerInv.ClearItem();
                PlayerController2D.IsLocked = false;
                return;
            }
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = true;
        filter.layerMask = ~LayerMask.GetMask("Player");

        List<Collider2D> resultList = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, range, filter, resultList);
        Collider2D[] hits = resultList.ToArray();

        // 0.2 อุ้ม DATACAT1 ขึ้นหัว
        if (playerInv != null && playerInv.IsEmpty())
        {
            foreach (var hit in hits)
            {
                CatIdentity identity = hit.GetComponent<CatIdentity>();
                if (identity != null && identity.IsDataCat1())
                {
                    NPCController npcCtrl = hit.GetComponent<NPCController>();
                    if (npcCtrl != null && npcCtrl.hasFinishedEating)
                    {
                        Sprite catSprite = identity.catData != null ? identity.catData.catPortrait : null;
                        playerInv.PickUpItem("DATACAT1", catSprite);

                        if (npcCtrl.assignedTable != null) assignedTableRemove(npcCtrl);
                        if (npcCtrl.currentZone != null) { npcCtrl.currentZone.Release(); npcCtrl.currentZone = null; }

                        Destroy(hit.gameObject);
                        return;
                    }
                }
            }
        }

        // 1. ปลดล็อกเฟอร์นิเจอร์
        foreach (var hit in hits)
        {
            FurnitureObject furn = hit.GetComponent<FurnitureObject>();
            if (furn != null && !furn.isUnlocked) { furn.AttemptUnlock(); return; }
        }

        // 2. ซ่อม Furniture
        foreach (var hit in hits)
        {
            DamageableObject dmg = hit.GetComponent<DamageableObject>();
            if (dmg != null && dmg.CanRepair()) { dmg.StartRepair(); return; }
        }

        // 3. เก็บเงินที่โต๊ะ
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.hasMoneyToCollect) { table.TryCollectMoney(); return; }
        }

        // 4. เสิร์ฟอาหารที่โต๊ะ
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.HasAnyNPCWaiting()) { table.TryServeFood(); return; }
        }

        // 5. เปิด Relationship Book
        foreach (var hit in hits)
        {
            BookStation book = hit.GetComponent<BookStation>();
            if (book != null) { book.OpenBook(); return; }
        }

        // 6. เปิด Cooking/Drink station
        foreach (var hit in hits)
        {
            StationInteract station = hit.GetComponent<StationInteract>();
            if (station != null)
            {
                bool hasActiveTable = false;
                foreach (var h in hits)
                {
                    CustomerTable t = h.GetComponent<CustomerTable>();
                    if (t != null && t.HasAnyNPCWaiting()) { hasActiveTable = true; break; }
                }

                if (!hasActiveTable)
                {
                    PlayerController2D.IsLocked = true;
                    station.OpenCanvas();
                    return;
                }
            }
        }

        // 7. ทิ้งขยะ Garbage Zone
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

        // 8. หยิบ/วาง TraySlot
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

        // 9. อัปเกรดเฟอร์นิเจอร์
        foreach (var hit in hits)
        {
            FurnitureObject furn = hit.GetComponent<FurnitureObject>();
            if (furn != null && furn.isUnlocked && !furn.isUpgraded)
            {
                CustomerTable tableCheck = hit.GetComponent<CustomerTable>();
                if (tableCheck != null && tableCheck.hasMoneyToCollect) continue;

                furn.AttemptUpgrade();
                return;
            }
        }

        // 10. NPC interact
        NPCInteract closestNPC = GetClosestNPC(hits);
        if (closestNPC != null)
        {
            NPCController npcCtrl = closestNPC.GetComponent<NPCController>();

            // 🟢 เช็คพิเศษ: ถ้ายืนใกล้แมวที่หลงทางหน้าร้าน และยังไม่ได้กดพาเดินตาม
            if (npcCtrl != null && npcCtrl.currentState == NPCController.NPCState.InQueue && npcCtrl.isLostInQueue && !npcCtrl.isBeingEscorted)
            {
                Debug.Log($"[TRACE_INPUT] 🗺️ Start escorting lost cat: {closestNPC.gameObject.name}");
                npcCtrl.StartEscort(transform); // เริ่มต้นระบบพาเดินตาม
                return;
            }

            // ถ้ายืนประจำจุด InteractionZone
            if (npcCtrl != null && npcCtrl.isInteractionAllowed && npcCtrl.currentState == NPCController.NPCState.AtInteractionZone)
            {
                closestNPC.RequestZoneInteraction();
                return;
            }
            // ถ้านั่งที่โต๊ะอาหาร
            else if (closestNPC.CanInteract())
            {
                PlayerInventory playerInvRef = GetComponent<PlayerInventory>();
                if (playerInvRef != null && playerInvRef.HasItem()) return;

                closestNPC.RelationShip();
                return;
            }
            // คิวปกติหน้าร้าน
            else if (npcCtrl != null && npcCtrl.currentState == NPCController.NPCState.InQueue)
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

    private void assignedTableRemove(NPCController npcCtrl)
    {
        if (npcCtrl.assignedTable != null)
        {
            npcCtrl.assignedTable.RemoveNPCFromTable(npcCtrl);
        }
    }

    private void SpawnAndSetupDataCat1AtZone(InteractionZone zone, Sprite catSprite)
    {
        if (dataCat1Prefab != null)
        {
            Vector3 spawnPos = zone.point.position;
            if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            GameObject spawnedCat = Instantiate(dataCat1Prefab, spawnPos, Quaternion.identity);
            NPCController newNpc = spawnedCat.GetComponent<NPCController>();
            if (newNpc != null) newNpc.InitAtInteractionZone(zone);
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
            if (!currentTrays.Contains(tray)) tray.RefreshFeedback(isPlayerNearby: false);

        foreach (var tray in currentTrays) tray.RefreshFeedback(isPlayerNearby: true);

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

    public void TrySeatEscortedCat()
    {
        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (var npc in allNPCs)
        {
            if (npc != null && npc.isBeingEscorted)
            {
                CustomerTable[] tables = FindObjectsOfType<CustomerTable>();
                CustomerTable closestTable = null;
                float minDist = 3.0f;

                foreach (var t in tables)
                {
                    if (t != null && !t.isFull)
                    {
                        float d = Vector2.Distance(transform.position, t.transform.position);
                        if (d < minDist)
                        {
                            minDist = d;
                            closestTable = t;
                        }
                    }
                }

                if (closestTable != null)
                {
                    Transform seat = closestTable.GetAvailableSeat();
                    if (seat != null)
                    {
                        // 🟢 ตัด assignedTable.AssignNPCToTable ออก ให้ ArriveAndSitAtTable จัดการทีเดียวจบ
                        npc.ArriveAndSitAtTable(closestTable, seat);
                        Debug.Log("[PlayerInteract2D] ✅ พาแมวหลงทางนั่งโต๊ะสำเร็จ!");
                        return;
                    }
                }
            }
        }
    }
}