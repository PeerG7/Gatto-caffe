using UnityEngine;
using System.Collections.Generic;

public class CustomerTable : MonoBehaviour
{
    [Header("Table Configuration")]
    [Tooltip("ตำแหน่งเก้าอี้ทั้งหมดที่มีในโต๊ะนี้ (เช่น Seat1, Seat2)")]
    public List<Transform> seatPoints = new List<Transform>();

    // เก็บรายการ NPC ที่กำลังนั่งอยู่จริงในโต๊ะนี้
    [HideInInspector] public List<NPCController> sittingNPCs = new List<NPCController>();

    // โต๊ะจะถือว่าเต็ม เมื่อจำนวนแมวที่นั่งอยู่เท่ากับจำนวนเก้าอี้ที่มี
    public bool isFull => sittingNPCs.Count >= seatPoints.Count;
    public bool isOccupied => sittingNPCs.Count > 0;

    public string wantedItem;
    public int dishReward = 100;

    [Header("Visual Elements")]
    public SpriteRenderer tableItemRenderer;
    public GameObject angryIcon;

    [Header("VIP Visual")]
    public GameObject vipBadgeIcon;

    [Header("Interaction Chance")]
    [Range(0, 100)] public int interactionChance = 70;

    [Header("Chat Interaction Settings")]
    [Range(0, 100)]
    [Tooltip("โอกาสที่จะเกิด Pop-up ก้อนเมฆสุ่มไอคอนคุยกัน เมื่อแมวนั่งโต๊ะ 2 ที่นั่งครบทั้งคู่ (0-100%)")]
    public int chatTriggerChance = 70;

    [Header("SFX")]
    public AudioClip coinSoundClip;
    public AudioSource sfxSource;

    [Header("Group Serve Settings")]
    [Tooltip("เวลาที่จะบวกเพิ่มให้เพื่อนในโต๊ะเมื่อมีคนในโต๊ะได้รับอาหาร (วินาที)")]
    public float patienceBonusOnFriendServed = 15f;

    private void Awake()
    {
        SetWorldSpaceCamera();
        AutoFindSeatPoints();
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

    /// <summary>
    /// หาเก้าอี้ที่ยังว่างอยู่ (ตรวจสอบเฉพาะเก้าอี้ที่ไม่มีแมวจับจองจริงๆ)
    /// </summary>
    public Transform GetAvailableSeat()
    {
        if (seatPoints == null || seatPoints.Count == 0) return transform;

        // ถ้าระหว่างที่มีแมวนั่งอยู่ ห้ามให้คนนอกร้านมาแจมเด็ดขาด
        if (sittingNPCs.Count > 0)
        {
            foreach (Transform seat in seatPoints)
            {
                bool isSeatTaken = sittingNPCs.Exists(npc => npc != null && npc.seatPoint == seat);
                if (!isSeatTaken)
                {
                    return seat; // ส่งเก้าอี้ที่ว่างจริง ๆ ให้เฉพาะแมวในกลุ่มเดียวกัน
                }
            }
            return null;
        }

        // กรณีโต๊ะว่างสนิท
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

    /// <summary>
    /// สั่งให้แมวบนโต๊ะแสดงบทสนทนาร่วมกัน
    /// </summary>
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
                Debug.Log($"💬 [CustomerTable] แมวบนโต๊ะ {gameObject.name} เริ่มพูดคุยกันแทนการสั่งอาหาร!");
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

            int finalReward = dishReward;
            if (targetNPC.isVIP)
                finalReward = Mathf.RoundToInt(dishReward * targetNPC.vipMoneyMultiplier);

            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.catsServedToday++;
                DayNightManager.Instance.moneyEarnedToday += finalReward;
            }

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.AddMoney(finalReward);

            PlayCoinSound();
            player.ClearItem();

            bool willInteract = Random.Range(0, 101) <= interactionChance;

            RemoveNPCFromTable(targetNPC);

            targetNPC.FinishServingAndProceed(willInteract);
        }
        else
        {
            Debug.Log("ไม่มีแมวตัวไหนบนโต๊ะนี้ที่รออาหาร: " + heldItem);
        }
    }

    /// <summary>
    /// เอาแมวออกจากโต๊ะ (คงสถานะโต๊ะไว้จนกว่าจะลุกออกหมดทุกตัว)
    /// </summary>
    public void RemoveNPCFromTable(NPCController npc)
    {
        if (sittingNPCs.Contains(npc))
        {
            sittingNPCs.Remove(npc);
        }

        // ถ้ายังมีแมวตัวอื่นนั่งอยู่บนโต๊ะ -> ห้ามรีเซ็ตโต๊ะเด็ดขาด
        if (sittingNPCs.Count > 0)
        {
            Debug.Log($"🪑 [CustomerTable] แมวตัวหนึ่งลุกออกจากโต๊ะ {gameObject.name} แล้ว แต่ยังมีเพื่อนร่วมโต๊ะนั่งอยู่ คงสถานะโต๊ะไว้");
            return;
        }

        // ถ้าไม่มีแมวเหลืออยู่บนโต๊ะแล้ว ถึงค่อยเคลียร์และรีเซ็ตโต๊ะ
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
        if (tableItemRenderer != null) tableItemRenderer.enabled = false;
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