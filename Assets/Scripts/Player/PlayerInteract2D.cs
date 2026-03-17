using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    // เปลี่ยนจาก NPCInteract เป็น CatTrigger
    private CatTrigger currentCat;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentCat != null)
        {
            currentCat.Interact(); // เรียกใช้ฟังก์ชันที่เราเพิ่งสร้าง
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // เปลี่ยนมาหา Component ตัวใหม่ที่เราใช้
        CatTrigger cat = other.GetComponent<CatTrigger>();

        if (cat != null)
        {
            currentCat = cat;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        CatTrigger cat = other.GetComponent<CatTrigger>();

        if (cat == currentCat)
        {
            currentCat = null;
        }
    }
}