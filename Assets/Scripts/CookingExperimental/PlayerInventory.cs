using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer heldItemRenderer; // ลาก SpriteRenderer เหนือหัวมาใส่ตรงนี้

    [Header("Data")]
    public string currentItem = ""; // เก็บชื่อไอเทมที่ถืออยู่

    public void PickUpItem(string itemName, Sprite itemSprite)
    {
        currentItem = itemName;
        heldItemRenderer.sprite = itemSprite;
        heldItemRenderer.enabled = true; // เปิดการมองเห็น
    }

    public void ClearItem()
    {
        currentItem = "";
        heldItemRenderer.sprite = null;
        heldItemRenderer.enabled = false; // ปิดการมองเห็น
    }

    public bool HasItem()
    {
        return !string.IsNullOrEmpty(currentItem);
    }
}