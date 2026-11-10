using UnityEngine;

public class PlacedFurniture : MonoBehaviour
{
    [Header("Item Data Reference (สำหรับตัวที่วางไว้ใน Scene ตั้งแต่แรก)")]
    public FurnitureItemData defaultItemData;

    [Header("Current State Data")]
    public FurnitureInstanceData instanceData;
    public string instanceID;
    public FurnitureItemData itemData;

    [Tooltip("0 = โต๊ะเงา, 1 = โต๊ะไม้, 2 = โต๊ะหินอ่อน")]
    public int currentLevel = 0;

    [Tooltip("จำนวนขั้นการอัปเกรดสูงสุด (เงา -> ไม้ -> หินอ่อน รวมเป็น Max = 2)")]
    public int maxLevel = 2;

    [Header("Visual Components")]
    public bool useLevelSprites = false;
    public SpriteRenderer spriteRenderer;
    public Sprite[] levelSprites;

    private bool isPickingUp = false;

    private void Awake()
    {
        if (string.IsNullOrEmpty(instanceID))
        {
            instanceID = System.Guid.NewGuid().ToString();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (instanceData == null)
        {
            if (itemData == null && defaultItemData != null)
            {
                itemData = defaultItemData;
            }
            instanceData = new FurnitureInstanceData(itemData, currentLevel);
        }
        UpdateVisual();
    }

    public void Setup(FurnitureInstanceData instance)
    {
        if (instance == null) return;

        instanceData = instance;
        itemData = instance.itemData;
        instanceID = instance.instanceID;
        currentLevel = instance.currentLevel;

        UpdateVisual();
    }

    public bool UpgradeFurniture()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.LogWarning($"[PlacedFurniture] {gameObject.name} อยู่ที่ Level สูงสุด ({maxLevel}) แล้ว");
            return false;
        }

        currentLevel++;

        if (instanceData != null)
        {
            instanceData.currentLevel = currentLevel;
            instanceData.isUnlocked = (currentLevel >= 1);
            instanceData.isUpgraded = (currentLevel >= 2);
        }

        UpdateVisual();
        Debug.Log($"[PlacedFurniture] {gameObject.name} Upgraded to Level: {currentLevel}");
        return true;
    }

    public void UpdateVisual()
    {
        FurnitureObject furnObj = GetComponent<FurnitureObject>();
        if (furnObj != null)
        {
            bool unlockedState = (currentLevel >= 1);
            bool upgradedState = (currentLevel >= 2);

            furnObj.isUnlocked = unlockedState;
            furnObj.isUpgraded = upgradedState;
            furnObj.UpdateVisuals();
            return;
        }

        if (useLevelSprites && spriteRenderer != null && levelSprites != null && levelSprites.Length > 0)
        {
            int index = Mathf.Clamp(currentLevel, 0, levelSprites.Length - 1);
            if (levelSprites[index] != null)
            {
                spriteRenderer.sprite = levelSprites[index];
            }
        }
    }

    /// <summary>
    /// 🗑️ ลบแมวทุกตัวที่ผูกกับโต๊ะ (CustomerTable) ของเฟอร์นิเจอร์ชิ้นนี้ — คืนค่าจำนวนแมวที่ถูกลบ
    /// เรียกซ้ำได้ปลอดภัย (ครั้งที่สองจะไม่เหลือแมวให้ลบ)
    /// </summary>
    public int EvictCats()
    {
        int removed = 0;
        foreach (CustomerTable table in GetComponentsInChildren<CustomerTable>(true))
        {
            if (table != null) removed += table.DestroySittingNPCs();
        }

        if (removed > 0) Debug.Log($"[PlacedFurniture] เก็บ {gameObject.name} → ลบแมวบนโต๊ะ {removed} ตัว");
        return removed;
    }

    public void PickupFurniture()
    {
        if (isPickingUp) return;
        isPickingUp = true;

        // 🗑️ กันไว้ทุกทางที่เก็บเฟอร์นิเจอร์ (ปุ่ม V, UI อื่น ๆ) — แมวบนโต๊ะต้องหายไปด้วยเสมอ
        EvictCats();

        // เสียงเก็บเล่นที่ผู้เรียก (FurnitureManager.HandleFurniturePickup) แล้ว ไม่เล่นซ้ำที่นี่
        StartCoroutine(AnimatePickupAndDestroy());
    }

    private System.Collections.IEnumerator AnimatePickupAndDestroy()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders) col.enabled = false;

        Vector3 originalScale = transform.localScale;
        Vector3 popScale = originalScale * 1.25f;

        float duration = 0.08f;
        float elapsed = 0f;

        // 1. พองตัวขึ้นนิดนึง
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, popScale, elapsed / duration);
            yield return null;
        }

        // 2. ยุบเล็กลงจนเป็น 0
        elapsed = 0f;
        duration = 0.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(popScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        // 3. บันทึกและส่งเข้า Inventory
        if (instanceData == null)
        {
            instanceData = new FurnitureInstanceData(itemData, currentLevel);
        }
        else
        {
            instanceData.currentLevel = currentLevel;
            instanceData.isUnlocked = (currentLevel >= 1);
            instanceData.isUpgraded = (currentLevel >= 2);
        }

        if (FurnitureInventory.Instance != null)
        {
            FurnitureInventory.Instance.AddInstance(instanceData);
            Debug.Log($"[PlacedFurniture] Store {gameObject.name} (Level: {currentLevel})in Inventory");

            // 🌟 [เพิ่มตรงนี้] แสดง UI Notification ตอนเก็บโต๊ะเข้าคลัง
            if (UINotificationManager.Instance != null)
            {
                string tableName = itemData != null ? itemData.name : gameObject.name;
                // ตัดคำว่า "(Clone)" ออกจากชื่อ GameObject ถ้ามี เพื่อความสวยงาม
                tableName = tableName.Replace("(Clone)", "").Trim();
                UINotificationManager.Instance.ShowNotification($"Store {tableName} Inventory");
            }

            if (FurnitureEditUI.Instance != null)
            {
                FurnitureEditUI.Instance.RefreshUI();
            }
        }

        // 🟢 เก็บเข้าคลังแล้วต้องอัปเดต NavMesh ให้แมวเดินผ่านพื้นที่เดิมได้ (FurnitureManager จะรอให้ Destroy เสร็จก่อนค่อย Bake)
        if (FurnitureManager.Instance != null) FurnitureManager.Instance.RequestNavMeshRebake();

        Destroy(gameObject);
    }
}