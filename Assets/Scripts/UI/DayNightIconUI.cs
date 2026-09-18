using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// =====================================================================
// DayNightIconUI — v2 (Sunrise / Sunset Crossfade)
//
// Setup ใน Unity:
//   วาง 4 Image ซ้อนกันตำแหน่งเดียวกัน (Image Type = Simple พอ ไม่ต้องใช้ Filled
//   อีกแล้วในเวอร์ชันนี้), sibling order ไม่สำคัญเพราะเราคุม alpha เอง:
//     - dayIcon
//     - sunsetIcon
//     - nightIcon
//     - sunriseIcon
//   ลากทั้ง 4 มาใส่ field ด้านล่าง แล้ววางสคริปต์นี้ที่ parent
//
// พฤติกรรม:
//   1) ระหว่างวันปกติ (ก่อนเข้า warning window) → dayIcon เต็ม, ตัวอื่น 0
//   2) เข้า warning window (ก่อนหมดวัน endOfDayWarningTime วิ) →
//      day ไล่จาง, sunset ไล่ขึ้นแล้วไล่ลง (peak กลาง window), night ไล่เข้ม
//      จนสุดวัน night เต็ม 100%
//   3) ตอน ResetNewDay() ถูกเรียก (ขึ้นวันใหม่) → เล่น coroutine
//      night ไล่จาง, sunrise ไล่ขึ้นแล้วไล่ลง, day ไล่เข้ม จนกลับมา day เต็ม
// =====================================================================
public class DayNightIconUI : MonoBehaviour
{
    [Header("Icons (ซ้อนตำแหน่งเดียวกันทั้ง 4 ตัว)")]
    public Image dayIcon;
    public Image sunsetIcon;
    public Image nightIcon;
    public Image sunriseIcon;

    [Header("Sunrise Transition")]
    [Tooltip("ระยะเวลา (วินาที) ของ transition night → sunrise → day ตอนขึ้นวันใหม่")]
    public float sunriseDuration = 2f;

    private bool _inSunriseTransition = false;
    private DayNightManager _manager;

    void Start()
    {
        StartCoroutine(BindToManager());
        SetAlphas(day: 1f, sunset: 0f, night: 0f, sunrise: 0f);
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

        // t: 0 = ยังไม่เข้า warning window, 1 = หมดวันเต็มที่
        float t = 1f - _manager.DayIconFillAmount;

        float dayAlpha = Mathf.Clamp01(1f - t);
        float nightAlpha = Mathf.Clamp01(t);
        // สามเหลี่ยม peak ตรงกลาง window แล้วจางลงทั้งสองฝั่ง (0 ที่ t=0 และ t=1)
        float sunsetAlpha = Mathf.Clamp01(1f - Mathf.Abs(2f * t - 1f));

        SetAlphas(dayAlpha, sunsetAlpha, nightAlpha, 0f);
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

        SetAlphas(day: 1f, sunset: 0f, night: 0f, sunrise: 0f);
        _inSunriseTransition = false;
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