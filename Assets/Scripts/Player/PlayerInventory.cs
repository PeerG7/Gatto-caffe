using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Item Renderers (แยกตามท่าทาง/มุมมอง)")]
    public SpriteRenderer idleSideRenderer;  // สำหรับตอนยืนนิ่ง หรือเดินซ้าย/ขวา/ลง
    public SpriteRenderer walkUpRenderer;    // สำหรับตอนเดินขึ้น (Walk Up)

    [Header("Data")]
    public string currentItem = "";
    private Sprite currentSprite = null;

    public void PickUpItem(string itemName, Sprite itemSprite)
    {
        currentItem = itemName;
        currentSprite = itemSprite;

        // อัปเดต Sprite ให้ทุก Holder เตรียมไว้
        if (idleSideRenderer != null) idleSideRenderer.sprite = itemSprite;
        if (walkUpRenderer != null) walkUpRenderer.sprite = itemSprite;
    }

    public void ClearItem()
    {
        currentItem = "";
        currentSprite = null;

        if (idleSideRenderer != null)
        {
            idleSideRenderer.sprite = null;
            idleSideRenderer.enabled = false;
        }
        if (walkUpRenderer != null)
        {
            walkUpRenderer.sprite = null;
            walkUpRenderer.enabled = false;
        }
    }

    // ฟังก์ชันช่วยเปิด/ปิดการแสดงผล Holder ตัวที่ใช้งานอยู่
    public void UpdateHolderVisibility(bool isHolding, bool isWalkUp)
    {
        if (!HasItem())
        {
            if (idleSideRenderer != null) idleSideRenderer.enabled = false;
            if (walkUpRenderer != null) walkUpRenderer.enabled = false;
            return;
        }

        if (isWalkUp)
        {
            if (walkUpRenderer != null) walkUpRenderer.enabled = true;
            if (idleSideRenderer != null) idleSideRenderer.enabled = false;
        }
        else
        {
            if (idleSideRenderer != null) idleSideRenderer.enabled = true;
            if (walkUpRenderer != null) walkUpRenderer.enabled = false;
        }
    }

    public bool HasItem() => !string.IsNullOrEmpty(currentItem);
    public bool IsEmpty() => string.IsNullOrEmpty(currentItem);
    public string GetCurrentItemName() => currentItem;
    public Sprite GetCurrentItemSprite() => currentSprite;
}