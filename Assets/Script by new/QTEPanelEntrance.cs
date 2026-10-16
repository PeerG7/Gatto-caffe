using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class QTEPanelEntrance : MonoBehaviour
{
    [Header("Pop Animation Settings")]
    [Tooltip("Starting scale when opening panel")]
    [SerializeField] private Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);

    [Tooltip("Overshoot scale for pop bounce effect")]
    [SerializeField] private Vector3 popScale = new Vector3(1.08f, 1.08f, 1f);

    [Tooltip("Final scale of the panel")]
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [Tooltip("Animation duration in seconds")]
    [SerializeField] private float duration = 0.35f;

    [Header("Fade Settings")]
    [SerializeField] private bool useFadeIn = true;

    private CanvasGroup canvasGroup;
    private Coroutine entranceRoutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        PlayEntranceAnimation();
    }

    public void PlayEntranceAnimation()
    {
        if (entranceRoutine != null)
        {
            StopCoroutine(entranceRoutine);
        }
        entranceRoutine = StartCoroutine(AnimateEntrance());
    }

    private IEnumerator AnimateEntrance()
    {
        // Set initial state
        transform.localScale = startScale;
        if (useFadeIn && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        float elapsed = 0f;

        // Phase 1: Pop up slightly bigger than target scale (Bounce overshoot)
        float popDuration = duration * 0.7f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.localScale = Vector3.Lerp(startScale, popScale, smoothT);

            if (useFadeIn && canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            }

            yield return null;
        }

        // Phase 2: Settle back down to normal target scale
        elapsed = 0f;
        float settleDuration = duration * 0.3f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.localScale = Vector3.Lerp(popScale, targetScale, smoothT);
            yield return null;
        }

        // Ensure final exact values
        transform.localScale = targetScale;
        if (useFadeIn && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        entranceRoutine = null;
    }
}