using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// =====================================================================
// DailyEventBanner (v3 — รองรับหลาย Event)
// Fade เฉพาะ TMP text ที่ลากใส่ในสคริปต์นี้เท่านั้น (ไม่ใช้ CanvasGroup)
//   titleText       = ชื่อ Event ทั้งหมด (คั่นด้วย titleSeparator)
//   descriptionText = คำอธิบายของแต่ละ Event (คั่นด้วยบรรทัดใหม่)
// =====================================================================
public class DailyEventBanner : MonoBehaviour
{
    [Header("UI References (fade เฉพาะ 2 ตัวนี้)")]
    public TMP_Text titleText;
    [Tooltip("Optional: ข้อความรองใต้ชื่อ (รวม description ของทุก Event)")]
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
    [Tooltip("{0} = ชื่อ Event แต่ละอัน")]
    public string titleFormat = "{0}";
    [Tooltip("ตัวคั่นระหว่างชื่อ Event เช่น \"\\n\" (ขึ้นบรรทัดใหม่) หรือ \"  +  \"")]
    public string titleSeparator = "\n";

    private Coroutine _routine;

    void Awake()
    {
        SetAlpha(0f);
    }

    void Start()
    {
        var mgr = DailyEventManager.Instance;
        if (mgr == null) return;

        mgr.OnEventsChanged += HandleEventsChanged;

        // ถ้า Manager สุ่มของวันที่ 1 ไปก่อนที่เราจะ subscribe ทัน ให้แสดงของที่สุ่มไว้แล้ว
        if (mgr.CurrentEvents.Count > 0)
            HandleEventsChanged(mgr.CurrentEvents);
    }

    void OnDestroy()
    {
        if (DailyEventManager.Instance != null)
            DailyEventManager.Instance.OnEventsChanged -= HandleEventsChanged;
    }

    void HandleEventsChanged(IReadOnlyList<DailyEventManager.DailyEvent> evs)
    {
        if (evs == null || evs.Count == 0)
        {
            if (!showOnNormalDay) { HideImmediately(); return; }
            StartShow(normalDayText, "");
            return;
        }

        var titles = new List<string>();
        var descs = new List<string>();
        foreach (var e in evs)
        {
            titles.Add(string.Format(titleFormat, e.eventName));
            if (!string.IsNullOrEmpty(e.description)) descs.Add(e.description);
        }

        string sep = titleSeparator.Replace("\\n", "\n");
        StartShow(string.Join(sep, titles), string.Join("\n", descs));
    }

    void StartShow(string title, string desc)
    {
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