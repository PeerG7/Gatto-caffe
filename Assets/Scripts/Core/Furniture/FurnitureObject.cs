using UnityEngine;

public class FurnitureObject : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject lockedVisual;   // เงาดำหรือผ้าคลุม
    public GameObject unlockedVisual; // เฟอร์นิเจอร์ตัวจริง

    [Header("Settings")]
    public int price = 200;
    public bool isUnlocked = false;

    void Start() => UpdateVisuals();

    public void AttemptUnlock()
    {
        if (isUnlocked) return;

        if (CurrencyManager.Instance.TrySpendMoney(price))
        {
            isUnlocked = true;
            UpdateVisuals();
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("Furniture unlock successful !");
        }
        else
        {
            // แจ้งเตือนเมื่อเงินไม่พอ
            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification("Not Enoght Money Need " + price + " $!");
        }
    }

    void UpdateVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
    }
}