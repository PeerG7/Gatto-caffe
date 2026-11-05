using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 3.0f;

    [Header("Data Cat Prefabs (ลาก Prefab ของแมวทั้งหมดมาใส่ในนี้)")]
    public List<GameObject> dataCatPrefabs = new List<GameObject>();

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
        PlayerController2D playerController = GetComponent<PlayerController2D>();

        // ==========================================
        // 0.1 🟢 วางแมวลงใน InteractionZone
        // ==========================================
        if (playerController != null && playerController.isHoldingCat)
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
                Vector3 spawnPos = closestZone.point.position;
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hitNav, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    spawnPos = hitNav.position;
                }

                GameObject releasedCat = playerController.ReleaseHeldCat(spawnPos);
                NPCController newNpc = releasedCat != null ? releasedCat.GetComponent<NPCController>() : null;
                if (newNpc != null)
                {
                    newNpc.InitAtInteractionZone(closestZone);
                }

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

        // =========================================================================
        // 🌟 0.1.5: เช็กแมวที่รอจูง (WaitingForGuidance) หรืออยู่ที่ InteractionZone
        // =========================================================================
        NPCInteract closestNPC = GetClosestNPC(hits);
        if (closestNPC != null)
        {
            NPCController npcCtrl = closestNPC.GetComponent<NPCController>();

            if (npcCtrl != null && npcCtrl.currentState == NPCController.NPCState.WaitingForGuidance)
            {
                npcCtrl.StartFollowingPlayer(transform);
                return;
            }

            if (npcCtrl != null && npcCtrl.isInteractionAllowed && npcCtrl.currentState == NPCController.NPCState.AtInteractionZone)
            {
                Debug.Log($"[TRACE_INPUT] ✅ Triggering Zone QTE for: {closestNPC.gameObject.name}");
                closestNPC.RequestZoneInteraction();
                return;
            }
        }

        // ==========================================
        // 0.2 🟢 อุ้มแมวขึ้นตัว (🔒 เข้มงวด: ป้องกันการอุ้มแมวเดินเล่นทั่วไป)
        // ==========================================
        if (playerController != null && !playerController.isHoldingCat)
        {
            foreach (var hit in hits)
            {
                CatIdentity identity = hit.GetComponent<CatIdentity>();
                NPCController npcCtrl = hit.GetComponent<NPCController>();

                if (identity != null && npcCtrl != null)
                {
                    // 1. ห้ามอุ้มเด็ดขาดถ้าแมวอยู่ใน InteractionZone แล้ว
                    bool isInInteractionZone = (npcCtrl.currentState == NPCController.NPCState.AtInteractionZone || npcCtrl.currentZone != null);
                    if (isInInteractionZone) continue;

                    // 2. ห้ามอุ้มเด็ดขาดถ้าแมวกำลังเดินเล่นปกติหรือยังไม่ถึงสถานะต้องอุ้ม
                    // เงื่อนไขต้องผ่านทั้งหมด: กินเสร็จแล้ว + อยู่ในสถานะที่อนุญาตให้อุ้ม (เช่น ต้องการให้อุ้ม หรือถูกกำหนดให้ถือได้)
                    bool isReadyToCarry = npcCtrl.hasFinishedEating && npcCtrl.needsToBeCarried;

                    if (isReadyToCarry)
                    {
                        Sprite catSprite = identity.catData != null ? identity.catData.catPortrait : null;

                        playerController.PickUpCat(identity.catData, catSprite, hit.gameObject);

                        if (npcCtrl.assignedTable != null)
                        {
                            npcCtrl.assignedTable.RemoveNPCFromTable(npcCtrl);
                        }

                        if (npcCtrl.currentZone != null)
                        {
                            npcCtrl.currentZone.Release();
                            npcCtrl.currentZone = null;
                        }

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
                if (playerInv != null) garbage.TryDiscard(playerInv);
                return;
            }
        }

        // 8. หยิบ/วาง/swap อาหารจาก TraySlot
        foreach (var hit in hits)
        {
            TraySlot tray = hit.GetComponent<TraySlot>();
            if (tray != null)
            {
                if (playerInv != null) tray.TryInteract(playerInv);
                return;
            }
        }

        // สั่งให้แมวที่จูงอยู่ นั่งลงที่โต๊ะ
        foreach (var hit in hits)
        {
            CustomerTable table = hit.GetComponent<CustomerTable>();
            if (table != null)
            {
                NPCController[] allNPCs = FindObjectsOfType<NPCController>();
                foreach (var npc in allNPCs)
                {
                    if (npc != null && npc.currentState == NPCController.NPCState.FollowingPlayer)
                    {
                        if (npc.TrySitAtTable(table)) return;
                    }
                }
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
        if (closestNPC != null)
        {
            NPCController npcCtrl = closestNPC.GetComponent<NPCController>();

            if (closestNPC.CanInteract())
            {
                if (playerInv != null && playerInv.HasItem()) return;
                closestNPC.RelationShip();
                return;
            }
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