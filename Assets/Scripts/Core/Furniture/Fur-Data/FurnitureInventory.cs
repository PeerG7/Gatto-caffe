using System.Collections.Generic;
using UnityEngine;

// 🌟 สร้างโครงสร้างสำหรับเก็บข้อมูลที่ Stack แล้ว (แก้ไขให้ใช้ FurnitureItemData ให้ตรงกับโปรเจกต์)
public class GroupedFurnitureData
{
    public FurnitureItemData itemData;
    public int level;
    public List<FurnitureInstanceData> instances = new List<FurnitureInstanceData>(); // เก็บรายการจริงข้างในสำหรับการหยิบไปวาง
    public int Count => instances.Count;
}

public class FurnitureInventory : MonoBehaviour
{
    public static FurnitureInventory Instance;

    public List<FurnitureInstanceData> storedInstances = new List<FurnitureInstanceData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddInstance(FurnitureInstanceData instance)
    {
        if (instance != null)
        {
            storedInstances.Add(instance);
        }
    }

    public bool RemoveInstance(FurnitureInstanceData instance)
    {
        return storedInstances.Remove(instance);
    }

    // 🌟 จัดกลุ่มไอเทมโดยอิงจาก itemData และ currentLevel เพื่อให้ Stack รวมกันได้ถูกต้อง
    public List<GroupedFurnitureData> GetGroupedInventory()
    {
        Dictionary<string, GroupedFurnitureData> groupDict = new Dictionary<string, GroupedFurnitureData>();

        foreach (var inst in storedInstances)
        {
            if (inst == null || inst.itemData == null) continue;

            // ใช้ id หรือชื่อจาก itemData และ currentLevel เป็นกุญแจหลักในการรวมกลุ่ม
            string itemId = !string.IsNullOrEmpty(inst.itemData.id) ? inst.itemData.id : inst.itemData.name;
            string key = $"{itemId}_{inst.currentLevel}";

            if (!groupDict.ContainsKey(key))
            {
                groupDict[key] = new GroupedFurnitureData
                {
                    itemData = inst.itemData,
                    level = inst.currentLevel
                };
            }

            groupDict[key].instances.Add(inst);
        }

        return new List<GroupedFurnitureData>(groupDict.Values);
    }
}