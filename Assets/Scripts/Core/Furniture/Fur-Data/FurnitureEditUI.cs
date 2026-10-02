using UnityEngine;

public class FurnitureEditUI : MonoBehaviour
{
    public static FurnitureEditUI Instance;

    public GameObject editModePanel;
    public Transform gridContainer; // ลาก Content มาใส่
    public GameObject slotPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (editModePanel != null)
        {
            editModePanel.SetActive(false);
        }
    }

    public void ToggleEditUI(bool active)
    {
        if (editModePanel != null)
        {
            editModePanel.SetActive(active);
            if (active)
            {
                RefreshUI();
            }
        }
    }

    public void RefreshUI()
    {
        if (gridContainer == null || slotPrefab == null)
        {
            Debug.LogWarning("FurnitureEditUI: ยังไม่ได้ใส่ gridContainer หรือ slotPrefab ใน Inspector!");
            return;
        }

        // 1. เคลียร์ Slot เก่าออก
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. ดึงรายการจาก storedInstances ใน FurnitureInventory มาสร้าง Slot
        if (FurnitureInventory.Instance != null)
        {
            foreach (var instance in FurnitureInventory.Instance.storedInstances)
            {
                if (instance != null && instance.itemData != null)
                {
                    GameObject slotObj = Instantiate(slotPrefab, gridContainer);
                    slotObj.transform.localScale = Vector3.one;

                    FurnitureSlotUI slot = slotObj.GetComponent<FurnitureSlotUI>();
                    if (slot != null)
                    {
                        // ส่ง Instance Data ไปให้ Slot UI
                        slot.SetupSlot(instance);
                    }
                }
            }
        }
    }
}