using UnityEngine;
using System.Collections.Generic;

public class FurnitureObject : MonoBehaviour
{
    [Header("Visuals - Table")]
    [Tooltip("เงาที่เห็นตอนยังไม่ปลดล็อก")]
    public GameObject lockedVisual;
    [Tooltip("โต๊ะไม้ — โผล่มาแทนเงาทันทีที่ปลดล็อก (ซื้อครั้งแรก)")]
    public GameObject woodVisual;

    [Header("Visuals - Chair (Optional)")]
    [Tooltip("เก้าอี้ลายไม้ — คู่กับ woodVisual (ถ้าไม่มีเก้าอี้แยก ปล่อยว่างไว้ได้)")]
    public GameObject woodChairVisual;

    [Header("Unlock Settings (เงา -> โต๊ะไม้)")]
    public int price = 200;
    public bool isUnlocked = false;

    [Header("Upgrade Settings (โต๊ะไม้ -> โต๊ะหินอ่อน)")]
    public int upgradePrice = 500;
    public bool isUpgraded = false;
    [Tooltip("เมื่อ Upgrade เป็นโต๊ะหินอ่อนสำเร็จ จะเพิ่มโอกาส Spawn แมว VIP ทั้งร้านขึ้นกี่ % (บวกเข้ากับ vipSpawnChance ของ NPCSpawner)")]
    public int vipChanceBonusOnUpgrade = 15;

    [Header("Marble Skins (เลือกได้เฉพาะโต๊ะที่ Upgrade แล้วเท่านั้น)")]
    [Tooltip("รายการลายหินอ่อนที่เลือกได้หลัง Upgrade — Skin ตัวแรก (index 0) จะปลดล็อกให้ฟรีทันทีที่ Upgrade สำเร็จ")]
    public List<MarbleSkin> marbleSkins = new List<MarbleSkin>();
    public int currentMarbleSkinIndex = 0;

    void Start() => UpdateVisuals();

    /// <summary>เงา -> โต๊ะไม้ (ปลดล็อกครั้งแรก)</summary>
    public void AttemptUnlock()
    {
        if (isUnlocked) return;

        // 🔥 FIX: เช็คก่อนว่า CurrencyManager.Instance มีตัวตนหรือไม่
        if (CurrencyManager.Instance != null)
        {
            if (CurrencyManager.Instance.TrySpendMoney(price))
            {
                isUnlocked = true;
                UpdateVisuals();
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("Furniture unlock successful!");
            }
            else
            {
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("Not Enough Money! Need " + price + " $");
            }
        }
        else
        {
            Debug.LogError("หา CurrencyManager ไม่เจอใน Scene! กรุณาวาง Script ไว้บน GameObject ด้วย");
        }
    }

    /// <summary>โต๊ะไม้ -> โต๊ะหินอ่อน (ต้องปลดล็อกโต๊ะไม้ก่อน) — สำเร็จแล้วเพิ่มโอกาส Spawn แมว VIP ทั้งร้าน + ปลดล็อก Skin แรกให้ฟรี</summary>
    public void AttemptUpgrade()
    {
        if (!isUnlocked)
        {
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("ต้องซื้อโต๊ะนี้ก่อนถึงจะ Upgrade ได้");
            return;
        }

        if (isUpgraded) return; // Upgrade ไปแล้ว

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("หา CurrencyManager ไม่เจอใน Scene! กรุณาวาง Script ไว้บน GameObject ด้วย");
            return;
        }

        if (CurrencyManager.Instance.TrySpendMoney(upgradePrice))
        {
            isUpgraded = true;

            // ✅ Skin แรกปลดล็อกให้ฟรีทันทีที่ Upgrade สำเร็จ (มีอย่างน้อย 1 ลายให้ใช้เสมอ)
            if (marbleSkins.Count > 0)
            {
                marbleSkins[0].isUnlocked = true;
                currentMarbleSkinIndex = 0;
            }

            UpdateVisuals();

            // ✅ Upgrade โต๊ะสำเร็จ -> เพิ่มโอกาส Spawn แมว VIP ทั้งร้าน
            if (NPCSpawner.Instance != null)
                NPCSpawner.Instance.IncreaseVIPChance(vipChanceBonusOnUpgrade);
            else
                Debug.LogWarning("หา NPCSpawner.Instance ไม่เจอ — โอกาส VIP จะไม่ถูกเพิ่ม");

            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("Upgrade to Marble Table successful!");
        }
        else
        {
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("Not Enough Money! Need " + upgradePrice + " $");
        }
    }

    /// <summary>ซื้อปลดล็อก Marble Skin ตาม index — ทำได้เฉพาะโต๊ะที่ Upgrade แล้วเท่านั้น</summary>
    public bool AttemptUnlockMarbleSkin(int index)
    {
        if (!isUpgraded)
        {
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("ต้อง Upgrade โต๊ะนี้ก่อนถึงจะเปลี่ยน Skin ได้");
            return false;
        }

        if (index < 0 || index >= marbleSkins.Count) return false;

        MarbleSkin skin = marbleSkins[index];
        if (skin.isUnlocked) return true;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("หา CurrencyManager ไม่เจอใน Scene!");
            return false;
        }

        if (CurrencyManager.Instance.TrySpendMoney(skin.unlockPrice))
        {
            skin.isUnlocked = true;
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("Unlocked skin: " + skin.skinName);
            return true;
        }
        else
        {
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("Not Enough Money! Need " + skin.unlockPrice + " $");
            return false;
        }
    }

    /// <summary>สลับไปใช้ Marble Skin ตาม index (ต้องปลดล็อก Skin นั้นแล้ว และโต๊ะต้อง Upgrade แล้ว)</summary>
    public void SelectMarbleSkin(int index)
    {
        if (!isUpgraded) return;
        if (index < 0 || index >= marbleSkins.Count) return;
        if (!marbleSkins[index].isUnlocked) return;

        currentMarbleSkinIndex = index;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (woodVisual != null) woodVisual.SetActive(isUnlocked && !isUpgraded);
        if (woodChairVisual != null) woodChairVisual.SetActive(isUnlocked && !isUpgraded);

        for (int i = 0; i < marbleSkins.Count; i++)
        {
            bool show = isUnlocked && isUpgraded && i == currentMarbleSkinIndex;
            if (marbleSkins[i].tableVisual != null) marbleSkins[i].tableVisual.SetActive(show);
            if (marbleSkins[i].chairVisual != null) marbleSkins[i].chairVisual.SetActive(show);
        }
    }
}

[System.Serializable]
public class MarbleSkin
{
    public string skinName = "Marble";
    [Tooltip("GameObject โต๊ะลายนี้")]
    public GameObject tableVisual;
    [Tooltip("GameObject เก้าอี้คู่กับลายนี้ (ถ้าไม่มีเก้าอี้แยกปล่อยว่างได้)")]
    public GameObject chairVisual;
    [Tooltip("ราคาปลดล็อก Skin นี้ (Skin index 0 ไม่ต้องจ่ายเพราะปลดล็อกให้ฟรีทันทีที่ Upgrade)")]
    public int unlockPrice = 0;
    [HideInInspector] public bool isUnlocked = false;
}