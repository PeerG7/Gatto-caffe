using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
#if TMPro
using TMPro;
#endif

/// <summary>
/// UpgradeComputer — ระบบคอมพิวเตอร์จัดการร้าน
/// - ปุ่ม Upgrade All: ตรวจสอบและอัปเกรดโต๊ะทั้งหมดที่พร้อมอัปเกรดพร้อมกันในคลิกเดียว
/// - ปุ่ม Skin: ปลดล็อกและเปลี่ยน Skin ให้โต๊ะที่ผ่านการอัปเกรดแล้วทุกตัวพร้อมกัน
/// </summary>
public class UpgradeComputer : MonoBehaviour
{
    [Header("Canvas & Windows")]
    [Tooltip("Panel หรือ Canvas หน้าจอคอมพิวเตอร์")]
    public GameObject computerCanvas;
    [Tooltip("ปุ่มกากบาท หรือปุ่มสำหรับปิดหน้าจอคอม")]
    public Button closeButton;

    [Header("Upgrade All Section")]
    [Tooltip("ปุ่มสำหรับกด Upgrade โต๊ะทั้งหมดพร้อมกัน")]
    public Button upgradeAllButton;
    [Tooltip("Text แสดงสถานะ เช่น จำนวนโต๊ะที่พร้อมอัปเกรด และราคารวม")]
    public Text upgradeAllStatusLabel;

    [Header("Skin Section")]
    [Tooltip("Panel แสดงส่วนเลือก Skin (จะซ่อนถ้ายังไม่มีโต๊ะไหนอัปเกรดเลย)")]
    public GameObject skinSection;
    [Tooltip("รายการปุ่ม Skin เรียงลำดับตาม Index (0, 1, 2...)")]
    public List<SkinButtonUI> skinButtons = new List<SkinButtonUI>();

    [Header("Data / Furniture List")]
    [Tooltip("หากปล่อยว่าง ระบบจะค้นหา FurnitureObject ในฉากทั้งหมดให้อัตโนมัติ")]
    public List<FurnitureObject> tables = new List<FurnitureObject>();

    void Awake()
    {
        // ปิดหน้าจอคอมไว้ก่อนตอนเริ่มเกม
        if (computerCanvas != null)
            computerCanvas.SetActive(false);

        // ผูก Event ปุ่มกดหลัก
        if (upgradeAllButton != null)
            upgradeAllButton.onClick.AddListener(OnUpgradeAllClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseComputer);

        // ผูก Listener ให้กับปุ่ม Skin ทั้งหมดตาม Index อัตโนมัติ
        for (int i = 0; i < skinButtons.Count; i++)
        {
            int index = i; // Local copy เพื่อป้องกัน Closure capture ใน Loop
            if (skinButtons[i] != null && skinButtons[i].button != null)
            {
                skinButtons[i].button.onClick.AddListener(() => OnSkinButtonClicked(index));
            }
        }
    }

    /// <summary>
    /// เรียกใช้งานจาก PlayerInteract2D เมื่อผู้เล่นกด E ที่คอมพิวเตอร์
    /// </summary>
    public void OpenComputer()
    {
        // หากไม่มีการระบุรายการโต๊ะไว้ใน Inspector ให้สแกนหาโต๊ะทั้งหมดใน Scene
        if (tables == null || tables.Count == 0)
        {
            tables = new List<FurnitureObject>(FindObjectsOfType<FurnitureObject>());
        }

        if (computerCanvas != null)
            computerCanvas.SetActive(true);

        PlayerController2D.IsLocked = true;

        RefreshUI();
    }

    /// <summary>
    /// ปิดหน้าต่างคอมพิวเตอร์และปลดล็อกการควบคุมของผู้เล่น
    /// </summary>
    public void CloseComputer()
    {
        if (computerCanvas != null)
            computerCanvas.SetActive(false);

        PlayerController2D.IsLocked = false;
    }

    /// <summary>
    /// ทำการ Upgrade โต๊ะทุกตัวที่ปลดล็อกแล้วและยังไม่ได้อัปเกรดพร้อมกัน
    /// </summary>
    public void OnUpgradeAllClicked()
    {
        if (tables == null || tables.Count == 0) return;

        int upgradedCount = 0;
        foreach (var table in tables)
        {
            if (table == null) continue;

            // ตรวจสอบว่าโต๊ะถูกปลดล็อกแล้ว และยังไม่ได้อัปเกรด
            if (table.isUnlocked && !table.isUpgraded)
            {
                table.AttemptUpgrade();
                if (table.isUpgraded)
                    upgradedCount++;
            }
        }

        if (upgradedCount == 0 && UINotificationManager.Instance != null)
        {
            UINotificationManager.Instance.ShowNotification("ไม่มีโต๊ะที่พร้อม Upgrade ตอนนี้ (เงินไม่พอ หรือ Upgrade ครบแล้ว)");
        }

        RefreshUI();
    }

    /// <summary>
    /// เปลี่ยน Skin ของโต๊ะที่ Upgrade แล้วทั้งหมดพร้อมกัน
    /// </summary>
    public void OnSkinButtonClicked(int skinIndex)
    {
        if (tables == null || tables.Count == 0) return;

        int appliedCount = 0;
        foreach (var table in tables)
        {
            if (table == null || !table.isUpgraded) continue;
            if (table.marbleSkins == null || skinIndex < 0 || skinIndex >= table.marbleSkins.Count) continue;

            MarbleSkin skin = table.marbleSkins[skinIndex];
            if (!skin.isUnlocked)
            {
                // ถ้าสกินยังไม่ปลดล็อก ให้พยายามปลดล็อกก่อน
                bool success = table.AttemptUnlockMarbleSkin(skinIndex);
                if (success)
                {
                    table.SelectMarbleSkin(skinIndex);
                    appliedCount++;
                }
            }
            else
            {
                table.SelectMarbleSkin(skinIndex);
                appliedCount++;
            }
        }

        if (appliedCount == 0 && UINotificationManager.Instance != null)
        {
            UINotificationManager.Instance.ShowNotification("ยังไม่มีโต๊ะที่ Upgrade แล้วเลย ต้อง Upgrade ก่อนถึงจะเปลี่ยน Skin ได้");
        }

        RefreshUI();
    }

    /// <summary>
    /// อัปเดตการแสดงผลของ UI ทั้งหมด
    /// </summary>
    public void RefreshUI()
    {
        if (tables == null) return;

        // 1. คำนวณจำนวนโต๊ะที่รออัปเกรดและราคารวม
        int pendingCount = 0;
        int totalPrice = 0;
        foreach (var t in tables)
        {
            if (t != null && t.isUnlocked && !t.isUpgraded)
            {
                pendingCount++;
                totalPrice += t.upgradePrice;
            }
        }

        if (upgradeAllStatusLabel != null)
        {
            upgradeAllStatusLabel.text = pendingCount > 0
                ? $"Upgrade ทั้งหมด ({pendingCount} โต๊ะ) — {totalPrice} $"
                : "Upgrade ครบทุกโต๊ะแล้ว";
        }

        if (upgradeAllButton != null)
        {
            upgradeAllButton.interactable = (pendingCount > 0);
        }

        // 2. แสดงผลส่วนของ Skin
        // อ้างอิงจากโต๊ะตัวแรกที่อัปเกรดแล้ว
        FurnitureObject reference = tables.Find(t => t != null && t.isUpgraded && t.marbleSkins != null && t.marbleSkins.Count > 0);
        bool hasUpgradedTable = (reference != null);

        if (skinSection != null)
            skinSection.SetActive(hasUpgradedTable);

        for (int i = 0; i < skinButtons.Count; i++)
        {
            if (skinButtons[i] == null) continue;

            if (!hasUpgradedTable || i >= reference.marbleSkins.Count)
            {
                skinButtons[i].SetVisible(false);
                continue;
            }

            skinButtons[i].SetVisible(true);
            bool isSelected = reference.currentMarbleSkinIndex == i;
            skinButtons[i].UpdateVisual(reference.marbleSkins[i], isSelected);
        }
    }
}

/// <summary>
/// โครงสร้างข้อมูล UI สำหรับปุ่มเลือกแต่ละ Skin
/// </summary>
[System.Serializable]
public class SkinButtonUI
{
    public GameObject rootObject;      // วัตถุของปุ่มทั้งหมด (ใช้สำหรับซ่อน/แสดง)
    public Button button;              // Component ปุ่มสำหรับคลิก
    public Text nameLabel;             // ข้อความแสดงชื่อสกิน
    public Text priceLabel;            // ข้อความแสดงราคา/สถานะการเลือก
    public GameObject selectedHighlight; // ภาพกรอบที่แสดงเมื่อกำลังใช้งานสกินนี้อยู่

    public void SetVisible(bool visible)
    {
        if (rootObject != null)
            rootObject.SetActive(visible);
    }

    public void UpdateVisual(MarbleSkin skin, bool isSelected)
    {
        if (nameLabel != null)
            nameLabel.text = skin.skinName;

        if (priceLabel != null)
        {
            if (!skin.isUnlocked)
            {
                priceLabel.text = skin.unlockPrice + " $";
            }
            else
            {
                priceLabel.text = isSelected ? "กำลังใช้อยู่" : "เลือกใช้";
            }
        }

        if (selectedHighlight != null)
            selectedHighlight.SetActive(isSelected);
    }
}