using UnityEngine;

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
    public GameObject angryIcon;

    [Header("VIP Visual")]
    [Tooltip("ไอคอน/ป้าย VIP ที่จะโชว์เมื่อลูกค้าที่นั่งโต๊ะนี้เป็นแมว VIP")]
    public GameObject vipBadgeIcon;

    [Header("Interaction Chance")]
    [Tooltip("โอกาส % ที่ลูกค้าจะอยากทำ Interaction (เดินไป Interaction Zone) หลังกินเสร็จ — " +
             "การเลือกว่าจะไปหรือไม่เกิดขึ้นทันทีตอนเสิร์ฟเสร็จ ไม่มี Heart Icon รอที่โต๊ะแล้ว")]
    [Range(0, 100)] public int interactionChance = 70;

    [Header("SFX")]
    [Tooltip("เสียงเหรียญตอนได้รับเงิน — ถ้าไม่ใส่จะใช้ sfxCoin ของ AudioManager")]
    public AudioClip coinSoundClip;
    public AudioSource sfxSource;   // Optional: AudioSource เฉพาะโต๊ะนี้

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

            NPCController servedNPC = sittingNPC; // เก็บ reference ไว้ก่อน เพราะ ResetTable() จะเคลียร์ sittingNPC ทิ้ง

            // ✅ ถ้าเป็นแมว VIP ให้เงินมากขึ้นตาม vipMoneyMultiplier
            int finalReward = dishReward;
            if (servedNPC != null && servedNPC.isVIP)
                finalReward = Mathf.RoundToInt(dishReward * servedNPC.vipMoneyMultiplier);

            // ✅ นับแมวที่ Serve และเงินที่ได้วันนี้
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.catsServedToday++;
                DayNightManager.Instance.moneyEarnedToday += finalReward;
            }

            if (tableItemRenderer != null)
                tableItemRenderer.enabled = false;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(finalReward);

            // ✅ เล่นเสียงเหรียญหลังได้รับเงิน
            PlayCoinSound();

            player.ClearItem();

            // ✅ สุ่มว่าแมวตัวนี้จะไป Interaction Zone ต่อไหม ตัดสินใจทันที ณ จุดนี้เลย
            bool willInteract = Random.Range(0, 101) <= interactionChance;

            // ✅ ปลดโต๊ะให้ว่างทันที ไม่ต้องรอ Interaction จบก่อนแล้วค่อยว่าง
            //    (NPCController.FinishServingAndProceed จะ ResetTable() ให้เอง แล้วค่อย
            //    ตัดสินใจไป InteractionZone หรือออกจากร้านเลยตาม willInteract)
            if (servedNPC != null)
                servedNPC.FinishServingAndProceed(willInteract);
            else
                ResetTable();
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

    public void ResetTable()
    {
        wantedItem = "";
        sittingNPC = null;
        isOccupied = false;
        if (tableItemRenderer != null) tableItemRenderer.enabled = false;
        SetVIPVisual(false);
    }

    /// <summary>เปิด/ปิด Badge บอกว่าโต๊ะนี้มีลูกค้า VIP นั่งอยู่</summary>
    public void SetVIPVisual(bool isVIP)
    {
        if (vipBadgeIcon != null) vipBadgeIcon.SetActive(isVIP);
    }
}