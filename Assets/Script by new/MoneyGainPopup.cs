using System.Collections;
using UnityEngine;
using TMPro;

public class MoneyGainPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private float floatDistance = 25f;
    [SerializeField] private float duration = 0.8f;

    private Vector2 baseAnchoredPosition;
    private Color originalColor;
    private Coroutine activeRoutine;

    void Awake()
    {
        if (popupText == null)
            popupText = GetComponent<TextMeshProUGUI>();

        if (popupText != null)
        {
            baseAnchoredPosition = popupText.rectTransform.anchoredPosition;
            originalColor = popupText.color;
            // Hide on awake
            Color c = originalColor;
            c.a = 0f;
            popupText.color = c;
        }
    }

    public void ShowGain(int amount)
    {
        if (popupText == null) return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(AnimateGainRoutine(amount));
    }

    private IEnumerator AnimateGainRoutine(int amount)
    {
        popupText.text = "+$" + amount.ToString();
        popupText.rectTransform.anchoredPosition = baseAnchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Float upward slightly
            float currentY = Mathf.Lerp(baseAnchoredPosition.y, baseAnchoredPosition.y + floatDistance, t);
            popupText.rectTransform.anchoredPosition = new Vector2(baseAnchoredPosition.x, currentY);

            // Fade out
            Color c = originalColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            popupText.color = c;

            yield return null;
        }

        // Ensure fully transparent and reset position
        Color endColor = originalColor;
        endColor.a = 0f;
        popupText.color = endColor;
        popupText.rectTransform.anchoredPosition = baseAnchoredPosition;
        activeRoutine = null;
    }
}