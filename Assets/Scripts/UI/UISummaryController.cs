using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// =====================================================================
// UISummaryController — v3 (3-Day Session Limit + Hide UI During Summary)
//
// ของเดิม: fade sequence, Next Day, Main Menu, 3-Day Session ทำงานปกติ
// เพิ่มใหม่:
//   - hideDuringSummary: UI ที่จะซ่อนตอนขึ้นหน้าสรุป (เช่น ช่องเก็บของ)
//     ซ่อนตอนเริ่ม fade มืด และเปิดกลับตอนกด Next Day
// =====================================================================
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

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    // ── 3-Day Session ─────────────────────────────────────────────
    [Header("Day Session Settings")]
    [Tooltip("จำนวนวันสูงสุดก่อนจบเกม — default 3")]
    public int maxDays = 3;

    [Tooltip("ชื่อ scene ที่จะโหลดเมื่อครบ maxDays")]
    public string endingSceneName = "Ending";

    [Tooltip("ปุ่ม Next Day — script จะเปลี่ยน label ให้อัตโนมัติ")]
    public Button nextDayButton;

    [Tooltip("Text บน nextDayButton — ลาก TMP_Text ของปุ่มมาใส่")]
    public TextMeshProUGUI nextDayButtonText;

    public string labelNextDay = "Next Day";
    public string labelEndGame = "See Ending";

    // ✅ เพิ่มใหม่: UI ที่ต้องการซ่อนตอนขึ้นหน้าสรุป ──────────────
    [Header("Hide During Summary (เพิ่มใหม่)")]
    [Tooltip("UI ที่จะซ่อนตอนขึ้นหน้าสรุป เช่น ช่องเก็บของ / hotbar")]
    public GameObject[] hideDuringSummary;

    // ── track current day ─────────────────────────────────────────
    private int _currentDay = 1;
    private bool _isLastDay = false;

    void Start()
    {
        if (summaryCanvas != null) summaryCanvas.SetActive(false);
        if (blackOverlay != null) blackOverlay.gameObject.SetActive(false);
    }

    // ✅ เพิ่มใหม่: ซ่อน/แสดง UI ตามรายการ
    private void SetHiddenUI(bool hidden)
    {
        if (hideDuringSummary == null) return;

        foreach (var go in hideDuringSummary)
        {
            if (go != null) go.SetActive(!hidden);
        }
    }

    public void StartSummarySequence(int day, int served, int earnings)
    {
        _currentDay = day;
        _isLastDay = day >= maxDays;

        summaryCanvas.SetActive(true);

        // ปรับ label ปุ่ม Next Day ตามวัน
        if (nextDayButtonText != null)
            nextDayButtonText.text = _isLastDay ? labelEndGame : labelNextDay;

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

        // ✅ เพิ่มใหม่: ซ่อนช่องเก็บของ (และ UI อื่นๆ ในลิสต์) ก่อนเริ่ม fade มืด
        SetHiddenUI(true);

        // 2. Fade มืด
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

        // 3. Set ข้อมูล
        if (dayText != null) dayText.text = "Day " + day + " Finished!";
        if (servedText != null) servedText.text = "Cats Served: " + served;
        if (earningsText != null) earningsText.text = "Total Earnings: " + earnings + " $";

        // แสดง "Final Day!" ถ้าเป็นวันสุดท้าย
        if (_isLastDay && dayText != null)
            dayText.text = "Day " + day + " — Final Day!";

        // 4. Fade ตัวหนังสือขึ้น
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

    // ── ปุ่ม Next Day / End Game ───────────────────────────────────
    public void OnNextDayButton()
    {
        if (_isLastDay)
            StartCoroutine(EndGameSequence());
        else
            StartCoroutine(NextDaySequence());
    }

    // ── End Game sequence ──────────────────────────────────────────
    private IEnumerator EndGameSequence()
    {
        // lock ปุ่ม
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Fade ออก
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

        // Crossfade เพลงแล้วโหลด ending scene
        if (AudioManager.instance != null && AudioManager.instance.menuMusic != null)
        {
            AudioManager.instance.CrossfadeTo(
                AudioManager.instance.menuMusic,
                onComplete: () => SceneManager.LoadScene(endingSceneName)
            );
        }
        else
        {
            SceneManager.LoadScene(endingSceneName);
        }
    }

    // ── Next Day sequence ──────────────────────────────────────────
    private IEnumerator NextDaySequence()
    {
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

        if (blackOverlay != null) blackOverlay.gameObject.SetActive(false);
        if (summaryCanvas != null) summaryCanvas.SetActive(false);

        // ✅ เพิ่มใหม่: เปิดช่องเก็บของกลับมา
        SetHiddenUI(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        PlayerController2D.IsLocked = false;

        CustomerTable[] tables = FindObjectsOfType<CustomerTable>();
        foreach (var table in tables) table.ResetTable();

        if (QueueManager.Instance != null) QueueManager.Instance.ResetQueue();
        if (DayNightManager.Instance != null) DayNightManager.Instance.ResetNewDay();

        DamageableObject[] damages = FindObjectsOfType<DamageableObject>();
        foreach (var dmg in damages) dmg.ResetToNormal();
    }

    // ── Main Menu ──────────────────────────────────────────────────
    public void OnMainMenuButton()
    {
        // จบวันแล้ว (เห็นหน้าสรุป) แต่ไม่ได้กด Next Day → เซฟเป็นจุดเริ่มต้นของวันถัดไป จะได้ไม่ต้องเล่นวันนี้ซ้ำ
        // (วันสุดท้ายไม่เซฟ เพราะจะไปหน้า Ending)
        if (!_isLastDay && DayNightManager.Instance != null)
            DayNightManager.Instance.SaveNextDayCheckpoint();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (AudioManager.instance != null && AudioManager.instance.menuMusic != null)
        {
            AudioManager.instance.CrossfadeTo(
                AudioManager.instance.menuMusic,
                onComplete: () => SceneManager.LoadScene(mainMenuSceneName)
            );
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}