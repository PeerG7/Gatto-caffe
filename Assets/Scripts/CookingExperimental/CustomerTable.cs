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

    [Header("VIP Visual")]
    [Tooltip("ไอคอน/ป้าย VIP ที่จะโชว์เมื่อลูกค้าที่นั่งโต๊ะนี้เป็นแมว VIP")]
    public GameObject vipBadgeIcon;

    [Header("Interaction Phase Settings")]
    public GameObject interactionCanvas;
    public GameObject interactionButtonGroup;
    public float interactionDuration = 5.0f;
    [Range(0, 100)] public int interactionChance = 70;

    [Header("SFX")]
    [Tooltip("เสียงเหรียญตอนได้รับเงิน — ถ้าไม่ใส่จะใช้ sfxCoin ของ AudioManager")]
    public AudioClip coinSoundClip;
    public AudioSource sfxSource;   // Optional: AudioSource เฉพาะโต๊ะนี้

    private Coroutine interactionCoroutine;

    void Awake()
    {
        SetWorldSpaceCamera();
    }

    void Start()
    {
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

            // ✅ ถ้าเป็นแมว VIP ให้เงินมากขึ้นตาม vipMoneyMultiplier
            int finalReward = dishReward;
            if (sittingNPC != null && sittingNPC.isVIP)
                finalReward = Mathf.RoundToInt(dishReward * sittingNPC.vipMoneyMultiplier);

            // ✅ นับแมวที่ Serve และเงินที่ได้วันนี้
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.catsServedToday++;
                DayNightManager.Instance.moneyEarnedToday += finalReward;
            }

            if (sittingNPC != null && sittingNPC.orderCanvas != null)
                sittingNPC.orderCanvas.SetActive(false);

            if (tableItemRenderer != null)
                tableItemRenderer.enabled = false;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(finalReward);

            // ✅ เล่นเสียงเหรียญหลังได้รับเงิน
            PlayCoinSound();

            player.ClearItem();

            if (Random.Range(0, 101) <= interactionChance)
                interactionCoroutine = StartCoroutine(StartInteractionPhase());
            else
                FinishServing();
        }
    }

    /// <summary>เล่นเสียงเหรียญ — ใช้ coinSoundClip ถ้ามี ไม่งั้นใช้ AudioManager.sfxCoin</summary>
    void PlayCoinSound()
    {
        if (sfxSource != null && coinSoundClip != null)
            sfxSource.PlayOneShot(coinSoundClip);
        else if (coinSoundClip != null)
            AudioSource.PlayClipAtPoint(coinSoundClip, transform.position);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayCoin();
    }

    IEnumerator StartInteractionPhase()
    {
        if (heartIcon != null) heartIcon.SetActive(true);
        yield return new WaitForSeconds(interactionDuration);
        EndInteraction();
    }

    // ── แก้: ไม่เปิด Canvas ทันทีอีกต่อไป — สั่งให้ NPC เดินไป InteractionZone
    //    ก่อน แล้วค่อยเปิด UI ตอนไปถึงจริง (ผ่าน callback HandleNPCArrivedAtZone) ──
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

        // ⚠️ ไม่ล็อก Player ตรงนี้ — Heart Icon QTE (ผ่าน CatSystemManager) ไม่ได้เรียก
        // DayNightManager.PauseGame() เกมส่วนอื่นทั้งร้านยังทำงานปกติ ผู้เล่นต้องเดิน
        // ไปเสิร์ฟ/จัดการโต๊ะอื่นต่อได้ระหว่างแมวตัวนี้ไปทำ QTE ที่โซนแยก
        // (การล็อก Player จริงๆ ต้องทำเฉพาะตอนเปิด Relationship Book ที่ pause ทั้งเกม
        //  ดูใน NPCInteract.RelationShip() / RelationshipSceneUI.cs แทน)

        sittingNPC.GoToInteractionZone(HandleNPCArrivedAtZone);
    }

    void HandleNPCArrivedAtZone()
    {
        // เผื่อระหว่างเดินไปโซน sittingNPC ถูกเคลียร์ไปแล้ว (เช่นโดน reset ตารางกลางทาง)
        if (sittingNPC == null) return;

        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.StartInteraction(this);

        if (interactionCanvas != null) interactionCanvas.SetActive(true);
        if (interactionButtonGroup != null) interactionButtonGroup.SetActive(true);
    }

    public void OpenNPCInteractCanvas()
    {
        if (interactionCanvas != null) interactionCanvas.SetActive(false);
        if (sittingNPC != null && sittingNPC.qteCanvasInPrefab != null)
            sittingNPC.qteCanvasInPrefab.SetActive(true);
    }

    // ── แก้: จบ Interaction แล้วให้ NPC เดินกลับที่นั่งก่อน ค่อย LeaveSeat() ──
    public void CloseInteractionUI()
    {
        if (interactionCanvas != null) interactionCanvas.SetActive(false);

        if (sittingNPC != null)
        {
            if (sittingNPC.qteCanvasInPrefab != null)
                sittingNPC.qteCanvasInPrefab.SetActive(false);

            sittingNPC.isInQTE = false;

            // ⚠️ ไม่แตะ PlayerController2D.IsLocked ตรงนี้ — Heart Icon QTE ไม่เคยล็อก
            // Player ไว้ตั้งแต่แรก (ดูเหตุผลใน OnHeartClicked ด้านบน) การ set false ที่นี่
            // อาจไปปลดล็อกการล็อกที่มาจากฟีเจอร์อื่นโดยไม่ตั้งใจ (เช่น station canvas)

            NPCController npc = sittingNPC;

            // ✅ แก้: ให้แมวออกจากร้าน "ตรงจาก Interaction Zone" เลย
            //    ไม่ต้องเดินกลับที่นั่งเดิมก่อน — กัน race condition ที่โต๊ะถูกปลดล็อก
            //    (ResetTable() ด้านล่าง) ให้ลูกค้าใหม่จองซ้อนได้ ทั้งที่แมวตัวเก่ายัง
            //    เดินกลับไม่ถึงที่นั่ง (เห็นแมว 2 ตัวทับกันที่โต๊ะเดียวกันชั่วขณะ)
            //    GoExit() จัดการ release currentZone / CancelRequest / ซ่อน canvas
            //    ให้ครบอยู่แล้วในตัวมันเอง ไม่ต้องผ่าน LeaveSeat() อีกที
            npc.GoExit();

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
        SetVIPVisual(false);
    }

    /// <summary>เปิด/ปิด Badge บอกว่าโต๊ะนี้มีลูกค้า VIP นั่งอยู่</summary>
    public void SetVIPVisual(bool isVIP)
    {
        if (vipBadgeIcon != null) vipBadgeIcon.SetActive(isVIP);
    }
}