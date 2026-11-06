using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CatMenuChatBubble : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bubbleCanvas;
    public Image iconImage;

    [Header("Food Icons Pool")]
    [Tooltip("ลาก Sprites อาหารหรือไอคอนเมนูมาใส่ที่นี่")]
    public Sprite[] foodIcons;

    [Header("Settings")]
    public float iconSwapInterval = 0.5f;
    public bool artTailPointsRight = true;
    public bool mirrorPosition = true;
    public float popDuration = 0.15f;
    public float bobAmount = 0.03f;
    public float bobSpeed = 6f;

    private Coroutine routine;
    private Vector3 baseCanvasScale = Vector3.one;
    private Vector3 baseCanvasPos;
    private Vector3 baseIconScale = Vector3.one;
    private int lastIconIndex = -1;
    private bool cached = false;

    private void CacheBase()
    {
        if (cached || bubbleCanvas == null) return;
        cached = true;

        Transform ct = bubbleCanvas.transform;
        baseCanvasScale = ct.localScale;
        baseCanvasScale.x = Mathf.Abs(baseCanvasScale.x);
        baseCanvasPos = ct.localPosition;

        if (iconImage != null)
        {
            baseIconScale = iconImage.transform.localScale;
            baseIconScale.x = Mathf.Abs(baseIconScale.x);
        }
    }

    void Start()
    {
        CacheBase();
        if (bubbleCanvas != null) bubbleCanvas.SetActive(false);
    }

    /// <summary>
    /// สั่งให้แสดงก้อนเมฆรูปอาหารเฉพาะตอนอ่านเมนู
    /// </summary>
    public void ShowFoodMenuBubble(bool faceRight, float duration, List<RecipeSO> availableRecipes = null)
    {
        if (bubbleCanvas == null) return;
        CacheBase();

        // ถ้ามีรายชื่อ Recipes ส่งมา ให้ดึงรูปอาหารจาก RecipeSO โดยตรงอัตโนมัติได้
        if (availableRecipes != null && availableRecipes.Count > 0)
        {
            List<Sprite> extractedSprites = new List<Sprite>();
            foreach (var recipe in availableRecipes)
            {
                if (recipe != null && recipe.finalDishSprite != null)
                    extractedSprites.Add(recipe.finalDishSprite);
            }
            if (extractedSprites.Count > 0)
            {
                foodIcons = extractedSprites.ToArray();
            }
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FoodBubbleRoutine(faceRight, duration));
    }

    public void HideFoodMenuBubble()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (bubbleCanvas != null) bubbleCanvas.SetActive(false);
    }

    private IEnumerator FoodBubbleRoutine(bool faceRight, float duration)
    {
        Transform ct = bubbleCanvas.transform;
        float sign = (faceRight == artTailPointsRight) ? 1f : -1f;

        Vector3 basePos = baseCanvasPos;
        if (mirrorPosition) basePos.x = baseCanvasPos.x * sign;

        if (iconImage != null)
        {
            Vector3 s = baseIconScale;
            s.x *= sign;
            iconImage.transform.localScale = s;
        }

        PickRandomFoodIcon();
        ct.localPosition = basePos;
        ct.localScale = Vector3.zero;
        bubbleCanvas.SetActive(true);

        float t = 0f;
        float swapTimer = 0f;

        while (t < duration)
        {
            float dt = Time.deltaTime;
            t += dt;
            swapTimer += dt;

            float pop = popDuration > 0f ? Mathf.Clamp01(t / popDuration) : 1f;
            float k = pop < 1f ? pop * (1f + 0.15f * Mathf.Sin(pop * Mathf.PI)) : 1f;

            ct.localScale = new Vector3(baseCanvasScale.x * sign * k, baseCanvasScale.y * k, baseCanvasScale.z * k);
            ct.localPosition = basePos + Vector3.up * (Mathf.Sin(t * bobSpeed) * bobAmount);

            if (iconSwapInterval > 0f && swapTimer >= iconSwapInterval)
            {
                swapTimer = 0f;
                PickRandomFoodIcon();
            }
            yield return null;
        }

        bubbleCanvas.SetActive(false);
        routine = null;
    }

    private void PickRandomFoodIcon()
    {
        if (foodIcons == null || foodIcons.Length == 0 || iconImage == null) return;

        int index = Random.Range(0, foodIcons.Length);
        if (foodIcons.Length > 1)
        {
            int guard = 0;
            while (index == lastIconIndex && guard++ < 10)
                index = Random.Range(0, foodIcons.Length);
        }
        lastIconIndex = index;
        iconImage.sprite = foodIcons[index];
    }
}