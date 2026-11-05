using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class AutoInviter : MonoBehaviour
{
    [Header("Shop & Price Settings")]
    public int botPrice = 500;

    [Header("Automation Settings")]
    public bool isActivated = false;
    public float checkInterval = 0.5f;
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
            TryAutoInviteWhenQueueReached();
        }
    }

    void TryAutoInviteWhenQueueReached()
    {
        if (QueueManager.Instance == null) return;

        // 🌟 ลบบล็อกเช็ก hasCatWaitingForGuidance ออกไปเลยครับ
        // เพื่อให้ AutoInviter สนใจแค่คิวปกติหน้าร้านอย่างเดียว ไม่สน LostZone

        // 1. ดึงแมวตัวแรกที่ยืนรออยู่หน้าสุดของคิวปกติ
        NPCController frontNpc = QueueManager.Instance.GetFrontNPC();
        if (frontNpc != null)
        {
            NavMeshAgent agent = frontNpc.GetComponent<NavMeshAgent>();
            if (agent != null && !agent.pathPending)
            {
                bool hasReachedQueue = agent.remainingDistance <= agent.stoppingDistance;
                if (hasReachedQueue)
                {
                    int groupSize = frontNpc.GetGroupSize();
                    if (NPCController.HasAvailableTableForGroup(groupSize))
                    {
                        QueueManager.Instance.InviteGroup(frontNpc);
                        Debug.Log($"[AutoInviter Bot] 🤖 พาแมวคิวแรกกลุ่ม {frontNpc.groupID} เข้าโต๊ะแล้ว");
                    }
                }
            }
        }
    }

    public void TryBuyBot()
    {
        if (isActivated) return;

        if (CurrencyManager.Instance != null)
        {
            if (CurrencyManager.Instance.TrySpendMoney(botPrice))
            {
                ActivateBot();
                if (UINotificationManager.Instance != null)
                    UINotificationManager.Instance.ShowNotification("Buy AutoInviter Success"); //เปลี่ยนรายละเอียดตรงนี้
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
    }

    void UpdateVisualState()
    {
        gameObject.SetActive(isActivated);
    }
}