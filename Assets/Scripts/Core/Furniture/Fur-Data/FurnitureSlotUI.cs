using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FurnitureSlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI countText;
    public Button selectButton;

    private FurnitureInstanceData currentInstance;

    public void SetupSlot(FurnitureInstanceData instance)
    {
        currentInstance = instance;
        if (instance == null || instance.itemData == null) return;

        // 1. ตั้งค่ารูปภาพ Icon
        if (iconImage != null)
        {
            iconImage.sprite = instance.itemData.icon;
        }

        // 2. จัดรูปแบบชื่อ + Level ล่าสุด (ขึ้นบรรทัดใหม่ และย่อขนาด Lv. ให้เล็กลง)
        if (nameText != null)
        {
            nameText.text = $"{instance.itemData.furnitureName}\n<size=75%>(Lv.{instance.currentLevel})</size>";
        }

        // 3. แสดงจำนวนชิ้น
        if (countText != null)
        {
            countText.text = "x1"; // แต่ละ Slot ถือ 1 Unique Instance
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
        // เมื่อกดเลือกเฟอร์นิเจอร์ ให้ส่ง Instance ตัวที่มี State เดิมไปเริ่มโหมดวาง
        if (FurnitureManager.Instance != null && currentInstance != null)
        {
            FurnitureManager.Instance.StartPlacingFurnitureInstance(currentInstance);
        }
    }
}