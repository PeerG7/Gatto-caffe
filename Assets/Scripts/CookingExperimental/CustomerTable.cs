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
    public GameObject heartIcon;
    public GameObject angryIcon;

    [Header("Interaction Phase Settings")]
    public GameObject interactionCanvas;
    public float interactionDuration = 5.0f;
    [Range(0, 100)] public int interactionChance = 70;

    private Coroutine interactionCoroutine;

    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        if (player.currentItem.Trim().Equals(wantedItem.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            if (sittingNPC != null && sittingNPC.orderCanvas != null)
                sittingNPC.orderCanvas.SetActive(false);

            if (tableItemRenderer != null)
                tableItemRenderer.enabled = false;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(dishReward);

            player.ClearItem();

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
        if (heartIcon != null) heartIcon.SetActive(true);
        yield return new WaitForSeconds(interactionDuration);
        EndInteraction();
    }

    public void OnHeartClicked()
    {
        if (sittingNPC == null) return;

        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }

        if (heartIcon != null) heartIcon.SetActive(false);

        // บอก NPC หยุดนับ patience
        sittingNPC.isInQTE = true;

        // FIX: CatSystemManager อยู่เสมอ ไม่ต้อง SetActive(true) แล้ว
        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.StartInteraction(this);

        // เปิดเมนูเลือก Pet/Hug/Play
        if (interactionCanvas != null) interactionCanvas.SetActive(true);
    }

    // เรียกจาก CatSystemManager.StartQTE() — ปิดเมนูกลาง เปิด canvas บน NPC
    public void OpenNPCInteractCanvas()
    {
        if (interactionCanvas != null) interactionCanvas.SetActive(false);

        if (sittingNPC != null && sittingNPC.qteCanvasInPrefab != null)
            sittingNPC.qteCanvasInPrefab.SetActive(true);
    }

    // เรียกจาก CatSystemManager เมื่อจบ QTE
    public void CloseInteractionUI()
    {
        if (interactionCanvas != null) interactionCanvas.SetActive(false);

        if (sittingNPC != null)
        {
            if (sittingNPC.qteCanvasInPrefab != null)
                sittingNPC.qteCanvasInPrefab.SetActive(false);

            sittingNPC.isInQTE = false;
            sittingNPC.LeaveSeat();
            sittingNPC = null;
        }

        ResetTable();
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
            sittingNPC.isInQTE = false;
            sittingNPC.LeaveSeat();
        }
        ResetTable();
    }

    public void ResetTable()
    {
        wantedItem = "";
        sittingNPC = null;
        isOccupied = false;
        interactionCoroutine = null;
        if (tableItemRenderer != null) tableItemRenderer.enabled = false;
        if (heartIcon != null) heartIcon.SetActive(false);
        if (interactionCanvas != null) interactionCanvas.SetActive(false);
    }
}