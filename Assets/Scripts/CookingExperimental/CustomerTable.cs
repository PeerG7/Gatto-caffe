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
    /// หาเก้าอี้ที่ยังว่างอยู่
    /// </summary>
    public Transform GetAvailableSeat()
    {
        if (seatPoints == null || seatPoints.Count == 0) return transform;

        if (sittingNPCs.Count < seatPoints.Count)
        {
            return seatPoints[sittingNPCs.Count];
        }
        return null;
    }

    public void AssignNPCToTable(NPCController npc)
    {
        if (!sittingNPCs.Contains(npc))
        {
            sittingNPCs.Add(npc);
            npc.assignedTable = this;

            // ตรวจสอบเมื่อแมวนั่งลง หากนั่งครบ 2 ที่นั่ง จะสุ่มเปิด Pop-up คุยกัน
            CheckAndTriggerChat();
        }
    }

    /// <summary>
    /// สุ่ม Trigger เปิดก้อนเมฆบนหัวแมว 2 ตัวเมื่อนั่งโต๊ะเดียวกัน
    /// </summary>
    private void CheckAndTriggerChat()
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
                Debug.Log($"💬 [CustomerTable] แมวบนโต๊ะ {gameObject.name} เกิด Pop-up นั่งคุยกัน!");
            }
        }
    }

    public void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player == null || !player.HasItem()) return;

        string heldItem = player.currentItem.Trim();

        // ค้นหาแมวบนโต๊ะที่นั่งอยู่ ยังไม่ได้กิน และสั่งเมนูตรงกับอาหารในมือของผู้เล่น
        NPCController targetNPC = sittingNPCs.Find(n =>
            n != null &&
            !n.hasBeenServed &&
            n.requestedRecipe != null &&
            n.requestedRecipe.recipeName.Trim().Equals(heldItem, System.StringComparison.OrdinalIgnoreCase)
        );

        // กรณีฉุกเฉิน: ถ้าไม่ได้ตั้ง requestedRecipe บนตัวแมว ให้เช็กเทียบกับ wantedItem ของโต๊ะ
        if (targetNPC == null && !string.IsNullOrEmpty(wantedItem) && heldItem.Equals(wantedItem.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            targetNPC = sittingNPCs.Find(n => n != null && !n.hasBeenServed);
        }

        if (targetNPC != null)
        {
            PlayerController2D.IsLocked = false;
            targetNPC.hasBeenServed = true;

            // บวกเวลาความอดทนให้แมวตัวอื่นในกลุ่มที่ยังนั่งรออยู่
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

            // สุ่มการไป Interaction Zone
            bool willInteract = Random.Range(0, 101) <= interactionChance;

            // ถอดแมวออกจากรายการนั่งของโต๊ะ
            RemoveNPCFromTable(targetNPC);

            // สั่งให้แมวตัวนี้ลุกไปทาน/เดินออกจากร้าน
            targetNPC.FinishServingAndProceed(willInteract);
        }
        else
        {
            Debug.Log("ไม่มีแมวตัวไหนบนโต๊ะนี้ที่รออาหาร: " + heldItem);
        }
    }

    /// <summary>
    /// เอาแมวออกจากโต๊ะเมื่อกินเสร็จหรือหมดความอดทนลุกออกไป
    /// </summary>
    public void RemoveNPCFromTable(NPCController npc)
    {
        if (sittingNPCs.Contains(npc))
        {
            sittingNPCs.Remove(npc);
        }

        // ตรวจสอบว่าแมวทุกตัวบนโต๊ะได้รับการเสิร์ฟหรือลุกออกไปหมดแล้วหรือยัง
        bool hasUnservedNPC = sittingNPCs.Exists(n => n != null && !n.hasBeenServed);
        if (!hasUnservedNPC)
        {
            ResetTable();
        }
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