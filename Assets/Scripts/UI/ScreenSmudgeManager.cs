using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenSmudgeManager : MonoBehaviour
{
    public static ScreenSmudgeManager Instance;

    [Header("Settings")]
    [Range(0f, 100f)] public float triggerChance = 100f; // ตั้ง 100 ไว้เทส
    public int requiredPresses = 5;                     // จำนวนครั้งที่ต้องกด Space bar หรือคลิกเพื่อเคลียร์

    [Header("Cat Slide Animation Settings (ใช้เฉพาะตอนทำอาหาร)")]
    public RectTransform catRectTransform;               // ลาก CatPopUp มาใส่
    public Vector2 leftOffScreenPos = new Vector2(-1300f, 690f); // จุดเริ่มต้นนอกจอฝั่งซ้าย
    public Vector2 rightTargetPos = new Vector2(-830f, 150f);    // ตำแหน่งเป้าหมายด้านขวา
    public float slideInDuration = 0.8f;                         // เวลาตอนแมวสไลด์เข้ามา
    public float slideOutDuration = 0.8f;                        // เวลาตอนแมวสไลด์ออก

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

    [Header("Cat Normal Size Adjustment")]
    public Vector3 normalCatScale = new Vector3(5f, 5f, 5f); // 🟢 ขนาดปกติของแมว (ปรับเพิ่ม/ลดได้ตามต้องการ)

    private int currentPresses = 0;
    private bool isMinigameActive = false;
    private Coroutine activeSequenceCoroutine;
    private Coroutine punchScaleCoroutine;

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

        // รองรับทั้งกด Space bar หรือคลิกซ้ายเมาส์รัวๆ
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            currentPresses++;
            Debug.Log($"[ScreenSmudge] กด/คลิก แล้ว! ({currentPresses}/{requiredPresses})");

            if (sfxSource != null && cleanSound != null)
                sfxSource.PlayOneShot(cleanSound);

            // 1. ถ้าเป็นหน้าทำอาหาร (มีแมวอยู่) ให้แมวเด้งดั๊บ
            if (catPopupObject != null && catPopupObject.activeSelf && catRectTransform != null)
            {
                if (punchScaleCoroutine != null) StopCoroutine(punchScaleCoroutine);
                punchScaleCoroutine = StartCoroutine(PunchScaleRoutine());
            }

            // 2. ถ้าเป็นซอสเลอะ (มี smudgeRootObject เปิดอยู่) ให้ซอสค่อยๆ จางลงตามจำนวนครั้งที่กด
            if (smudgeRootObject != null && smudgeRootObject.activeSelf && smudgeCanvasGroup != null)
            {
                float alpha = 1f - ((float)currentPresses / requiredPresses);
                smudgeCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }

            // เมื่อกดครบจำนวนครั้งที่กำหนด
            if (currentPresses >= requiredPresses)
            {
                StartCoroutine(EndMinigameRoutine());
            }
        }
    }

    /// <summary>
    /// 🟢 เอฟเฟกต์ทำให้แมวเด้งขยายแล้วหดกลับขนาดเดิมพอดี
    /// </summary>
    IEnumerator PunchScaleRoutine()
    {
        Vector3 targetScale = normalCatScale * 1.15f; // ขยายขึ้น 15%

        float duration = 0.08f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            catRectTransform.localScale = Vector3.Lerp(normalCatScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            catRectTransform.localScale = Vector3.Lerp(targetScale, normalCatScale, t);
            yield return null;
        }

        catRectTransform.localScale = normalCatScale;
    }

    /// <summary>
    /// 🟢 1. เรียกใช้งานตอนเปิดหน้าทำอาหาร
    /// </summary>
    public void TryTriggerCookingSmudge()
    {
        if (smudgeScreenObject == null || catRectTransform == null || catPopupObject == null)
        {
            Debug.LogError("[ScreenSmudgeManager] ❌ กรุณาลาก UI References (Cat) ใส่ใน Inspector ให้ครบ!");
            return;
        }

        if (!IsDataCatSitting())
        {
            Debug.Log("[ScreenSmudge] ⏳ DATACAT ยังไม่ได้อยู่ในร้าน ข้ามการทำงานแมวกวนตอนทำอาหาร");
            return;
        }

        CancelActiveSmudge();

        float roll = Random.Range(0f, 100f);
        if (roll <= triggerChance)
        {
            activeSequenceCoroutine = StartCoroutine(CookingCatOnlySequenceRoutine());
        }
    }

    /// <summary>
    /// 🟢 2. เรียกใช้งานตอน Serve อาหารให้ลูกค้า (เมื่อความอดทน <= 50%)
    /// </summary>
    public void TryTriggerServeSmudge()
    {
        if (smudgeScreenObject == null || smudgeRootObject == null)
        {
            Debug.LogError("[ScreenSmudgeManager] ❌ กรุณาลาก UI References (Smudge) ใส่ใน Inspector ให้ครบ!");
            return;
        }

        if (!IsDataCatSitting())
        {
            Debug.Log("[ScreenSmudge] ⏳ DATACAT ยังไม่ได้อยู่ในร้าน ข้ามการสาดซอสตอนเสิร์ฟ");
            return;
        }

        CancelActiveSmudge();

        float roll = Random.Range(0f, 100f);
        if (roll <= triggerChance)
        {
            activeSequenceCoroutine = StartCoroutine(ServeSmudgeSequenceRoutine());
        }
    }

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

    IEnumerator CookingCatOnlySequenceRoutine()
    {
        isMinigameActive = true;
        currentPresses = 0;

        PlayerController2D.IsLocked = true;

        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(true);
        if (catPopupObject != null) catPopupObject.SetActive(true);
        if (smudgeRootObject != null) smudgeRootObject.SetActive(false); // ปิดซอส

        if (catRectTransform != null)
        {
            catRectTransform.localScale = normalCatScale;
            catRectTransform.anchoredPosition = leftOffScreenPos;
            Canvas.ForceUpdateCanvases();
        }

        if (catRectTransform != null)
        {
            if (sfxSource != null && catSlideSound != null)
                sfxSource.PlayOneShot(catSlideSound);

            yield return StartCoroutine(SlideToPosition(leftOffScreenPos, rightTargetPos, slideInDuration));
        }
    }

    IEnumerator ServeSmudgeSequenceRoutine()
    {
        isMinigameActive = true;
        currentPresses = 0;

        PlayerController2D.IsLocked = true;

        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(true);
        if (catPopupObject != null) catPopupObject.SetActive(false); // ปิดแมว

        if (smudgeRootObject != null) smudgeRootObject.SetActive(true);
        if (smudgeCanvasGroup != null) smudgeCanvasGroup.alpha = 1f;

        if (sfxSource != null && splashSound != null)
            sfxSource.PlayOneShot(splashSound);

        yield break;
    }

    /// <summary>
    /// 🟢 เมื่อกดครบ 5 ครั้ง: แยกการทำงานระหว่างแมวกับซอส
    /// </summary>
    IEnumerator EndMinigameRoutine()
    {
        isMinigameActive = false;

        if (sfxSource != null && cleanSound != null)
            sfxSource.PlayOneShot(cleanSound);

        // กรณีมีแมว (หน้าทำอาหาร): สไลด์แมวกลับออกไปทันที (ไม่มีการ Fade ตัวแมว)
        if (catPopupObject != null && catPopupObject.activeSelf && catRectTransform != null)
        {
            catRectTransform.localScale = normalCatScale;
            if (sfxSource != null && catSlideSound != null)
                sfxSource.PlayOneShot(catSlideSound);

            yield return StartCoroutine(SlideToPosition(rightTargetPos, leftOffScreenPos, slideOutDuration));
            catPopupObject.SetActive(false);
        }

        // กรณีมีซอส (หน้าเสิร์ฟอาหาร): ให้ซอสค่อยๆ Fade จางออก
        if (smudgeRootObject != null && smudgeRootObject.activeSelf)
        {
            float fadeTimer = 0.2f;
            float elapsed = 0f;
            float startAlpha = smudgeCanvasGroup != null ? smudgeCanvasGroup.alpha : 1f;

            while (elapsed < fadeTimer)
            {
                elapsed += Time.deltaTime;
                if (smudgeCanvasGroup != null)
                {
                    smudgeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeTimer);
                }
                yield return null;
            }
            smudgeRootObject.SetActive(false);
        }

        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(false);
        PlayerController2D.IsLocked = false;
    }

    IEnumerator SlideToPosition(Vector2 fromPos, Vector2 toPos, float duration)
    {
        if (catRectTransform == null) yield break;

        float elapsed = 0f;
        catRectTransform.anchoredPosition = fromPos;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            catRectTransform.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, t);
            yield return null;
        }
        catRectTransform.anchoredPosition = toPos;
    }

    public void CancelActiveSmudge()
    {
        if (activeSequenceCoroutine != null)
        {
            StopCoroutine(activeSequenceCoroutine);
            activeSequenceCoroutine = null;
        }

        if (punchScaleCoroutine != null)
        {
            StopCoroutine(punchScaleCoroutine);
            punchScaleCoroutine = null;
        }

        if (catRectTransform != null) catRectTransform.localScale = normalCatScale;

        if (smudgeRootObject != null) smudgeRootObject.SetActive(false);
        if (catPopupObject != null) catPopupObject.SetActive(false);
        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(false);
        isMinigameActive = false;

        PlayerController2D.IsLocked = false;
    }
}