using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CustomizableFurniture : MonoBehaviour
{
    public string furnitureID; // ID สำหรับ Save/Load
    public int refundPrice = 50;

    void OnMouseDown()
    {
        // หากคลิกที่เฟอร์นิเจอร์ขณะอยู่ในโหมดแก้ไข
        if (FurniturePlacementManager.Instance != null)
        {
            // ตัวอย่าง: คลิกเพื่อย้ายตำแหน่ง
            // FurniturePlacementManager.Instance.StartPlacement(gameObjectPrefab);
            // Destroy(gameObject);
        }
    }
}

[System.Serializable]
public class FurnitureData
{
    public string furnitureID;
    public float posX, posY, posZ;
    public float rotZ;
}

[System.Serializable]
public class PlacementSaveData
{
    public List<FurnitureData> placedFurnitures = new List<FurnitureData>();
}

public class FurnitureSaveSystem : MonoBehaviour
{
    private string savePath;

    void Awake()
    {
        savePath = Application.persistentDataPath + "/furniture_save.json";
    }

    public void SaveLayout()
    {
        PlacementSaveData saveData = new PlacementSaveData();
        CustomizableFurniture[] allFurnitures = FindObjectsOfType<CustomizableFurniture>();

        foreach (var f in allFurnitures)
        {
            FurnitureData data = new FurnitureData
            {
                furnitureID = f.furnitureID,
                posX = f.transform.position.x,
                posY = f.transform.position.y,
                posZ = f.transform.position.z,
                rotZ = f.transform.rotation.eulerAngles.z
            };
            saveData.placedFurnitures.Add(data);
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("✅ บันทึกตำแหน่งเฟอร์นิเจอร์เรียบร้อย");
    }

    public void LoadLayout()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        PlacementSaveData saveData = JsonUtility.FromJson<PlacementSaveData>(json);

        foreach (var f in FindObjectsOfType<CustomizableFurniture>())
            Destroy(f.gameObject);

        foreach (var data in saveData.placedFurnitures)
        {
            Vector3 pos = new Vector3(data.posX, data.posY, data.posZ);
            Quaternion rot = Quaternion.Euler(0, 0, data.rotZ);

            GameObject prefab = Resources.Load<GameObject>("Furnitures/" + data.furnitureID);
            if (prefab != null)
            {
                Instantiate(prefab, pos, rot);
            }
        }

        FurniturePlacementManager.Instance?.RebakeNavMesh();
    }
}