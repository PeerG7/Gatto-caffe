using System.Collections;
using UnityEngine;

public class CoinFlipAnimator : MonoBehaviour
{
    [Header("Target RectTransform")]
    [SerializeField] private RectTransform coinRect;

    [Header("Flip Settings")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private int fullRotations = 1;

    private Quaternion initialRotation;
    private Coroutine flipRoutine;

    void Awake()
    {
        if (coinRect == null)
            coinRect = GetComponent<RectTransform>();

        if (coinRect != null)
            initialRotation = coinRect.localRotation;
        else
            initialRotation = Quaternion.identity;
    }

    /// <summary>
    /// Call this function to trigger coin flip animation.
    /// </summary>
    public void PlayCoinFlip()
    {
        if (flipRoutine != null)
            StopCoroutine(flipRoutine);

        flipRoutine = StartCoroutine(AnimateFlipRoutine());
    }

    private IEnumerator AnimateFlipRoutine()
    {
        coinRect.localRotation = initialRotation;

        float targetAngle = 360f * fullRotations;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth ease out rotation
            float curve = Mathf.SmoothStep(0f, 1f, t);
            float currentYAngle = Mathf.Lerp(0f, targetAngle, curve);

            coinRect.localRotation = initialRotation * Quaternion.Euler(0f, currentYAngle, 0f);

            yield return null;
        }

        coinRect.localRotation = initialRotation;
        flipRoutine = null;
    }
}