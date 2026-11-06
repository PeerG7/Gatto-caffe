using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerInventory))]
public class AutoServerBot : MonoBehaviour
{
    [Header("Shop & Price Settings")]
    public int botPrice = 800;
    public bool isActivated = false;

    [Header("Bot Settings")]
    public float checkInterval = 1.0f;
    public float interactRange = 2.0f; // ระยะที่ใกล้พอจะหยิบจาก Tray หรือเสิร์ฟที่โต๊ะ
    private float timer = 0f;

    private NavMeshAgent agent;
    private PlayerInventory playerInventory;

    private enum BotState { None, GoingToTray, GoingToTable }
    private BotState currentState = BotState.None;

    private TraySlot targetTraySlot = null;
    private CustomerTable targetTable = null;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerInventory = GetComponent<PlayerInventory>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    void Start()
    {
        UpdateVisualState();
    }

    void Update()
    {
        if (!isActivated) return;

        bool isPaused = DayNightManager.Instance != null && DayNightManager.Instance.isPaused;
        bool isNotWork = DayNightManager.Instance != null && !DayNightManager.Instance.isWorkTime;
        if (isPaused || isNotWork)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        // 1. ถ้าบอทกำลังถือของอยู่ -> เดินไปเสิร์ฟที่โต๊ะทุกเฟรม
        if (playerInventory != null && playerInventory.HasItem())
        {
            HandleServingState();
        }
        else
        {
            // 2. ถ้ากำลังเดินไปที่ Tray ให้สั่งเดินและเช็กการมาถึงทุกเฟรม
            if (currentState == BotState.GoingToTray && targetTraySlot != null)
            {
                MoveToPosition(targetTraySlot.transform.position);
                CheckArrivalAtTray();
            }
            else
            {
                // 3. ถ้ายังไม่มีเป้าหมาย ให้เช็ครอบเวลาเพื่อค้นหาอาหารใน Tray
                timer += Time.deltaTime;
                if (timer >= checkInterval)
                {
                    timer = 0f;
                    SearchMatchingFoodInTray();
                }
            }
        }
    }

    void SearchMatchingFoodInTray()
    {
        if (TrayManager.instance == null) return;

        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();
        if (allTables == null || allTables.Length == 0) return;

        foreach (var table in allTables)
        {
            if (table != null && table.HasAnyNPCWaiting() && !string.IsNullOrEmpty(table.wantedItem))
            {
                foreach (var slot in TrayManager.instance.traySlots)
                {
                    if (slot != null && slot.HasFood())
                    {
                        string trayItemName = slot.GetFoodItemName();
                        if (trayItemName.Trim().Equals(table.wantedItem.Trim(), System.StringComparison.OrdinalIgnoreCase))
                        {
                            targetTraySlot = slot;
                            targetTable = table;
                            currentState = BotState.GoingToTray;
                            break;
                        }
                    }
                }
            }
            if (currentState == BotState.GoingToTray) break;
        }
    }

    void CheckArrivalAtTray()
    {
        if (targetTraySlot == null)
        {
            currentState = BotState.None;
            return;
        }

        float dist = Vector2.Distance(transform.position, targetTraySlot.transform.position);
        if (dist <= interactRange)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            string itemName = targetTraySlot.GetFoodItemName();
            Sprite itemSprite = targetTraySlot.GetFoodSprite();

            if (playerInventory != null)
            {
                playerInventory.PickUpItem(itemName, itemSprite);
                targetTraySlot.ClearSlot();
                Debug.Log($"[AutoServerBot] หยิบอาหาร '{itemName}' จาก Tray ขึ้นมือสำเร็จ!");
            }

            currentState = BotState.GoingToTable;
        }
    }

    void HandleServingState()
    {
        if (targetTable == null)
        {
            currentState = BotState.None;
            return;
        }

        currentState = BotState.GoingToTable;
        MoveToPosition(targetTable.transform.position);

        float dist = Vector2.Distance(transform.position, targetTable.transform.position);
        if (dist <= interactRange)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            if (targetTable.HasAnyNPCWaiting())
            {
                targetTable.TryServeFood();
                playerInventory.ClearItem();
                Debug.Log($"[AutoServerBot] เสิร์ฟอาหารสำเร็จที่โต๊ะเรียบร้อย!");
            }

            targetTraySlot = null;
            targetTable = null;
            currentState = BotState.None;
        }
    }

    void MoveToPosition(Vector3 targetPos)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(targetPos);
        }
    }

    public void TryBuyAutoServerBot()
    {
        if (isActivated) return;

        if (CurrencyManager.Instance != null)
        {
            if (CurrencyManager.Instance.TrySpendMoney(botPrice))
            {
                ActivateBot();
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("Buy AutoServerBot Success");
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
        Debug.Log("[AutoServerBot] เปิดใช้งานบอทเสิร์ฟอาหารแล้ว!");
    }

    void UpdateVisualState()
    {
        gameObject.SetActive(isActivated);
    }
}