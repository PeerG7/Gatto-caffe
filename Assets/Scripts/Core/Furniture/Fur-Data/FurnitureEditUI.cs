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

    [Header("Button Guide Panel Mechanics (Top Center)")]
    public GameObject buttonGuidePanel;        // Panel กลุ่มรูปภาพและข้อความแนะนำปุ่มกด (วางไว้ตรงกลางด้านบน)
    public RectTransform buttonGuideRect;      // RectTransform ของ Button Guide Panel
    public float guideShownPosY = -30f;        // ตำแหน่ง Y ตอนโผล่มาแสดง
    public float guideHiddenPosY = 150f;       // ตำแหน่ง Y ตอนสไลด์ขึ้นไปซ่อนพ้นจอข้างบน

    [Header("Y Position Settings")]
    public float normalPosY = -406f;           // ตำแหน่ง Y ปกติ (เปิดแสดงคลังเต็ม)
    public float minimisedPosY = -640f;        // ตำแหน่ง Y ตอนถือวางเฟอร์นิเจอร์
    public float moveSpeed = 12f;              // ความเร็วในการเลื่อน Smooth (ยิ่งมากยิ่งเร็ว)

    private float targetPosY;
    private float targetGuidePosY;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (uiPanelRect == null && editUIPanel != null)
        {
            uiPanelRect = editUIPanel.GetComponent<RectTransform>();
        }

        if (buttonGuideRect == null && buttonGuidePanel != null)
        {
            buttonGuideRect = buttonGuidePanel.GetComponent<RectTransform>();
        }

        targetPosY = normalPosY;
        targetGuidePosY = guideHiddenPosY;
    }

    private void Update()
    {
        // 1. เลื่อนตำแหน่ง Main Inventory UI เข้าหา targetPosY อย่าง Smooth
        if (uiPanelRect != null)
        {
            Vector2 currentPos = uiPanelRect.anchoredPosition;
            if (Mathf.Abs(currentPos.y - targetPosY) > 0.01f)
            {
                currentPos.y = Mathf.Lerp(currentPos.y, targetPosY, Time.deltaTime * moveSpeed);
                uiPanelRect.anchoredPosition = currentPos;
            }
        }

        // 2. เลื่อนตำแหน่ง Button Guide UI เข้าหา targetGuidePosY อย่าง Smooth (GameObject ยังคง active ตลอดเวลา)
        if (buttonGuideRect != null)
        {
            Vector2 currentGuidePos = buttonGuideRect.anchoredPosition;
            if (Mathf.Abs(currentGuidePos.y - targetGuidePosY) > 0.01f)
            {
                currentGuidePos.y = Mathf.Lerp(currentGuidePos.y, targetGuidePosY, Time.deltaTime * moveSpeed);
                buttonGuideRect.anchoredPosition = currentGuidePos;
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

        // สไลด์ Button Guide ลงมาโชว์เมื่อเปิดหน้าต่าง Edit Mode
        SetButtonGuideActive(active);

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

    /// <summary>
    /// สั่งสไลด์แผงแนะนำปุ่มกดขึ้นหรือลง (โดย GameObject จะยังคง Active อยู่ตลอดเวลา)
    /// </summary>
    public void SetButtonGuideActive(bool show)
    {
        if (buttonGuidePanel == null) return;

        // ให้มั่นใจว่าตัว Panel เปิด Active ไว้เสมอไม่เคยปิด
        if (!buttonGuidePanel.activeSelf)
            buttonGuidePanel.SetActive(true);

        if (show)
        {
            targetGuidePosY = guideShownPosY; // สไลด์ลงมาโชว์ในจอ
        }
        else
        {
            targetGuidePosY = guideHiddenPosY; // สไลด์ขึ้นไปซ่อนพ้นจอ
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
            List<GroupedFurnitureData> groupedItems = FurnitureInventory.Instance.GetGroupedInventory();

            foreach (var group in groupedItems)
            {
                if (group == null || group.itemData == null || group.instances.Count == 0) continue;

                GameObject btnObj = Instantiate(furnitureItemButtonPrefab, container);

                FurnitureSlotUI slotUI = btnObj.GetComponent<FurnitureSlotUI>();
                if (slotUI != null)
                {
                    slotUI.SetupSlot(group);
                }

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

        // 🌟 สั่งให้สไลด์ Button Guide หนีขึ้นไปซ่อนพ้นจอ เพื่อเพิ่มพื้นที่ให้ผู้เล่นวางเฟอร์นิเจอร์
        SetButtonGuideActive(false);

        FurnitureInstanceData instanceToPlace = group.instances[0];

        if (FurnitureManager.Instance != null && instanceToPlace != null)
        {
            FurnitureManager.Instance.StartPlacingFurnitureInstance(instanceToPlace);
        }
    }

    public void SetPanelPositionY(float targetY)
    {
        targetPosY = targetY;
    }

    public void RestorePanelPosition()
    {
        SetPanelPositionY(normalPosY);

        // 🌟 สั่งให้สไลด์ Button Guide เลื่อนกลับลงมาโชว์เมื่อกลับมาเลือกไอเท็มในหน้าต่างปกติ
        SetButtonGuideActive(true);
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