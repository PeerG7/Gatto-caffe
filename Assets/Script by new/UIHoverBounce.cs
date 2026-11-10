using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverBounce : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    [Header("Hover Settings")]
    [Tooltip("Offset position when hovered (e.g., Y = 15 moves up slightly)")]
    [SerializeField] private Vector2 hoverOffset = new Vector2(0f, 15f);

    [Tooltip("Scale multiplier when hovered (e.g., 1.08 = 8% larger)")]
    [SerializeField] private float hoverScale = 1.08f;

    [Tooltip("Duration of the transition")]
    [SerializeField] private float transitionSpeed = 0.15f;

    [Header("Hover SFX")]
    [Tooltip("เสียงตอนเอาเมาส์ไปชี้ (ลาก mouse_chee_ui_sfx.wav มาใส่)")]
    [SerializeField] private AudioClip hoverSfx;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [Tooltip("ถ้าไม่ใส่ จะใช้ AudioSource กลางที่สร้างให้อัตโนมัติ")]
    [SerializeField] private AudioSource audioSource;

    [Header("Smudge / Cat Block")]
    [Tooltip("ปิดแอนิเมชัน hover และเสียง ตอนแมว/ซอสบังจอ")]
    [SerializeField] private bool disableWhileSmudge = true;

    // AudioSource กลาง ใช้ร่วมกันทุกปุ่ม ไม่ต้องใส่ AudioSource ทีละอัน
    private static AudioSource sharedSource;

    private RectTransform targetRect;
    private Vector2 defaultPosition;
    private Vector3 defaultScale;
    private Coroutine activeRoutine;

    // เมาส์ค้างอยู่บนปุ่มไหม / ตอนนี้ปุ่มอยู่ในสถานะ hover จริงไหม
    private bool isPointerOver = false;
    private bool hoverApplied = false;

    private bool IsBlocked => disableWhileSmudge && ScreenSmudgeManager.IsInterrupting;

    void Awake()
    {
        targetRect = GetComponent<RectTransform>();
        if (targetRect != null)
        {
            defaultPosition = targetRect.anchoredPosition;
            defaultScale = targetRect.localScale;
        }
    }

    void OnDisable()
    {
        isPointerOver = false;
        hoverApplied = false;
        activeRoutine = null;

        if (targetRect != null)
        {
            targetRect.anchoredPosition = defaultPosition;
            targetRect.localScale = defaultScale;
        }
    }

    void Update()
    {
        // แมวโผล่ตอนเมาส์ค้างบนปุ่ม -> ปุ่มกลับลงมา
        // แมวหายแล้วเมาส์ยังอยู่ -> ปุ่มเด้งขึ้นอีกครั้ง (ไม่มีเสียง)
        bool shouldHover = isPointerOver && !IsBlocked;
        if (shouldHover == hoverApplied) return;

        hoverApplied = shouldHover;
        if (shouldHover)
            StartTransition(defaultPosition + hoverOffset, defaultScale * hoverScale);
        else
            StartTransition(defaultPosition, defaultScale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        if (IsBlocked) return;   // ไม่มีเสียง ไม่มีแอนิเมชัน

        PlayHoverSfx();
        hoverApplied = true;
        StartTransition(defaultPosition + hoverOffset, defaultScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        hoverApplied = false;
        StartTransition(defaultPosition, defaultScale);
    }

    // เสียงตอนเลือกด้วยคีย์บอร์ด/จอย (เมาส์ชี้ก็ทำให้เกิด select ได้ในบางกรณี
    // ถ้าได้ยินเสียงซ้ำสองครั้ง ให้ลบ ISelectHandler กับเมธอดนี้ออก)
    public void OnSelect(BaseEventData eventData)
    {
        if (IsBlocked) return;
        PlayHoverSfx();
    }

    private void PlayHoverSfx()
    {
        if (hoverSfx == null) return;

        AudioSource src = audioSource != null ? audioSource : GetSharedSource();
        src.PlayOneShot(hoverSfx, sfxVolume);
    }

    private static AudioSource GetSharedSource()
    {
        if (sharedSource == null)
        {
            GameObject go = new GameObject("UIHoverSfxPlayer");
            DontDestroyOnLoad(go);
            sharedSource = go.AddComponent<AudioSource>();
            sharedSource.playOnAwake = false;
            sharedSource.spatialBlend = 0f; // 2D sound
        }
        return sharedSource;
    }

    private void StartTransition(Vector2 targetPos, Vector3 targetSc)
    {
        if (!isActiveAndEnabled || targetRect == null) return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(AnimateRoutine(targetPos, targetSc));
    }

    private IEnumerator AnimateRoutine(Vector2 targetPos, Vector3 targetSc)
    {
        Vector2 startPos = targetRect.anchoredPosition;
        Vector3 startSc = targetRect.localScale;
        float elapsed = 0f;

        while (elapsed < transitionSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionSpeed);
            float curve = Mathf.SmoothStep(0f, 1f, t);

            targetRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curve);
            targetRect.localScale = Vector3.Lerp(startSc, targetSc, curve);

            yield return null;
        }

        targetRect.anchoredPosition = targetPos;
        targetRect.localScale = targetSc;
        activeRoutine = null;
    }
}