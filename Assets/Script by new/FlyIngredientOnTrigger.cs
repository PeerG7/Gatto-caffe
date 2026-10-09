using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FlyIngredientOnTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sprite ingredientSprite;
    [SerializeField] private Transform canvasParent;
    [SerializeField] private CookingManager cookingManager;

    [Header("Flight Settings")]
    [Tooltip("Flight travel duration in seconds")]
    [SerializeField] private float duration = 0.4f;

    [Tooltip("Arc curve peak height")]
    [SerializeField] private float arcHeight = 90f;

    [Tooltip("Random rotation angle during flight")]
    [SerializeField] private float rotationWobble = 25f;

    [Tooltip("Peak scale during mid-air flight")]
    [SerializeField] private float peakScale = 1.25f;

    [Tooltip("Base visual size of the flying ingredient")]
    [SerializeField] private Vector2 itemSize = new Vector2(70f, 70f);

    public void PlayFlyAnimation()
    {
        if (cookingManager == null) return;
        if (cookingManager.currentIngredients.Count >= 3) return;

        int targetIndex = cookingManager.currentIngredients.Count;
        if (targetIndex >= cookingManager.boardSlots.Length) return;

        Image targetSlot = cookingManager.boardSlots[targetIndex];
        if (targetSlot == null) return;

        StartCoroutine(JuicyFlyRoutine(targetSlot.rectTransform));
    }

    private IEnumerator JuicyFlyRoutine(RectTransform targetRect)
    {
        Transform parent = canvasParent != null ? canvasParent : transform.root;

        // Instantiate temporary ghost item
        GameObject flyObj = new GameObject("FlyingGhost", typeof(RectTransform), typeof(Image));
        flyObj.transform.SetParent(parent, false);

        RectTransform flyRect = flyObj.GetComponent<RectTransform>();
        flyRect.sizeDelta = itemSize;
        flyRect.position = transform.position;

        Image flyImg = flyObj.GetComponent<Image>();
        flyImg.sprite = ingredientSprite;
        flyImg.raycastTarget = false;

        Vector2 startPos = flyRect.anchoredPosition;
        Vector2 endPos = GetAnchoredPos(targetRect, (RectTransform)parent);

        // Random rotation direction (-1 or 1)
        float randomDir = Random.value > 0.5f ? 1f : -1f;
        float targetRotation = rotationWobble * randomDir;

        float elapsed = 0f;

        // 1. Arc Flight with Smooth Easing, Scale & Rotation
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth ease in and out
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            // Position & Parabolic Arc
            Vector2 basePos = Vector2.Lerp(startPos, endPos, easeT);
            float arc = 4f * t * (1f - t) * arcHeight;
            flyRect.anchoredPosition = new Vector2(basePos.x, basePos.y + arc);

            // Scale arc (Pops up toward screen in mid-air, then shrinks back)
            float scaleMultiplier = Mathf.Lerp(1f, peakScale, Mathf.Sin(t * Mathf.PI));
            flyRect.localScale = Vector3.one * scaleMultiplier;

            // Rotation tilt during airtime
            float currentRotation = Mathf.Sin(t * Mathf.PI) * targetRotation;
            flyRect.localRotation = Quaternion.Euler(0f, 0f, currentRotation);

            yield return null;
        }

        // 2. Landing Squash & Bounce (เด้งดึ๋งตอนตกกระทบเขียง)
        float bounceDuration = 0.12f;
        float bounceElapsed = 0f;
        Vector3 squashScale = new Vector3(1.2f, 0.8f, 1f); // ยุบตัวลงตามแนวดิ่ง

        while (bounceElapsed < bounceDuration)
        {
            bounceElapsed += Time.deltaTime;
            float bt = Mathf.Clamp01(bounceElapsed / bounceDuration);
            flyRect.localScale = Vector3.Lerp(Vector3.one, squashScale, Mathf.Sin(bt * Mathf.PI));
            yield return null;
        }

        Destroy(flyObj);
    }

    private Vector2 GetAnchoredPos(RectTransform target, RectTransform parent)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out Vector2 localPoint);
        return localPoint;
    }
}