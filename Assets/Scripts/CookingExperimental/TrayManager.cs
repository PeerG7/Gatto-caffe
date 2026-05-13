using UnityEngine;

/// <summary>
/// จัดการ Tray ทั้งหมด 3 slot
/// รับอาหารจาก CookingManager แล้วส่งไปยัง slot ที่ว่าง
/// </summary>
public class TrayManager : MonoBehaviour
{
    public static TrayManager instance;

    [Header("Tray Slots (ลาก TraySlot GameObjects มาใส่)")]
    public TraySlot[] traySlots = new TraySlot[3];

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // ═══════════════════════════════════════════════
    //  เรียกจาก CookingManager ตอน Cook เสร็จ
    // ═══════════════════════════════════════════════
    public bool ReceiveFood(string itemName, Sprite itemSprite)
    {
        TraySlot emptySlot = GetEmptySlot();

        if (emptySlot == null)
        {
            Debug.LogWarning("[TrayManager] Tray เต็มทุก slot! รอ player หยิบก่อน");
            return false;
        }

        emptySlot.PlaceFood(itemName, itemSprite);
        Debug.Log($"[TrayManager] วางอาหาร '{itemName}' ลง Slot {emptySlot.slotIndex}");
        return true;
    }

    // ═══════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════
    TraySlot GetEmptySlot()
    {
        foreach (var slot in traySlots)
            if (slot != null && slot.IsEmpty()) return slot;
        return null;
    }

    public bool HasEmptySlot() => GetEmptySlot() != null;

    public bool IsAllEmpty()
    {
        foreach (var slot in traySlots)
            if (slot != null && slot.HasFood()) return false;
        return true;
    }
}