using UnityEngine;
using TMPro;
using System.Collections;
using System.Globalization;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

// =====================================================================
// UISummaryController — v5 (Localization + Cafe Rating)
//
// - ข้อความทุกบรรทัดในหน้าสรุป (Day / Served / Earnings / Rating / Detail)
//   ให้ Localize String Event เป็นคนใส่ แล้วสคริปต์ส่งแค่ "ตัวเลข" เข้าไปเป็นตัวแปร Smart String
// - ตัวแปรถูกตั้งค่าเริ่มต้นก่อน SetActive(true) ของ summaryCanvas เสมอ
//   (กัน FormattingException หา {day} ไม่เจอตอน component เปิด)
// - ถ้าไม่ได้ใส่ Localize ของ Rating ไว้ จะใช้ ratingText / ratingDetailText แบบเดิม (ภาษาอังกฤษ)
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

    [Header("Cafe Rating (ปล่อยว่างได้ถ้าไม่ต้องการแสดง)")]
    public TextMeshProUGUI ratingText;        // ใช้เฉพาะตอนไม่ได้ใส่ Rating Localize (แบบเดิม)
    public TextMeshProUGUI ratingDetailText;  // ใช้เฉพาะตอนไม่ได้ใส่ Rating Detail Localize (แบบเดิม)

    [Header("Cafe Rating — แบบรูปดาว (ไม่บังคับ)")]
    [Tooltip("ดาวของ Rating เดิม (ก่อนจบวัน) — ลาก Image ดาวเรียงซ้าย→ขวา")]
    public Image[] oldStarImages;

    [Tooltip("ดาวของ Rating ใหม่ (หลังจบวัน) — ลาก Image ดาวเรียงซ้าย→ขวา")]
    public Image[] newStarImages;

    // ── Localization ──────────────────────────────────────────────
    [Header("Localization")]
    [Tooltip("Localize String Event ของ DayText (key: day_result) ตัวแปร: {day}")]
    public LocalizeStringEvent dayLocalize;

    [Tooltip("Localize String Event ของ ServeText (key: ServeResult) ตัวแปร: {served}")]
    public LocalizeStringEvent servedLocalize;

    [Tooltip("Localize String Event ของ Earnings Text (key: EarningResult) ตัวแปร: {earnings}")]
    public LocalizeStringEvent earningsLocalize;

    [Tooltip("String วันสุดท้าย (key: day_final) ตัวแปร: {day}")]
    public LocalizedString dayFinalString;

    [Tooltip("Localize String Event ของ Rating Text (key: rating_result) ตัวแปร: {oldRating} {newRating} {arrow}")]
    public LocalizeStringEvent ratingLocalize;

    [Tooltip("Localize String Event ของ Rating Detail Text (key: rating_detail) ตัวแปร: {arrived} {served} {missed} {interact}")]
    public LocalizeStringEvent ratingDetailLocalize;

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

    // ── Hide UI ระหว่างหน้าสรุป ─────────────────────────────────────
    [Header("Hide During Summary")]
    [Tooltip("UI ที่จะซ่อนตอนขึ้นหน้าสรุป เช่น ช่องเก็บของ / hotbar")]
    public GameObject[] hideDuringSummary;

    // ── track current day ─────────────────────────────────────────
    private int _currentDay = 1;
    private bool _isLastDay = false;

    // เก็บ reference เดิมของ DayText ไว้ เผื่อสลับกลับจาก day_final
    private LocalizedString _originalDayString;

    void Start()
    {
        if (summaryCanvas != null) summaryCanvas.SetActive(false);
        if (blackOverlay != null) blackOverlay.gameObject.SetActive(false);

        if (dayLocalize != null)
            _originalDayString = dayLocalize.StringReference;
    }

    // ซ่อน/แสดง UI ตามรายการ
    private void SetHiddenUI(bool hidden)
    {
        if (hideDuringSummary == null) return;

        foreach (var go in hideDuringSummary)
        {
            if (go != null) go.SetActive(!hidden);
        }
    }

    // ── Localization helpers ──────────────────────────────────────
    private static void SetInt(LocalizedString str, string varName, int value)
    {
        if (str == null) return;

        if (str.TryGetValue(varName, out var existing) && existing is IntVariable iv)
            iv.Value = value;
        else
            str[varName] = new IntVariable { Value = value };
    }

    private static void SetString(LocalizedString str, string varName, string value)
    {
        if (str == null) return;

        if (str.TryGetValue(varName, out var existing) && existing is StringVariable sv)
            sv.Value = value;
        else
            str[varName] = new StringVariable { Value = value };
    }

    private static void RefreshIfActive(LocalizeStringEvent evt)
    {
        // ถ้ายังไม่ได้เปิด component จะแปลเองตอน OnEnable (และตัวแปรพร้อมแล้ว)
        if (evt != null && evt.isActiveAndEnabled)
            evt.RefreshString();
    }

    // ตั้งดาวตามค่า rating — ดวงที่ i เต็มตาม (rating - i) รองรับครึ่งดาว
    private static void ApplyStars(Image[] stars, float rating)
    {
        if (stars == null || stars.Length == 0) return;

        int max = CafeRatingManager.Instance != null
            ? Mathf.RoundToInt(CafeRatingManager.Instance.maxRating)
            : stars.Length;

        for (int i = 0; i < stars.Length; i++)
        {
            Image img = stars[i];
            if (img == null) continue;

            // บังคับเป็น Filled เสมอ (ถ้าเป็น Simple ดาวจะเต็มทุกดวง)
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;

            img.fillAmount = Mathf.Clamp01(rating - i);
            img.gameObject.SetActive(i < max);
        }
    }

    // ส่งข้อมูลทั้งหมดเข้า Localization
    // result = null → ใส่ค่า placeholder ให้ตัวแปรของ Rating ครบ (ใช้ก่อนเปิด Canvas)
    private void ApplyLocalizedData(int day, int served, int earnings, CafeRatingManager.DayResult result)
    {
        // Day — ตั้งตัวแปรบน string ปลายทางก่อน แล้วค่อยสลับ reference (กัน error หา {day} ไม่เจอ)
        if (dayLocalize != null)
        {
            LocalizedString target = (_isLastDay && !dayFinalString.IsEmpty)
                ? dayFinalString
                : _originalDayString;

            if (target != null)
            {
                SetInt(target, "day", day);
                if (dayLocalize.StringReference != target)
                    dayLocalize.StringReference = target;
            }
            RefreshIfActive(dayLocalize);
        }

        if (servedLocalize != null)
        {
            SetInt(servedLocalize.StringReference, "served", served);
            RefreshIfActive(servedLocalize);
        }

        if (earningsLocalize != null)
        {
            SetInt(earningsLocalize.StringReference, "earnings", earnings);
            RefreshIfActive(earningsLocalize);
        }

        // Rating
        string oldR = "0.0", newR = "0.0", arrow = "-";
        int arrived = 0, rServed = 0, rMissed = 0, rInteract = 0;

        if (result != null)
        {
            oldR = result.oldRating.ToString("0.0", CultureInfo.InvariantCulture);
            newR = result.newRating.ToString("0.0", CultureInfo.InvariantCulture);
            arrow = result.change > 0.001f ? "▲" : (result.change < -0.001f ? "▼" : "-");
            arrived = result.arrived;
            rServed = result.served;
            rMissed = result.missed;
            rInteract = result.interactSuccess;
        }

        // ⭐ ดาวของ Rating (ถ้าไม่ได้ผูก Image ไว้ จะข้ามไปเอง)
        ApplyStars(oldStarImages, result != null ? result.oldRating : 0f);
        ApplyStars(newStarImages, result != null ? result.newRating : 0f);

        if (ratingLocalize != null)
        {
            SetString(ratingLocalize.StringReference, "oldRating", oldR);
            SetString(ratingLocalize.StringReference, "newRating", newR);
            SetString(ratingLocalize.StringReference, "arrow", arrow);
            RefreshIfActive(ratingLocalize);
        }
        else if (ratingText != null && result != null)
        {
            // แบบเดิม (ไม่ได้ผูก Localize)
            ratingText.text = CafeRatingManager.BuildRatingLine(result);
        }

        if (ratingDetailLocalize != null)
        {
            SetInt(ratingDetailLocalize.StringReference, "arrived", arrived);
            SetInt(ratingDetailLocalize.StringReference, "served", rServed);
            SetInt(ratingDetailLocalize.StringReference, "missed", rMissed);
            SetInt(ratingDetailLocalize.StringReference, "interact", rInteract);
            RefreshIfActive(ratingDetailLocalize);
        }
        else if (ratingDetailText != null && result != null)
        {
            ratingDetailText.text = CafeRatingManager.BuildDetailLine(result);
        }
    }

    public void StartSummarySequence(int day, int served, int earnings)
    {
        _currentDay = day;
        _isLastDay = day >= maxDays;

        // ✅ ตั้งตัวแปรให้ครบก่อนเปิด Canvas (Rating จริงยังไม่รู้ → ใส่ placeholder ไปก่อน)
        ApplyLocalizedData(day, served, earnings, null);

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

        // ⭐ คิด Rating ของวันนี้ (หลังแมวออกหมดแล้ว เพื่อให้แมวที่เพิ่งจ่ายเงินตอนปิดร้านถูกนับครบ)
        CafeRatingManager.DayResult ratingResult = CafeRatingManager.Instance != null
            ? CafeRatingManager.Instance.EvaluateEndOfDay(day)
            : null;

        // ซ่อนช่องเก็บของ (และ UI อื่นๆ ในลิสต์) ก่อนเริ่ม fade มืด
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

        // 3. Set ข้อมูล (ผ่าน Localization)
        // ใช้ตัวเลขเสิร์ฟจากการคิด Rating (นับหลังแมวออกหมดแล้ว) เพื่อให้ตรงกับ Served x/y ด้านล่าง
        int servedForDisplay = ratingResult != null ? ratingResult.served : served;
        ApplyLocalizedData(day, servedForDisplay, earnings, ratingResult);

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

        // เปิดช่องเก็บของกลับมา
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