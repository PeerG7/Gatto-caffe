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

    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        string order = wantedItem.Trim();
        string holding = player.currentItem.Trim();

        Debug.Log($"[Table] Checking: '{holding}' vs '{order}'");

        if (holding.Equals(order, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Serve Success!");
            if (tableItemRenderer != null)
            {
                tableItemRenderer.sprite = player.heldItemRenderer.sprite;
                tableItemRenderer.enabled = true;
            }

            if (heartIcon != null) heartIcon.SetActive(true);
            if (sittingNPC != null) sittingNPC.LeaveSeat();

            player.ClearItem();
            Invoke("ResetTable", 2.0f);
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