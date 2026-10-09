using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CookButtonAnimator : MonoBehaviour
{
    [Header("Target RectTransform")]
    [SerializeField] private RectTransform buttonRect;

    [Header("Click Punch Settings")]
    [SerializeField] private Vector3 clickSquashScale = new Vector3(1.15f, 0.85f, 1f);
    [SerializeField] private float clickDuration = 0.15f;

    [Header("Ready Wobble / Pulse Settings")]
    [SerializeField] private bool playReadyAnimation = false;
    [SerializeField] private float wobbleSpeed = 4f;
    [SerializeField] private float wobbleAngle = 4f;
    [SerializeField] private float pulseScale = 1.05f;

    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Coroutine clickRoutine;
    private Button button;

    void Awake()
    {
        if (buttonRect == null)
            buttonRect = GetComponent<RectTransform>();

        button = GetComponent<Button>();

        if (buttonRect != null)
        {
            initialScale = buttonRect.localScale;
            initialRotation = buttonRect.localRotation;
        }
    }

    void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(PlayClickEffect);
    }

    void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickEffect);

        ResetTransforms();
    }

    void Update()
    {
        if (playReadyAnimation && (clickRoutine == null))
        {
            // Gentle wobble tilt and slight pulse
            float tilt = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
            float scaleMultiplier = 1f + (Mathf.Sin(Time.time * wobbleSpeed * 1.5f) * (pulseScale - 1f));

            buttonRect.localRotation = initialRotation * Quaternion.Euler(0f, 0f, tilt);
            buttonRect.localScale = initialScale * scaleMultiplier;
        }
    }

    /// <summary>
    /// Play punch/squash animation on click.
    /// </summary>
    public void PlayClickEffect()
    {
        if (clickRoutine != null)
            StopCoroutine(clickRoutine);

        clickRoutine = StartCoroutine(AnimateClickRoutine());
    }

    /// <summary>
    /// Call this from your cooking system when ingredients are complete.
    /// </summary>
    public void SetReadyState(bool isReady)
    {
        playReadyAnimation = isReady;

        if (!isReady)
        {
            ResetTransforms();
        }
    }

    private void ResetTransforms()
    {
        if (buttonRect != null)
        {
            buttonRect.localScale = initialScale;
            buttonRect.localRotation = initialRotation;
        }
    }

    private IEnumerator AnimateClickRoutine()
    {
        Vector3 targetScale = Vector3.Scale(initialScale, clickSquashScale);
        float halfDuration = clickDuration * 0.5f;
        float elapsed = 0f;

        // 1. Squash down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            buttonRect.localScale = Vector3.Lerp(initialScale, targetScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        buttonRect.localScale = targetScale;
        elapsed = 0f;

        // 2. Pop back
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            buttonRect.localScale = Vector3.Lerp(targetScale, initialScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        buttonRect.localScale = initialScale;
        buttonRect.localRotation = initialRotation;
        clickRoutine = null;
    }
}