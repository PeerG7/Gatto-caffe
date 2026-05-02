using UnityEngine;

// =====================================================================
// ใส่บน GameObject ที่เป็น book station ใน scene
// ต้องมี Collider2D (Is Trigger = true)
// PlayerInteract2D จะเรียก OpenBook() เมื่อผู้เล่นกด E ใกล้ๆ
// =====================================================================
public class BookStation : MonoBehaviour
{
    public RelationshipBookUI bookUI;

    public void OpenBook()
    {
        if (bookUI != null)
            bookUI.OpenBook();
    }
}