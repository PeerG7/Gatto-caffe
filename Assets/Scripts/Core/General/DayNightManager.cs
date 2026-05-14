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

    [Header("End-of-Day Audio Settings")]
    public float endOfDayWarningTime = 10f;
    private bool endOfDayAudioTriggered = false;

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
    }
}