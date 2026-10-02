using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// DailyEventManager (v3 — Player Choice)
// โหมด PlayerChoice : ต้นวันสุ่มเสนอ choicesOffered อัน → ผู้เล่นเลือก 1 อัน
//                     (DailyEventChoiceUI ฟัง OnEventsOffered แล้วเรียก SelectEvent)
// โหมด RandomAuto   : สุ่มให้เลย eventsPerDay อัน ไม่ต้องเลือก (แบบเดิม)
// ผลของ Event มีอายุแค่วันนั้น — ขึ้นวันใหม่สุ่มใหม่ (ผ่าน OnNewDayStarted)
// ชื่อ static property (MoneyMult ฯลฯ) เหมือนเดิม → สคริปต์อื่นไม่ต้องแก้
// =====================================================================
public class DailyEventManager : MonoBehaviour
{
    public static DailyEventManager Instance;

    public enum EventSelectionMode { PlayerChoice, RandomAuto }

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

    [Header("Selection")]
    public EventSelectionMode selectionMode = EventSelectionMode.PlayerChoice;
    [Min(2)]
    [Tooltip("PlayerChoice: จำนวนตัวเลือกที่เสนอให้ผู้เล่น")]
    public int choicesOffered = 2;
    [Min(1)]
    [Tooltip("RandomAuto: จำนวน Event ที่สุ่มให้ต่อวัน")]
    public int eventsPerDay = 2;
    [Range(0, 100)]
    [Tooltip("RandomAuto เท่านั้น: โอกาส % ที่วันนั้นจะไม่มี Event")]
    public int noEventChance = 20;
    [Tooltip("กันไม่ให้เสนอ/สุ่มได้ Event ซ้ำกับของเมื่อวาน")]
    public bool avoidRepeatYesterday = true;

    [Header("Combined Multiplier Limits (RandomAuto ที่ซ้อนหลาย Event)")]
    public float minCombinedMultiplier = 0.25f;
    public float maxCombinedMultiplier = 4f;

    private readonly List<DailyEvent> _current = new List<DailyEvent>();
    private readonly List<DailyEvent> _offers = new List<DailyEvent>();
    private readonly List<DailyEvent> _yesterday = new List<DailyEvent>();

    /// <summary>Event ที่ Active วันนี้ (ว่าง = วันปกติ หรือยังไม่ได้เลือก)</summary>
    public IReadOnlyList<DailyEvent> CurrentEvents => _current;

    /// <summary>ตัวเลือกที่รอผู้เล่นเลือกอยู่ (ว่าง = ไม่ได้อยู่ระหว่างเลือก)</summary>
    public IReadOnlyList<DailyEvent> PendingOffers => _offers;
    public bool IsChoosing => _offers.Count > 0;

    /// <summary>ยิงเมื่อมีตัวเลือกให้ผู้เล่นเลือก — ให้ Choice UI ฟัง</summary>
    public event Action<IReadOnlyList<DailyEvent>> OnEventsOffered;

    /// <summary>ยิงเมื่อ Event ของวันนี้ "ยืนยันแล้ว" (list ว่าง = วันปกติ) — ให้ Banner ฟัง</summary>
    public event Action<IReadOnlyList<DailyEvent>> OnEventsChanged;

    // ── Safe accessors: เรียกจากที่ไหนก็ได้ (ไม่มี Instance = 1x) ──
    public static float MoneyMult =>
        Instance != null ? Instance.Combine(e => e.moneyMultiplier) : 1f;
    public static float SpawnRateMult =>
        Instance != null ? Instance.Combine(e => e.spawnRateMultiplier) : 1f;
    public static float FurniturePriceMult =>
        Instance != null ? Instance.Combine(e => e.furniturePriceMultiplier) : 1f;
    public static float CookingSpeedMult =>
        Instance != null ? Instance.Combine(e => e.cookingSpeedMultiplier) : 1f;

    float Combine(Func<DailyEvent, float> selector)
    {
        float result = 1f;
        foreach (var e in _current) result *= selector(e);
        return Mathf.Clamp(result, minCombinedMultiplier, maxCombinedMultiplier);
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // subscribe ใน Start เพื่อให้ DayNightManager.Instance ถูกตั้งใน Awake เสร็จก่อน
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDayStarted += RollNewEvents;

        RollNewEvents(); // วันที่ 1
    }

    void OnDestroy()
    {
        if (DayNightManager.Instance != null)
            DayNightManager.Instance.OnNewDayStarted -= RollNewEvents;
        if (Instance == this) Instance = null;
    }

    public void RollNewEvents()
    {
        _current.Clear();
        _offers.Clear();

        if (events.Count == 0) { FinalizeToday(); return; }

        if (selectionMode == EventSelectionMode.PlayerChoice)
        {
            PickDistinctWeighted(_offers, choicesOffered);

            if (_offers.Count == 0) { FinalizeToday(); return; }

            // เหลือตัวเลือกเดียว หรือไม่มี UI ฟังอยู่ → เลือกให้เอง กันเกมค้าง
            if (_offers.Count == 1 || OnEventsOffered == null)
            {
                SelectEvent(_offers[UnityEngine.Random.Range(0, _offers.Count)]);
                return;
            }

            Debug.Log($"🎲 Daily Event Offers: {string.Join(" / ", _offers.ConvertAll(e => e.eventName))}");
            OnEventsOffered.Invoke(_offers);
            return;
        }

        // RandomAuto
        if (UnityEngine.Random.Range(0, 100) >= noEventChance)
            PickDistinctWeighted(_current, eventsPerDay);
        FinalizeToday();
    }

    /// <summary>ผู้เล่นเลือก Event (เรียกจาก Choice UI) — ต้องเป็นอันที่ถูกเสนอเท่านั้น</summary>
    public void SelectEvent(DailyEvent chosen)
    {
        if (chosen == null || !_offers.Contains(chosen)) return;

        _current.Clear();
        _current.Add(chosen);
        _offers.Clear();
        FinalizeToday();
    }

    void FinalizeToday()
    {
        _yesterday.Clear();
        _yesterday.AddRange(_current);

        Debug.Log(_current.Count > 0
            ? $"🎲 Today's Events: {string.Join(" + ", _current.ConvertAll(e => e.eventName))}"
            : "🎲 Today's Events: (วันปกติ)");

        OnEventsChanged?.Invoke(_current);
    }

    void PickDistinctWeighted(List<DailyEvent> result, int count)
    {
        int want = Mathf.Min(count, events.Count);

        var pool = new List<DailyEvent>();
        foreach (var e in events)
            if (e.weight > 0f) pool.Add(e);

        if (avoidRepeatYesterday)
        {
            var filtered = pool.FindAll(e => !_yesterday.Contains(e));
            if (filtered.Count >= want) pool = filtered;
        }

        want = Mathf.Min(want, pool.Count);

        // สุ่มแบบไม่คืน (without replacement)
        for (int n = 0; n < want; n++)
        {
            float total = 0f;
            foreach (var e in pool) total += e.weight;

            float roll = UnityEngine.Random.Range(0f, total);
            int pickIndex = pool.Count - 1;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= pool[i].weight;
                if (roll <= 0f) { pickIndex = i; break; }
            }

            result.Add(pool[pickIndex]);
            pool.RemoveAt(pickIndex);
        }
    }
}