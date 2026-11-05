using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class AutoCooking : MonoBehaviour
{
    [Header("Shop & Price Settings")]
    [Tooltip("ราคาสำหรับซื้อบอททำอาหารอัตโนมัติ")]
    public int botPrice = 800;

    [Header("Automation Settings")]
    [Tooltip("สถานะการทำงาน (True = ซื้อและเปิดใช้งานแล้ว, False = ยังไม่ถูกซื้อ)")]
    public bool isActivated = false;

    [Tooltip("ระยะเวลาในการเช็กออเดอร์ (วินาที)")]
    public float checkInterval = 1.0f;
    private float timer = 0f;

    void Start()
    {
        UpdateVisualState();
    }

    void Update()
    {
        if (!isActivated) return;

        bool isPaused = DayNightManager.Instance != null && DayNightManager.Instance.isPaused;
        bool isNotWork = DayNightManager.Instance != null && !DayNightManager.Instance.isWorkTime;
        if (isPaused || isNotWork) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            TryAutoCookExactDemand();
        }
    }

    void TryAutoCookExactDemand()
    {
        // 1. เช็กก่อนว่า Tray มีช่องว่างเหลือไหม ถ้าเต็มแล้วให้หยุดทำรอก่อน
        if (TrayManager.instance == null || !TrayManager.instance.HasEmptySlot()) return;

        // 2. รวบรวมรายชื่อลูกค้า (NPC) ทั้งหมดในร้านที่กำลังนั่งรออาหารอยู่
        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        if (allTables == null || allTables.Length == 0) return;

        List<RecipeSO> waitingOrders = new List<RecipeSO>();
        foreach (var table in allTables)
        {
            if (table != null && table.sittingNPCs != null)
            {
                foreach (var npc in table.sittingNPCs)
                {
                    if (npc != null && !npc.hasBeenServed && npc.requestedRecipe != null)
                    {
                        waitingOrders.Add(npc.requestedRecipe);
                    }
                }
            }
        }

        // ถ้าไม่มีลูกค้าสั่งอาหารค้างไว้เลย บอทจะไม่ทำ
        if (waitingOrders.Count == 0) return;

        // 3. นับจำนวนอาหารที่วางรออยู่ใน Tray (ช่องที่ไม่ว่าง) ตอนนี้
        int occupiedTrayCount = 0;
        foreach (var slot in TrayManager.instance.traySlots)
        {
            if (slot != null && slot.HasFood())
            {
                occupiedTrayCount++;
            }
        }

        // 4. เปรียบเทียบ: ถ้าจำนวนออเดอร์ที่ลูกค้ารอ มากกว่าจำนวนอาหารใน Tray
        // บอทจะทำเพิ่มทีละ 1 จานเพื่อให้จำนวนอาหารพอดีกับความต้องการของลูกค้า
        if (waitingOrders.Count > occupiedTrayCount)
        {
            RecipeSO recipeToCook = waitingOrders[0];
            if (recipeToCook != null)
            {
                // ส่งอาหารเข้าไปวางใน Array ของ Tray ผ่าน TrayManager
                bool success = TrayManager.instance.ReceiveFood(recipeToCook.recipeName, recipeToCook.finalDishSprite);

                if (success)
                {
                    Debug.Log($"[AutoCooking Bot] 🍳 ทำเมนู [{recipeToCook.recipeName}] สำเร็จ (ทำพอดีตามจำนวนลูกค้า) และวางลง Tray แล้ว");
                }
            }
        }
    }

    public void TryBuyAutoCookBot()
    {
        if (isActivated) return;

        if (CurrencyManager.Instance != null)
        {
            if (CurrencyManager.Instance.TrySpendMoney(botPrice))
            {
                ActivateBot();
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("Buy AutoCooking Success");
            }
            else
            {
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("เงินไม่พอ! ต้องการ " + botPrice + " $");
            }
        }
    }

    public void ActivateBot()
    {
        isActivated = true;
        UpdateVisualState();
        Debug.Log("[AutoCooking Bot] 🍳 เปิดใช้งานบอททำอาหาร AutoCooking แล้ว!");
    }

    void UpdateVisualState()
    {
        gameObject.SetActive(isActivated);
    }
}