using UnityEngine;
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

    public void TriggerTableChat()
    {
        if (seatPoints.Count >= 2 && sittingNPCs.Count >= 2)
        {
            int roll = Random.Range(0, 100);
            if (roll < chatTriggerChance)
            {
                for (int i = 0; i < sittingNPCs.Count; i++)
                {
                    if (sittingNPCs[i] != null)
                    {
                        CatChatBubble bubble = sittingNPCs[i].GetComponentInChildren<CatChatBubble>();
                        if (bubble != null)
                        {
                            bubble.ShowChatBubble();
                        }
                    }
                }
            }
        }
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
            PlayerController2D.IsLocked = false;
            targetNPC.hasBeenServed = true;

            BoostRemainingNPCsPatience(targetNPC);
            player.ClearItem();

            targetNPC.StartEatingProcess();
        }
        else
        {
            Debug.Log("ไม่มีแมวตัวไหนบนโต๊ะนี้ที่รออาหาร: " + heldItem);
        }
    }

    /// <summary>
    /// สะสมเงินไว้ที่โต๊ะเมื่อแมวกินเสร็จทีละตัว (รองรับการ Stack เงินรวมกัน)
    /// </summary>
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