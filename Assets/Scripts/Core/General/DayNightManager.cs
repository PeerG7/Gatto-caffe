using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    [Header("Time Settings")]
    public float dayDuration = 60f;
    private float timer = 0f;
    public bool isWorkTime = true;
    private bool isEnding = false;
    public int currentDay = 1;

    // ✅ ตัวนับรายวัน
    [HideInInspector] public int catsServedToday = 0;
    [HideInInspector] public int moneyEarnedToday = 0;

    [Header("Lighting Settings")]
    public Light2D globalLight;
    public Gradient dayNightGradient;

    [Header("References")]
    public UISummaryController summaryUI;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.isPaused) return;

        UpdateLightColor();

        if (!isWorkTime) return;

        timer += Time.deltaTime;

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

        Debug.Log("Time is up! Sending all NPCs to exit...");

        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (var npc in allNPCs)
        {
            if (npc.currentState != NPCController.NPCState.Leaving)
                npc.GoExit();
        }

        if (summaryUI != null)
        {
            // ✅ ส่งเงินที่ได้เฉพาะวันนี้
            summaryUI.StartSummarySequence(currentDay, catsServedToday, moneyEarnedToday);
        }
    }

    public void ResetNewDay()
    {
        timer = 0;
        currentDay++;
        isWorkTime = true;
        isEnding = false;

        // ✅ Reset ตัวนับทุกวัน
        catsServedToday = 0;
        moneyEarnedToday = 0;

        UpdateLightColor();
    }
}