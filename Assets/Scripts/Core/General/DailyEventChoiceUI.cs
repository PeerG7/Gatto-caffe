using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// =====================================================================
// DailyEventChoiceUI
// แสดง Canvas ให้ผู้เล่นเลือก Event ของวันนี้จากตัวเลือกที่ DailyEventManager เสนอ
//  - Pause เกม (DayNightManager) + ล็อกผู้เล่นระหว่างเลือก
//  - รองรับเมาส์ / คีย์บอร์ด / เกมแพด (ผ่าน EventSystem selection) + กดปุ่ม 1,2,3...
//
// ⚠️ วางสคริปต์นี้บน GameObject ที่ "Active ตลอด" (เช่น Canvas root หรือ object ว่าง)
//    แล้วลาก panel (ที่ปิดไว้ได้) มาใส่ช่อง Panel — ห้ามวางบนตัว panel เอง
// =====================================================================
public class DailyEventChoiceUI : MonoBehaviour
{
    [System.Serializable]
    public class ChoiceCard
    {
        public Button button;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        [Tooltip("Optional: สรุปผลอัตโนมัติ เช่น Cat payment x1.5")]
        public TMP_Text effectsText;
    }

    [Header("UI References")]
    public GameObject panel;
    public ChoiceCard[] cards;

    [Header("Behaviour")]
    [Tooltip("รอก่อนเปิด Canvas (วินาที) — ตั้งให้ตรงกับจบ sunrise transition")]
    public float openDelay = 1.0f;
    public bool pauseGameWhileChoosing = true;
    public bool lockPlayerWhileChoosing = true;
    public bool enableNumberHotkeys = true;

    private readonly List<DailyEventManager.DailyEvent> _shown = new List<DailyEventManager.DailyEvent>();
    private Coroutine _openRoutine;
    private bool _didPause;
    private bool _didLock;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Start()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            int idx = i; // capture
            if (cards[i].button != null)
                cards[i].button.onClick.AddListener(() => Choose(idx));
        }

        var mgr = DailyEventManager.Instance;
        if (mgr == null) return;

        mgr.OnEventsOffered += HandleOffers;

        // Manager อาจสุ่มของวันที่ 1 ไปก่อนที่เราจะ subscribe ทัน
        if (mgr.IsChoosing) HandleOffers(mgr.PendingOffers);
    }

    void OnDestroy()
    {
        if (DailyEventManager.Instance != null)
            DailyEventManager.Instance.OnEventsOffered -= HandleOffers;

        ReleaseHolds(); // กันเกมค้าง pause ถ้าถูกทำลายระหว่างเลือก
    }

    void HandleOffers(IReadOnlyList<DailyEventManager.DailyEvent> offers)
    {
        // copy ไว้ เพราะ Manager จะเคลียร์ list ของเขาหลังเลือก
        _shown.Clear();
        for (int i = 0; i < offers.Count && i < cards.Length; i++)
            _shown.Add(offers[i]);

        if (_shown.Count == 0) return;

        // Pause ทันที ไม่ให้เวลา/แมวเดินระหว่างรอ delay
        if (pauseGameWhileChoosing && !_didPause && DayNightManager.Instance != null)
        {
            DayNightManager.Instance.PauseGame();
            _didPause = true;
        }
        if (lockPlayerWhileChoosing && !_didLock)
        {
            PlayerController2D.IsLocked = true;
            _didLock = true;
        }

        if (_openRoutine != null) StopCoroutine(_openRoutine);
        _openRoutine = StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        float t = 0f;
        while (t < openDelay) { t += Time.unscaledDeltaTime; yield return null; }

        for (int i = 0; i < cards.Length; i++)
        {
            bool active = i < _shown.Count;
            var c = cards[i];
            if (c.button != null) c.button.gameObject.SetActive(active);
            if (!active) continue;

            var ev = _shown[i];
            if (c.nameText != null) c.nameText.text = ev.eventName;
            if (c.descriptionText != null) c.descriptionText.text = ev.description;
            if (c.effectsText != null) c.effectsText.text = BuildEffects(ev);
        }

        if (panel != null) panel.SetActive(true);

        if (EventSystem.current != null && cards.Length > 0 && cards[0].button != null)
            EventSystem.current.SetSelectedGameObject(cards[0].button.gameObject);

        _openRoutine = null;
    }

    void Update()
    {
        if (!enableNumberHotkeys || panel == null || !panel.activeSelf) return;

        for (int i = 0; i < _shown.Count; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                Choose(i);
                return;
            }
        }
    }

    void Choose(int index)
    {
        if (index < 0 || index >= _shown.Count) return;

        var chosen = _shown[index];
        _shown.Clear();

        if (panel != null) panel.SetActive(false);
        ReleaseHolds();

        if (DailyEventManager.Instance != null)
            DailyEventManager.Instance.SelectEvent(chosen);
    }

    void ReleaseHolds()
    {
        if (_openRoutine != null) { StopCoroutine(_openRoutine); _openRoutine = null; }

        if (_didPause && DayNightManager.Instance != null)
            DayNightManager.Instance.ResumeGame();
        _didPause = false;

        if (_didLock) PlayerController2D.IsLocked = false;
        _didLock = false;
    }

    // ── สรุปผลของ Event จากตัวคูณที่ไม่ใช่ 1 ─────────────────────
    static string BuildEffects(DailyEventManager.DailyEvent e)
    {
        var lines = new List<string>();
        AddLine(lines, "Cat payment", e.moneyMultiplier);
        AddLine(lines, "Cat arrival", e.spawnRateMultiplier);
        AddLine(lines, "Furniture price", e.furniturePriceMultiplier);
        AddLine(lines, "Cooking speed", e.cookingSpeedMultiplier);
        return string.Join("\n", lines);
    }

    static void AddLine(List<string> lines, string label, float mult)
    {
        if (Mathf.Approximately(mult, 1f)) return;
        lines.Add($"{label} x{mult:0.##}");
    }
}