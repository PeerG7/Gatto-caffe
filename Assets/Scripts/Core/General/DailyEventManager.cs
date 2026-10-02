using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// DailyEventManager
// สุ่ม Event 1 อันต่อวัน (หรือไม่มีเลยก็ได้) — ผลมีอายุแค่วันนั้น
// วันใหม่ (OnNewDayStarted) จะสุ่มใหม่ทับของเดิมทันที
// ระบบอื่นอ่านค่าผ่าน static property ด้านล่าง (ปลอดภัยแม้ไม่มี Instance = 1x)
// =====================================================================
public class DailyEventManager : MonoBehaviour
{
    public static DailyEventManager Instance;

    [System.Serializable]
    public class DailyEvent
    {
        public string eventName = "New Event";
        [TextArea] public string description;
        [Tooltip("น้ำหนักการสุ่ม ยิ่งมากยิ่งเจอบ่อย")]
        public float weight = 1f;

        [Header("Multipliers (1 = ปกติ)")]
        [Tooltip("เงินที่แมวจ่าย เช่น 1.5 = จ่ายเพิ่ม 50%")]
        public float moneyMultiplier = 1f;
        [Tooltip("อัตราแมวเกิด เช่น 2 = เกิดถี่ขึ้น 2 เท่า")]
        public float spawnRateMultiplier = 1f;
        [Tooltip("ราคาเฟอร์นิเจอร์ เช่น 0.7 = ลด 30%")]
        public float furniturePriceMultiplier = 1f;
        [Tooltip("ความเร็วทำอาหาร เช่น 1.5 = เร็วขึ้น 1.5 เท่า")]
        public float cookingSpeedMultiplier = 1f;
    }

    [Header("Event Pool")]
    public List<DailyEvent> events = new List<DailyEvent>();

    [Header("Settings")]
    [Range(0, 100)]
    [Tooltip("โอกาส % ที่วันนั้นจะไม่มี Event (วันปกติ)")]
    public int noEventChance = 30;
    [Tooltip("กันไม่ให้ได้ Event ซ้ำกับเมื่อวาน")]
    public bool avoidRepeatYesterday = true;

    public DailyEvent CurrentEvent { get; private set; }

    /// <summary>ยิงทุกครั้งที่สุ่มใหม่ (CurrentEvent เป็น null = วันปกติ) — ให้ UI ฟังไปแสดงป้ายได้</summary>
    public event System.Action<DailyEvent> OnEventChanged;

    // ── Safe accessors: เรียกจากที่ไหนก็ได้ ──────────────────────
    public static float MoneyMult =>
        Instance != null && Instance.CurrentEvent != null ? Instance.CurrentEvent.moneyMultiplier : 1f;
    public static float SpawnRateMult =>
        Instance != null && Instance.CurrentEvent != null ? Instance.CurrentEvent.spawnRateMultiplier : 1f;
    public static float FurniturePriceMult =>
        Instance != null && Instance.CurrentEvent != null ? Instance.CurrentEvent.furniturePriceMultiplier : 1f;
    public static float CookingSpeedMult =>
        Instance != null && Instance.CurrentEvent != null ? Instance.CurrentEvent.cookingSpeedMultiplier : 1f;

    private DailyEvent _lastEvent;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // subscribe ใน Start เพื่อให้ DayNightManager.Instance ถูกตั้งใน Awake เสร็จก่อน
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDayStarted += RollNewEvent;

        RollNewEvent(); // วันที่ 1
    }

    void OnDestroy()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDayStarted -= RollNewEvent;
        if (Instance == this) Instance = null;
    }

    public void RollNewEvent()
    {
        CurrentEvent = null;

        if (events.Count > 0 && Random.Range(0, 100) >= noEventChance)
        {
            CurrentEvent = PickWeighted();
        }

        _lastEvent = CurrentEvent;

        Debug.Log(CurrentEvent != null
            ? $"🎲 Daily Event: {CurrentEvent.eventName}"
            : "🎲 Daily Event: (วันปกติ)");

        OnEventChanged?.Invoke(CurrentEvent);
    }

    DailyEvent PickWeighted()
    {
        var pool = new List<DailyEvent>();
        foreach (var e in events)
        {
            if (e.weight <= 0f) continue;
            if (avoidRepeatYesterday && events.Count > 1 && e == _lastEvent) continue;
            pool.Add(e);
        }
        if (pool.Count == 0) return null;

        float total = 0f;
        foreach (var e in pool) total += e.weight;

        float roll = Random.Range(0f, total);
        foreach (var e in pool)
        {
            roll -= e.weight;
            if (roll <= 0f) return e;
        }
        return pool[pool.Count - 1];
    }
}