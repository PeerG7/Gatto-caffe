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

    // AudioSource กลาง ใช้ร่วมกันทุกปุ่ม ไม่ต้องใส่ AudioSource ทีละอัน
    private static AudioSource sharedSource;

    private RectTransform targetRect;
    private Vector2 defaultPosition;
    private Vector3 defaultScale;
    private Coroutine activeRoutine;

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
        if (targetRect != null)
        {
            targetRect.anchoredPosition = defaultPosition;
            targetRect.localScale = defaultScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSfx();
        Vector2 targetPos = defaultPosition + hoverOffset;
        Vector3 targetSc = defaultScale * hoverScale;
        StartTransition(targetPos, targetSc);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartTransition(defaultPosition, defaultScale);
    }

    // เสียงตอนเลือกด้วยคีย์บอร์ด/จอย (เมาส์ชี้ก็ทำให้เกิด select ได้ในบางกรณี
    // ถ้าได้ยินเสียงซ้ำสองครั้ง ให้ลบ ISelectHandler กับเมธอดนี้ออก)
    public void OnSelect(BaseEventData eventData)
    {
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