using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerTable : MonoBehaviour
{
    [Header("Table Configuration")]
    [Tooltip("ตำแหน่งเก้าอี้ทั้งหมดที่มีในโต๊ะนี้ (เช่น Seat1, Seat2)")]
    public List<Transform> seatPoints = new List<Transform>();

    [HideInInspector] public List<NPCController> sittingNPCs = new List<NPCController>();

    public bool isFull => sittingNPCs.Count >= seatPoints.Count;
    public bool isOccupied => sittingNPCs.Count > 0;

    public string wantedItem;
    public int dishReward = 100;

    [Header("Visual Elements")]
    public SpriteRenderer tableItemRenderer;
    public GameObject angryIcon;

    [Header("VIP Visual")]
    public GameObject vipBadgeIcon;

    [Header("Money on Table Settings")]
    public GameObject moneyIconOnTable;
    [HideInInspector] public bool hasMoneyToCollect = false;
    private int pendingMoneyReward = 0;

    [Header("Money Pop Animation")]
    [Tooltip("เวลาที่เหรียญเด้งขึ้นมา (วินาที)")]
    public float popDuration = 0.55f;
    [Tooltip("ความสูงที่เหรียญกระโดดขึ้นตอนเด้ง (หน่วยโลก)")]
    public float popHopHeight = 0.35f;
    [Tooltip("ความสูงที่เหรียญโยกขึ้นลงตอนวางรออยู่")]
    public float idleBobHeight = 0.04f;
    [Tooltip("ความเร็วการโยก")]
    public float idleBobSpeed = 4f;

    private Vector3 moneyIconBaseScale = Vector3.one;
    private Vector3 moneyIconBasePos;
    private Coroutine moneyAnimRoutine;

    [Header("Interaction Chance")]
    [Range(0, 100)] public int interactionChance = 70;

    [Header("Chat Interaction Settings")]
    [Range(0, 100)]
    public int chatTriggerChance = 80; // 🟢 ปรับโอกาสคุยกันเป็น 80% ตามที่ต้องการ

    [Header("SFX")]
    public AudioClip coinSoundClip;
    public AudioSource sfxSource;

    [Header("Group Serve Settings")]
    public float patienceBonusOnFriendServed = 15f;

    private void Awake()
    {
        SetWorldSpaceCamera();
        AutoFindSeatPoints();
        if (moneyIconOnTable != null)
        {
            moneyIconBaseScale = moneyIconOnTable.transform.localScale;
            moneyIconBasePos = moneyIconOnTable.transform.localPosition;
            moneyIconOnTable.SetActive(false);
        }
    }

    private void Start()
    {
        SetWorldSpaceCamera();
    }

    private void AutoFindSeatPoints()
    {
        if (seatPoints == null || seatPoints.Count == 0)
        {
            seatPoints = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Seat"))
                {
                    seatPoints.Add(child);
                }
            }
        }
    }

    private void SetWorldSpaceCamera()
    {
        if (Camera.main == null) return;
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace)
                c.worldCamera = Camera.main;
        }
    }

    public Transform GetAvailableSeat()
    {
        if (seatPoints == null || seatPoints.Count == 0) return transform;

        if (sittingNPCs.Count > 0)
        {
            foreach (Transform seat in seatPoints)
            {
                bool isSeatTaken = sittingNPCs.Exists(npc => npc != null && npc.seatPoint == seat);
                if (!isSeatTaken)
                {
                    return seat;
                }
            }
            return null;
        }

        return seatPoints[0];
    }

    public void AssignNPCToTable(NPCController npc)
    {
        if (!sittingNPCs.Contains(npc))
        {
            sittingNPCs.Add(npc);
            npc.assignedTable = this;
        }
    }

    private Coroutine chatRoutine;
    private CatChatBubble activeBubble;

    /// <summary>
    /// Cats take turns talking. Each speaker shows a bubble with random icons,
    /// mirrored toward the cat they are talking to.
    /// NOTE: NPCController already rolls chatTriggerChance before calling this,
    /// so no second roll here (it used to roll twice = 80% x 80% = 64%).
    /// </summary>
    public void TriggerTableChat(float totalDuration = 5f)
    {
        if (seatPoints.Count < 2 || sittingNPCs.Count < 2) return;

        if (chatRoutine != null) StopCoroutine(chatRoutine);
        chatRoutine = StartCoroutine(TableChatRoutine(totalDuration));
    }

    private IEnumerator TableChatRoutine(float totalDuration)
    {
        float elapsed = 0f;
        int turn = Random.Range(0, sittingNPCs.Count);

        while (elapsed < totalDuration)
        {
            List<NPCController> chatters = sittingNPCs.FindAll(n =>
                n != null && n.currentState == NPCController.NPCState.Sitting);
            if (chatters.Count < 2) break;

            NPCController speaker = chatters[turn % chatters.Count];
            NPCController listener = chatters[(turn + 1) % chatters.Count];

            bool faceRight = listener.transform.position.x >= speaker.transform.position.x;
            float duration = Mathf.Min(Random.Range(1.0f, 1.6f), totalDuration - elapsed);

            if (activeBubble != null) activeBubble.HideChatBubble();
            activeBubble = speaker.GetComponentInChildren<CatChatBubble>(true);
            if (activeBubble != null) activeBubble.ShowChatBubble(faceRight, duration);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            elapsed += duration;
            turn++;
        }

        StopTableChat();
    }

    private void StopTableChat()
    {
        if (chatRoutine != null) { StopCoroutine(chatRoutine); chatRoutine = null; }
        if (activeBubble != null) { activeBubble.HideChatBubble(); activeBubble = null; }
    }

    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        string heldItem = player.currentItem.Trim();

        NPCController targetNPC = sittingNPCs.Find(n =>
            n != null &&
            !n.hasBeenServed &&
            n.requestedRecipe != null &&
            n.requestedRecipe.recipeName.Trim().Equals(heldItem, System.StringComparison.OrdinalIgnoreCase)
        );

        if (targetNPC == null && !string.IsNullOrEmpty(wantedItem) && heldItem.Equals(wantedItem.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            targetNPC = sittingNPCs.Find(n => n != null && !n.hasBeenServed);
        }

        if (targetNPC != null)
        {
            // ปลดล็อคผู้เล่นและปิด UI QTE ทันทีที่เสิร์ฟ
            PlayerController2D.SetLock(false);
            CatSystemManager.Instance?.HideInteractionChoice();

            targetNPC.hasBeenServed = true;
            BoostRemainingNPCsPatience(targetNPC);
            player.ClearItem();

            CheckAndTriggerSmudgeOnServe(targetNPC);

            targetNPC.StartEatingProcess();

            // 🌟 ย้ายมาไว้ข้างในนี้ (อัปเดตเควสเฉพาะตอนเสิร์ฟสำเร็จจริงๆ เท่านั้น)
            if (MainQuestManager.Instance != null && MainQuestManager.Instance.currentQuest == MainQuestManager.QuestStep.ServeFood)
            {
                MainQuestManager.Instance.AdvanceQuest();
            }
        }
        else
        {
            Debug.Log("ไม่มีแมวตัวไหนบนโต๊ะนี้ที่รออาหาร: " + heldItem);
        }
    }

    /// <summary>
    /// เช็คค่า Patience ของแมวตอนเสิร์ฟ ว่าต่ำกว่า 50% หรือไม่ เพื่อสุ่มทำซอสเลอะหน้าจอ
    /// </summary>
    private void CheckAndTriggerSmudgeOnServe(NPCController npc)
    {
        if (ScreenSmudgeManager.Instance != null && npc != null)
        {
            // ดึงค่าสัดส่วนความอดทนปัจจุบัน (เทียบจากฟังก์ชันคำนวณหลอดพลังใน NPCController หรือเช็ค Ratio)
            float patienceRatio = npc.GetPatienceRatio(); // เดี๋ยวเราจะเพิ่มเมธอดนี้ใน NPCController

            Debug.Log($"[CustomerTable] ตรวจสอบความอดทนของแมว {npc.gameObject.name} ตอนเสิร์ฟ: {patienceRatio * 100f}%");

            // ถ้าความอดทนเหลือน้อยกว่า 50% (0.5) ให้ลองสุ่มเรียก Screen Smudge
            if (patienceRatio <= 0.5f)
            {
                ScreenSmudgeManager.Instance.TryTriggerServeSmudge();
            }
        }
    }
    public void LeaveMoneyOnTable(int reward, bool isVIP)
    {
        int finalReward = isVIP ? Mathf.RoundToInt(reward * 2f) : reward;
        pendingMoneyReward += finalReward;

        bool alreadyVisible = hasMoneyToCollect; // แมวตัวที่ 2+ จ่ายเพิ่ม -> เด้งเบาๆ
        hasMoneyToCollect = true;

        ShowMoneyIcon(alreadyVisible);

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.catsServedToday++;
            DayNightManager.Instance.moneyEarnedToday += finalReward;
        }
    }

    /// <summary>
    /// ผู้เล่นเดินมาเก็บเงินที่โต๊ะ
    /// </summary>
    public bool TryCollectMoney()
    {
        if (!hasMoneyToCollect) return false;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(pendingMoneyReward);
        }

        PlayCoinSound();

        // 🟢 เพิ่ม Debug ตรงนี้เพื่อเช็คว่า Manager ถูกเรียกไหม
        if (MoneyFlyEffectManager.Instance != null)
        {
            Debug.Log("[CustomerTable] ✅ สั่งให้เงินบินแล้ว!");
            MoneyFlyEffectManager.Instance.PlayFlyToUIPool(transform.position);
        }
        else
        {
            Debug.LogWarning("[CustomerTable] ❌ หา MoneyFlyEffectManager.Instance ไม่พบใน Scene!");
        }

        hasMoneyToCollect = false;
        pendingMoneyReward = 0;
        HideMoneyIcon();

        if (sittingNPCs.Count == 0)
        {
            ResetTable();
        }

        return true;
    }

    /// <summary>
    /// จัดการเมื่อแมวแต่ละตัวลุกออกจากโต๊ะ
    /// </summary>
    public void RemoveNPCFromTable(NPCController npc)
    {
        if (sittingNPCs.Contains(npc))
        {
            sittingNPCs.Remove(npc);
        }

        if (sittingNPCs.Count > 0)
        {
            return;
        }

        if (hasMoneyToCollect)
        {
            return;
        }

        ResetTable();
    }

    private void BoostRemainingNPCsPatience(NPCController servedNPC)
    {
        if (sittingNPCs == null) return;

        foreach (var npc in sittingNPCs)
        {
            if (npc != null && npc != servedNPC && !npc.hasBeenServed && npc.currentState == NPCController.NPCState.Sitting)
            {
                npc.AddExtraPatience(patienceBonusOnFriendServed);
            }
        }
    }

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
        StopTableChat();
        wantedItem = "";
        sittingNPCs.Clear();
        hasMoneyToCollect = false;
        pendingMoneyReward = 0;
        if (tableItemRenderer != null) tableItemRenderer.enabled = false;
        HideMoneyIcon();
        SetVIPVisual(false);
    }

    public void SetVIPVisual(bool isVIP)
    {
        if (vipBadgeIcon != null) vipBadgeIcon.SetActive(isVIP);
    }

    public bool HasAnyNPCWaiting()
    {
        if (sittingNPCs == null || sittingNPCs.Count == 0) return false;

        foreach (var npc in sittingNPCs)
        {
            if (npc != null && npc.currentState == NPCController.NPCState.Sitting && !npc.hasBeenServed)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 🟢 true = อาหารที่ผู้เล่นถืออยู่ (heldItem) ตรงกับที่แมวตัวใดตัวหนึ่งบนโต๊ะนี้สั่งไว้ และยังไม่ได้เสิร์ฟ
    /// ใช้เงื่อนไขเดียวกับ TryServeFood() เพื่อให้ Tutorial Pointer ชี้ตรงกับที่เสิร์ฟได้จริง
    /// </summary>
    public bool CanServeItem(string heldItem)
    {
        if (string.IsNullOrEmpty(heldItem) || sittingNPCs == null) return false;
        heldItem = heldItem.Trim();

        foreach (var n in sittingNPCs)
        {
            if (n == null || n.hasBeenServed) continue;
            if (n.currentState != NPCController.NPCState.Sitting) continue;
            if (n.requestedRecipe != null &&
                n.requestedRecipe.recipeName.Trim().Equals(heldItem, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // fallback เหมือน TryServeFood: ตรงกับ wantedItem ของโต๊ะ และมีแมวที่ยังไม่ได้เสิร์ฟ
        if (!string.IsNullOrEmpty(wantedItem) &&
            heldItem.Equals(wantedItem.Trim(), System.StringComparison.OrdinalIgnoreCase))
            return HasAnyNPCWaiting();

        return false;
    }

    /// <summary>
    /// 🟢 true = มีแมวที่โต๊ะนี้สั่งอาหารแล้วและยังไม่ได้รับอาหาร
    /// </summary>
    public bool HasAnyNPCOrdered()
    {
        if (sittingNPCs == null || sittingNPCs.Count == 0) return false;

        foreach (var npc in sittingNPCs)
        {
            if (npc != null && npc.HasPlacedOrder) return true;
        }
        return false;
    }

    // ---------------------------------------------------------------
    //  Money Pop Animation
    // ---------------------------------------------------------------
    private void ShowMoneyIcon(bool bumpOnly)
    {
        if (moneyIconOnTable == null) return;

        if (moneyAnimRoutine != null) StopCoroutine(moneyAnimRoutine);
        moneyIconOnTable.SetActive(true);
        moneyAnimRoutine = StartCoroutine(MoneyPopRoutine(bumpOnly));
    }

    private void HideMoneyIcon()
    {
        if (moneyAnimRoutine != null)
        {
            StopCoroutine(moneyAnimRoutine);
            moneyAnimRoutine = null;
        }

        if (moneyIconOnTable != null)
        {
            // คืนค่าเดิม เพื่อให้รอบหน้าเริ่มจากจุดเดิมเสมอ
            moneyIconOnTable.transform.localScale = moneyIconBaseScale;
            moneyIconOnTable.transform.localPosition = moneyIconBasePos;
            moneyIconOnTable.SetActive(false);
        }
    }

    private IEnumerator MoneyPopRoutine(bool bumpOnly)
    {
        Transform icon = moneyIconOnTable.transform;

        float duration = bumpOnly ? popDuration * 0.6f : popDuration;
        float startMul = bumpOnly ? 0.8f : 0f;

        icon.localScale = moneyIconBaseScale * startMul;
        icon.localPosition = moneyIconBasePos;

        // ---------- 1) เด้งขึ้นมา ----------
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            float k = Mathf.Clamp01(t);

            // โตแบบเด้งเกินแล้วหดกลับ
            float mul = startMul + (1f - startMul) * EaseOutBackMoney(k);

            // ยืด-หด (squash & stretch) ให้ดูนุ่มๆ น่ารัก
            float stretch = 1f + Mathf.Sin(k * Mathf.PI * 2f) * 0.15f * (1f - k);
            Vector3 scaleMul = new Vector3(mul / stretch, mul * stretch, mul);
            icon.localScale = Vector3.Scale(moneyIconBaseScale, scaleMul);

            // กระโดดขึ้นเป็นวงโค้งเล็กๆ
            float hop = Mathf.Sin(k * Mathf.PI) * popHopHeight * (bumpOnly ? 0.5f : 1f);
            icon.localPosition = moneyIconBasePos + Vector3.up * hop;

            yield return null;
        }

        icon.localScale = moneyIconBaseScale;
        icon.localPosition = moneyIconBasePos;

        // ---------- 2) โยกดุ๊กดิ๊กรอให้เก็บ ----------
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime;
            float bob = Mathf.Sin(time * idleBobSpeed);
            icon.localPosition = moneyIconBasePos + Vector3.up * (bob * idleBobHeight);

            float pulse = 1f + Mathf.Sin(time * idleBobSpeed + Mathf.PI * 0.5f) * 0.04f;
            icon.localScale = moneyIconBaseScale * pulse;

            yield return null;
        }
    }

    private float EaseOutBackMoney(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}