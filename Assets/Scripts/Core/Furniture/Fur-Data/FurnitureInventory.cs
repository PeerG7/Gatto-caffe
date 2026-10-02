using System.Collections.Generic;
using UnityEngine;

public class FurnitureInventory : MonoBehaviour
{
    public static FurnitureInventory Instance;

    // เปลี่ยนจากเก็บแค่ Count เป็นการเก็บ List ของ Instance Data
    public List<FurnitureInstanceData> storedInstances = new List<FurnitureInstanceData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // เพิ่มไอเทมเข้าคลังพร้อม State เดิม
    public void AddInstance(FurnitureInstanceData instance)
    {
        if (instance != null)
        {
            storedInstances.Add(instance);
        }
    }

    // นำไอเทมออกจากคลัง
    public bool RemoveInstance(FurnitureInstanceData instance)
    {
        return storedInstances.Remove(instance);
    }
}