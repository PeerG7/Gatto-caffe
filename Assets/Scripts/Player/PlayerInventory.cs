using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer heldItemRenderer; // ลาก SpriteRenderer เหนือหัวมาใส่ตรงนี้

    [Header("Data")]
    public string currentItem = "";         // เก็บชื่อไอเทมที่ถืออยู่
    private Sprite currentSprite = null;    // เก็บ Sprite ของไอเทมที่ถืออยู่

    public void PickUpItem(string itemName, Sprite itemSprite)
    {
        currentItem = itemName;
        currentSprite = itemSprite;
        heldItemRenderer.sprite = itemSprite;
        heldItemRenderer.enabled = true;
    }

    public void ClearItem()
    {
        currentItem = "";
        currentSprite = null;
        heldItemRenderer.sprite = null;
        heldItemRenderer.enabled = false;
    }

    public bool HasItem()
    {
        return !string.IsNullOrEmpty(currentItem);
    }

    // ── เพิ่มใหม่ — ใช้โดย TraySlot ──────────────────
    public bool IsEmpty() => string.IsNullOrEmpty(currentItem);
    public string GetCurrentItemName() => currentItem;
    public Sprite GetCurrentItemSprite() => currentSprite;
}