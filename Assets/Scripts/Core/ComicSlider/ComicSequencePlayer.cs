using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// =====================================================================
// ComicSequencePlayer — เล่นฉาก Comic บน UI
//
//  Layout = SlideStrip : แผงเรียงต่อกันเป็นแถบยาว เลื่อนมุมมองไปทางขวาทีละแผง
//  Layout = PageFocus  : รูปทั้งหน้ากระดาษรูปเดียว กล้องซูม/เลื่อนไปทีละช่อง (ตาม Focus Rect ของแต่ละ Panel)
//
//  ควบคุม: คลิกซ้าย / Space / Enter / ลูกศรขวา = ไปต่อ | skipKey (Esc) = ข้ามทั้งชุด
//
//  วางสคริปต์นี้บน GameObject ที่ "เปิดอยู่เสมอ" (เช่น ComicSystem) ส่วน Root ของ UI ให้ปิดไว้ตอนเริ่ม
//      ComicSequencePlayer.Instance.Play(mySequence, () => { /* ทำต่อเมื่อจบ */ });
//  หรือจากปุ่ม (OnClick): ComicSequencePlayer → PlayAssigned()
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
    [Tooltip("แถบ/หน้ากระดาษที่เลื่อนและซูม (ลูกของ Viewport, ว่างเปล่า)")]
    public RectTransform strip;
    [Tooltip("Image ต้นแบบ (ลูกของ Strip, ปิดไว้ — สคริปต์จะ Clone ไปใช้)")]
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
    [Tooltip("ไม่รับการกดไปต่อในช่วงแรกของแต่ละแผง (กันกดรัวข้ามโดยไม่ได้ตั้งใจ)")]
    public float inputDelay = 0.3f;
    public bool preserveAspect = true;

    [Header("Controls")]
    public KeyCode skipKey = KeyCode.Escape;
    [Tooltip("ปุ่มจอยสำหรับไปต่อ (ค่าเริ่มต้น = ปุ่ม A / JoystickButton0)")]
    public KeyCode gamepadAdvanceKey = KeyCode.JoystickButton0;
    [Tooltip("ปุ่มจอยสำหรับข้ามทั้งชุด (ค่าเริ่มต้น = ปุ่ม Start / JoystickButton7)")]
    public KeyCode gamepadSkipKey = KeyCode.JoystickButton7;

    [Header("Input Blocking (กันคลิก/กดทะลุไปโดนปุ่มเมนูข้างหลัง)")]
    [Tooltip("สร้างฉากหลังเต็มจอที่รับ Raycast ให้เองตอนเล่น — กันคลิกทะลุไปโดนปุ่ม New Game / Quit ที่อยู่ข้างหลัง")]
    public bool blockInputBehind = true;
    [Tooltip("สีฉากหลังเต็มจอ (ดำ = ไม่เห็นเมนูทะลุตอนภาพไม่เต็มจอ)")]
    public Color backdropColor = Color.black;

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
    private GameObject _blocker;

    // PageFocus
    private struct CamState { public Vector2 center; public float zoom; }
    private float _pageW, _pageH, _viewW, _viewH;
    private float _imgW, _imgH; // ขนาดพิกเซลจริงของรูป (ใช้แปลง Focus Rect ที่ใส่เป็นพิกเซลโดยไม่ตั้งใจ)
    private bool _clampToPage;

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
            Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(gamepadAdvanceKey))
            _advanceRequested = true;

        if (_current.allowSkip && (Input.GetKeyDown(skipKey) || Input.GetKeyDown(gamepadSkipKey)))
        {
            _skipRequested = true;
            Debug.Log("[Comic] ผู้เล่นกดข้าม");
        }
    }

    /// <summary>
    /// เริ่มเล่น 1 ชุด — คืน false ถ้ากำลังเล่นอีกชุดอยู่ (ผู้เรียกควรรอแล้วเรียกใหม่)
    /// onFinished ถูกเรียกเมื่อจบหรือถูกข้าม
    /// </summary>
    public bool Play(ComicSequenceData sequence, Action onFinished = null)
    {
        if (IsPlaying) return false;

        bool invalid = sequence == null || sequence.panels == null || sequence.panels.Count == 0 ||
                       root == null || viewport == null || strip == null || panelTemplate == null ||
                       (sequence.layout == ComicLayout.PageFocus && sequence.pageImage == null);
        if (invalid)
        {
            Debug.LogWarning("[ComicSequencePlayer] เล่นไม่ได้: ข้อมูลหรือ UI Reference ไม่ครบ (PageFocus ต้องใส่ Page Image ด้วย)");
            onFinished?.Invoke();
            return true;
        }

        if (sequence.layout == ComicLayout.SlideStrip && sequence.pageImage != null)
            Debug.LogWarning($"[ComicSequencePlayer] '{sequence.name}' ใส่ Page Image ไว้ แต่ Layout เป็น SlideStrip จึงไม่ซูม — ตั้ง Layout เป็น PageFocus");

        StartCoroutine(PlayRoutine(sequence, onFinished));
        return true;
    }

    /// <summary>สำหรับปุ่ม (OnClick): เล่นฉากในช่อง Sequence To Play แล้วเรียก On Finished เมื่อจบ</summary>
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
        Debug.Log($"[Comic] เริ่มเล่น '{seq.name}' ({seq.layout}, {seq.panels.Count} จุด) ที่ {Time.unscaledTime:0.00}s");

        root.SetActive(true);
        PrepareInputBlock();
        if (rootGroup != null) rootGroup.alpha = 0f;
        if (skipHint != null) skipHint.SetActive(seq.allowSkip);
        SetCaptionAlpha(0f);
        Canvas.ForceUpdateCanvases();

        bool pageMode = seq.layout == ComicLayout.PageFocus;
        float stripPanelWidth = pageMode ? BuildPage(seq) : BuildPanels(seq);

        yield return StartCoroutine(FadeRoot(0f, 1f));

        if (pageMode) yield return StartCoroutine(RunPageFocus(seq));
        else yield return StartCoroutine(RunStrip(seq, stripPanelWidth));

        Debug.Log($"[Comic] จบ '{seq.name}' ที่ {Time.unscaledTime:0.00}s (ถูกกดข้าม: {_skipRequested})");

        yield return StartCoroutine(FadeRoot(1f, 0f));

        Cleanup();
        onFinished?.Invoke();
    }

    // ทำให้ Comic อยู่หน้าสุด + รับ Raycast ทั้งจอ (ไม่งั้นคลิกที่ใช้ข้าม Comic จะทะลุไปกดปุ่มเมนูข้างหลังด้วย)
    private void PrepareInputBlock()
    {
        root.transform.SetAsLastSibling();

        if (rootGroup != null)
        {
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
        }

        // ถ้า Root เป็น Canvas ของตัวเอง ต้องมี GraphicRaycaster ถึงจะบล็อกได้
        if (root.GetComponent<Canvas>() != null && root.GetComponent<GraphicRaycaster>() == null)
            root.AddComponent<GraphicRaycaster>();

        if (!blockInputBehind) return;

        if (_blocker == null)
        {
            _blocker = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image));
            _blocker.transform.SetParent(root.transform, false);

            RectTransform rt = (RectTransform)_blocker.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Image img = _blocker.GetComponent<Image>();
        img.color = backdropColor;
        img.raycastTarget = true;

        _blocker.SetActive(true);
        _blocker.transform.SetAsFirstSibling();   // อยู่หลังภาพ Comic แต่หน้าเมนู
    }

    // ค่าเริ่มต้นของ Element ใหม่ใน List ของ Unity เป็น 0 ทั้งหมด (ไม่ใช่ค่าที่ตั้งในโค้ด)
    // จึงตีความให้ "0 = ใช้ค่าปกติ" เพื่อกันกรณีลืมใส่ค่า แล้วฉากเลื่อนผ่านไปทันที
    //  • Slide Duration  ≤ 0  → ใช้ Default Slide Duration ของ Sequence
    //  • ไปเองอัตโนมัติ เฉพาะเมื่อ "ไม่ติ๊ก Wait For Input" และ Auto Advance Seconds > 0 | นอกนั้น = รอผู้เล่นกด
    private static float SlideDuration(ComicPanel p, ComicSequenceData seq)
    {
        return p.slideDuration > 0f ? p.slideDuration : seq.defaultSlideDuration;
    }

    private static bool IsAutoAdvance(ComicPanel p)
    {
        return !p.waitForInput && p.autoAdvanceSeconds > 0.01f;
    }

    // ═════════════════════════ SlideStrip ═════════════════════════
    private IEnumerator RunStrip(ComicSequenceData seq, float w)
    {
        for (int i = 0; i < seq.panels.Count && !_skipRequested; i++)
        {
            ComicPanel p = seq.panels[i];
            float baseX = -i * (w + panelGap);

            if (i > 0)
            {
                SetCaptionAlpha(0f);
                float dur = SlideDuration(p, seq);
                yield return StartCoroutine(SlideTo(baseX, dur));
            }
            if (_skipRequested) break;

            ShowCaption(p.caption);
            PlaySfx(p);

            yield return StartCoroutine(HoldPanel(p, seq, baseX));
        }
    }

    private float BuildPanels(ComicSequenceData seq)
    {
        float w = Mathf.Max(1f, viewport.rect.width);

        strip.anchorMin = new Vector2(0f, 0f);
        strip.anchorMax = new Vector2(0f, 1f);
        strip.pivot = new Vector2(0f, 0.5f);
        strip.sizeDelta = new Vector2(0f, 0f);
        strip.localScale = Vector3.one;
        SetStripX(0f);

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
        float drift = p.driftPixelsPerSecond > 0f ? p.driftPixelsPerSecond : seq.defaultDriftPixelsPerSecond;
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
            if (IsAutoAdvance(p) && t >= p.autoAdvanceSeconds) break;
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

    // ═════════════════════════ PageFocus ═════════════════════════
    // กล้อง = (center, zoom)  center เป็นหน่วยพิกเซลของหน้า วัดจากกึ่งกลางหน้า (แกน Y ชี้ขึ้น)
    // ภาพจริง = strip ที่ถูกขยาย (localScale = zoom) และเลื่อนให้จุด center ไปอยู่กลางจอ
    private float BuildPage(ComicSequenceData seq)
    {
        _viewW = Mathf.Max(1f, viewport.rect.width);
        _viewH = Mathf.Max(1f, viewport.rect.height);
        _clampToPage = seq.clampToPage;

        Sprite sp = seq.pageImage;
        float imgW = Mathf.Max(1f, sp.rect.width);
        float imgH = Mathf.Max(1f, sp.rect.height);
        _imgW = imgW;
        _imgH = imgH;
        float fit = Mathf.Min(_viewW / imgW, _viewH / imgH); // ย่อรูปให้พอดีจอ (zoom = 1 = เห็นทั้งหน้า)
        _pageW = imgW * fit;
        _pageH = imgH * fit;

        strip.anchorMin = new Vector2(0.5f, 0.5f);
        strip.anchorMax = new Vector2(0.5f, 0.5f);
        strip.pivot = new Vector2(0.5f, 0.5f);
        strip.sizeDelta = new Vector2(_pageW, _pageH);

        GameObject go = Instantiate(panelTemplate.gameObject, strip);
        go.name = "ComicPage";
        go.SetActive(true);
        _spawned.Add(go);

        Image img = go.GetComponent<Image>();
        img.sprite = sp;
        img.preserveAspect = false; // ขนาดคำนวณให้ตรงสัดส่วนรูปแล้ว
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        ApplyCamera(new CamState { center = Vector2.zero, zoom = 1f });
        return _pageW;
    }

    private IEnumerator RunPageFocus(ComicSequenceData seq)
    {
        CamState overview = new CamState { center = Vector2.zero, zoom = 1f };
        CamState cur = overview;
        ApplyCamera(cur);

        // เริ่มด้วยภาพทั้งหน้า
        if (seq.overviewHoldSeconds > 0f)
            yield return StartCoroutine(HoldSimple(seq.overviewHoldSeconds, false));

        for (int i = 0; i < seq.panels.Count && !_skipRequested; i++)
        {
            ComicPanel p = seq.panels[i];
            CamState target = StateForRect(p.focusRect, seq.focusPadding);

            Debug.Log($"[Comic] จุดที่ {i + 1}/{seq.panels.Count}: Focus Rect {p.focusRect} → ซูม x{target.zoom:0.00}");
            if (target.zoom < 1.05f)
                Debug.LogWarning($"[Comic] จุดที่ {i + 1} แทบไม่ซูม — Focus Rect ครอบเกือบทั้งหน้า (ค่าเริ่มต้น 0,0,1,1) ให้ใส่พื้นที่ช่องที่ต้องการซูม");

            SetCaptionAlpha(0f);
            float dur = SlideDuration(p, seq);
            yield return StartCoroutine(MoveCamera(cur, target, dur));
            cur = target;
            if (_skipRequested) break;

            ShowCaption(p.caption);
            PlaySfx(p);

            yield return StartCoroutine(HoldFocus(p, seq, target));
        }

        // จบด้วยการซูมออกให้เห็นทั้งหน้า (ถ้าเปิดไว้ และไม่ได้ถูกข้าม)
        if (seq.endWithOverview && !_skipRequested)
        {
            SetCaptionAlpha(0f);
            yield return StartCoroutine(MoveCamera(cur, overview, seq.defaultSlideDuration));
            if (!_skipRequested)
                yield return StartCoroutine(HoldSimple(Mathf.Max(1f, seq.overviewHoldSeconds), true));
        }
    }

    private CamState StateForRect(Rect r, float padding)
    {
        // ถ้าใส่ค่าเป็นพิกเซลของรูป (มีค่าเกิน 1) ให้แปลงเป็นสัดส่วน 0–1 ให้เอง (แกน Y นับจากด้านบนเหมือนกัน)
        if (r.xMax > 1.01f || r.yMax > 1.01f || r.width > 1.01f || r.height > 1.01f)
            r = new Rect(r.x / _imgW, r.y / _imgH, r.width / _imgW, r.height / _imgH);

        float rw = Mathf.Max(0.001f, r.width) * _pageW;
        float rh = Mathf.Max(0.001f, r.height) * _pageH;

        float z = Mathf.Min(_viewW / rw, _viewH / rh) * Mathf.Clamp(padding, 0.1f, 1f);
        z = Mathf.Max(1f, z);

        // Rect ใช้แกน Y จากด้านบน → แปลงเป็นแกน Y ขึ้นด้านบนจากกึ่งกลางหน้า
        Vector2 c = new Vector2(
            (r.x + r.width * 0.5f - 0.5f) * _pageW,
            (0.5f - (r.y + r.height * 0.5f)) * _pageH);

        return new CamState { center = c, zoom = z };
    }

    private void ApplyCamera(CamState s)
    {
        float z = Mathf.Max(1f, s.zoom);
        Vector2 c = s.center;

        if (_clampToPage)
        {
            // กล้องมองเห็นพื้นที่กว้าง (viewW / zoom) → ไม่ให้ขอบกล้องหลุดออกนอกหน้า
            float halfW = _viewW / (2f * z);
            float halfH = _viewH / (2f * z);
            float maxX = Mathf.Max(0f, _pageW * 0.5f - halfW);
            float maxY = Mathf.Max(0f, _pageH * 0.5f - halfH);
            c.x = Mathf.Clamp(c.x, -maxX, maxX);
            c.y = Mathf.Clamp(c.y, -maxY, maxY);
        }

        strip.localScale = new Vector3(z, z, 1f);
        strip.anchoredPosition = -c * z;
    }

    private IEnumerator MoveCamera(CamState from, CamState to, float duration)
    {
        if (duration <= 0f) { ApplyCamera(to); yield break; }

        float logFrom = Mathf.Log(Mathf.Max(1f, from.zoom));
        float logTo = Mathf.Log(Mathf.Max(1f, to.zoom));

        float t = 0f;
        while (t < duration && !_skipRequested)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));

            CamState s;
            s.center = Vector2.Lerp(from.center, to.center, k);
            s.zoom = Mathf.Exp(Mathf.Lerp(logFrom, logTo, k)); // ซูมแบบ log ให้ความรู้สึกสม่ำเสมอ
            ApplyCamera(s);
            yield return null;
        }
        ApplyCamera(to);
    }

    private IEnumerator HoldFocus(ComicPanel p, ComicSequenceData seq, CamState target)
    {
        float t = 0f;
        _advanceRequested = false;

        while (!_skipRequested)
        {
            t += Time.unscaledDeltaTime;

            // ค่อยๆ ซูมเข้าเพิ่มนิดหน่อยระหว่างอ่านให้ภาพดูมีชีวิต (ช้าลงเรื่อยๆ ไม่กระชาก)
            if (seq.holdPushIn > 0f)
            {
                CamState s = target;
                s.zoom = target.zoom * (1f + seq.holdPushIn * (1f - Mathf.Exp(-t * 0.6f)));
                ApplyCamera(s);
            }

            if (t >= inputDelay && _advanceRequested) break;
            if (IsAutoAdvance(p) && t >= p.autoAdvanceSeconds) break;
            yield return null;
        }
        _advanceRequested = false;
    }

    // รอเวลาที่กำหนด (กดข้ามได้หลัง inputDelay) — ใช้กับภาพทั้งหน้าตอนเริ่ม/จบ
    private IEnumerator HoldSimple(float seconds, bool alsoWaitForInput)
    {
        float t = 0f;
        _advanceRequested = false;

        while (!_skipRequested)
        {
            t += Time.unscaledDeltaTime;
            if (t >= inputDelay && _advanceRequested) break;
            if (!alsoWaitForInput && t >= seconds) break;
            yield return null;
        }
        _advanceRequested = false;
    }

    // ═════════════════════════ Common ═════════════════════════
    private void PlaySfx(ComicPanel p)
    {
        if (p.sfx != null && sfxSource != null) sfxSource.PlayOneShot(p.sfx);
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

        if (strip != null) strip.localScale = Vector3.one; // คืนค่า (PageFocus ขยาย strip ไว้)
        if (root != null) root.SetActive(false);
        EndHold();

        _current = null;
        _advanceRequested = false;
        _skipRequested = false;
        IsPlaying = false;
    }
}