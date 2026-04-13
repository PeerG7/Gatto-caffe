using UnityEngine;

public class CustomerTable : MonoBehaviour
{
    [Header("Bypass Seat System")]
    public Transform seatPoint;
    public bool isOccupied = false;

    [Header("Table Status")]
    public string wantedItem;
    public NPCController sittingNPC;

    [Header("Visuals")]
    public SpriteRenderer tableItemRenderer;
    public GameObject heartIcon;
    public GameObject angryIcon;

    // ฟังก์ชันสำหรับเสิร์ฟอาหาร (เรียกจาก PlayerInteract2D)
    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        // 🚩 ป้องกันปัญหาเรื่องช่องว่างแฝง เช่น "FishNoodle " (มี space ข้างหลัง)
        string order = wantedItem.Trim();
        string holding = player.currentItem.Trim();

        Debug.Log($"[Table] Checking: '{holding}' vs '{order}'");

        if (holding.Equals(order, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Serve Success!");

            // แสดงอาหารบนโต๊ะ
            if (tableItemRenderer != null)
            {
                tableItemRenderer.sprite = player.heldItemRenderer.sprite;
                tableItemRenderer.enabled = true;
            }

            if (heartIcon != null) heartIcon.SetActive(true); //
            if (sittingNPC != null) sittingNPC.LeaveSeat();

            // เคลียร์ไอเทมในมือผู้เล่น
            player.ClearItem();

            // รีเซ็ตสถานะโต๊ะหลังจากผ่านไป 2 วินาที
            Invoke("ResetTable", 2.0f);
        }
        else
        {
            Debug.Log("Wrong Item! Player holds: " + holding + " but Table wants: " + order);
            if (angryIcon != null)
            {
                angryIcon.SetActive(true); //
                Invoke("HideAngryIcon", 1.5f);
            }
        }
    }

    void ResetTable()
    {
        wantedItem = "";
        sittingNPC = null;
        isOccupied = false;
        if (tableItemRenderer != null) tableItemRenderer.enabled = false; //
        if (heartIcon != null) heartIcon.SetActive(false); //
    }

    void HideAngryIcon() => angryIcon.SetActive(false); //
}