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

        if (iconImage != null) iconImage.sprite = instance.itemData.icon;

        // แสดงชื่อ + Level ล่าสุดที่เคย Upgrade ไว้
        if (nameText != null) nameText.text = $"{instance.itemData.furnitureName} (Lv.{instance.currentLevel})";

        if (countText != null) countText.text = "x1"; // แต่ละ Slot จะถือ 1 Unique Instance

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