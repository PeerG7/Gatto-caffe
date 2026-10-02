using System.Collections;
using UnityEngine;
using TMPro;

// =====================================================================
// DailyEventBanner (v2 — Text only)
// Fade เฉพาะ TMP text ที่ลากใส่ในสคริปต์นี้เท่านั้น (ไม่ใช้ CanvasGroup)
// Panel / Image / ลูกอื่นๆ ใน parent เดียวกันจะไม่ถูกแตะต้องเลย
// วางสคริปต์ไว้บน GameObject ไหนก็ได้ (ต้อง Active ไว้ตลอด)
// =====================================================================
public class DailyEventBanner : MonoBehaviour
{
    [Header("UI References (fade เฉพาะ 2 ตัวนี้)")]
    public TMP_Text titleText;
    [Tooltip("Optional: ข้อความรองใต้ชื่อ (ใช้ description ของ Event)")]
    public TMP_Text descriptionText;

    [Header("Timing (seconds)")]
    [Tooltip("รอก่อนเริ่มแสดง — ตั้งให้ตรงกับจบ sunrise transition")]
    public float startDelay = 1.5f;
    public float fadeInTime = 0.6f;
    public float holdTime = 2.5f;
    public float fadeOutTime = 1.0f;

    [Header("Normal Day (ไม่มี Event)")]
    public bool showOnNormalDay = false;
    public string normalDayText = "Normal Day";

    [Header("Text Format")]
    [Tooltip("{0} = ชื่อ Event")]
    public string titleFormat = "{0}";

    private Coroutine _routine;

    void Awake()
    {
        SetAlpha(0f);
    }

    void Start()
    {
        var mgr = DailyEventManager.Instance;
        if (mgr == null) return;

        mgr.OnEventChanged += HandleEventChanged;

        // ถ้า Manager สุ่มของวันที่ 1 ไปก่อนที่เราจะ subscribe ทัน ให้แสดงของที่สุ่มไว้แล้ว
        if (mgr.CurrentEvent != null)
            HandleEventChanged(mgr.CurrentEvent);
    }

    void OnDestroy()
    {
        if (DailyEventManager.Instance != null)
            DailyEventManager.Instance.OnEventChanged -= HandleEventChanged;
    }

    void HandleEventChanged(DailyEventManager.DailyEvent ev)
    {
        if (ev == null && !showOnNormalDay) { HideImmediately(); return; }

        string title = ev != null ? string.Format(titleFormat, ev.eventName) : normalDayText;
        string desc = ev != null ? ev.description : "";

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(title, desc));
    }

    void HideImmediately()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        SetAlpha(0f);
    }

    // ตั้ง alpha เฉพาะ text 2 ตัวที่ผูกไว้
    void SetAlpha(float a)
    {
        if (titleText != null) titleText.alpha = a;
        if (descriptionText != null) descriptionText.alpha = a;
    }

    IEnumerator ShowRoutine(string title, string desc)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null)
        {
            descriptionText.text = desc;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
        }

        SetAlpha(0f);

        // unscaled: ไม่โดนกระทบแม้เกมมีการ pause ด้วย timeScale
        yield return WaitUnscaled(startDelay);

        yield return Fade(0f, 1f, fadeInTime);
        yield return WaitUnscaled(holdTime);
        yield return Fade(1f, 0f, fadeOutTime);

        SetAlpha(0f);
        _routine = null;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { SetAlpha(to); yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        SetAlpha(to);
    }

    IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}