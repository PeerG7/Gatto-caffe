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
            // Level 0 : (isUnlocked = false, isUpgraded = false) -> โต๊ะเงา
            // Level 1 : (isUnlocked = true,  isUpgraded = false) -> โต๊ะไม้
            // Level 2 : (isUnlocked = true,  isUpgraded = true)  -> โต๊ะหินอ่อน
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

    public void PickupFurniture()
    {
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
            // 1. เพิ่มไอเทมเข้า Inventory
            FurnitureInventory.Instance.AddInstance(instanceData);
            Debug.Log($"[PlacedFurniture] เก็บ {gameObject.name} (Level: {currentLevel}) เข้า Inventory");

            // 2. สั่งรีเฟรช UI ทันที เพื่อให้ช่องไอเทมอัปเดตโดยไม่ต้องกด B ปิด/เปิด ใหม่
            if (FurnitureEditUI.Instance != null)
            {
                FurnitureEditUI.Instance.RefreshUI();
            }
        }

        Destroy(gameObject);
    }
}