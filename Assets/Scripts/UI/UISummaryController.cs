using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UISummaryController : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject summaryCanvas;
    public CanvasGroup canvasGroup;
    public Image blackOverlay;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI servedText;
    public TextMeshProUGUI earningsText;

    [Header("Settings")]
    public float fadeDuration = 1.5f;

    void Start()
    {
        // ✅ ปิด SummaryCanvas ตั้งแต่ต้น
        if (summaryCanvas != null) summaryCanvas.SetActive(false);

        // ✅ ปิด BlackOverlay ตั้งแต่ต้น
        if (blackOverlay != null) blackOverlay.gameObject.SetActive(false);
    }

    public void StartSummarySequence(int day, int served, int earnings)
    {
        summaryCanvas.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        StartCoroutine(WaitThenStart(day, served, earnings));
    }

    private IEnumerator WaitThenStart(int day, int served, int earnings)
    {
        yield return null;
        StartCoroutine(EndOfDayRoutine(day, served, earnings));
    }

    private IEnumerator EndOfDayRoutine(int day, int served, int earnings)
    {
        // 1. รอ NPC ออกหมดก่อน
        float timeout = 30f;
        float elapsed = 0f;
        while (GameObject.FindObjectsOfType<NPCController>().Length > 0)
        {
            elapsed += 0.5f;
            if (elapsed >= timeout) break;
            yield return new WaitForSeconds(0.5f);
        }

        // 2. เปิด BlackOverlay แล้ว Fade มืด
        if (blackOverlay != null)
        {
            blackOverlay.gameObject.SetActive(true);
            Color c = blackOverlay.color;
            c.a = 0;
            blackOverlay.color = c;
        }

        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            if (blackOverlay != null)
            {
                Color c = blackOverlay.color;
                c.a = alpha;
                blackOverlay.color = c;
            }
            yield return null;
        }

        // 3. Set ข้อมูลตอนหน้าจอมืดสนิท
        if (dayText != null) dayText.text = "Day " + day + " Finished!";
        if (servedText != null) servedText.text = "Cats Served: " + served;
        if (earningsText != null) earningsText.text = "Total Earnings: " + earnings + " $";

        // 4. Fade ตัวหนังสือขึ้นมา
        timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            if (canvasGroup != null) canvasGroup.alpha = alpha;
            yield return null;
        }

        // 5. เปิดให้กดปุ่มได้
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void OnNextDayButton()
    {
        StartCoroutine(NextDaySequence());
    }

    private IEnumerator NextDaySequence()
    {
        // 1. Fade ทุกอย่างออก
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, timer / fadeDuration);
            if (canvasGroup != null) canvasGroup.alpha = alpha;
            if (blackOverlay != null)
            {
                Color c = blackOverlay.color;
                c.a = alpha;
                blackOverlay.color = c;
            }
            yield return null;
        }

        // 2. ปิด BlackOverlay และ SummaryCanvas
        if (blackOverlay != null) blackOverlay.gameObject.SetActive(false);
        if (summaryCanvas != null) summaryCanvas.SetActive(false);

        // 3. Reset CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 4. ✅ ปลดล็อก Player
        PlayerController2D.IsLocked = false;

        // 5. ✅ Reset โต๊ะทุกตัว
        CustomerTable[] tables = FindObjectsOfType<CustomerTable>();
        foreach (var table in tables)
        {
            table.ResetTable();
        }

        // 6. ✅ Clear Queue
        if (QueueManager.Instance != null) QueueManager.Instance.ResetQueue();

        // 7. ✅ Reset วันใหม่
        if (DayNightManager.Instance != null) DayNightManager.Instance.ResetNewDay();

        DamageableObject[] damages = FindObjectsOfType<DamageableObject>();
        foreach (var dmg in damages)
        {
            dmg.ResetToNormal();
        }
    }
}