using System.Collections;
using UnityEngine;

public class CoinPunchAnimator : MonoBehaviour
{
    [Header("Target RectTransform")]
    [SerializeField] private RectTransform coinRect;

    [Header("Punch Settings")]
    [SerializeField] private float punchScaleMultiplier = 1.35f;
    [SerializeField] private float duration = 0.25f;

    private Vector3 initialScale;
    private Coroutine punchRoutine;

    void Awake()
    {
        if (coinRect == null)
            coinRect = GetComponent<RectTransform>();

        if (coinRect != null)
            initialScale = coinRect.localScale;
        else
            initialScale = Vector3.one;
    }

    /// <summary>
    /// Call this function every time money is added.
    /// </summary>
    public void PlayCoinPunch()
    {
        if (punchRoutine != null)
            StopCoroutine(punchRoutine);

        punchRoutine = StartCoroutine(AnimatePunchRoutine());
    }

    private IEnumerator AnimatePunchRoutine()
    {
        // Reset scale before starting
        coinRect.localScale = initialScale;

        Vector3 targetScale = initialScale * punchScaleMultiplier;
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        // 1. Pop Up (Scale Up)
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            coinRect.localScale = Vector3.Lerp(initialScale, targetScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        coinRect.localScale = targetScale;
        elapsed = 0f;

        // 2. Bounce Back (Scale Down to Original)
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            coinRect.localScale = Vector3.Lerp(targetScale, initialScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        coinRect.localScale = initialScale;
        punchRoutine = null;
    }
}