using UnityEngine;

public class ShopItemButton : MonoBehaviour
{
    [Header("Item Data To Buy")]
    public FurnitureItemData itemData;

    // ฟังก์ชันซื้อแล้วสร้าง Instance ใหม่ส่งเข้าคลัง
    public void OnClickBuyToInventory()
    {
        if (itemData == null)
        {
            Debug.LogWarning("ยังไม่ได้ใส่ FurnitureItemData ใน Inspector!");
            return;
        }

        // เช็กและหักเงินผ่าน CurrencyManager
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendMoney(itemData.price))
        {
            if (FurnitureInventory.Instance != null)
            {
                // สร้าง Instance ใหม่เริ่มต้นที่ Level 1
                FurnitureInstanceData newInstance = new FurnitureInstanceData(itemData, 1, false);
                FurnitureInventory.Instance.AddInstance(newInstance);
                Debug.Log($"ซื้อ {itemData.furnitureName} สำเร็จ! เข้าคลังเรียบร้อย");
            }
        }
        else
        {
            Debug.Log("เงินไม่พอซื้อ!");
        }
    }
}