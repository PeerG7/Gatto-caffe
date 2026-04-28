using UnityEngine;
using System.Collections;

public class CustomerTable : MonoBehaviour
{
    [Header("Table Configuration")]
    public Transform seatPoint;
    public bool isOccupied = false;
    public NPCController sittingNPC;
    public string wantedItem;
    public int dishReward = 100;

    [Header("Visual Elements")]
    public SpriteRenderer tableItemRenderer;
    public GameObject heartIcon;         // ไอคอนหัวใจที่ลอยบนโต๊ะ (World Space)
    public GameObject angryIcon;

    [Header("Interaction Phase Settings")]
    public GameObject interactionCanvas; // หน้าจอเมนูหลักที่มีปุ่ม Pet, Hug, Play (Screen Space)
    public float interactionDuration = 5.0f;
    [Range(0, 100)] public int interactionChance = 70;

    private Coroutine interactionCoroutine;

    // --- ส่วนของการจัดการอาหาร ---
    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        // ตรวจสอบว่าชื่อไอเทมในตัวผู้เล่นตรงกับที่โต๊ะต้องการหรือไม่
        if (player.currentItem.Trim().Equals(wantedItem.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            // ปิดออเดอร์ของ NPC
            if (sittingNPC != null && sittingNPC.orderCanvas != null)
                sittingNPC.orderCanvas.SetActive(false);

            if (tableItemRenderer != null)
                tableItemRenderer.enabled = false;

            // ให้เงินรางวัล
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(dishReward);

            player.ClearItem();

            // สุ่มโอกาสเกิดช่วง "ลูบแมว" (Interaction Phase)
            if (Random.Range(0, 101) <= interactionChance)
            {
                interactionCoroutine = StartCoroutine(StartInteractionPhase());
            }
            else
            {
                FinishServing();
            }
        }
    }

    IEnumerator StartInteractionPhase()
    {
        // เปิดปุ่มหัวใจบนโต๊ะให้ผู้เล่นมองเห็นและกดได้
        if (heartIcon != null) heartIcon.SetActive(true);

        // รอจนกว่าจะหมดเวลา ถ้าผู้เล่นไม่กด แมวจะลุกไปเอง
        yield return new WaitForSeconds(interactionDuration);

        EndInteraction();
    }

    // --- ฟังก์ชันหลัก: เรียกใช้เมื่อผู้เล่นคลิกที่ปุ่มหัวใจบนโต๊ะ ---
    public void OnHeartClicked()
    {
        if (sittingNPC == null) return;

        // 1. เปิด Manager และส่งค่า
        if (CatSystemManager.Instance != null)
        {
            CatSystemManager.Instance.gameObject.SetActive(true);
            CatSystemManager.Instance.StartInteraction(this);
        }

        // 2. จัดการ UI (เปิดปุ่มกดบนตัวแมว)
        if (heartIcon != null) heartIcon.SetActive(false);
        if (interactionCanvas != null) interactionCanvas.SetActive(true);
    }

    // --- ฟังก์ชันปิดระบบและรีเซ็ตค่า (เรียกจาก CatSystemManager เมื่อจบ QTE) ---
    public void CloseInteractionUI()
    {
        // ปิดเมนูปุ่มกด
        if (interactionCanvas != null) interactionCanvas.SetActive(false);

        // สั่งแมวเดินออก
        if (sittingNPC != null)
        {
            sittingNPC.LeaveSeat();
            sittingNPC = null;
        }
    }

    void EndInteraction()
    {
        if (heartIcon != null) heartIcon.SetActive(false);
        FinishServing();
    }

    void FinishServing()
    {
        if (sittingNPC != null)
        {
            sittingNPC.LeaveSeat();
        }

        ResetTable();
    }

    public void ResetTable()
    {
        wantedItem = "";
        sittingNPC = null; // เคลียร์ข้อมูลแมวตัวเก่าทิ้งเพื่อให้โต๊ะว่าง
        isOccupied = false;
        if (tableItemRenderer != null) tableItemRenderer.enabled = false;
        if (heartIcon != null) heartIcon.SetActive(false);
    }
}