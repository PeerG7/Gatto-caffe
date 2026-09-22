using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// UpgradeComputer — กด E ที่คอมพิวเตอร์เพื่อเปิดหน้าจอ
/// เลื่อนดูโต๊ะทีละตัว (ปุ่มซ้าย/ขวา) แล้ว Upgrade หรือเปลี่ยน Skin ได้
/// ปุ่มเปลี่ยน Skin จะโชว์ก็ต่อเมื่อโต๊ะตัวที่กำลังดูอยู่ Upgrade แล้วเท่านั้น
/// </summary>
public class UpgradeComputer : MonoBehaviour
{
    [Header("Canvas")]
    public GameObject computerCanvas;

    [Header("Table Carousel (ปุ่มซ้าย/ขวา + ชื่อโต๊ะ)")]
    public Text tableNameLabel;
    public Image tablePreviewIcon; // optional — โชว์รูปตัวอย่างโต๊ะปัจจุบัน
    public Button prevTableButton;
    public Button nextTableButton;

    [Header("Upgrade Section (โชว์เมื่อโต๊ะยังไม่ Upgrade)")]
    public GameObject upgradeSection;
    public Text upgradePriceLabel;
    public Button upgradeButton;

    [Header("Skin Section (โชว์เฉพาะโต๊ะที่ Upgrade แล้วเท่านั้น)")]
    public GameObject skinSection;
    [Tooltip("ปุ่มเลือก Skin เรียงลำดับให้ตรงกับ marbleSkins ของโต๊ะแต่ละตัว")]
    public List<SkinButtonUI> skinButtons = new List<SkinButtonUI>();

    [Header("Data")]
    [Tooltip("ปล่อยว่างไว้ได้ ถ้าปล่อยว่างจะหาโต๊ะทั้งหมดในซีนให้อัตโนมัติตอนเปิดหน้าจอ")]
    public List<FurnitureObject> tables = new List<FurnitureObject>();
    private int currentTableIndex = 0;

    FurnitureObject CurrentTable =>
        (tables != null && tables.Count > 0 && currentTableIndex >= 0 && currentTableIndex < tables.Count)
            ? tables[currentTableIndex] : null;

    void Awake()
    {
        if (computerCanvas != null) computerCanvas.SetActive(false);

        if (prevTableButton != null) prevTableButton.onClick.AddListener(PrevTable);
        if (nextTableButton != null) nextTableButton.onClick.AddListener(NextTable);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
    }

    /// <summary>เรียกจาก PlayerInteract2D ตอนกด E ที่คอมพิวเตอร์</summary>
    public void OpenComputer()
    {
        if (tables == null || tables.Count == 0)
            tables = new List<FurnitureObject>(FindObjectsOfType<FurnitureObject>());

        if (tables.Count == 0)
        {
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("ยังไม่มีโต๊ะในร้านเลย");
            return;
        }

        currentTableIndex = Mathf.Clamp(currentTableIndex, 0, tables.Count - 1);

        if (computerCanvas != null) computerCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;

        RefreshUI();
    }

    /// <summary>ผูกกับปุ่ม Close/Exit บนหน้าจอคอมพิวเตอร์</summary>
    public void CloseComputer()
    {
        if (computerCanvas != null) computerCanvas.SetActive(false);
        PlayerController2D.IsLocked = false;
    }

    void PrevTable()
    {
        if (tables == null || tables.Count == 0) return;
        currentTableIndex = (currentTableIndex - 1 + tables.Count) % tables.Count;
        RefreshUI();
    }

    void NextTable()
    {
        if (tables == null || tables.Count == 0) return;
        currentTableIndex = (currentTableIndex + 1) % tables.Count;
        RefreshUI();
    }

    void OnUpgradeClicked()
    {
        FurnitureObject table = CurrentTable;
        if (table == null) return;

        table.AttemptUpgrade();
        RefreshUI();
    }

    /// <summary>ผูกกับ OnClick ของปุ่ม Skin แต่ละอันใน Inspector โดยใส่ index ให้ตรงกับ skinButtons</summary>
    public void OnSkinButtonClicked(int skinIndex)
    {
        FurnitureObject table = CurrentTable;
        if (table == null) return;
        if (skinIndex < 0 || skinIndex >= table.marbleSkins.Count) return;

        MarbleSkin skin = table.marbleSkins[skinIndex];

        if (!skin.isUnlocked)
        {
            bool success = table.AttemptUnlockMarbleSkin(skinIndex);
            if (success) table.SelectMarbleSkin(skinIndex);
        }
        else
        {
            table.SelectMarbleSkin(skinIndex);
        }

        RefreshUI();
    }

    /// <summary>อัปเดตหน้าจอทั้งหมดให้ตรงกับโต๊ะที่กำลังดูอยู่</summary>
    void RefreshUI()
    {
        FurnitureObject table = CurrentTable;
        if (table == null) return;

        if (tableNameLabel != null) tableNameLabel.text = table.gameObject.name;

        // ── Upgrade Section: โชว์เฉพาะตอนยังไม่ Upgrade ──────────────
        bool showUpgrade = table.isUnlocked && !table.isUpgraded;
        if (upgradeSection != null) upgradeSection.SetActive(showUpgrade);
        if (upgradePriceLabel != null) upgradePriceLabel.text = table.upgradePrice + " $";

        // ── Skin Section: โชว์เฉพาะตอน Upgrade แล้วเท่านั้น ──────────
        bool showSkins = table.isUnlocked && table.isUpgraded;
        if (skinSection != null) skinSection.SetActive(showSkins);

        for (int i = 0; i < skinButtons.Count; i++)
        {
            if (!showSkins || i >= table.marbleSkins.Count)
            {
                skinButtons[i].SetVisible(false);
                continue;
            }

            skinButtons[i].SetVisible(true);
            bool isSelected = table.currentMarbleSkinIndex == i;
            skinButtons[i].UpdateVisual(table.marbleSkins[i], isSelected);
        }
    }
}

/// <summary>
/// ปุ่มเดียวสำหรับ 1 Marble Skin — ลาก Reference ของ Component บนปุ่มมาใส่ให้ครบ
/// (ถ้าโปรเจกต์ใช้ TextMeshPro ให้เปลี่ยน Text เป็น TMPro.TMP_Text ทั้งหมดในไฟล์นี้)
/// </summary>
[System.Serializable]
public class SkinButtonUI
{
    public GameObject rootObject;   // ตัว GameObject ของปุ่มทั้งอัน (สำหรับซ่อน/โชว์)
    public Button button;
    public Text nameLabel;
    public Text priceLabel;
    public GameObject selectedHighlight;

    public void SetVisible(bool visible)
    {
        if (rootObject != null) rootObject.SetActive(visible);
    }

    public void UpdateVisual(MarbleSkin skin, bool isSelected)
    {
        if (nameLabel != null) nameLabel.text = skin.skinName;

        if (priceLabel != null)
        {
            if (!skin.isUnlocked)
                priceLabel.text = skin.unlockPrice + " $";
            else
                priceLabel.text = isSelected ? "กำลังใช้อยู่" : "เลือกใช้";
        }

        if (selectedHighlight != null) selectedHighlight.SetActive(isSelected);
    }
}