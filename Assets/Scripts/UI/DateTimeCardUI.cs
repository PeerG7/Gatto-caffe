using UnityEngine;
using TMPro;
using System.Collections;

// =====================================================================
// DateTimeCardUI — แสดง "ชื่อวัน" (MON/TUE/...) + "เวลาในเกม" (8:00 AM)
// บนการ์ด UI (ใช้คู่กับ DayNightIconUI ที่โชว์ไอคอนพระอาทิตย์/พระจันทร์)
//
// Setup ใน Unity:
//   1. ในการ์ด UI (ตามภาพร่าง) สร้าง TextMeshPro - Text (UI) 2 ตัว:
//      - ตัวสำหรับ "MON" (ชื่อวัน)
//      - ตัวสำหรับ "8:00 AM" (เวลา)
//   2. วางสคริปต์นี้ไว้ที่ parent ของการ์ด (หรือที่ไหนก็ได้ในซีน)
//   3. ลาก Text ทั้ง 2 ตัวมาใส่ dayText / timeText ตามลำดับ
//
// การตั้งค่าช่วงเวลาในเกม (8:00 - 22:00 เป็นต้น) ไปตั้งที่
// DayNightManager > In-Game Clock Settings > gameOpenHour / gameCloseHour
// =====================================================================
public class DateTimeCardUI : MonoBehaviour
{
    [Header("Text References")]
    [Tooltip("แสดงชื่อวัน เช่น MON, TUE, WED")]
    public TextMeshProUGUI dayText;
    [Tooltip("แสดงเวลาปัจจุบันในเกม เช่น 8:00 AM")]
    public TextMeshProUGUI timeText;

    [Tooltip("ความถี่ในการอัปเดตข้อความ (วินาที) — ใส่ 0 เพื่ออัปเดตทุกเฟรม (ไม่จำเป็นต้องละเอียดขนาดนั้น)")]
    public float updateInterval = 0.2f;

    private float _sinceLastUpdate = 0f;
    private DayNightManager _manager;

    void Start()
    {
        StartCoroutine(BindToManager());
    }

    // เผื่อ DayNightManager.Instance ยังไม่ set ใน frame แรก (เหมือน DayNightIconUI)
    private IEnumerator BindToManager()
    {
        while (DayNightManager.Instance == null)
            yield return null;

        _manager = DayNightManager.Instance;
        RefreshNow();
    }

    void Update()
    {
        if (_manager == null) return;

        _sinceLastUpdate += Time.deltaTime;
        if (_sinceLastUpdate < updateInterval) return;
        _sinceLastUpdate = 0f;

        RefreshNow();
    }

    void RefreshNow()
    {
        if (dayText != null) dayText.text = _manager.CurrentWeekdayAbbrev;
        if (timeText != null) timeText.text = _manager.GetCurrentClockLabel();
    }
}