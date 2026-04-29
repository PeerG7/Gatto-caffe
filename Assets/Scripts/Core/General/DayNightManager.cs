using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal; // ต้องใช้สำหรับ Light 2D

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    [Header("Time Settings")]
    public float dayDuration = 60f;
    private float timer = 0f;
    public bool isWorkTime = true;
    private bool isEnding = false; // 🔥 ตัวแปรควบคุมการจบวัน
    public int currentDay = 1;

    [Header("Lighting Settings")]
    public Light2D globalLight; // ลาก Global Light 2D มาใส่ใน Inspector
    public Gradient dayNightGradient; // ตั้งค่าสี ขาว -> ส้ม -> น้ำเงินเข้ม

    [Header("References")]
    public UISummaryController summaryUI;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.isPaused) return;

        // ☀️ อัปเดตสีของแสงตามเวลาที่ผ่านไป
        UpdateLightColor();

        if (!isWorkTime) return;

        timer += Time.deltaTime;

        if (timer >= dayDuration)
        {
            EndWorkDay();
        }
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
        if (isEnding) return; // ป้องกันการเรียกซ้ำ
        isEnding = true;
        isWorkTime = false;

        Debug.Log("Time is up! Waiting for customers to leave...");

        if (summaryUI != null)
        {
            int earned = CurrencyManager.Instance != null ? CurrencyManager.Instance.currentMoney : 0;
            summaryUI.StartSummarySequence(currentDay, 0, earned);
        }
    }

    public void ResetNewDay()
    {
        timer = 0;
        currentDay++;
        isWorkTime = true;
        isEnding = false; // รีเซ็ตสถานะจบวัน
        UpdateLightColor();
    }
}