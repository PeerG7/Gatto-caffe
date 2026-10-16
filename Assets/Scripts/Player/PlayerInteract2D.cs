using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    [Header("DATACAT 1 Prefab (ลาก Prefab ของ DATACAT1 มาใส่ที่นี่)")]
    public GameObject dataCat1Prefab;

    [Header("Legacy Interact Keys")]
    public KeyCode[] interactKeys = new KeyCode[] { KeyCode.E };

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System")]
    public UnityEngine.InputSystem.InputActionReference interactAction;
#endif

    [Header("Anti-Spam / Cooldown Settings")]
    [Tooltip("ระยะเวลาหน่วงระหว่างการกด E แต่ละครั้ง ป้องกัน Frame Collision / Race Condition")]
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

        if (!pressed) return;

        // 🟢 บล็อกการสแปมกด E รัวๆ ในเฟรมติดๆ กัน (Anti-Spam Debounce)
        if (Time.time - lastInteractTime < interactCooldown)
        {
            Debug.LogWarning($"[TRACE_INPUT] ⚠️ Ignore Spam E Input! (Cooldown: {Time.time - lastInteractTime:F2}s)");
            return;
        }
        lastInteractTime = Time.time;

        if (activeCanvas != null) { activeCanvas.OnConfirm(); return; }

        TriggerInteract();
    }

    public void TriggerInteract()
    {
        if (PlayerController2D.IsLocked) return;

        PlayerInventory playerInv = GetComponent<PlayerInventory>();

        // ==========================================
        // 0.1 🟢 วาง DATACAT1 ลงใน InteractionZone ที่ใกล้ที่สุดและว่างอยู่
        // ==========================================
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

        Debug.Log($"[TRACE_INPUT] 🔘 Player Triggered Interact at Time: {Time.time:F2} | Hits Count: {hits.Length}");

        // ==========================================
        // 0.2 🟢 อุ้ม DATACAT1 ขึ้นหัว (ต้องกินอาหารเสร็จแล้วถึงจะอุ้มได้)
        // ==========================================
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

                        if (npcCtrl.assignedTable != null)
                        {
                            npcCtrl.assignedTable.RemoveNPCFromTable(npcCtrl);
                        }

                        if (npcCtrl.currentZone != null)
                        {
                            npcCtrl.currentZone.Release();
                            npcCtrl.currentZone = null;
                        }

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

        // 2. ซ่อม Furniture ที่พัง
        foreach (var hit in hits)
        {
            DamageableObject dmg = hit.GetComponent<DamageableObject>();
            if (dmg != null && dmg.CanRepair()) { dmg.StartRepair(); return; }
        }

        // 3. เก็บเงินที่โต๊ะ
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.hasMoneyToCollect)
            {
                table.TryCollectMoney();
                return;
            }
        }

        // 4. เสิร์ฟอาหารที่โต๊ะ
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null && table.HasAnyNPCWaiting())
            {
                table.TryServeFood();
                return;
            }
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

        // 7. ทิ้งอาหารที่ Garbage Zone
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

            // 🟢 1. ถ้ายืนประจำจุด InteractionZone และปลดล็อคสิทธิ์แล้ว -> เปิดหน้า Choice / QTE
            if (npcCtrl != null && npcCtrl.isInteractionAllowed && npcCtrl.currentState == NPCController.NPCState.AtInteractionZone)
            {
                Debug.Log($"[TRACE_INPUT] ✅ Triggering Zone QTE for: {closestNPC.gameObject.name}");
                closestNPC.RequestZoneInteraction();
                return;
            }
            // 🟢 2. ถ้านั่งที่โต๊ะอาหารอยู่ (Sitting) -> เปิดหน้า Relationship Book เท่านั้น
            else if (closestNPC.CanInteract())
            {
                PlayerInventory playerInvRef = GetComponent<PlayerInventory>();
                if (playerInvRef != null && playerInvRef.HasItem()) return; // ถ้าถืออาหารอยู่ให้ Bypass ไปเสิร์ฟ

                Debug.Log($"[TRACE_INPUT] ❤️ Opening Relationship for Sitting Cat: {closestNPC.gameObject.name}");
                closestNPC.RelationShip();
                return;
            }
            // 🟢 3. ถ้ารอคิวหน้าร้าน -> เรียกเข้าคิว
            else if (npcCtrl != null && npcCtrl.currentState == NPCController.NPCState.InQueue)
            {
                Debug.Log($"[TRACE_INPUT] 📋 Inviting Queue NPC: {closestNPC.gameObject.name}");
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
            if (newNpc != null)
            {
                newNpc.InitAtInteractionZone(zone);
            }
        }
        else
        {
            Debug.LogWarning("PlayerInteract2D: ยังไม่ได้ลาก Prefab ของ DATACAT1 มาใส่ที่ช่อง dataCat1Prefab ใน Inspector!");
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
}