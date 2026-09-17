using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// =====================================================================
// DayNightIconUI — v4 (4 ช่วงเวลาอิสระ มีช่วงว่างคั่นกลาง)
//
// v3 เดิม: Day/Sunset/Night ต่อกันสนิท (dayEnd = จุดเริ่ม Sunset,
//   sunsetEnd = จุดเริ่ม Night) ไม่มีช่วงว่างคั่น
//
// v4 (ตอนนี้): ทั้ง 4 ช่วงเวลาแยกอิสระจากกันแล้ว มีช่วงว่างคั่นกลาง
//   (11:00-12:00, 16:00-17:00, 19:00-20:00) — ระหว่างช่วงว่างพวกนี้
//   ไม่มีไอคอนไหนโชว์เต็ม (alpha ใกล้ 0 ทั้งหมด) ตามช่วงเวลาที่ระบุ:
//
//     Sunrise : sunriseStart → sunriseEnd   (default 8:00 - 11:00, เต็ม 100% ทันทีตอน sunriseStart แล้วไล่จางลง)
//     Day     : dayStart     → dayEnd       (default 12:00 - 16:00, เต็ม 100% ตลอดช่วง, ไล่ขึ้น/ลงเข้า-ออกช่วงว่างข้างเคียง)
//     Sunset  : sunsetStart  → sunsetEnd    (default 17:00 - 19:00, ไล่ขึ้น-ลงเป็นสามเหลี่ยม อิสระจาก Day)
//     Night   : nightStart   → nightFullHour (default 20:00 - 22:00, ไล่ขึ้นจนเต็มแล้วค้างเต็มไปจนถึง Sunrise รอบถัดไป)
//
//   ปรับตัวเลขช่วงเวลาใน Inspector ได้เลยถ้าต้องการเปลี่ยนต่อ ไม่ต้องแก้โค้ด
//
// Setup ใน Unity: เหมือนเดิมทุกอย่าง (ดูคอมเมนต์ header ด้านบน dayIcon เป็นต้นไป)
// =====================================================================
public class DayNightIconUI : MonoBehaviour
{
    [Header("Icons (ซ้อนตำแหน่งเดียวกันทั้ง 4 ตัว)")]
    public Image dayIcon;
    public Image sunsetIcon;
    public Image nightIcon;
    public Image sunriseIcon;

    [Header("Sunrise Transition (ตอนขึ้นวันใหม่ — เอฟเฟกต์ไว ไม่อิงชั่วโมงจริง)")]
    [Tooltip("ระยะเวลา (วินาที) ของ transition night → sunrise → day ตอนขึ้นวันใหม่")]
    public float sunriseDuration = 2f;

    [Header("ช่วงเวลาจริงในเกม (24hr) — 4 ช่วงแยกอิสระ ปรับได้ตามต้องการ")]
    [Tooltip("Sunrise เต็ม 100% ทันที — ค่าเริ่มต้น 8:00")]
    public float sunriseStart = 8f;
    [Tooltip("Sunrise จางจนเหลือ 0 — ค่าเริ่มต้น 11:00")]
    public float sunriseEnd = 11f;
    [Tooltip("Day เต็ม 100% เริ่มที่ชั่วโมงนี้ — ค่าเริ่มต้น 12:00")]
    public float dayStart = 12f;
    [Tooltip("Day เต็ม 100% จนถึงชั่วโมงนี้ — ค่าเริ่มต้น 16:00")]
    public float dayEnd = 16f;
    [Tooltip("Sunset เริ่มไล่ขึ้น (alpha 0) — ค่าเริ่มต้น 17:00")]
    public float sunsetStart = 17f;
    [Tooltip("Sunset ไล่กลับเป็น 0 (จุดสิ้นสุดสามเหลี่ยม) — ค่าเริ่มต้น 19:00")]
    public float sunsetEnd = 19f;
    [Tooltip("Night เริ่มไล่ขึ้น (alpha 0) — ค่าเริ่มต้น 20:00")]
    public float nightStart = 20f;
    [Tooltip("Night ไล่ขึ้นจนเต็ม 100% ที่ชั่วโมงนี้ แล้วค้างเต็มไปจนถึง Sunrise รอบถัดไป — ค่าเริ่มต้น 22:00 (พอดีเวลาปิดร้าน)")]
    public float nightFullHour = 22f;

    private bool _inSunriseTransition = false;
    private DayNightManager _manager;

    void Start()
    {
        StartCoroutine(BindToManager());
        SetAlphas(day: 0f, sunset: 0f, night: 1f, sunrise: 0f); // เริ่มต้นเป็นกลางคืนไว้ก่อน รอ Update() คำนวณจริง
    }

    private IEnumerator BindToManager()
    {
        // เผื่อ DayNightManager.Instance ยังไม่ set ใน frame แรก
        while (DayNightManager.Instance == null)
            yield return null;

        _manager = DayNightManager.Instance;
        _manager.OnNewDayStarted += HandleNewDayStarted;
    }

    void OnDestroy()
    {
        if (_manager != null)
            _manager.OnNewDayStarted -= HandleNewDayStarted;
    }

    void Update()
    {
        if (_inSunriseTransition || _manager == null) return;

        float hour = _manager.GetCurrentGameHour24();

        float sunriseAlpha = FadeOutWindow(hour, sunriseStart, sunriseEnd);
        float sunsetAlpha = TriangleWindow(hour, sunsetStart, sunsetEnd);
        float dayAlpha = ComputeDayAlpha(hour);
        float nightAlpha = ComputeNightAlpha(hour);

        SetAlphas(dayAlpha, sunsetAlpha, nightAlpha, sunriseAlpha);
    }

    // ── สามเหลี่ยม: 0 นอกช่วง, ไล่ขึ้นจาก start แล้วไล่ลงไป end, peak ตรงกลาง ──
    private float TriangleWindow(float hour, float start, float end)
    {
        if (end <= start) return 0f; // กันตั้งค่าผิด
        if (hour < start || hour > end) return 0f;

        float t = (hour - start) / (end - start); // 0 → 1
        return Mathf.Clamp01(1f - Mathf.Abs(2f * t - 1f));
    }

    // ── ไล่จาง: เต็ม 100% ทันทีที่ start แล้วค่อยๆ จางลงเหลือ 0 ตอน end ──
    // ใช้กับ Sunrise เพื่อให้เห็นเป็น Sunrise ทันทีตอนเปิดร้าน ไม่ต้องไล่ขึ้นจาก 0 ก่อน
    private float FadeOutWindow(float hour, float start, float end)
    {
        if (end <= start) return 0f; // กันตั้งค่าผิด
        if (hour < start || hour > end) return 0f;

        float t = (hour - start) / (end - start); // 0 → 1
        return Mathf.Clamp01(1f - t);
    }

    // ── Day: ไล่ขึ้นระหว่าง [sunriseEnd, dayStart] (คาบว่างก่อน Day),
    //         เต็มตลอด [dayStart, dayEnd],
    //         ไล่ลงระหว่าง [dayEnd, sunsetStart] (คาบว่างก่อน Sunset) ──
    private float ComputeDayAlpha(float hour)
    {
        if (hour < sunriseEnd || hour > sunsetStart) return 0f;
        if (hour >= dayStart && hour <= dayEnd) return 1f;

        if (hour < dayStart)
        {
            // ช่วงไล่ขึ้น sunriseEnd → dayStart
            if (dayStart <= sunriseEnd) return 1f; // กันตั้งค่าผิด
            float t = (hour - sunriseEnd) / (dayStart - sunriseEnd);
            return Mathf.Clamp01(t);
        }
        else
        {
            // ช่วงไล่ลง dayEnd → sunsetStart
            if (sunsetStart <= dayEnd) return 0f; // กันตั้งค่าผิด
            float t = (hour - dayEnd) / (sunsetStart - dayEnd);
            return Mathf.Clamp01(1f - t);
        }
    }

    // ── Night: เต็ม 100% ก่อน sunriseStart, หล่นเหลือ 0 ทันทีที่ sunriseStart
    //           (ให้เห็นเป็น Sunrise ทันที ไม่มี Night ค้างอยู่),
    //           เป็น 0 ตลอด Sunrise/Day/Sunset และคาบว่างหลัง Sunset,
    //           ไล่ขึ้นเต็มระหว่าง [nightStart, nightFullHour] แล้วค้างเต็ม ──
    private float ComputeNightAlpha(float hour)
    {
        if (hour < sunriseStart) return 1f;

        if (hour < nightStart) return 0f;

        if (hour < nightFullHour)
        {
            // ไล่ขึ้น nightStart → nightFullHour
            if (nightFullHour <= nightStart) return 1f; // กันตั้งค่าผิด
            float t = (hour - nightStart) / (nightFullHour - nightStart);
            return Mathf.Clamp01(t);
        }

        return 1f; // เต็มค้างไว้จนกว่าจะถึง sunriseStart รอบถัดไป
    }

    private void HandleNewDayStarted()
    {
        StopAllCoroutines();
        StartCoroutine(SunriseTransition());
    }

    private IEnumerator SunriseTransition()
    {
        _inSunriseTransition = true;

        // ตั้งค่าเริ่มต้น: กลางคืนเต็ม (สภาพก่อนขึ้นวันใหม่)
        SetAlphas(day: 0f, sunset: 0f, night: 1f, sunrise: 0f);

        float timer = 0f;
        while (timer < sunriseDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / sunriseDuration); // 0 → 1

            float nightAlpha = Mathf.Clamp01(1f - t);
            float dayAlpha = Mathf.Clamp01(t);
            float sunriseAlpha = Mathf.Clamp01(1f - Mathf.Abs(2f * t - 1f));

            SetAlphas(dayAlpha, 0f, nightAlpha, sunriseAlpha);
            yield return null;
        }

        _inSunriseTransition = false;
        // ปล่อยให้ Update() เข้าคุมต่อทันทีตามชั่วโมงจริง ณ ตอนนั้น (ควรจะเป็นช่วงเช้าอยู่แล้ว)
    }

    private void SetAlphas(float day, float sunset, float night, float sunrise)
    {
        SetAlpha(dayIcon, day);
        SetAlpha(sunsetIcon, sunset);
        SetAlpha(nightIcon, night);
        SetAlpha(sunriseIcon, sunrise);
    }

    private void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}