using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationAnimationHelper : MonoBehaviour
{
    private TextMeshProUGUI notiText;
    private Coroutine animRoutine;
    private Coroutine typeRoutine;
    private Vector3 originalScale;

    void Awake()
    {
        notiText = GetComponent<TextMeshProUGUI>();
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        StartTypewriterAndPop();
    }

    public void StartTypewriterAndPop()
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        if (typeRoutine != null) StopCoroutine(typeRoutine);

        animRoutine = StartCoroutine(PopRoutine());
        typeRoutine = StartCoroutine(TypewriterRoutine());
    }

    IEnumerator PopRoutine()
    {
        float duration = 0.15f;
        float elapsed = 0f;

        Vector3 startScale = originalScale * 0.5f;
        Vector3 peakScale = originalScale * 1.25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(peakScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        animRoutine = null;
    }

    IEnumerator TypewriterRoutine()
    {
        if (notiText == null) yield break;

        // ซ่อนตัวอักษรทั้งหมดก่อนเริ่มพิมพ์
        notiText.maxVisibleCharacters = 0;
        yield return null; // รอ 1 เฟรมให้ข้อความใหม่จากสคริปต์หลักอัปเดตลง TextMeshPro

        int totalVisibleCharacters = notiText.textInfo.characterCount;
        float typingSpeed = 0.03f; // ความเร็วพิมพ์ต่อตัวอักษร (วินาที)

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            notiText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        // คืนค่าให้แสดงผลได้ตามปกติ
        notiText.maxVisibleCharacters = 99999;
        typeRoutine = null;
    }
}