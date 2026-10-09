using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PuffySmokeFX : MonoBehaviour
{
    [Header("Smoke Visual")]
    [Tooltip("ใส่รูป Sprite ก้อนเมฆ หรือวงกลมสีขาว")]
    [SerializeField] private Sprite smokeSprite;
    [SerializeField] private Color smokeColor = new Color(1f, 1f, 1f, 0.85f);

    [Header("Smoke Behavior")]
    [SerializeField] private float spawnInterval = 0.18f;
    [SerializeField] private float floatDuration = 0.8f;
    [SerializeField] private float floatDistance = 90f;
    [SerializeField] private Vector2 startSize = new Vector2(35f, 35f);
    [SerializeField] private float endScaleMultiplier = 1.8f;

    private Coroutine smokeLoopRoutine;

    void OnEnable()
    {
        StartSmoke();
    }

    void OnDisable()
    {
        StopSmoke();
    }

    public void StartSmoke()
    {
        if (smokeLoopRoutine != null) StopCoroutine(smokeLoopRoutine);
        smokeLoopRoutine = StartCoroutine(SmokeLoop());
    }

    public void StopSmoke()
    {
        if (smokeLoopRoutine != null)
        {
            StopCoroutine(smokeLoopRoutine);
            smokeLoopRoutine = null;
        }
    }

    private IEnumerator SmokeLoop()
    {
        while (true)
        {
            SpawnSinglePuff();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnSinglePuff()
    {
        GameObject puff = new GameObject("SmokePuff", typeof(RectTransform), typeof(Image));
        puff.transform.SetParent(transform, false);

        RectTransform rect = puff.GetComponent<RectTransform>();
        rect.sizeDelta = startSize;

        // สุ่มขยับตำแหน่งซ้าย-ขวาเล็กน้อยตรงบริเวณเขียง
        float randomX = Random.Range(-25f, 25f);
        rect.anchoredPosition = new Vector2(randomX, 0f);

        Image img = puff.GetComponent<Image>();
        if (smokeSprite != null) img.sprite = smokeSprite;
        img.color = smokeColor;
        img.raycastTarget = false;

        StartCoroutine(AnimatePuff(rect, img));
    }

    private IEnumerator AnimatePuff(RectTransform rect, Image img)
    {
        float elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(Random.Range(-15f, 15f), floatDistance);

        Color initialCol = img.color;

        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / floatDuration);

            // 1. ลอยขึ้นด้านบน
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));

            // 2. ขยายขนาดใหญ่ขึ้น
            float scale = Mathf.Lerp(1f, endScaleMultiplier, t);
            rect.localScale = new Vector3(scale, scale, 1f);

            // 3. ค่อยๆ โปร่งแสงและจางหายไป
            float alpha = Mathf.Lerp(initialCol.a, 0f, t);
            img.color = new Color(initialCol.r, initialCol.g, initialCol.b, alpha);

            yield return null;
        }

        Destroy(rect.gameObject);
    }
}