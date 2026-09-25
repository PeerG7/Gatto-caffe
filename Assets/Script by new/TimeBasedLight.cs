using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// สคริปต์ควบคุมการเปิด-ปิดไฟตามเวลาในเกม
/// เชื่อมกับ DayNightManager อัตโนมัติ พร้อมระบบ Fade ทั้งตอนเปิดและปิด
/// </summary>
public class TimeBasedLight : MonoBehaviour
{
    [Header("ใส่หลอดไฟ Light 2D ที่ต้องการให้เปิดตอนค่ำ")]
    public List<Light2D> lights = new List<Light2D>();

    [Header("ตั้งค่าเวลาเปิด-ปิดไฟ (ตามเวลา 24 ชม. ในเกม)")]
    [Tooltip("ชั่วโมงที่ไฟเริ่มเปิด เช่น 17 = 5 โมงเย็น")]
    public float turnOnHour = 17f;

    [Tooltip("ชั่วโมงที่ไฟดับ เช่น 6 = 6 โมงเช้า")]
    public float turnOffHour = 6f;

    [Header("ความนุ่มนวล")]
    [Tooltip("ระยะเวลาในการค่อยๆ สว่างขึ้นจนเต็ม หรือค่อยๆ ดับลงจนสนิท (วินาที)")]
    public float fadeDuration = 2.5f;

    private List<float> maxIntensities = new List<float>();

    void Start()
    {
        // บันทึกความสว่างดั้งเดิมของไฟแต่ละดวงที่ตั้งไว้ใน Inspector
        foreach (var l in lights)
        {
            if (l != null)
            {
                maxIntensities.Add(l.intensity);
                // เริ่มต้นเกม ให้ไฟดับสนิท (0) ทันที เพื่อให้เห็นช่วงที่มันค่อยๆ เฟดสว่างขึ้น
                l.intensity = 0f;
            }
            else
            {
                maxIntensities.Add(0f);
            }
        }
    }

    void Update()
    {
        if (DayNightManager.Instance == null) return;

        // ดึงเวลาชั่วโมงปัจจุบันจาก DayNightManager
        float currentHour = DayNightManager.Instance.GetCurrentGameHour24();

        // ตรวจสอบว่าอยู่ในช่วงเวลาที่ต้องเปิดไฟหรือไม่
        bool shouldTurnOn = IsNightTime(currentHour);

        // ปรับความสว่างของไฟแต่ละดวงอย่างนุ่มนวล
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] == null) continue;

            float targetIntensity = shouldTurnOn ? maxIntensities[i] : 0f;

            // คำนวณความเร็วเฟดตามค่าความสว่างสูงสุดของหลอดนั้นๆ เพื่อให้ทุกดวงเฟดเสร็จพร้อมกันตามเวลา fadeDuration
            float stepSpeed = (fadeDuration > 0f) ? (maxIntensities[i] / fadeDuration) : 999f;

            lights[i].intensity = Mathf.MoveTowards(
                lights[i].intensity,
                targetIntensity,
                stepSpeed * Time.deltaTime
            );
        }
    }

    private bool IsNightTime(float hour)
    {
        // ข้ามเที่ยงคืน เช่น เปิดตั้งแต่ 17:00 ถึง 06:00
        if (turnOnHour > turnOffHour)
        {
            return hour >= turnOnHour || hour < turnOffHour;
        }
        else
        {
            return hour >= turnOnHour && hour < turnOffHour;
        }
    }
}