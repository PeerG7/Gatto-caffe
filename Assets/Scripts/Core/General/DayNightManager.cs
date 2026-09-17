using UnityEngine;
using UnityEngine.Rendering.Universal;

// =====================================================================
// DayNightManager — v3 (Pause Counter)
//
// ใช้ _pauseCount แทน bool เดียว
// ป้องกัน canvas หลายตัวซ้อนกัน แล้ว ResumeGame() ตัวแรกทำให้เวลาเดิน
//
//   PauseGame()   → _pauseCount++
//   ResumeGame()  → _pauseCount-- (resume จริงเมื่อถึง 0)
//   ForceResume() → reset เป็น 0 ทันที (ใช้ตอน ResetNewDay)
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

    [HideInInspector] public int catsServedToday = 0;
    [HideInInspector] public int moneyEarnedToday = 0;

    [Header("Lighting Settings")]
    public Light2D globalLight;
    public Gradient dayNightGradient;

    [Header("References")]
    public UISummaryController summaryUI;

    // ── ใหม่: event สำหรับ sunrise transition ─────────────────────
    // ยิงตอน ResetNewDay() ทำงาน — ให้ UI ฟังแล้วเล่น night → sunrise → day
    public event System.Action OnNewDayStarted;

    [Header("End-of-Day Audio Settings")]
    public float endOfDayWarningTime = 10f;
    private bool endOfDayAudioTriggered = false;

    // ── ใหม่: In-Game Clock (แปลง timer จริง -> เวลาในเกม) ─────────
    [Header("In-Game Clock Settings")]
    [Tooltip("ชั่วโมงเริ่มต้นของวัน (24hr) เช่น 8 = 8 โมงเช้า — ตอน timer = 0")]
    public int gameOpenHour = 8;
    [Tooltip("ชั่วโมงสิ้นสุดของวัน (24hr) เช่น 22 = 4 ทุ่ม — ตอน timer = dayDuration")]
    public int gameCloseHour = 22;

    // ── ใหม่: ชื่อวันในสัปดาห์ — Day 1 = จันทร์เสมอ แล้ววนทุก 7 วัน ──
    private static readonly string[] WeekdayAbbrev =
        { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

    /// <summary>ชื่อวันย่อ (MON/TUE/...) ของ currentDay ปัจจุบัน — Day 1 = จันทร์เสมอ</summary>
    public string CurrentWeekdayAbbrev
    {
        get
        {
            int index = (currentDay - 1) % 7;
            if (index < 0) index += 7; // กันเผื่อ currentDay ผิดปกติเป็นค่าติดลบ
            return WeekdayAbbrev[index];
        }
    }

    /// <summary>ชั่วโมงปัจจุบันแบบทศนิยม (เช่น 9.5 = 9:30) ตาม gameOpenHour/gameCloseHour ที่ตั้งไว้ — ใช้ภายในร่วมกันทั้ง label และไอคอน</summary>
    private float ComputeCurrentHourFloat()
    {
        float ratio = dayDuration > 0f ? Mathf.Clamp01(timer / dayDuration) : 0f;
        float totalGameHours = gameCloseHour - gameOpenHour;
        return gameOpenHour + ratio * totalGameHours;
    }

    /// <summary>ชั่วโมงปัจจุบันแบบทศนิยม (0-24) — ให้ UI อื่น (เช่น DayNightIconUI) เอาไปคำนวณเองได้</summary>
    public float GetCurrentGameHour24() => ComputeCurrentHourFloat();

    /// <summary>
    /// แปลง timer (0 → dayDuration วินาทีจริง) เป็นข้อความเวลาในเกมแบบ 12 ชม.
    /// เช่น "8:00 AM" → "10:00 PM" ตาม gameOpenHour/gameCloseHour ที่ตั้งไว้
    /// </summary>
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

    // ── ใหม่: Day/Night Icon Gauge (0-1) ──────────────────────────
    // 1 = เต็ม (day icon ปิด night icon ไว้ทั้งหมด)
    // ค่อยๆลดลงเหลือ 0 ระหว่าง warning window แล้วเผย night icon
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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (isPaused) return;

        UpdateLightColor();
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

        // ✅ Force reset เพราะขึ้นวันใหม่ทุก canvas ปิดหมดแล้ว
        ForceResume();

        if (AudioManager.instance != null)
            AudioManager.instance.ResumeGameMusic();

        UpdateLightColor();

        // ✅ ใหม่: บอก UI ให้เล่น sunrise transition
        OnNewDayStarted?.Invoke();
    }
}