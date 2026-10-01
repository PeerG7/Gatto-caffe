using UnityEngine;

public class ShopItemButton : MonoBehaviour
{
    [Header("Furniture Data")]
    public GameObject furniturePrefab; // Prefab โต๊ะที่จะสั่งวาง

    [Header("UI Reference")]
    public GameObject computerShopCanvas; // ลาก ComputerUI มาใส่ตรงนี้

    public void OnClickBuyAndPlace()
    {
        if (furniturePrefab != null && FurniturePlacementManager.Instance != null)
        {
            // 1. ซ่อน UI หน้าคอมพิวเตอร์ก่อน
            if (computerShopCanvas != null)
            {
                computerShopCanvas.SetActive(false);
            }

            // 2. ส่ง Prefab เข้าสู่ระบบวาง
            FurniturePlacementManager.Instance.StartPlacement(furniturePrefab);
        }
    }
}