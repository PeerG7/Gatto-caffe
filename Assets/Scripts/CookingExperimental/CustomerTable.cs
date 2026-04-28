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
    public int interactionChance = 50;

    private Coroutine interactionCoroutine;

    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        string order = wantedItem.Trim();
        string holding = player.currentItem.Trim();

        if (holding.Equals(order, System.StringComparison.OrdinalIgnoreCase))
        {
            if (sittingNPC != null && sittingNPC.orderCanvas != null)
            {
                sittingNPC.orderCanvas.SetActive(false);
            }
            if (tableItemRenderer != null)
            {
                tableItemRenderer.enabled = false;
            }

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(dishReward);

            if (UINotificationManager.Instance != null)
                UINotificationManager.Instance.ShowNotification($"+ ${dishReward}");

            player.ClearItem();

            int roll = Random.Range(0, 101);
            if (roll <= interactionChance)
            {
                interactionCoroutine = StartCoroutine(StartInteractionPhase());
            }
            else
            {
                FinishServing();
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

        // --- เพิ่มส่วนการซูมกล้องตรงนี้ ---
        if (CameraManager.Instance != null && sittingNPC != null)
        {
            CameraManager.Instance.ZoomIn(sittingNPC.transform);
        }

        yield return new WaitForSeconds(interactionDuration);

        EndInteraction();
    }

    public void OnHeartClicked()
    {
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);

        if (sittingNPC != null)
        {
            NPCInteract interact = sittingNPC.GetComponent<NPCInteract>();
            if (interact != null) interact.RelationShip();
        }

        EndInteraction();
    }

    void EndInteraction()
    {
        // --- สั่งให้กล้องซูมออกเมื่อจบการปฏิสัมพันธ์ ---
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.ZoomOut();
        }

        if (relationshipIcon != null) relationshipIcon.SetActive(false);
        if (heartIcon != null) heartIcon.SetActive(false);

        FinishServing();
    }

    void FinishServing()
    {
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