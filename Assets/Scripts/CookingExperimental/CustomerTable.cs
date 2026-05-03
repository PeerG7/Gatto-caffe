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
    public GameObject interactionButtonGroup; // ✅ ลาก button group GameObject มาใส่ตรงนี้
    public float interactionDuration = 5.0f;
    [Range(0, 100)] public int interactionChance = 70;

    private Coroutine interactionCoroutine;

    void Awake()
    {
        // ✅ Set Camera ใน Awake ก่อนเลย
        SetWorldSpaceCamera();
    }

    void Start()
    {
        // ✅ Set อีกครั้งใน Start เผื่อ Camera ยังไม่พร้อมใน Awake
        SetWorldSpaceCamera();
    }

    void SetWorldSpaceCamera()
    {
        if (Camera.main == null) return;
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace)
                c.worldCamera = Camera.main;
        }
    }

    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        if (player.currentItem.Trim().Equals(wantedItem.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            PlayerController2D.IsLocked = false;

            // ✅ นับแมวที่ Serve และเงินที่ได้วันนี้
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.catsServedToday++;
                DayNightManager.Instance.moneyEarnedToday += dishReward;
            }

            if (sittingNPC != null && sittingNPC.orderCanvas != null)
                sittingNPC.orderCanvas.SetActive(false);

            if (tableItemRenderer != null)
                tableItemRenderer.enabled = false;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(dishReward);

            player.ClearItem();

            if (Random.Range(0, 101) <= interactionChance)
                interactionCoroutine = StartCoroutine(StartInteractionPhase());
            else
                FinishServing();
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

        sittingNPC.isInQTE = true;

        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.StartInteraction(this);

        if (interactionCanvas != null) interactionCanvas.SetActive(true);

        // ✅ Force เปิด button group ทุกครั้ง แม้ button ข้างในจะเคย set ปิดมันไว้
        if (interactionButtonGroup != null) interactionButtonGroup.SetActive(true);
    }

    public void OpenNPCInteractCanvas()
    {
        if (interactionCanvas != null) interactionCanvas.SetActive(false);

        if (sittingNPC != null && sittingNPC.qteCanvasInPrefab != null)
            sittingNPC.qteCanvasInPrefab.SetActive(true);
    }

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
        if (interactionButtonGroup != null) interactionButtonGroup.SetActive(false);
    }
}