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
        // 🟢 เลื่อนตำแหน่ง UI เข้าหา targetPosY อย่าง Smooth
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
            List<FurnitureInstanceData> items = FurnitureInventory.Instance.storedInstances;

            foreach (var item in items)
            {
                if (item == null || item.itemData == null) continue;

                GameObject btnObj = Instantiate(furnitureItemButtonPrefab, container);

                FurnitureSlotUI slotUI = btnObj.GetComponent<FurnitureSlotUI>();
                if (slotUI != null)
                {
                    slotUI.SetupSlot(item);
                }

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        OnSelectFurnitureItem(item);
                    });
                }
            }
        }
    }

    public void OnSelectFurnitureItem(FurnitureInstanceData instance)
    {
        // ย่อ Panel ลงไปที่ Pos Y = -640 แบบ Smooth
        SetPanelPositionY(minimisedPosY);
        ScrollToBottom();

        if (FurnitureManager.Instance != null)
        {
            FurnitureManager.Instance.StartPlacingFurnitureInstance(instance);
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