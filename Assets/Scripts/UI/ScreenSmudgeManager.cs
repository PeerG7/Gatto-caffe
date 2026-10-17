using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenSmudgeManager : MonoBehaviour
{
    public static ScreenSmudgeManager Instance;

    [Header("Settings")]
    [Range(0f, 100f)] public float triggerChance = 100f; // ตั้ง 100 ไว้เทส
    public int requiredPresses = 5;                     // จำนวนครั้งที่ต้องกด Space bar เพื่อลบซอส

    [Header("Cat Slide Animation Settings")]
    public RectTransform catRectTransform;               // ลาก CatPopUp มาใส่
    public Vector2 leftOffScreenPos = new Vector2(-1300f, 690f); // จุดเริ่มต้นนอกจอฝั่งซ้าย
    public Vector2 rightTargetPos = new Vector2(-830f, 150f);    // ตำแหน่งเป้าหมายด้านขวา
    public float slideInDuration = 0.8f;                         // เวลาตอนแมวสไลด์เข้ามา
    public float rightStayDuration = 0.5f;                       // เวลายืนนิ่งรอก่อนซอสสาด
    public float slideOutDuration = 0.8f;

    [Header("UI References (ลากใส่ให้ครบ)")]
    public GameObject smudgeScreenObject;  // ลาก SmudgeScreen (ตัวแม่) มาใส่
    public GameObject smudgeRootObject;    // ลาก Panel (รูปซอสเลอะ) มาใส่
    public CanvasGroup smudgeCanvasGroup;  // ลาก CanvasGroup ของ Panel มาใส่
    public GameObject catPopupObject;      // ลาก CatPopUp มาใส่

    [Header("Audio (Optional)")]
    public AudioSource sfxSource;
    public AudioClip catSlideSound;
    public AudioClip splashSound;
    public AudioClip cleanSound;

    private int currentPresses = 0;
    private bool isMinigameActive = false;
    private Coroutine activeSequenceCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // ปิดทั้งหมดตอนเริ่มเกม
        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(false);
        if (smudgeRootObject != null) smudgeRootObject.SetActive(false);
        if (catPopupObject != null) catPopupObject.SetActive(false);
    }

    void Update()
    {
        if (!isMinigameActive) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentPresses++;
            Debug.Log($"[ScreenSmudge] กด Space bar แล้ว! ({currentPresses}/{requiredPresses})");

            if (sfxSource != null && cleanSound != null)
                sfxSource.PlayOneShot(cleanSound);

            float alpha = 1f - ((float)currentPresses / requiredPresses);
            if (smudgeCanvasGroup != null)
            {
                smudgeCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }

            if (currentPresses >= requiredPresses)
            {
                StartCoroutine(ClearSmudgeRoutine());
            }
        }
    }

    public void TryTriggerDelayedSmudge()
    {
        if (smudgeScreenObject == null || catRectTransform == null || smudgeRootObject == null || catPopupObject == null)
        {
            Debug.LogError("[ScreenSmudgeManager] ❌ กรุณาลาก UI References ใส่ใน Inspector ให้ครบทุกช่อง!");
            return;
        }

        // 🟢 ตรวจสอบว่า DATACAT นั่งอยู่ที่โต๊ะแล้วจริงๆ หรือไม่
        if (!IsDataCatSitting())
        {
            Debug.Log("[ScreenSmudge] ⏳ DATACAT ยังไม่ได้นั่งที่โต๊ะ ข้ามการทำงานซอสป่วน");
            return;
        }

        CancelActiveSmudge();

        float roll = Random.Range(0f, 100f);
        if (roll <= triggerChance)
        {
            activeSequenceCoroutine = StartCoroutine(SmudgeSequenceRoutine());
        }
    }

    /// <summary>
    /// ฟังก์ชันเช็กว่ามี DATACAT นั่งอยู่ที่โต๊ะใดโต๊ะหนึ่งในร้านแล้วหรือยัง
    /// </summary>
    bool IsDataCatSitting()
    {
        CustomerTable[] tables = Object.FindObjectsOfType<CustomerTable>();
        foreach (var table in tables)
        {
            if (table.sittingNPCs != null)
            {
                foreach (var npc in table.sittingNPCs)
                {
                    if (npc != null && npc.currentState == NPCController.NPCState.Sitting)
                    {
                        CatIdentity identity = npc.GetComponent<CatIdentity>();
                        if (identity != null && identity.catData != null)
                        {
                            string id = identity.catData.catID;
                            string name = identity.catData.catName;

                            // ตรวจสอบเงื่อนไข DATACAT (รองรับทั้ง AIDum, AIKow หรือชื่อที่มีคำว่า DATACAT)
                            if (id == "AIDum" || id == "AIKow" || (name != null && name.Contains("DATACAT")))
                            {
                                Debug.Log($"[ScreenSmudge] ✅ พบ DATACAT นั่งอยู่ที่โต๊ะแล้ว! (ID: {id}, Name: {name})");
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    IEnumerator SmudgeSequenceRoutine()
    {
        isMinigameActive = true;
        currentPresses = 0;

        // 1. เปิด Canvas แม่ (SmudgeScreen) และเปิดตัวแมว แต่ยังปิดซอส (Panel) ไว้ก่อน
        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(true);
        if (catPopupObject != null) catPopupObject.SetActive(true);
        if (smudgeRootObject != null) smudgeRootObject.SetActive(false);

        // 2. เซ็ตตำแหน่งแมวไปรอนอกจอด้านซ้ายทันที และอัปเดต Canvas
        if (catRectTransform != null)
        {
            catRectTransform.anchoredPosition = leftOffScreenPos;
            Canvas.ForceUpdateCanvases();
        }

        // 3. แมวสไลด์จากซ้าย -> ขวา
        if (catRectTransform != null)
        {
            if (sfxSource != null && catSlideSound != null)
                sfxSource.PlayOneShot(catSlideSound);

            Debug.Log("[ScreenSmudge] 🐱 แมวกำลังสไลด์เข้ามา...");
            yield return StartCoroutine(SlideToPosition(leftOffScreenPos, rightTargetPos, slideInDuration));

            // 4. ยืนนิ่งรอ
            Debug.Log("[ScreenSmudge] 🛑 แมวยืนนิ่งรอ...");
            yield return new WaitForSecondsRealtime(rightStayDuration);

            // 5. 💦 ซอส (Panel) สาดเลอะหน้าจอตามที่ต้องการ
            Debug.Log("[ScreenSmudge] 💦 ซอสสาดเลอะหน้าจอแล้ว!");
            if (smudgeRootObject != null) smudgeRootObject.SetActive(true);
            if (smudgeCanvasGroup != null) smudgeCanvasGroup.alpha = 1f;

            if (sfxSource != null && splashSound != null)
                sfxSource.PlayOneShot(splashSound);

            // 6. แมวสไลด์ถอยออกไปทางซ้าย
            Debug.Log("[ScreenSmudge] 🐱 แมวสไลด์ออก...");
            yield return StartCoroutine(SlideToPosition(rightTargetPos, leftOffScreenPos, slideOutDuration));

            if (catPopupObject != null) catPopupObject.SetActive(false);
        }
    }

    IEnumerator SlideToPosition(Vector2 fromPos, Vector2 toPos, float duration)
    {
        if (catRectTransform == null) yield break;

        float elapsed = 0f;
        catRectTransform.anchoredPosition = fromPos;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f); // Ease Out สมูทๆ
            catRectTransform.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, t);
            yield return null;
        }
        catRectTransform.anchoredPosition = toPos;
    }

    IEnumerator ClearSmudgeRoutine()
    {
        Debug.Log("[ScreenSmudge] ✅ เคลียร์คราบซอสสำเร็จ!");
        isMinigameActive = false;

        float fadeTimer = 0.2f;
        float elapsed = 0f;
        while (elapsed < fadeTimer)
        {
            elapsed += Time.unscaledDeltaTime;
            if (smudgeCanvasGroup != null)
                smudgeCanvasGroup.alpha = Mathf.Lerp(smudgeCanvasGroup.alpha, 0f, elapsed / fadeTimer);
            yield return null;
        }

        if (smudgeRootObject != null) smudgeRootObject.SetActive(false);
        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(false);
    }

    public void CancelActiveSmudge()
    {
        if (activeSequenceCoroutine != null)
        {
            StopCoroutine(activeSequenceCoroutine);
            activeSequenceCoroutine = null;
        }

        if (smudgeRootObject != null) smudgeRootObject.SetActive(false);
        if (catPopupObject != null) smudgeRootObject.SetActive(false);
        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(false);
        isMinigameActive = false;
    }
}