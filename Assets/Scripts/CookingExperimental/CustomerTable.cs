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
        if (moneyIconOnTable != null) moneyIconOnTable.SetActive(false);
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

            // 🟢 ตรวจสอบค่าความอดทน (Patience) ตอนที่เสิร์ฟ ถ้าเหลือต่ำกว่า 50% มีโอกาสสุ่ม Screen Smudge
            CheckAndTriggerSmudgeOnServe(targetNPC);

            targetNPC.StartEatingProcess();
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
        hasMoneyToCollect = true;

        if (moneyIconOnTable != null)
            moneyIconOnTable.SetActive(true);

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
        if (moneyIconOnTable != null)
            moneyIconOnTable.SetActive(false);

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
        if (moneyIconOnTable != null) moneyIconOnTable.SetActive(false);
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
}