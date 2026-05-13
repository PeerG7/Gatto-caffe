using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ติดกับ GameObject ของแต่ละ Tray Slot ในฉาก
/// แสดงแค่ Food Icon — ไม่มี Text
/// รองรับ Swap: ถือ A กด E ที่ Tray ที่มี B → สลับกัน
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TraySlot : MonoBehaviour
{
    [Header("Slot Info")]
    public int slotIndex;           // 0, 1, 2

    [Header("World Feedback UI")]
    public GameObject feedbackUI;   // Canvas (World Space) ลอยเหนือ Tray
    public Image foodIcon;          // รูป icon อาหาร — แสดงเมื่อมีของ

    // ─── State ───
    private bool isEmpty = true;
    private string heldItemName;
    private Sprite heldItemSprite;

    void Start()
    {
        RefreshFeedback(isPlayerNearby: false);
    }

    // ═══════════════════════════════════════════════
    //  เรียกจาก TrayManager ตอน Cook เสร็จ
    // ═══════════════════════════════════════════════
    public void PlaceFood(string itemName, Sprite itemSprite)
    {
        isEmpty = false;
        heldItemName = itemName;
        heldItemSprite = itemSprite;
        RefreshFeedback(isPlayerNearby: false);
        Debug.Log($"[TraySlot {slotIndex}] รับอาหาร: {itemName}");
    }

    // ═══════════════════════════════════════════════
    //  เรียกจาก PlayerInteract2D
    //
    //  3 กรณี:
    //  1. Tray มีของ + Player มือว่าง   → หยิบ
    //  2. Tray ว่าง   + Player ถือของ   → วาง
    //  3. Tray มีของ + Player ถือของ   → SWAP
    // ═══════════════════════════════════════════════
    public bool TryInteract(PlayerInventory player)
    {
        if (player == null) return false;

        bool trayHasFood = !isEmpty;
        bool playerHasFood = !player.IsEmpty();

        // ── กรณี 1: หยิบ ──────────────────────────
        if (trayHasFood && !playerHasFood)
        {
            player.PickUpItem(heldItemName, heldItemSprite);
            ClearSlot();
            Debug.Log($"[TraySlot {slotIndex}] Player หยิบ: {heldItemName}");
            return true;
        }

        // ── กรณี 2: วาง ──────────────────────────
        if (!trayHasFood && playerHasFood)
        {
            string name = player.GetCurrentItemName();
            Sprite sprite = player.GetCurrentItemSprite();
            player.ClearItem();
            PlaceFood(name, sprite);
            Debug.Log($"[TraySlot {slotIndex}] Player วาง: {name}");
            return true;
        }

        // ── กรณี 3: Swap ─────────────────────────
        if (trayHasFood && playerHasFood)
        {
            // เก็บของใน Tray ไว้ก่อน
            string trayName = heldItemName;
            Sprite traySprite = heldItemSprite;

            // เอาของจาก Player มาวางลง Tray
            string playerName = player.GetCurrentItemName();
            Sprite playerSprite = player.GetCurrentItemSprite();

            // วางของ Player ลง Tray
            PlaceFood(playerName, playerSprite);

            // ส่งของ Tray ไปให้ Player
            player.PickUpItem(trayName, traySprite);

            Debug.Log($"[TraySlot {slotIndex}] Swap: มือ({playerName}) ↔ Tray({trayName})");
            return true;
        }

        // ── กรณี 4: ทั้งคู่ว่าง → ไม่ทำอะไร ──────
        Debug.Log("[TraySlot] ทั้ง Tray และมือว่าง — ไม่มีอะไรให้ทำ");
        return false;
    }

    // ═══════════════════════════════════════════════
    //  เคลียร์ slot
    // ═══════════════════════════════════════════════
    public void ClearSlot()
    {
        isEmpty = true;
        heldItemName = null;
        heldItemSprite = null;
        RefreshFeedback(isPlayerNearby: false);
    }

    // ═══════════════════════════════════════════════
    //  อัปเดต UI — แสดงแค่ food icon
    // ═══════════════════════════════════════════════
    public void RefreshFeedback(bool isPlayerNearby)
    {
        if (feedbackUI != null)
            feedbackUI.SetActive(!isEmpty);

        if (foodIcon != null)
        {
            foodIcon.sprite = heldItemSprite;
            foodIcon.enabled = !isEmpty;
        }
    }

    // ─── Getters ───
    public bool HasFood() => !isEmpty;
    public bool IsEmpty() => isEmpty;
    public string GetItemName() => heldItemName;
}