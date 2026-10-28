using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MoneyFlyEffectManager : MonoBehaviour
{
    public static MoneyFlyEffectManager Instance;

    [Header("UI References")]
    public Canvas mainCanvas;               // ลาก Main Canvas มาใส่
    public GameObject flyingCoinPrefab;     // Prefab รูปไอคอนเงินที่เป็น UI Image
    public RectTransform targetUIIconRect;  // RectTransform ของไอคอนเงินหลักที่มุมจอ

    [Header("Animation Settings")]
    public float flightDuration = 0.6f;     // เวลาในการบินไปถึง (วินาที)

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    /// <summary>
    /// สั่งให้เงินบินจากตำแหน่ง World (ที่โต๊ะ) ไปหา UI หลัก
    /// </summary>
    public void PlayFlyToUIPool(Vector3 worldPos)
    {
        if (mainCanvas == null || flyingCoinPrefab == null || targetUIIconRect == null)
        {
            Debug.LogWarning("[MoneyFlyEffectManager] ยังไม่ได้ตั้งค่า References ใน Inspector ให้ครบ!");
            return;
        }

        StartCoroutine(FlyCoinRoutine(worldPos));
    }

    IEnumerator FlyCoinRoutine(Vector3 worldPos)
    {
        // 1. สร้าง GameObject รูปเงินขึ้นมาภายใต้ Canvas
        GameObject coinObj = Instantiate(flyingCoinPrefab, mainCanvas.transform);
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();

        if (coinRect == null) yield break;

        // 2. แปลงตำแหน่ง World Space ของโต๊ะอาหาร ให้เป็น Screen Position
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        // 3. แปลง Screen Position เป็น Local Position ของ Canvas เพื่อให้วาง UI ได้ถูกต้อง
        Vector2 startLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            screenPoint,
            mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera,
            out startLocalPos
        );

        coinRect.anchoredPosition = startLocalPos;

        // ดึงตำแหน่งเป้าหมาย (UI ไอคอนเงินหลักที่มุมจอ)
        Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(
            mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera,
            targetUIIconRect.position
        );

        Vector2 targetLocalPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            targetScreenPos,
            mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera,
            out targetLocalPos
        );

        // 4. สั่งอนิเมชันให้ไอคอนเงินค่อยๆ เคลื่อนที่ไปหาเป้าหมาย (Lerp)
        float elapsed = 0f;
        Vector3 startScale = coinRect.localScale;
        Vector3 targetScale = startScale * 0.5f; // ให้ตอนบินไปถึงค่อยๆ หดเล็กลงนิดหน่อยดูมีมิติ

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            // ใช้ Ease-In หรือ SmoothStep เพื่อความนุ่มนวล
            t = t * t * (3f - 2f * t);

            coinRect.anchoredPosition = Vector2.Lerp(startLocalPos, targetLocalPos, t);
            coinRect.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        // 5. เมื่อบินมาถึงเป้าหมาย ให้ทำลายไอคอนชั่วคราวนทิ้ง
        Destroy(coinObj);
    }
}