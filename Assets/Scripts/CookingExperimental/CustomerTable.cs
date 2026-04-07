using UnityEngine;

public class CustomerTable : MonoBehaviour
{
    public string wantedItem; // ชื่อเมนูที่แมวตัวนี้ต้องการ

    [Header("Visuals")]
    public SpriteRenderer tableItemRenderer; // จุดวางจานบนโต๊ะ
    public GameObject heartIcon; // Object รูปหัวใจ
    public GameObject angryIcon; // Object รูปแมวโกรธ
    public GameObject moneyPopupPrefab; // Prefab ตัวเลขเงินเด้ง

    private bool playerInRange = false;

    void Update()
    {
        // กด E เพื่อเสิร์ฟ
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            TryServeFood();
        }
    }

    void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();

        if (player != null && player.HasItem())
        {
            // เอาอาหารไปวางบนโต๊ะ
            tableItemRenderer.sprite = player.heldItemRenderer.sprite;
            tableItemRenderer.enabled = true;

            // ตรวจสอบเงื่อนไข
            if (player.currentItem == wantedItem)
            {
                // 1. เสิร์ฟถูกต้อง
                if (heartIcon != null) heartIcon.SetActive(true);
                if (angryIcon != null) angryIcon.SetActive(false);
                GiveReward(100);
            }
            else if (player.currentItem == "Failed Dish")
            {
                // 2. เสิร์ฟอาหารขยะ
                if (angryIcon != null) angryIcon.SetActive(true);
                if (heartIcon != null) heartIcon.SetActive(false);
                GiveReward(0); // หรืออาจจะติดลบ
            }
            else
            {
                // 3. เสิร์ฟผิดจาน (แต่ไม่ใช่ของเสีย)
                if (angryIcon != null) angryIcon.SetActive(true);
                if (heartIcon != null) heartIcon.SetActive(false);
                GiveReward(10); // ปลอบใจนิดหน่อย
            }

            player.ClearItem(); // เคลียร์ของที่หัวผู้เล่น
        }
    }

    void GiveReward(int amount)
    {
        if (amount > 0 && moneyPopupPrefab != null)
        {
            // สร้างเงินเด้งขึ้นมา
            Instantiate(moneyPopupPrefab, transform.position + Vector3.up, Quaternion.identity);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInRange = false;
    }
}