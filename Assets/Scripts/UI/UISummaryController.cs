using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// =====================================================================
// UISummaryController — v2 (3-Day Session Limit Added)
//
// ของเดิม: fade sequence, Next Day, Main Menu ทำงานปกติ
// เพิ่มใหม่:
//   - maxDays: จำนวนวันสูงสุด (default 3)
//   - ถ้า day >= maxDays → ปุ่ม Next Day เปลี่ยนเป็น "Game End"
//     แล้ว route ไป ending scene แทน ResetNewDay()
//   - endingSceneName: ชื่อ scene สำหรับ ending
//   - nextDayButtonText / endGameButtonText: เปลี่ยน label ปุ่มอัตโนมัติ
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

    // ── ใหม่: 3-Day Session ───────────────────────────────────────
    [Header("Day Session Settings (ใหม่)")]
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

    // ── ใหม่: track current day ───────────────────────────────────
    private int _currentDay = 1;
    private bool _isLastDay = false;

    void Start()
    {
        if (summaryCanvas != null) summaryCanvas.SetActive(false);
        if (blackOverlay != null) blackOverlay.gameObject.SetActive(false);
    }

    public void StartSummarySequence(int day, int served, int earnings)
    {
        _currentDay = day;
        _isLastDay = day >= maxDays;

        summaryCanvas.SetActive(true);

        // ✅ ใหม่: ปรับ label ปุ่ม Next Day ตามวัน
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

        // ✅ ใหม่: แสดง "Final Day!" ถ้าเป็นวันสุดท้าย
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

    // ── ใหม่: End Game sequence ────────────────────────────────────
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

        // ✅ Crossfade เพลงแล้วโหลด ending scene
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

    // ── ของเดิม: Next Day sequence ────────────────────────────────
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

    // ── ของเดิม: Main Menu ────────────────────────────────────────
    public void OnMainMenuButton()
    {
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