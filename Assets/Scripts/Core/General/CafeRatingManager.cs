using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// CafeRatingManager — Rating ของร้าน (5 ระดับ: 0–4)
//
//  • หลัก  : จำนวนแมวที่ "เสิร์ฟสำเร็จ" ในแต่ละวัน เทียบกับเป้าหมายของวัน (เป้าโตตามระดับ)
//  • เสริม : Interact (QTE) สำเร็จ ให้โบนัสเล็กน้อย (นับเป็น "เสิร์ฟเพิ่ม" ทีละนิด มีเพดาน)
//  • แมวที่เข้าร้านแล้วออกไปโดยไม่ได้กิน (รอคิวนาน / รอที่โต๊ะนาน / คิวเต็ม) = เสิร์ฟไม่สำเร็จ
//  • คิด Rating ใหม่ตอนจบวัน → ปรับ "วินาทีต่อการเกิดแมว" ของ NPCSpawner ตามระดับ
//  • ไม่นับช่วง Tutorial / เซฟพร้อม Checkpoint เดียวกับ วัน-เงิน-เฟอร์นิเจอร์
//
//  วางสคริปต์นี้บน GameObject ใน Scene (เช่น GameManager) — ใช้ Destroy(this) ไม่ใช่ Destroy(gameObject)
//  ดูอัตราการเกิดแมว: คลิกขวาที่หัว Component → "Print Spawn Table to Console"
// =====================================================================
public class CafeRatingManager : MonoBehaviour
{
    public static CafeRatingManager Instance;

    [Serializable]
    public struct RatingTier
    {
        [Tooltip("ผลงานของวัน (0–1+) ขั้นต่ำที่ได้ tier นี้ — เช็กจากบนลงล่าง ใช้ tier แรกที่ผ่าน")]
        public float minPerformance;
        [Tooltip("Rating เปลี่ยนเท่าไหร่ เช่น +1 = ขึ้นหนึ่งระดับ, -0.5 = ลดครึ่งระดับ")]
        public float ratingChange;

        public RatingTier(float minPerformance, float ratingChange)
        { this.minPerformance = minPerformance; this.ratingChange = ratingChange; }
    }

    [Serializable]
    public class DayResult
    {
        public int day;
        public int arrived;   // แมวที่เข้าร้านแล้วมีผลชัดเจนในวันนี้ = เสิร์ฟสำเร็จ + เดินออกไปเฉยๆ
        public int served;
        public int missed;
        public int interactSuccess;
        public int target;
        public float effectiveServed;
        public float performance;
        public float oldRating;
        public float newRating;
        public float change;
        public bool hadCustomers;
    }

    // ───────────────────────── Rating Range ─────────────────────────
    [Header("Rating Range (5 ระดับ = 0, 1, 2, 3, 4)")]
    public float minRating = 0f;
    public float maxRating = 4f;
    public float startRating = 0f;

    // ───────────────────────── Spawn Frequency ──────────────────────
    [Header("Spawn Time per Level (วินาทีต่อการเกิด 1 ครั้ง) ← ปรับตรงนี้เป็นหลัก")]
    [Tooltip("เวลาสั้นสุดระหว่างการเกิด | Element 0 = Rating 0, Element 1 = Rating 1, ...")]
    public float[] spawnMinByLevel = { 10f, 9f, 8f, 7f, 6f };
    [Tooltip("เวลายาวสุดระหว่างการเกิด | แต่ละครั้งสุ่มระหว่าง Min–Max ของระดับนั้น (ถ้า Rating เป็นเศษ จะไล่ค่าระหว่างระดับให้ต่อเนื่อง)")]
    public float[] spawnMaxByLevel = { 20f, 18f, 16f, 14f, 12f };

    // ───────────────────────── Daily Target ─────────────────────────
    [Header("Daily Target (เสิร์ฟกี่ตัวต่อวัน ตามระดับ)")]
    [Tooltip("Element 0 = Rating 0, Element 1 = Rating 1, ...")]
    public int[] servedTargetByLevel = { 6, 8, 10, 12, 14 };
    [Tooltip("ถ้าวันนั้นมีแมวมาน้อยกว่าเป้า จะวัดเทียบกับจำนวนที่มาจริง แต่ไม่ต่ำกว่าค่านี้ (กันแมวมา 1 ตัวแล้วได้เต็ม)")]
    public int minExpectedServed = 3;

    [Header("Interact Bonus (ส่วนช่วย)")]
    [Tooltip("Interact สำเร็จ 1 ครั้ง นับเป็นเสิร์ฟเพิ่มกี่ตัว")]
    public float interactBonusPerSuccess = 0.25f;
    [Tooltip("โบนัสจาก Interact รวมสูงสุดต่อวัน (นับเป็นเสิร์ฟกี่ตัว)")]
    public float maxInteractBonus = 2f;

    [Header("Rating Change Tiers (เรียงจากผลงานสูง → ต่ำ)")]
    public List<RatingTier> tiers = new List<RatingTier>
    {
        new RatingTier(1.0f,  1.0f),
        new RatingTier(0.7f,  0.5f),
        new RatingTier(0.4f,  0f),
        new RatingTier(0f,   -0.5f),
    };
    [Tooltip("ถึงวันนี้ (รวม) ยังไม่หัก Rating — ให้ผู้เล่นทำความเข้าใจระบบก่อน")]
    public int noPenaltyUntilDay = 1;

    // ───────────────────────── Live Info ────────────────────────────
    [Header("Live Info (ดูอย่างเดียว — อัปเดตตอน Play)")]
    [SerializeField] private float liveRating;
    [SerializeField] private float liveSpawnMin;
    [SerializeField] private float liveSpawnMax;
    [SerializeField] private float liveSecondsToNextSpawn;
    [SerializeField] private int liveTarget;
    [SerializeField] private int liveServed;
    [SerializeField] private int liveMissed;
    [SerializeField] private int liveInteract;

    // ── State ──────────────────────────────────────────────────────
    private float rating;
    private int servedToday, missedToday, interactToday;
    private bool evaluatedToday;
    private DayResult lastResult;

    /// <summary>เรียกทุกครั้งที่ตัวเลขเปลี่ยน (เสิร์ฟ / พลาด / Interact / จบวัน / ขึ้นวันใหม่) — HUD ใช้อัปเดตแบบ real time</summary>
    public event Action OnRatingDataChanged;

    /// <summary>เรียกเฉพาะตอน "ค่า Rating เปลี่ยนจริง" (จบวัน / ขึ้นวันใหม่) — HUD ที่โชว์แค่ Rating ใช้อันนี้ ไม่ต้องอัปเดตทุกครั้งที่เสิร์ฟ</summary>
    public event Action OnRatingValueChanged;

    private const string RatingSaveKey = "SavedCafeRating";
    private const string DaySaveKey = "SavedGameDay"; // ต้องตรงกับ DayNightManager

    public float Rating => rating;
    public int ServedToday => servedToday;
    public int MissedToday => missedToday;
    public int InteractToday => interactToday;
    public DayResult LastResult => lastResult;

    /// <summary>ระดับเต็มปัจจุบัน (0–4)</summary>
    public int Level
    {
        get
        {
            int max = Mathf.Max(0, LevelCount() - 1);
            return Mathf.Clamp(Mathf.FloorToInt(rating - minRating + 0.0001f), 0, max);
        }
    }

    private int LevelCount()
    {
        int n = Mathf.FloorToInt(maxRating - minRating + 0.0001f) + 1;
        return Mathf.Max(1, n);
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        LoadRating();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void LoadRating()
    {
        // New Game จะลบ SavedGameDay → ไม่มีเซฟวัน = เริ่ม Rating ใหม่ (และล้างค่าเก่าที่อาจค้าง)
        // Continue = มีเซฟวัน → โหลด Rating ของ Checkpoint
        if (PlayerPrefs.HasKey(DaySaveKey))
            rating = Mathf.Clamp(PlayerPrefs.GetFloat(RatingSaveKey, startRating), minRating, maxRating);
        else
        {
            rating = Mathf.Clamp(startRating, minRating, maxRating);
            PlayerPrefs.DeleteKey(RatingSaveKey);
        }
    }

    void Update()
    {
        // อัปเดตช่อง "Live Info" ใน Inspector ให้ดูได้ระหว่าง Play
        GetSpawnRange(out liveSpawnMin, out liveSpawnMax);
        liveRating = rating;
        liveTarget = GetDailyTarget();
        liveServed = servedToday;
        liveMissed = missedToday;
        liveInteract = interactToday;
        liveSecondsToNextSpawn = NPCSpawner.Instance != null ? NPCSpawner.Instance.SecondsUntilNextSpawn : 0f;
    }

    /// <summary>เรียกจาก DayNightManager.WriteCheckpoint — เซฟพร้อมวัน/เงิน/เฟอร์นิเจอร์</summary>
    public void SaveCheckpoint()
    {
        PlayerPrefs.SetFloat(RatingSaveKey, rating);
        PlayerPrefs.Save();
    }

    private void NotifyChanged() { OnRatingDataChanged?.Invoke(); }

    // ── เหตุการณ์ระหว่างวัน ─────────────────────────────────────────
    // ช่วง Tutorial (เควสยังไม่ Completed) ไม่นับอะไรทั้งนั้น
    private bool TutorialActive()
    {
        return MainQuestManager.Instance != null &&
               MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;
    }

    /// <summary>แมวจ่ายเงิน = เสิร์ฟสำเร็จ (เรียกจาก CustomerTable.LeaveMoneyOnTable)</summary>
    public void RegisterServed()
    {
        if (TutorialActive()) return;
        servedToday++;
        NotifyChanged();
    }

    /// <summary>แมวออกจากร้านโดยไม่ได้กิน (เรียกจาก NPCController.GoExit) — ไม่นับตอนปิดร้านสิ้นวัน</summary>
    public void RegisterMissed()
    {
        if (TutorialActive()) return;
        if (DayNightManager.Instance != null && !DayNightManager.Instance.isWorkTime) return;
        missedToday++;
        NotifyChanged();
    }

    /// <summary>Interact (QTE) สำเร็จ (เรียกจาก CatSystemManager)</summary>
    public void RegisterInteractSuccess()
    {
        if (TutorialActive()) return;
        interactToday++;
        NotifyChanged();
    }

    // ── จบวัน ──────────────────────────────────────────────────────
    /// <summary>คิด Rating ใหม่ — เรียกครั้งเดียวต่อวัน (เรียกซ้ำจะคืนผลเดิม) หลังแมวออกจากร้านหมดแล้ว</summary>
    public DayResult EvaluateEndOfDay(int day)
    {
        if (evaluatedToday && lastResult != null) return lastResult;
        evaluatedToday = true;

        DayResult r = new DayResult();
        r.day = day;
        r.served = servedToday;
        r.missed = missedToday;
        r.interactSuccess = interactToday;
        r.oldRating = rating;
        r.target = GetDailyTarget();

        int arrivals = servedToday + missedToday;
        r.arrived = arrivals;
        r.hadCustomers = arrivals > 0;
        r.effectiveServed = servedToday + Mathf.Min(interactToday * interactBonusPerSuccess, maxInteractBonus);

        float change = 0f;
        if (r.hadCustomers)
        {
            // วัดเทียบ "แมวที่มาจริง" (ไม่เกินเป้า) — วันที่แมวมาน้อยจะไม่ถูกหักเพราะเป้าสูงเกิน
            float expected = Mathf.Max(minExpectedServed, Mathf.Min(r.target, arrivals));
            r.performance = r.effectiveServed / Mathf.Max(1f, expected);

            for (int i = 0; i < tiers.Count; i++)
            {
                if (r.performance >= tiers[i].minPerformance) { change = tiers[i].ratingChange; break; }
            }
            if (day <= noPenaltyUntilDay && change < 0f) change = 0f;
        }

        rating = Mathf.Clamp(rating + change, minRating, maxRating);
        r.newRating = rating;
        r.change = rating - r.oldRating;
        lastResult = r;

        Debug.Log($"⭐ [CafeRating] วัน {day}: เสิร์ฟ {r.served} / ออกไปเฉยๆ {r.missed} / Interact {r.interactSuccess} " +
                  $"| ผลงาน {r.performance:0.00} (เป้า {r.target}) | Rating {r.oldRating:0.0} → {r.newRating:0.0}");

        NotifyChanged();
        OnRatingValueChanged?.Invoke();
        return r;
    }

    /// <summary>เรียกจาก DayNightManager.ResetNewDay — ล้างตัวนับของวัน + ใช้ช่วงเวลา Spawn ของระดับใหม่ทันที</summary>
    public void StartNewDay()
    {
        servedToday = missedToday = interactToday = 0;
        evaluatedToday = false;
        if (NPCSpawner.Instance != null) NPCSpawner.Instance.RefreshSpawnInterval();
        NotifyChanged();
        OnRatingValueChanged?.Invoke();
    }

    // ── ค่าที่ระบบอื่นอ่าน ─────────────────────────────────────────
    public int GetDailyTarget()
    {
        if (servedTargetByLevel == null || servedTargetByLevel.Length == 0) return 6;
        return servedTargetByLevel[Mathf.Clamp(Level, 0, servedTargetByLevel.Length - 1)];
    }

    /// <summary>ช่วงวินาทีต่อการเกิดแมว ตาม Rating ปัจจุบัน (ไล่ค่าต่อเนื่องระหว่างระดับ) — NPCSpawner เรียกใช้</summary>
    public void GetSpawnRange(out float min, out float max)
    {
        GetSpawnRangeAt(rating, out min, out max);
    }

    public void GetSpawnRangeAt(float ratingValue, out float min, out float max)
    {
        min = LerpArray(spawnMinByLevel, ratingValue, 10f);
        max = LerpArray(spawnMaxByLevel, ratingValue, 20f);
        if (max < min) max = min;
    }

    private float LerpArray(float[] arr, float ratingValue, float fallback)
    {
        if (arr == null || arr.Length == 0) return fallback;

        float pos = Mathf.Clamp(ratingValue - minRating, 0f, arr.Length - 1);
        int lo = Mathf.FloorToInt(pos);
        int hi = Mathf.Min(lo + 1, arr.Length - 1);
        return Mathf.Lerp(arr[lo], arr[hi], pos - lo);
    }

    // ── ข้อความสำหรับหน้าสรุปจบวัน ──────────────────────────────────
    public static string BuildRatingLine(DayResult r)
    {
        if (r == null) return "";
        string arrow = r.change > 0.001f ? "▲" : (r.change < -0.001f ? "▼" : "-");
        return $"Rating: {r.oldRating:0.0} → {r.newRating:0.0}  ({arrow})";
    }

    public static string BuildDetailLine(DayResult r)
    {
        if (r == null) return "";
        // บรรทัด 1: แมวเข้าร้านกี่ตัว | บรรทัด 2: เสิร์ฟได้ / เดินออกไป เทียบกับจำนวนที่เข้าร้าน
        return $"Cats Visited: {r.arrived}\n" +
               $"Served {r.served}/{r.arrived}  |  Missed {r.missed}/{r.arrived}  |  Interact {r.interactSuccess}";
    }

    // ── ตรวจอัตราการเกิดแมว ────────────────────────────────────────
    [ContextMenu("Print Spawn Table to Console")]
    private void PrintSpawnTable()
    {
        float dayLen = 180f;
        DayNightManager dn = FindObjectOfType<DayNightManager>();
        if (dn != null) dayLen = dn.dayDuration;

        float groupChance = 0f, avgGroupSize = 1f;
        NPCSpawner sp = FindObjectOfType<NPCSpawner>();
        if (sp != null)
        {
            groupChance = sp.groupSpawnChance / 100f;
            avgGroupSize = (sp.minGroupSize + sp.maxGroupSize) * 0.5f;
        }
        float catsPerSpawn = 1f + groupChance * (avgGroupSize - 1f);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[CafeRating] ตารางอัตราการเกิดแมว (วัน = {dayLen:0}s, แมว/การเกิด ≈ {catsPerSpawn:0.00} จากกลุ่ม {groupChance * 100f:0}%)");

        int levels = LevelCount();
        for (int i = 0; i < levels; i++)
        {
            float rv = minRating + i;
            GetSpawnRangeAt(rv, out float mn, out float mx);
            float avg = (mn + mx) * 0.5f;
            float spawns = avg > 0f ? dayLen / avg : 0f;
            int target = servedTargetByLevel != null && servedTargetByLevel.Length > 0
                ? servedTargetByLevel[Mathf.Clamp(i, 0, servedTargetByLevel.Length - 1)] : 0;

            sb.AppendLine($"  Rating {rv:0}: ทุก {mn:0.#}–{mx:0.#}s (เฉลี่ย {avg:0.#}s) → ~{spawns:0.#} ครั้ง/วัน → ~{spawns * catsPerSpawn:0} ตัว/วัน | เป้าเสิร์ฟ {target}");
        }
        Debug.Log(sb.ToString());
    }
}