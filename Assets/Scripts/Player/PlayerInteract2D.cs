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
    [Tooltip("ระยะเวลาหน่วงระหว่างการกด E/F แต่ละครั้ง ป้องกัน Frame Collision / Race Condition")]
    public float interactCooldown = 0.15f;
    private float lastInteractTime = 0f;

    [Header("Input Buffer (กดรัวแล้วไม่หาย)")]
    [Tooltip("เวลาที่ยอมเก็บการกดไว้รอ (วินาที) ถ้ายังติด Cooldown")]
    public float inputBufferTime = 0.3f;
    private bool bufferedPress = false;
    private bool bufferedIsF = false;
    private float bufferedAt = 0f;

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
        if (PlayerController2D.IsLocked)
        {
            bufferedPress = false; // ล็อกอยู่ = ทิ้งคิวที่ค้าง กันยิงซ้ำตอนปลดล็อก
            return;
        }

        UpdateTrayProximityFeedback();

        bool pressedE = false;
        foreach (KeyCode key in interactKeys)
        {
            if (Input.GetKeyDown(key)) { pressedE = true; break; }
        }

#if ENABLE_INPUT_SYSTEM
        if (!pressedE && interactAction != null && interactAction.action.WasPressedThisFrame())
            pressedE = true;
#endif

        bool pressedF = Input.GetKeyDown(KeyCode.F);

        // เก็บการกดล่าสุดไว้ในบัฟเฟอร์ (เก็บแค่ 1 ครั้ง ไม่สะสม)
        if (pressedE || pressedF)
        {
            bufferedPress = true;
            bufferedIsF = pressedF && !pressedE;
            bufferedAt = Time.unscaledTime;
        }

        if (!bufferedPress) return;

        // บัฟเฟอร์หมดอายุ
        if (Time.unscaledTime - bufferedAt > inputBufferTime)
        {
            bufferedPress = false;
            return;
        }

        // ยังติด Cooldown -> รอเฟรมถัดไป (ไม่ทิ้งการกด)
        if (Time.unscaledTime - lastInteractTime < interactCooldown) return;

        bufferedPress = false;
        lastInteractTime = Time.unscaledTime;

        if (activeCanvas != null && !bufferedIsF) { activeCanvas.OnConfirm(); return; }

        TriggerInteract(bufferedIsF);
    }

    public void TriggerInteract() => TriggerInteract(false);

    public void TriggerInteract(bool isUpgradeAction)
    {
        if (PlayerController2D.IsLocked) return;

        PlayerInventory playerInv = GetComponent<PlayerInventory>();
        PlayerController2D playerController = GetComponent<PlayerController2D>();

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = true;
        filter.layerMask = ~LayerMask.GetMask("Player");

        List<Collider2D> resultList = new List<Collider2D>();
        Physics2D.OverlapCircle(transform.position, range, filter, resultList);
        Collider2D[] hits = resultList.ToArray();

        // ==========================================
        // 🛠️ ถ้ากดปุ่ม F -> ทำเฉพาะการอัปเกรดเฟอร์นิเจอร์เท่านั้น
        // ==========================================
        if (isUpgradeAction)
        {
            foreach (var hit in hits)
            {
                FurnitureObject furn = hit.GetComponent<FurnitureObject>();
                if (furn != null && furn.isUnlocked && !furn.isUpgraded)
                {
                    CustomerTable tableCheck = hit.GetComponent<CustomerTable>();
                    if (tableCheck != null && tableCheck.hasMoneyToCollect) continue;

                    furn.AttemptUpgrade();
                    Debug.Log("[TRACE_INPUT] 🛠️ Upgrade triggered via F key.");
                    return;
                }
            }
            Debug.Log("[TRACE_INPUT] ⚠️ F pressed, but no upgradeable furniture in range.");
            return;
        }

        // ==========================================
        // 🟢 ถ้ากดปุ่ม E -> ทำงานอื่นๆ ปกติทั้งหมด (แยกจากการอัปเกรด)
        // ==========================================

        // 0.1 วางแมวลงใน InteractionZone
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
                closestNPC.RequestZoneInteraction();
                return;
            }
        }

        // 0.2 อุ้มแมวขึ้นตัว (🔒 เงื่อนไข: ต้องไม่ได้อุ้มแมวอยู่ และ Inventory ต้องว่างเปล่า ไม่มีอาหารถืออยู่)[cite: 3]
        if (playerController != null && !playerController.isHoldingCat && playerInv != null && playerInv.IsEmpty())
        {
            foreach (var hit in hits)
            {
                CatIdentity identity = hit.GetComponent<CatIdentity>();
                NPCController npcCtrl = hit.GetComponent<NPCController>();

                if (identity != null && npcCtrl != null)
                {
                    bool isInInteractionZone = (npcCtrl.currentState == NPCController.NPCState.AtInteractionZone || npcCtrl.currentZone != null);
                    if (isInInteractionZone) continue;

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