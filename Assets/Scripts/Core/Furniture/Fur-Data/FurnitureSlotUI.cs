using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FurnitureSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public Button selectButton;

    private GroupedFurnitureData currentGroupData; // 🌟 เปลี่ยนมารับข้อมูลแบบกลุ่ม

    public void SetupSlot(GroupedFurnitureData groupData)
    {
        currentGroupData = groupData;
        if (groupData == null || groupData.itemData == null) return;

        // 1. ตั้งค่ารูปภาพ Icon
        if (iconImage != null)
        {
            iconImage.sprite = groupData.itemData.icon;
        }

        // 2. จัดรูปแบบชื่อ + Level ล่าสุด
        if (nameText != null)
        {
            nameText.text = $"{groupData.itemData.furnitureName}\n<size=75%>(Lv.{groupData.level})</size>";
        }

        // 3. แสดงจำนวนชิ้นที่ Stack กันอยู่ (เช่น x3, x5)
        if (countText != null)
        {
            countText.text = $"x{groupData.Count}";
        }

        // 4. ผูกปุ่มกดเลือกวาง
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelect);
        }
    }

    void OnSelect()
    {
        // 🌟 เมื่อผู้เล่นกดเลือก จะดึง Instance ชิ้นแรกสุดในกองที่ถูก Stack มาใช้งานวาง
        if (FurnitureManager.Instance != null && currentGroupData != null && currentGroupData.instances.Count > 0)
        {
            FurnitureInstanceData instanceToPlace = currentGroupData.instances[0];
            FurnitureManager.Instance.StartPlacingFurnitureInstance(instanceToPlace);
        }
    }
}