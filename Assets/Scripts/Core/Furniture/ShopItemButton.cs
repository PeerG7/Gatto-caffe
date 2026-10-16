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

    // AudioSource กลาง ใช้ร่วมกันทุกปุ่มซื้อ ไม่ต้องใส่ AudioSource ทีละอัน
    private static AudioSource sharedSource;

    // ฟังก์ชันซื้อแล้วสร้าง Instance ใหม่ส่งเข้าคลัง
    public void OnClickBuyToInventory()
    {
        if (itemData == null)
        {
            Debug.LogWarning("ยังไม่ได้ใส่ FurnitureItemData ใน Inspector!");
            return;
        }

        // เช็กและหักเงินผ่าน CurrencyManager
        if (CurrencyManager.Instance != null && CurrencyManager.Instance.TrySpendMoney(itemData.price))
        {
            PlayBuySound();   // <-- เล่นเสียงเฉพาะตอนซื้อสำเร็จ

            if (FurnitureInventory.Instance != null)
            {
                // สร้าง Instance ใหม่เริ่มต้นที่ Level 1
                FurnitureInstanceData newInstance = new FurnitureInstanceData(itemData, 1, false);
                FurnitureInventory.Instance.AddInstance(newInstance);
                Debug.Log($"ซื้อ {itemData.furnitureName} สำเร็จ! เข้าคลังเรียบร้อย");
            }
        }
        else
        {
            Debug.Log("เงินไม่พอซื้อ!");
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
            sharedSource.spatialBlend = 0f; // 2D sound
        }
        return sharedSource;
    }
}