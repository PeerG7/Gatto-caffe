using UnityEngine;
using System.Collections;

public class CustomerTable : MonoBehaviour
{
    [Header("Bypass Seat System")]
    public Transform seatPoint;
    public bool isOccupied = false;

    [Header("Table Status")]
    public string wantedItem;
    public NPCController sittingNPC;
    public int dishReward = 50;

    [Header("Visuals")]
    public SpriteRenderer tableItemRenderer;
    public GameObject heartIcon;
    public GameObject angryIcon;

    [Header("Interaction Phase Settings")]
    public GameObject relationshipIcon;
    public float interactionDuration = 5.0f;
    [Range(0, 100)]
    public int interactionChance = 50; // โอกาสเกิด Interaction (เช่น 50%)

    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        string order = wantedItem.Trim();
        string holding = player.currentItem.Trim();

        if (holding.Equals(order, System.StringComparison.OrdinalIgnoreCase))
        {
            // 1. ปิดฟองออเดอร์ของแมว และรูปอาหารบนโต๊ะทันที
            if (sittingNPC != null && sittingNPC.orderCanvas != null)
            {
                sittingNPC.orderCanvas.SetActive(false);
            }
            if (tableItemRenderer != null)
            {
                tableItemRenderer.enabled = false;
            }

            // 2. ระบบเงิน
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(dishReward);

            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification($"+ ${dishReward}");

            player.ClearItem();

            // 3. ระบบสุ่ม (RNG) ว่าจะเกิด Interaction Phase หรือไม่
            int roll = Random.Range(0, 101);
            if (roll <= interactionChance)
            {
                // สุ่มสำเร็จ: เข้าสู่ช่วงปฏิสัมพันธ์
                StartCoroutine(StartInteractionPhase());
            }
            else
            {
                // สุ่มไม่สำเร็จ: แมวเดินออกจากร้านทันทีหลังจากกินเสร็จ
                if (sittingNPC != null) sittingNPC.LeaveSeat();
                ResetTable();
            }
        }
        else
        {
            if (angryIcon != null)
            {
                angryIcon.SetActive(true);
                Invoke("HideAngryIcon", 1.5f);
            }
        }
    }

    IEnumerator StartInteractionPhase()
    {
        if (relationshipIcon != null) relationshipIcon.SetActive(true);
        if (heartIcon != null) heartIcon.SetActive(true);

        yield return new WaitForSeconds(interactionDuration);

        if (relationshipIcon != null) relationshipIcon.SetActive(false);
        if (heartIcon != null) heartIcon.SetActive(false);

        if (sittingNPC != null) sittingNPC.LeaveSeat();
        ResetTable();
    }

    void ResetTable()
    {
        wantedItem = "";
        sittingNPC = null;
        isOccupied = false;
        if (tableItemRenderer != null) tableItemRenderer.enabled = false;
        if (heartIcon != null) heartIcon.SetActive(false);
    }

    void HideAngryIcon() => angryIcon.SetActive(false);
}