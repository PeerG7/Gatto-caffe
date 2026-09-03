using UnityEngine;
using System.Collections.Generic;

public class FurnitureObject : MonoBehaviour
{
    [Header("Visuals - Locked State")]
    public GameObject lockedVisual;

    [Header("Skins")]
    [Tooltip("รายการ Skin ทั้งหมดของโต๊ะนี้ — Skin ตัวแรก (index 0) จะถือว่าปลดล็อกให้ฟรีเสมอ (Default Skin)")]
    public List<FurnitureSkin> skins = new List<FurnitureSkin>();
    public int currentSkinIndex = 0;

    [Header("Settings")]
    [Tooltip("ราคาปลดล็อกโต๊ะตัวนี้ครั้งแรก")]
    public int price = 200;
    public bool isUnlocked = false;

    void Start()
    {
        // Skin แรกถือเป็น Default ที่ปลดล็อกอยู่แล้วเสมอ
        if (skins.Count > 0) skins[0].isUnlocked = true;
        UpdateVisuals();
    }

    /// <summary>ปลดล็อกตัวโต๊ะ (เหมือนของเดิม)</summary>
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

    /// <summary>ซื้อปลดล็อก Skin ตาม index (ต้องปลดล็อกโต๊ะก่อน)</summary>
    public bool AttemptUnlockSkin(int index)
    {
        if (!isUnlocked)
        {
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("กรุณาปลดล็อกโต๊ะนี้ก่อน");
            return false;
        }

        if (index < 0 || index >= skins.Count) return false;

        FurnitureSkin skin = skins[index];
        if (skin.isUnlocked) return true; // ปลดล็อกแล้ว

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

    /// <summary>สลับไปใช้ Skin ตาม index (ต้องปลดล็อก Skin นั้นแล้ว)</summary>
    public void SelectSkin(int index)
    {
        if (!isUnlocked) return;
        if (index < 0 || index >= skins.Count) return;
        if (!skins[index].isUnlocked)
        {
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("Skin นี้ยังไม่ได้ปลดล็อก");
            return;
        }

        currentSkinIndex = index;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);

        for (int i = 0; i < skins.Count; i++)
        {
            if (skins[i].visual != null)
                skins[i].visual.SetActive(isUnlocked && i == currentSkinIndex);
        }
    }
}

[System.Serializable]
public class FurnitureSkin
{
    public string skinName = "Default";
    public GameObject visual;
    [Tooltip("ราคาปลดล็อก Skin นี้ (Skin index 0 ไม่ต้องจ่ายเพราะปลดล็อกให้ฟรี)")]
    public int unlockPrice = 0;
    [HideInInspector] public bool isUnlocked = false;
}