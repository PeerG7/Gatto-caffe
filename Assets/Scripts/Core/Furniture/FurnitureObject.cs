using UnityEngine;

public class FurnitureObject : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject lockedVisual;
    public GameObject unlockedVisual;

    [Header("Settings")]
    public int price = 200;
    public bool isUnlocked = false;

    void Start() => UpdateVisuals();

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

    void UpdateVisuals()
    {
        if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
    }
}