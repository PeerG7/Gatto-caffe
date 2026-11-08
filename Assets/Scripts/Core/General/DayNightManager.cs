using UnityEngine;
using UnityEngine.Rendering.Universal;

// =====================================================================
// DayNightManager — v3 (Pause Counter & Save System Integrated)
// =====================================================================
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    // ── Pause System ───────────────────────────────────────────────
    private int _pauseCount = 0;

    public bool isPaused => _pauseCount > 0;

    public void PauseGame()
    {
        _pauseCount++;
        Debug.Log($"⏸ PauseGame() → pauseCount = {_pauseCount}");
    }

    public void ResumeGame()
    {
        _pauseCount = Mathf.Max(0, _pauseCount - 1);
        Debug.Log($"▶ ResumeGame() → pauseCount = {_pauseCount}");
    }

    /// <summary>บังคับ resume ทันที — ใช้ตอน ResetNewDay หรือ debug</summary>
    public void ForceResume()
    {
        _pauseCount = 0;
        Debug.Log("▶ ForceResume() → pauseCount = 0");
    }

    // ── Day/Night ──────────────────────────────────────────────────
    [Header("Time Settings")]
    public float dayDuration = 60f;
    private float timer = 0f;
    public bool isWorkTime = true;
    private bool isEnding = false;
    public int currentDay = 1;

    private const string DaySaveKey = "SavedGameDay"; // 🔑 Key สำหรับเซฟวันลง PlayerPrefs

    [HideInInspector] public int catsServedToday = 0;
    [HideInInspector] public int moneyEarnedToday = 0;

    [Header("Lighting Settings")]
    public Light2D globalLight;
    public Gradient dayNightGradient;

    [Header("References")]
    public UISummaryController summaryUI;

    // ── event สำหรับ sunrise transition ─────────────────────
    public event System.Action OnNewDayStarted;

    [Header("End-of-Day Audio Settings")]
    public float endOfDayWarningTime = 10f;
    private bool endOfDayAudioTriggered = false;

    // ── In-Game Clock ─────────────────────────
    [Header("In-Game Clock Settings")]
    [Tooltip("ชั่วโมงเริ่มต้นของวัน (24hr) เช่น 8 = 8 โมงเช้า — ตอน timer = 0")]
    public int gameOpenHour = 8;
    [Tooltip("ชั่วโมงสิ้นสุดของวัน (24hr) เช่น 22 = 4 ทุ่ม — ตอน timer = dayDuration")]
    public int gameCloseHour = 22;

    private static readonly string[] WeekdayAbbrev =
        { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

    public string CurrentWeekdayAbbrev
    {
        get
        {
            int index = (currentDay - 1) % 7;
            if (index < 0) index += 7;
            return WeekdayAbbrev[index];
        }
    }

    private float ComputeCurrentHourFloat()
    {
        float ratio = dayDuration > 0f ? Mathf.Clamp01(timer / dayDuration) : 0f;
        float totalGameHours = gameCloseHour - gameOpenHour;
        return gameOpenHour + ratio * totalGameHours;
    }

    public float GetCurrentGameHour24() => ComputeCurrentHourFloat();

    public string GetCurrentClockLabel()
    {
        float currentHourFloat = ComputeCurrentHourFloat();

        int hour24 = Mathf.FloorToInt(currentHourFloat) % 24;
        if (hour24 < 0) hour24 += 24;
        int minute = Mathf.FloorToInt((currentHourFloat - Mathf.Floor(currentHourFloat)) * 60f);

        bool isAM = hour24 < 12;
        int hour12 = hour24 % 12;
        if (hour12 == 0) hour12 = 12;

        return $"{hour12}:{minute:00} {(isAM ? "AM" : "PM")}";
    }

    public float DayIconFillAmount
    {
        get
        {
            if (!isWorkTime) return 0f;

            float warningStart = dayDuration - endOfDayWarningTime;
            if (timer <= warningStart) return 1f;

            float t = (timer - warningStart) / endOfDayWarningTime;
            return Mathf.Clamp01(1f - t);
        }
    }

    void Awake()
    {
        // ↩️ คืนเป็นพฤติกรรมเดิม (ตัวแรกชนะ) — เวอร์ชัน "ตัวใหม่ชนะ" ที่เคยใส่ไปทำลาย Component ของตัวที่ค้างอยู่บน GameManager
        // รอดูโครงสร้าง GameManager / DontDestroyOnLoad ก่อนแล้วค่อยแก้ที่ต้นเหตุ
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogWarning("[DayNightManager] พบ DayNightManager ซ้ำ (น่าจะเป็นตัวเก่าที่ค้างข้ามฉาก) — ตัวใหม่ถูกทำลาย");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 🌟 โหลดวันที่ล่าสุดที่ผู้เล่นเซฟไว้ตอนกด Continue
        if (PlayerPrefs.HasKey(DaySaveKey))
        {
            currentDay = PlayerPrefs.GetInt(DaySaveKey);
        }
        else
        {
            currentDay = 1;
        }

        Debug.Log($"[DayNightManager] โหลดข้อมูลสำเร็จ — วันปัจจุบันคือ: วันที่ {currentDay}");

        // 🟢 ตั้งสีแสงตามเวลาเริ่มวันทันที (เดิมไม่ได้ตั้ง ทำให้ใช้สีที่ค้างอยู่ในฉากจนกว่าเกมจะเลิก Pause)
        UpdateLightColor();
    }

    void Update()
    {
        // 🟢 อัปเดตแสงทุกเฟรมแม้เกม Pause อยู่ (เช่น ช่วง Tutorial) ไม่งั้นแสงจะค้างเป็นสีเดิมของฉาก
        UpdateLightColor();

        if (isPaused) return;
        if (!isWorkTime) return;

        timer += Time.deltaTime;

        if (!endOfDayAudioTriggered && timer >= dayDuration - endOfDayWarningTime)
        {
            endOfDayAudioTriggered = true;
            if (AudioManager.instance != null)
                AudioManager.instance.PlayEndOfDaySequence();
        }

        if (timer >= dayDuration)
            EndWorkDay();
    }

    void UpdateLightColor()
    {
        if (globalLight != null && dayNightGradient != null)
        {
            float timePercent = Mathf.Clamp01(timer / dayDuration);
            globalLight.color = dayNightGradient.Evaluate(timePercent);
        }
    }

    void EndWorkDay()
    {
        if (isEnding) return;
        isEnding = true;
        isWorkTime = false;

        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (var npc in allNPCs)
            if (npc.currentState != NPCController.NPCState.Leaving)
                npc.GoExit();

        if (summaryUI != null)
            summaryUI.StartSummarySequence(currentDay, catsServedToday, moneyEarnedToday);
    }

    public void ResetNewDay()
    {
        timer = 0;
        currentDay++;
        isWorkTime = true;
        isEnding = false;
        endOfDayAudioTriggered = false;
        catsServedToday = 0;
        moneyEarnedToday = 0;

        SaveDayProgress(); // 🌟 บันทึกวันใหม่ทันทีที่ขึ้นวันใหม่

        ForceResume();

        if (AudioManager.instance != null)
            AudioManager.instance.ResumeGameMusic();

        UpdateLightColor();
        OnNewDayStarted?.Invoke();
    }

    /// <summary>
    /// 🟢 CHECKPOINT: บันทึก "จุดเริ่มต้นของวันปัจจุบัน" (วัน + เงิน + เฟอร์นิเจอร์) ลง PlayerPrefs
    /// เรียกเฉพาะ 2 จังหวะเท่านั้น: (1) ตอนขึ้นวันใหม่ (ResetNewDay) (2) ตอนจบ Tutorial วันแรก
    /// ❌ ห้ามเรียกตอนกด Title/Exit กลางวัน — ไม่งั้นผู้เล่นจะเก็บเงิน/ของที่หาได้กลางวันไว้แล้วเริ่มวันใหม่ได้ (ช่องโหว่)
    /// ผู้เล่นกด Continue จะกลับไปที่จุดนี้เสมอ
    /// </summary>
    public void SaveDayProgress()
    {
        WriteCheckpoint(currentDay);
    }

    /// <summary>
    /// ใช้ตอนจบวันแล้ว (หน้าสรุป) แต่ผู้เล่นกลับ Main Menu โดยไม่กด Next Day
    /// → เซฟเป็นจุดเริ่มต้นของ "วันถัดไป" ด้วยเงิน/เฟอร์นิเจอร์ ณ ตอนจบวัน (วันนั้นเล่นจบแล้ว ไม่ต้องเล่นซ้ำ)
    /// </summary>
    public void SaveNextDayCheckpoint()
    {
        WriteCheckpoint(currentDay + 1);
    }

    private void WriteCheckpoint(int dayToSave)
    {
        PlayerPrefs.SetInt(DaySaveKey, dayToSave);
        PlayerPrefs.Save();

        // เซฟเงินและเฟอร์นิเจอร์เป็น Snapshot ชุดเดียวกับวัน
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.SaveMoney();
        FurnitureProgressSaveSystem.SaveIfAvailable();

        // ความสัมพันธ์กับแมวก็เป็นส่วนหนึ่งของ Checkpoint เดียวกัน
        if (RelationshipManager.Instance != null) RelationshipManager.Instance.SaveCheckpoint();

        Debug.Log($"💾 [DayNightManager] Checkpoint: เริ่มวันที่ {dayToSave} ด้วยเงินและเฟอร์นิเจอร์ ณ ตอนนี้");
    }
}