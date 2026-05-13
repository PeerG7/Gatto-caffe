using UnityEngine;

/// <summary>
/// ติดกับ GameObject ที่เป็น Garbage Zone
/// Player กด E ตอนอยู่ใน zone → ทิ้งอาหารที่ถืออยู่ทันที
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GarbageZone : MonoBehaviour
{
    // ไม่มีอะไรพิเศษ — PlayerInteract2D จะเรียก TryDiscard() เอง
    public bool TryDiscard(PlayerInventory player)
    {
        if (player == null || player.IsEmpty())
        {
            Debug.Log("[GarbageZone] ไม่มีอะไรให้ทิ้ง");
            return false;
        }

        Debug.Log($"[GarbageZone] ทิ้ง: {player.GetCurrentItemName()}");
        player.ClearItem();
        return true;
    }
}