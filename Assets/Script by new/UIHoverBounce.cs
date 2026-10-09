using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverBounce : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [Tooltip("Offset position when hovered (e.g., Y = 15 moves up slightly)")]
    [SerializeField] private Vector2 hoverOffset = new Vector2(0f, 15f);

    [Tooltip("Scale multiplier when hovered (e.g., 1.08 = 8% larger)")]
    [SerializeField] private float hoverScale = 1.08f;

    [Tooltip("Duration of the transition")]
    [SerializeField] private float transitionSpeed = 0.15f;

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
        // Reset state immediately if deactivated
        if (targetRect != null)
        {
            targetRect.anchoredPosition = defaultPosition;
            targetRect.localScale = defaultScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 targetPos = defaultPosition + hoverOffset;
        Vector3 targetSc = defaultScale * hoverScale;
        StartTransition(targetPos, targetSc);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartTransition(defaultPosition, defaultScale);
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
            elapsed += Time.deltaTime;
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