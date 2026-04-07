using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public RectTransform interactionPanel;
    public CanvasGroup canvasGroup;

    public float duration = 0.4f;

    Vector2 hiddenPos = new Vector2(-50, -300);
    Vector2 shownPos = new Vector2(-50, 50);

    void Awake()
    {
        Instance = this;

        interactionPanel.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 0;
    }

    public void ShowInteractionUI(NPCController npc)
    {
        StopAllCoroutines();
        StartCoroutine(SlideUI(true));
    }

    public void HideInteractionUI()
    {
        StopAllCoroutines();
        StartCoroutine(SlideUI(false));
    }

    IEnumerator SlideUI(bool show)
    {
        float time = 0f;

        Vector2 startPos = interactionPanel.anchoredPosition;
        Vector2 targetPos = show ? shownPos : hiddenPos;

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // ✨ Easing (สำคัญ)
            t = EaseOutBack(t);

            interactionPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        interactionPanel.anchoredPosition = targetPos;
        canvasGroup.alpha = targetAlpha;
    }

    // 🔥 Cozy easing
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }
}