using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// =====================================================================
// ComicSequencePlayer — เล่นฉาก Comic Slide บน UI
//
//  แผงทั้งหมดเรียงต่อกันเป็นแถบยาวในแนวนอน แล้ว "เลื่อนมุมมองไปทางขวา" ทีละแผง (ภาพเปลี่ยนตอนสไลด์)
//  ระหว่างอ่านแต่ละแผงภาพจะลอยไปทางขวาช้าๆ ให้ดูมีชีวิต
//
//  ควบคุม: คลิกซ้าย / Space / Enter / ลูกศรขวา = ไปแผงต่อไป | skipKey (Esc) = ข้ามทั้งชุด
//
//  วางสคริปต์นี้บน GameObject ที่ "เปิดอยู่เสมอ" (เช่น ComicSystem) ส่วน Root ของ UI ให้ปิดไว้ตอนเริ่ม
//  ไม่ผูกกับ New Game โดยตรง — ใช้เล่นจาก MainMenu หรือในเกมก็ได้:
//      ComicSequencePlayer.Instance.Play(mySequence, () => { /* ทำต่อเมื่อจบ */ });
// =====================================================================
public class ComicSequencePlayer : MonoBehaviour
{
    public static ComicSequencePlayer Instance { get; private set; }
    public static bool IsPlaying { get; private set; }

    [Header("UI References")]
    [Tooltip("Root ของ UI ทั้งชุด (ปิดไว้ตอนเริ่ม) — ควรเป็น Canvas ที่ Sort Order สูงกว่า UI อื่น")]
    public GameObject root;
    [Tooltip("CanvasGroup บน Root (ใช้ Fade เข้า/ออก)")]
    public CanvasGroup rootGroup;
    [Tooltip("กรอบที่มองเห็นภาพ (เต็มจอ) — ใส่ RectMask2D ไว้ด้วย")]
    public RectTransform viewport;
    [Tooltip("แถบที่เลื่อนไปมา (ลูกของ Viewport, ว่างเปล่า)")]
    public RectTransform strip;
    [Tooltip("Image ต้นแบบของแต่ละแผง (ลูกของ Strip, ปิดไว้ — สคริปต์จะ Clone ไปใช้)")]
    public Image panelTemplate;
    [Tooltip("ข้อความใต้ภาพ (เว้นว่างได้)")]
    public TextMeshProUGUI captionText;
    [Tooltip("CanvasGroup ของ Caption สำหรับ Fade (เว้นว่างได้)")]
    public CanvasGroup captionGroup;
    [Tooltip("ข้อความบอกวิธีข้าม เช่น 'ESC = Skip' (เว้นว่างได้)")]
    public GameObject skipHint;
    [Tooltip("เล่นเสียงของแต่ละแผง (เว้นว่างได้)")]
    public AudioSource sfxSource;

    [Header("Layout / Timing")]
    public float panelGap = 0f;
    public float fadeDuration = 0.4f;
    public float captionFadeDuration = 0.25f;
    [Tooltip("ไม่รับการกดไปแผงต่อไปในช่วงแรกของแต่ละแผง (กันกดรัวข้ามโดยไม่ได้ตั้งใจ)")]
    public float inputDelay = 0.3f;
    public bool preserveAspect = true;

    [Header("Controls")]
    public KeyCode skipKey = KeyCode.Escape;

    [Header("Button Hook (ใช้กับ OnClick ใน Inspector)")]
    [Tooltip("ฉากที่เล่นเมื่อปุ่มเรียก PlayAssigned()")]
    public ComicSequenceData sequenceToPlay;
    [Tooltip("เรียกเมื่อ PlayAssigned() เล่นจบหรือถูกข้าม — ต่อ Event ต่อได้ เช่น MainMenuIntro.TriggerTransitionEffect")]
    public UnityEvent onFinished;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private ComicSequenceData _current;
    private bool _skipRequested, _advanceRequested;
    private bool _pausedByComic, _lockedByComic;
    private Coroutine _captionRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        IsPlaying = false;
        if (root != null) root.SetActive(false);
        if (panelTemplate != null) panelTemplate.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            EndHold();
            IsPlaying = false;
            Instance = null;
        }
    }

    void Update()
    {
        if (!IsPlaying || _current == null) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.RightArrow))
            _advanceRequested = true;

        if (_current.allowSkip && Input.GetKeyDown(skipKey))
            _skipRequested = true;
    }

    /// <summary>
    /// เริ่มเล่น 1 ชุด — คืน false ถ้ากำลังเล่นอีกชุดอยู่ (ผู้เรียกควรรอแล้วเรียกใหม่)
    /// onFinished ถูกเรียกเมื่อจบหรือถูกข้าม
    /// </summary>
    public bool Play(ComicSequenceData sequence, Action onFinished = null)
    {
        if (IsPlaying) return false;

        if (sequence == null || sequence.panels == null || sequence.panels.Count == 0 ||
            root == null || viewport == null || strip == null || panelTemplate == null)
        {
            Debug.LogWarning("[ComicSequencePlayer] เล่นไม่ได้: ข้อมูลหรือ UI Reference ไม่ครบ");
            onFinished?.Invoke();
            return true;
        }

        StartCoroutine(PlayRoutine(sequence, onFinished));
        return true;
    }

    /// <summary>
    /// สำหรับปุ่ม (OnClick): เล่นฉากที่ลากไว้ในช่อง Sequence To Play แล้วเรียก On Finished เมื่อจบ
    /// (Play(seq, callback) ด้านบนไม่โผล่ในเมนู OnClick เพราะ Unity รองรับเมธอดที่มีพารามิเตอร์ได้แค่ 1 ตัวและชนิดง่ายๆ)
    /// </summary>
    public void PlayAssigned()
    {
        Play(sequenceToPlay, () => onFinished?.Invoke());
    }

    /// <summary>สำหรับปุ่ม (OnClick) ที่ต้องการระบุฉากเองที่ปุ่ม — ไม่มี Event ตอนจบ</summary>
    public void PlaySequence(ComicSequenceData sequence)
    {
        Play(sequence);
    }

    // ───────────────────────── Main routine ─────────────────────────
    private IEnumerator PlayRoutine(ComicSequenceData seq, Action onFinished)
    {
        IsPlaying = true;
        _current = seq;
        _skipRequested = false;
        _advanceRequested = false;
        BeginHold(seq);

        root.SetActive(true);
        if (rootGroup != null) rootGroup.alpha = 0f;
        if (skipHint != null) skipHint.SetActive(seq.allowSkip);
        SetCaptionAlpha(0f);
        Canvas.ForceUpdateCanvases();

        float w = BuildPanels(seq);
        SetStripX(0f);

        yield return StartCoroutine(FadeRoot(0f, 1f));

        for (int i = 0; i < seq.panels.Count && !_skipRequested; i++)
        {
            ComicPanel p = seq.panels[i];
            float baseX = -i * (w + panelGap);

            if (i > 0)
            {
                SetCaptionAlpha(0f);
                float dur = p.slideDuration >= 0f ? p.slideDuration : seq.defaultSlideDuration;
                yield return StartCoroutine(SlideTo(baseX, dur));
            }
            if (_skipRequested) break;

            ShowCaption(p.caption);
            if (p.sfx != null && sfxSource != null) sfxSource.PlayOneShot(p.sfx);

            yield return StartCoroutine(HoldPanel(p, seq, baseX));
        }

        yield return StartCoroutine(FadeRoot(1f, 0f));

        Cleanup();
        onFinished?.Invoke();
    }

    private float BuildPanels(ComicSequenceData seq)
    {
        float w = Mathf.Max(1f, viewport.rect.width);

        strip.anchorMin = new Vector2(0f, 0f);
        strip.anchorMax = new Vector2(0f, 1f);
        strip.pivot = new Vector2(0f, 0.5f);
        strip.sizeDelta = new Vector2(0f, 0f);

        for (int i = 0; i < seq.panels.Count; i++)
        {
            GameObject go = Instantiate(panelTemplate.gameObject, strip);
            go.name = "ComicPanel_" + i;
            go.SetActive(true);
            _spawned.Add(go);

            Image img = go.GetComponent<Image>();
            img.sprite = seq.panels[i].image;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(w, 0f);
            rt.anchoredPosition = new Vector2(i * (w + panelGap), 0f);
        }
        return w;
    }

    private IEnumerator HoldPanel(ComicPanel p, ComicSequenceData seq, float baseX)
    {
        float drift = p.driftPixelsPerSecond >= 0f ? p.driftPixelsPerSecond : seq.defaultDriftPixelsPerSecond;
        float limit = Mathf.Max(0f, seq.maxDriftPixels);
        float t = 0f;
        _advanceRequested = false;

        while (!_skipRequested)
        {
            t += Time.unscaledDeltaTime;

            // ภาพลอยไปทางขวาช้าๆ แล้วค่อยๆ หยุด (ไม่กระชาก)
            if (drift > 0f && limit > 0f)
            {
                float offset = limit * (1f - Mathf.Exp(-drift * t / limit));
                SetStripX(baseX - offset);
            }

            if (t >= inputDelay && _advanceRequested) break;
            if (!p.waitForInput && t >= p.autoAdvanceSeconds) break;
            yield return null;
        }
        _advanceRequested = false;
    }

    private IEnumerator SlideTo(float targetX, float duration)
    {
        float startX = strip.anchoredPosition.x;
        if (duration <= 0f) { SetStripX(targetX); yield break; }

        float t = 0f;
        while (t < duration && !_skipRequested)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetStripX(Mathf.Lerp(startX, targetX, k));
            yield return null;
        }
        SetStripX(targetX);
    }

    private IEnumerator FadeRoot(float from, float to)
    {
        if (rootGroup == null || fadeDuration <= 0f) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        rootGroup.alpha = to;
    }

    // ───────────────────────── Caption ─────────────────────────
    private void SetCaptionAlpha(float a)
    {
        if (captionGroup != null) captionGroup.alpha = a;
        else if (captionText != null) captionText.alpha = a;
    }

    private void ShowCaption(string text)
    {
        if (captionText == null) return;
        if (_captionRoutine != null) StopCoroutine(_captionRoutine);

        captionText.text = text ?? "";
        if (string.IsNullOrEmpty(text)) { SetCaptionAlpha(0f); return; }

        _captionRoutine = StartCoroutine(FadeCaption());
    }

    private IEnumerator FadeCaption()
    {
        float t = 0f;
        SetCaptionAlpha(0f);
        while (t < captionFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            SetCaptionAlpha(Mathf.Clamp01(t / captionFadeDuration));
            yield return null;
        }
        SetCaptionAlpha(1f);
        _captionRoutine = null;
    }

    // ───────────────────────── Pause / Lock ─────────────────────────
    private void BeginHold(ComicSequenceData seq)
    {
        if (seq.pauseGameTime && DayNightManager.Instance != null && !_pausedByComic)
        {
            DayNightManager.Instance.PauseGame();
            _pausedByComic = true;
        }
        if (seq.lockPlayer && !_lockedByComic)
        {
            PlayerController2D.SetLock(true);
            _lockedByComic = true;
        }
    }

    // Pause/Lock แบบจับคู่กับที่เราเปิดเองเท่านั้น (ไม่ไปปลดของระบบอื่น)
    private void EndHold()
    {
        if (_pausedByComic)
        {
            _pausedByComic = false;
            if (DayNightManager.Instance != null) DayNightManager.Instance.ResumeGame();
        }
        if (_lockedByComic)
        {
            _lockedByComic = false;
            PlayerController2D.SetLock(false);
        }
    }

    private void SetStripX(float x)
    {
        Vector2 p = strip.anchoredPosition;
        p.x = x;
        strip.anchoredPosition = p;
    }

    private void Cleanup()
    {
        foreach (GameObject go in _spawned) if (go != null) Destroy(go);
        _spawned.Clear();

        if (root != null) root.SetActive(false);
        EndHold();

        _current = null;
        IsPlaying = false;
    }
}