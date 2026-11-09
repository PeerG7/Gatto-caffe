using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenSmudgeManager : MonoBehaviour
{
    public static ScreenSmudgeManager Instance;

    [Header("Cat Interrupt — แมวป่วนตอนทำอาหาร (เปิดหน้าทำอาหารแล้วสุ่ม)")]
    [Range(0f, 100f)] public float cookingCatTriggerChance = 100f; // ตั้ง 100 ไว้เทส
    public int cookingCatRequiredPresses = 5;                       // จำนวนครั้งที่ต้องกด Space/คลิกเพื่อไล่แมว

    [Header("Sauce Splash — ซอสสาดตอนเสิร์ฟ (แมวความอดทน ≤ 50%)")]
    [Range(0f, 100f)] public float serveSauceTriggerChance = 100f; // ตั้ง 100 ไว้เทส
    public int serveSauceRequiredPresses = 5;                       // จำนวนครั้งที่ต้องกด Space/คลิกเพื่อเช็ดซอส

    [Header("Cat Slide Animation Settings (ใช้เฉพาะตอนทำอาหาร)")]
    public RectTransform catRectTransform;               // ลาก CatPopUp มาใส่
    public Image catImage;                               // ลาก Image component บน CatPopUp มาใส่
    public Vector2 leftOffScreenPos = new Vector2(-1300f, 690f); // จุดเริ่มต้นนอกจอฝั่งซ้าย
    public Vector2 rightTargetPos = new Vector2(-830f, 150f);    // ตำแหน่งเป้าหมายด้านขวา
    public float slideInDuration = 0.8f;                         // เวลาตอนแมวสไลด์เข้ามา
    public float slideOutDuration = 0.8f;                        // เวลาตอนแมวสไลด์ออก

    [Header("Cat Manual Direction Controls")]
    [Tooltip("ติ๊กถูกถ้าอยากให้ตอนเดินเข้าจอหันหน้าไปทางขวา / เอาออกถ้าอยากให้หันซ้าย")]
    public bool faceRightWhenWalkingIn = true;            // 🟢 ควบคุมทิศทางตอนเดินเข้า

    [Header("Cat Animation Sprites")]
    [Tooltip("Frames played during sliding in/out (Walking animation)")]
    public Sprite[] catWalkSprites;                      // ใส่ Sprite Walking 2+ เฟรม
    [Tooltip("Frames played while waiting / talking (Idle animation)")]
    public Sprite[] catChattingSprites;                  // ใส่ Sprite Chatting 2+ เฟรม
    public float animationFrameRate = 0.15f;             // ความเร็วการเปลี่ยนเฟรม (วินาทีต่อเฟรม)

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

    [Header("Cleaning Sauce Sounds (เสียงตอนเช็ดซอส)")]
    [Tooltip("ใส่ cleaningsauce 1-2 สุ่มเล่นทุกครั้งที่กดเช็ดซอส (ถ้าว่างจะใช้ Clean Sound เดิม)")]
    public AudioClip[] cleaningSauceSounds;
    [Range(0f, 1f)] public float cleaningSauceVolume = 1f;

    [Header("Cat Voice (เสียงแมวตอนกด)")]
    [Tooltip("ใส่ catattention 1-3 เล่นแบบสุ่มทุกครั้งที่กดแมว (กดครั้งใหม่จะตัดเสียงเก่า)")]
    public AudioClip[] catAttentionSounds;
    [Tooltip("cat_happy เล่นตอนกดครบจำนวน")]
    public AudioClip catHappySound;
    [Range(0f, 1f)] public float catVoiceVolume = 1f;
    [Tooltip("ถ้าไม่ลากใส่ โค้ดจะสร้าง AudioSource ให้เองอัตโนมัติ")]
    public AudioSource catVoiceSource;

    [Header("Cat Normal Size Adjustment")]
    public Vector3 normalCatScale = new Vector3(5f, 5f, 5f); // ขนาดปกติของแมว

    private int currentPresses = 0;
    private int requiredNow = 5;              // จำนวนครั้งที่ต้องกดของรอบนี้ (แมวป่วน / ซอส ใช้ค่าคนละตัว)
    private bool isMinigameActive = false;

    // 🟢 true ตั้งแต่แมว/ซอสเริ่มโผล่ จนถึงหายไปหมดจริง (รวมช่วงแมวสไลด์ออกด้วย)
    // ใช้ให้ CookingManager / DeepFryStation รู้ว่าตอนนี้ห้ามกดอะไรในครัว
    private bool _isInterrupting = false;
    public static bool IsInterrupting => Instance != null && Instance._isInterrupting;
    private Coroutine activeSequenceCoroutine;
    private Coroutine punchScaleCoroutine;
    private Coroutine spriteAnimCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(false);
        if (smudgeRootObject != null) smudgeRootObject.SetActive(false);
        if (catPopupObject != null) catPopupObject.SetActive(false);

        if (catImage == null && catPopupObject != null)
        {
            catImage = catPopupObject.GetComponent<Image>();
        }

        // AudioSource แยกสำหรับเสียงแมว เพื่อให้สั่ง Stop เสียงเก่าได้
        if (catVoiceSource == null)
        {
            catVoiceSource = gameObject.AddComponent<AudioSource>();
            catVoiceSource.playOnAwake = false;
            catVoiceSource.loop = false;
            catVoiceSource.spatialBlend = 0f; // 2D sound
        }
    }

    void PlayRandomCleaningSauce()
    {
        if (sfxSource == null) return;
        AudioClip clip = cleaningSauceSounds[Random.Range(0, cleaningSauceSounds.Length)];
        if (clip != null) sfxSource.PlayOneShot(clip, cleaningSauceVolume);
    }

    // ---------------------------------------------------------------
    //  Cat Voice
    // ---------------------------------------------------------------
    void PlayCatVoice(AudioClip clip)
    {
        if (catVoiceSource == null || clip == null) return;

        catVoiceSource.Stop();          // ตัดเสียงของครั้งก่อน
        catVoiceSource.clip = clip;
        catVoiceSource.volume = catVoiceVolume;
        catVoiceSource.Play();
    }

    void PlayRandomCatAttention()
    {
        if (catAttentionSounds == null || catAttentionSounds.Length == 0) return;
        AudioClip clip = catAttentionSounds[Random.Range(0, catAttentionSounds.Length)];
        PlayCatVoice(clip);
    }

    void PlayCatHappy()
    {
        PlayCatVoice(catHappySound);
    }

    void StopCatVoice()
    {
        if (catVoiceSource != null && catVoiceSource.isPlaying) catVoiceSource.Stop();
    }

    void Update()
    {
        if (!isMinigameActive) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            currentPresses++;
            Debug.Log($"[ScreenSmudge] กด/คลิก แล้ว! ({currentPresses}/{requiredNow})");

            // ตอนเช็ดซอส: สุ่มเสียง cleaningsauce / ตอนแมว: ใช้ cleanSound เดิม
            bool isSauceMode = smudgeRootObject != null && smudgeRootObject.activeSelf;
            if (isSauceMode && cleaningSauceSounds != null && cleaningSauceSounds.Length > 0)
                PlayRandomCleaningSauce();
            else if (sfxSource != null && cleanSound != null)
                sfxSource.PlayOneShot(cleanSound);

            if (catPopupObject != null && catPopupObject.activeSelf && catRectTransform != null)
            {
                if (punchScaleCoroutine != null) StopCoroutine(punchScaleCoroutine);
                punchScaleCoroutine = StartCoroutine(PunchScaleRoutine());

                // เสียงแมว: กดครบแล้วเล่น happy, ยังไม่ครบสุ่ม attention (ตัดเสียงเก่าทุกครั้ง)
                if (currentPresses >= requiredNow) PlayCatHappy();
                else PlayRandomCatAttention();
            }

            if (smudgeRootObject != null && smudgeRootObject.activeSelf && smudgeCanvasGroup != null)
            {
                float alpha = 1f - ((float)currentPresses / requiredNow);
                smudgeCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }

            if (currentPresses >= requiredNow)
            {
                activeSequenceCoroutine = StartCoroutine(EndMinigameRoutine()); // เก็บไว้ให้ Cancel หยุดได้
            }
        }
    }

    IEnumerator PunchScaleRoutine()
    {
        float currentSign = Mathf.Sign(catRectTransform.localScale.x);
        Vector3 baseScale = new Vector3(Mathf.Abs(normalCatScale.x) * currentSign, normalCatScale.y, normalCatScale.z);
        Vector3 targetScale = new Vector3(Mathf.Abs(normalCatScale.x * 1.15f) * currentSign, normalCatScale.y * 1.15f, normalCatScale.z);

        float duration = 0.08f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            catRectTransform.localScale = Vector3.Lerp(baseScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            catRectTransform.localScale = Vector3.Lerp(targetScale, baseScale, t);
            yield return null;
        }

        catRectTransform.localScale = baseScale;
    }

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

        CancelActiveSmudge(false); // ไม่ปลดล็อก Player (หน้าทำอาหารล็อกไว้อยู่แล้ว)

        float roll = Random.Range(0f, 100f);
        if (roll <= cookingCatTriggerChance)
        {
            activeSequenceCoroutine = StartCoroutine(CookingCatOnlySequenceRoutine());
        }
    }

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

        CancelActiveSmudge(false); // ไม่ปลดล็อก Player ถ้าสุ่มไม่ติด (ให้คงสถานะล็อกเดิมไว้)

        float roll = Random.Range(0f, 100f);
        if (roll <= serveSauceTriggerChance)
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

    // 🟢 ตั้งค่าการหันหน้าแบบแมนนวลผ่าน Inspector
    private void ApplyCatFacing(bool faceRight)
    {
        if (catRectTransform == null) return;
        float scaleX = faceRight ? Mathf.Abs(normalCatScale.x) : -Mathf.Abs(normalCatScale.x);
        catRectTransform.localScale = new Vector3(scaleX, normalCatScale.y, normalCatScale.z);
    }

    IEnumerator CookingCatOnlySequenceRoutine()
    {
        isMinigameActive = true;
        _isInterrupting = true;
        requiredNow = cookingCatRequiredPresses;
        currentPresses = 0;

        PlayerController2D.IsLocked = true;

        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(true);
        if (catPopupObject != null) catPopupObject.SetActive(true);
        if (smudgeRootObject != null) smudgeRootObject.SetActive(false);

        if (catRectTransform != null)
        {
            catRectTransform.anchoredPosition = leftOffScreenPos;
            Canvas.ForceUpdateCanvases();
        }

        if (catRectTransform != null)
        {
            if (sfxSource != null && catSlideSound != null)
                sfxSource.PlayOneShot(catSlideSound);

            // 🟢 กำหนดทิศทางตอนเดินเข้า (ตั้งค่าได้จาก Inspector)
            ApplyCatFacing(faceRightWhenWalkingIn);
            StartSpriteAnimation(catWalkSprites);

            yield return StartCoroutine(SlideToPosition(leftOffScreenPos, rightTargetPos, slideInDuration));

            StopSpriteAnimation();
            // เมื่อถึงจุดหมาย ให้หันหน้ากลับมามองตรงปกติ (ถ้าต้องการให้หันขวาตอนคุย ให้แก้ตรงนี้ได้)
            ApplyCatFacing(faceRightWhenWalkingIn);
            StartSpriteAnimation(catChattingSprites);
        }
    }

    IEnumerator ServeSmudgeSequenceRoutine()
    {
        isMinigameActive = true;
        _isInterrupting = true;
        requiredNow = serveSauceRequiredPresses;
        currentPresses = 0;

        PlayerController2D.IsLocked = true;

        if (smudgeScreenObject != null) smudgeScreenObject.SetActive(true);
        if (catPopupObject != null) catPopupObject.SetActive(false);

        if (smudgeRootObject != null) smudgeRootObject.SetActive(true);
        if (smudgeCanvasGroup != null) smudgeCanvasGroup.alpha = 1f;

        if (sfxSource != null && splashSound != null)
            sfxSource.PlayOneShot(splashSound);

        yield break;
    }

    IEnumerator EndMinigameRoutine()
    {
        isMinigameActive = false;

        if (sfxSource != null && cleanSound != null)
            sfxSource.PlayOneShot(cleanSound);

        if (catPopupObject != null && catPopupObject.activeSelf && catRectTransform != null)
        {
            if (sfxSource != null && catSlideSound != null)
                sfxSource.PlayOneShot(catSlideSound);

            StopSpriteAnimation();

            // 🟢 ตอนเดินกลับออกจอ ให้กลับด้านจากขามาโดยอัตโนมัติ
            ApplyCatFacing(!faceRightWhenWalkingIn);
            StartSpriteAnimation(catWalkSprites);

            yield return StartCoroutine(SlideToPosition(rightTargetPos, leftOffScreenPos, slideOutDuration));

            StopSpriteAnimation();
            catPopupObject.SetActive(false);
        }

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
        _isInterrupting = false;
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

    private void StartSpriteAnimation(Sprite[] spriteArray)
    {
        StopSpriteAnimation();

        if (spriteArray != null && spriteArray.Length > 0 && catImage != null)
        {
            spriteAnimCoroutine = StartCoroutine(PlaySpriteAnimationRoutine(spriteArray));
        }
    }

    private void StopSpriteAnimation()
    {
        if (spriteAnimCoroutine != null)
        {
            StopCoroutine(spriteAnimCoroutine);
            spriteAnimCoroutine = null;
        }
    }

    IEnumerator PlaySpriteAnimationRoutine(Sprite[] spriteArray)
    {
        int index = 0;
        while (true)
        {
            if (catImage != null && spriteArray.Length > 0)
            {
                catImage.sprite = spriteArray[index];
                index = (index + 1) % spriteArray.Length;
            }
            yield return new WaitForSeconds(animationFrameRate);
        }
    }

    /// <param name="unlockPlayer">false = ไม่แตะสถานะล็อก Player (ใช้ตอนเรียกจาก TryTrigger* เพื่อเคลียร์ของเก่าก่อนสุ่ม)</param>
    public void CancelActiveSmudge(bool unlockPlayer = true)
    {
        StopSpriteAnimation();
        StopCatVoice();

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
        _isInterrupting = false;

        if (unlockPlayer) PlayerController2D.IsLocked = false;
    }
}