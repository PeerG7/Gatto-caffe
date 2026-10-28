using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FurnitureEditUI : MonoBehaviour
{
    public static FurnitureEditUI Instance;

    [Header("UI Canvas & Panels")]
    public GameObject editUIPanel;             // Panel หลักของ Edit Mode
    public Transform container;                // Content Transform ใน ScrollView
    public GameObject furnitureItemButtonPrefab; // Prefab ปุ่มไอเท็มในคลัง

    [Header("Scroll & Position Mechanics")]
    public ScrollRect furnitureScrollRect;     // ลาก ScrollRect ของ UI เข้ามาใส่
    public RectTransform uiPanelRect;          // ลาก RectTransform ของ Scroll View / Panel เข้ามาใส่

    [Header("Y Position Settings")]
    public float normalPosY = -406f;           // ตำแหน่ง Y ปกติ (เปิดแสดงคลังเต็ม)
    public float minimisedPosY = -640f;        // ตำแหน่ง Y ตอนถือวางเฟอร์นิเจอร์
    public float moveSpeed = 12f;              // ความเร็วในการเลื่อน Smooth (ยิ่งมากยิ่งเร็ว)

    private float targetPosY;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (uiPanelRect == null && editUIPanel != null)
        {
            uiPanelRect = editUIPanel.GetComponent<RectTransform>();
        }

        targetPosY = normalPosY;
    }

    private void Update()
    {
        // เลื่อนตำแหน่ง UI เข้าหา targetPosY อย่าง Smooth
        if (uiPanelRect != null)
        {
            Vector2 currentPos = uiPanelRect.anchoredPosition;
            if (Mathf.Abs(currentPos.y - targetPosY) > 0.01f)
            {
                currentPos.y = Mathf.Lerp(currentPos.y, targetPosY, Time.deltaTime * moveSpeed);
                uiPanelRect.anchoredPosition = currentPos;
            }
        }
    }

    /// <summary>
    /// เปิด / ปิด UI โหมดแก้ไขร้าน
    /// </summary>
    public void ToggleEditUI(bool active)
    {
        if (editUIPanel != null)
            editUIPanel.SetActive(active);

        if (furnitureScrollRect != null)
            furnitureScrollRect.gameObject.SetActive(active);

        if (active)
        {
            RefreshUI();

            // วางตำแหน่งเริ่มต้นให้เปิดขึ้นมาจากข้างล่างแบบ smooth
            if (uiPanelRect != null)
            {
                Vector2 pos = uiPanelRect.anchoredPosition;
                pos.y = minimisedPosY;
                uiPanelRect.anchoredPosition = pos;
            }

            RestorePanelPosition(); // ค่อยๆ สไลด์ขึ้นมาที่ -406
            ScrollToTop();
        }
    }

    public void RefreshUI()
    {
        if (container == null || furnitureItemButtonPrefab == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        if (FurnitureInventory.Instance != null)
        {
            // 🌟 เปลี่ยนมาดึงข้อมูลแบบจัดกลุ่ม (Grouped Inventory) ที่ทำการ Stack แล้ว
            List<GroupedFurnitureData> groupedItems = FurnitureInventory.Instance.GetGroupedInventory();

            foreach (var group in groupedItems)
            {
                if (group == null || group.itemData == null || group.instances.Count == 0) continue;

                GameObject btnObj = Instantiate(furnitureItemButtonPrefab, container);

                FurnitureSlotUI slotUI = btnObj.GetComponent<FurnitureSlotUI>();
                if (slotUI != null)
                {
                    // ส่งข้อมูลกลุ่ม (GroupedFurnitureData) เข้าไปตั้งค่า Slot และปุ่มกด
                    slotUI.SetupSlot(group);
                }

                // หากใน FurnitureSlotUI มีการผูก listener ไว้แล้ว สามารถปล่อยผ่าน หรือกำหนด OnClick เพิ่มเติมที่นี่ได้
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        OnSelectGroupedFurniture(group);
                    });
                }
            }
        }
    }

    public void OnSelectGroupedFurniture(GroupedFurnitureData group)
    {
        if (group == null || group.instances.Count == 0) return;

        // ย่อ Panel ลงไปที่ Pos Y = -640 แบบ Smooth
        SetPanelPositionY(minimisedPosY);
        ScrollToBottom();

        // หยิบชิ้นแรกสุดในกลุ่มที่ถูก Stack มาเริ่มวาง
        FurnitureInstanceData instanceToPlace = group.instances[0];

        if (FurnitureManager.Instance != null && instanceToPlace != null)
        {
            FurnitureManager.Instance.StartPlacingFurnitureInstance(instanceToPlace);
        }
    }

    /// <summary>
    /// กำหนดค่าเป้าหมาย Y ที่ต้องการให้ UI เลื่อนไป
    /// </summary>
    public void SetPanelPositionY(float targetY)
    {
        targetPosY = targetY;
    }

    /// <summary>
    /// สั่งยกแผง UI กลับขึ้นมาที่ตำแหน่งปกติ (-406)
    /// </summary>
    public void RestorePanelPosition()
    {
        SetPanelPositionY(normalPosY);
    }

    public void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomRoutine());
    }

    private IEnumerator ScrollToBottomRoutine()
    {
        yield return new WaitForEndOfFrame();

        if (furnitureScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            furnitureScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ScrollToTop()
    {
        if (furnitureScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            furnitureScrollRect.verticalNormalizedPosition = 1f;
        }
    }
}