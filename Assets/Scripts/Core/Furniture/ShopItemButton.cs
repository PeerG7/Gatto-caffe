using UnityEngine;

public class ShopItemButton : MonoBehaviour
{
    [Header("Item Data To Buy")]
    public FurnitureItemData itemData;

    [Header("SFX")]
    [Tooltip("เสียงตอนซื้อสำเร็จ (ลาก cash_2.mp3 มาใส่)")]
    public AudioClip buySoundClip;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private static AudioSource sharedSource;

    public void OnClickBuyToInventory()
    {
        if (itemData == null)
        {
            Debug.LogWarning("ยังไม่ได้ใส่ FurnitureItemData ใน Inspector!");
            return;
        }

        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendMoney(itemData.price))
        {
            PlayBuySound();

            if (FurnitureInventory.Instance != null)
            {
                FurnitureInstanceData newInstance = new FurnitureInstanceData(itemData, 1, false);
                FurnitureInventory.Instance.AddInstance(newInstance);
                Debug.Log($"ซื้อ {itemData.furnitureName} สำเร็จ! เข้าคลังเรียบร้อย");
                UINotificationManager.Instance?.ShowNotification("NOTI_ITEM_BOUGHT");
            }
        }
        else
        {
            Debug.Log("เงินไม่พอซื้อ!");
            UINotificationManager.Instance?.ShowNotification("NOTI_NOT_ENOUGH_MONEY", itemData.price);
        }
    }

    private void PlayBuySound()
    {
        if (buySoundClip == null) return;
        GetSharedSource().PlayOneShot(buySoundClip, sfxVolume);
    }

    private static AudioSource GetSharedSource()
    {
        if (sharedSource == null)
        {
            GameObject go = new GameObject("ShopSfxPlayer");
            DontDestroyOnLoad(go);
            sharedSource = go.AddComponent<AudioSource>();
            sharedSource.playOnAwake = false;
            sharedSource.spatialBlend = 0f;
        }
        return sharedSource;
    }
}