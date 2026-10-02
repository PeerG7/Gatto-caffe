using UnityEngine;

public class FurnitureUpgrade : MonoBehaviour
{
    private PlacedFurniture placedFurniture;

    private void Awake()
    {
        placedFurniture = GetComponent<PlacedFurniture>();
    }

    // ฟังก์ชันเรียกทำงานตอนกด E หรือโต้ตอบเพื่อ Upgrade
    public void DoUpgrade()
    {
        if (placedFurniture == null) return;

        // ถ้ายังไม่มี instanceData ให้บังคับสร้างก่อน
        if (placedFurniture.instanceData == null && placedFurniture.defaultItemData != null)
        {
            placedFurniture.instanceData = new FurnitureInstanceData(placedFurniture.defaultItemData, placedFurniture.currentLevel, false);
        }

        // เรียกใช้ฟังก์ชันอัปเกรดหลักของ PlacedFurniture
        placedFurniture.UpgradeFurniture();

        Debug.Log($"[Upgrade Success] {gameObject.name} อัปเกรดเป็น Level {placedFurniture.currentLevel}");
    }
}