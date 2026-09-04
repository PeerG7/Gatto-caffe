using UnityEngine;

public class FurnitureObject : MonoBehaviour
{
    [Header("Visuals - Table")]
    [Tooltip("เงาที่เห็นตอนยังไม่ปลดล็อก")]
    public GameObject lockedVisual;
    [Tooltip("โต๊ะไม้ — โผล่มาแทนเงาทันทีที่ปลดล็อก (ซื้อครั้งแรก)")]
    public GameObject woodVisual;
    [Tooltip("โต๊ะหินอ่อน — โผล่มาแทนโต๊ะไม้หลัง Upgrade สำเร็จ")]
    public GameObject marbleVisual;

    [Header("Visuals - Chair (Optional)")]
    [Tooltip("เก้าอี้ลายไม้ — คู่กับ woodVisual (ถ้าไม่มีเก้าอี้แยก ปล่อยว่างไว้ได้)")]
    public GameObject woodChairVisual;
    [Tooltip("เก้าอี้ลายหินอ่อน — คู่กับ marbleVisual (ถ้าไม่มีเก้าอี้แยก ปล่อยว่างไว้ได้)")]
    public GameObject marbleChairVisual;

    [Header("Unlock Settings (เงา -> โต๊ะไม้)")]
    public int price = 200;
    public bool isUnlocked = false;

    [Header("Upgrade Settings (โต๊ะไม้ -> โต๊ะหินอ่อน)")]
    public int upgradePrice = 500;
    public bool isUpgraded = false;
    [Tooltip("เมื่อ Upgrade เป็นโต๊ะหินอ่อนสำเร็จ จะเพิ่มโอกาส Spawn แมว VIP ทั้งร้านขึ้นกี่ % (บวกเข้ากับ vipSpawnChance ของ NPCSpawner)")]
    public int vipChanceBonusOnUpgrade = 15;

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

    /// <summary>โต๊ะไม้ -> โต๊ะหินอ่อน (ต้องปลดล็อกโต๊ะไม้ก่อน) — สำเร็จแล้วเพิ่มโอกาส Spawn แมว VIP ทั้งร้าน</summary>
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

    void UpdateVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (woodVisual != null) woodVisual.SetActive(isUnlocked && !isUpgraded);
        if (marbleVisual != null) marbleVisual.SetActive(isUnlocked && isUpgraded);

        // ✅ เก้าอี้เปลี่ยนลายไปพร้อมกับโต๊ะ (ถ้ามีลากไว้)
        if (woodChairVisual != null) woodChairVisual.SetActive(isUnlocked && !isUpgraded);
        if (marbleChairVisual != null) marbleChairVisual.SetActive(isUnlocked && isUpgraded);
    }
}