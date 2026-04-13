using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerTable : MonoBehaviour
{
    [Header("Menu Settings")]
    public List<string> menuList;
    public string wantedItem;

    [Header("Visuals")]
    public GameObject orderBubble; // ตัวที่อยู่บนหัวแมว (ลากมาจาก Prefab แมว)
    public SpriteRenderer orderIconRenderer;
    public SpriteRenderer tableItemRenderer;
    public GameObject heartIcon;
    public GameObject angryIcon;
    public GameObject moneyPopupPrefab;

    private bool playerInRange = false;
    private bool isWaitingForFood = false;
    private NPCController currentNPC; // เพิ่มตัวแปรเก็บแมวที่นั่งอยู่

    void Start()
    {
        if (orderBubble != null) orderBubble.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            TryServeFood();
        }
    }

    // แก้ไข: รับค่าแมวเข้ามา
    public void OnCustomerSeated(NPCController npc)
    {
        currentNPC = npc; // เก็บค่า NPC ไว้ใช้งาน

        // 🔍 เคลียร์ค่าเก่าก่อนเริ่มใหม่
        if (orderBubble != null) orderBubble.SetActive(false);

        // 🔍 ค้นหา Bubble และ Icon จากตัวแมวที่เดินมานั่งจริง ๆ
        // โดยค้นหาจากลูก (Child) ของ NPC ที่ชื่อว่า "Bubble" และ "Wanted item render"
        Transform bubbleTransform = npc.transform.Find("Bubble");
        if (bubbleTransform != null)
        {
            orderBubble = bubbleTransform.gameObject;
            Transform iconTransform = bubbleTransform.Find("Wanted item render");
            if (iconTransform != null)
            {
                orderIconRenderer = iconTransform.GetComponent<SpriteRenderer>();
            }
        }

        StartCoroutine(CustomerThinkingRoutine());
    }

    IEnumerator CustomerThinkingRoutine()
    {
        Debug.Log("🐱 Customer is thinking...");
        float thinkingTime = Random.Range(2f, 4f); // รอ 2-4 วินาทีตาม Storyboard
        yield return new WaitForSeconds(thinkingTime);

        if (menuList.Count > 0)
        {
            wantedItem = menuList[Random.Range(0, menuList.Count)]; // สุ่มเมนูจาก list

            if (orderBubble != null)
            {
                orderBubble.SetActive(true); // เปิด Bubble บนหัวแมว
                                             // หากคุณมี Sprite ของอาหารแต่ละชนิด ให้สั่งเปลี่ยนที่นี่:
                                             // orderIconRenderer.sprite = หาจากระบบเมนูของคุณ;
            }

            isWaitingForFood = true;
            Debug.Log("🐱 Customer ordered: " + wantedItem);
        }
    }

    void TryServeFood()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();

        if (player != null && player.HasItem())
        {
            tableItemRenderer.sprite = player.heldItemRenderer.sprite;
            tableItemRenderer.enabled = true;

            if (player.currentItem == wantedItem)
            {
                if (heartIcon != null) heartIcon.SetActive(true);
                GiveReward(100);
                Invoke("StartRelationship", 1.5f);
            }
            else
            {
                if (angryIcon != null) angryIcon.SetActive(true);
                GiveReward(0);
            }

            if (orderBubble != null) orderBubble.SetActive(false);
            isWaitingForFood = false;
            player.ClearItem();
        }
    }

    void StartRelationship()
    {
        // ใช้ currentNPC ที่เก็บไว้มาเปิดหน้า Relationship
        if (currentNPC != null)
        {
            NPCInteract interact = currentNPC.GetComponent<NPCInteract>();
            if (interact != null) interact.RelationShip();
        }
    }

    void GiveReward(int amount) { /* โค้ดเดิมของคุณ */ }
    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) playerInRange = true; }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) playerInRange = false; }
}