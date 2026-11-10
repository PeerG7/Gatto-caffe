using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// =====================================================================
// PlayerGhostFade — Player Fade หายไปตอนเริ่ม Interaction กับแมวที่ InteractionZone
// แล้วกลับมาเป็นร่างลางๆ (Ghost) หลัง Interaction จบ
//
// Setup:
//   1. แปะ component นี้บน GameObject ของ Player (ตัวเดียวกับ PlayerController2D)
//   2. ปรับค่าใน Inspector ได้ตามต้องการ
//   3. ไม่ต้องแก้ PlayerController2D / PlayerInteract2D / NPCController
//      (NPCInteract.RequestZoneInteraction() เรียก BeginInteraction ให้แล้ว)
//
// การจบ Interaction ตรวจจับอัตโนมัติจาก:
//   - NPC ออกจากสถานะ AtInteractionZone / Performing (เช่น GoExit, ถูกลบ)
//   - หรือผู้เล่นถูกปลดล็อก (PlayerController2D.IsLocked = false) ต่อเนื่องเกิน unlockDebounce
//   ถ้าอยากควบคุมเอง เรียก PlayerGhostFade.EndInteraction() จากที่อื่นได้เลย
// =====================================================================
public class PlayerGhostFade : MonoBehaviour
{
    public static PlayerGhostFade Instance { get; private set; }

    [Header("Fade Out (ตอนเริ่ม Interaction)")]
    [Tooltip("เวลาที่ใช้ Fade หายไป (วินาที)")]
    public float fadeOutDuration = 0.35f;

    [Header("Ghost (ร่างลางๆ หลังจบ Interaction)")]
    [Range(0f, 1f)] public float ghostAlpha = 0.35f;
    [Tooltip("เวลาที่ใช้ Fade กลับมาเป็นร่างลางๆ")]
    public float ghostFadeInDuration = 0.5f;
    [Tooltip("เวลาที่คงร่างลางๆ ไว้ก่อนกลับมาทึบปกติ (ใช้เมื่อเปิด restoreToFull)")]
    public float ghostHoldSeconds = 1.5f;

    [Header("Restore")]
    [Tooltip("เปิด = หลังคงร่างลางๆ ครบเวลา จะค่อยๆ กลับมาทึบ 100% / ปิด = ค้างเป็นร่างลางๆ จนกว่าจะมี Interaction ครั้งถัดไป")]
    public bool restoreToFull = true;
    public float restoreDuration = 1.0f;

    [Header("End Detection")]
    [Tooltip("รอก่อนเริ่มเช็คว่า Interaction จบ (ให้ CatSystemManager มีเวลาล็อกผู้เล่นก่อน)")]
    public float startGrace = 0.6f;
    [Tooltip("ต้องปลดล็อกต่อเนื่องนานเท่านี้ถึงจะนับว่า Interaction จบ (กันล็อก/ปลดล็อกกระพริบระหว่าง UI ต่างๆ)")]
    public float unlockDebounce = 0.3f;

    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    private readonly Dictionary<SpriteRenderer, float> baseAlpha = new Dictionary<SpriteRenderer, float>();

    private float currentFactor = 1f;   // 1 = ทึบปกติ, 0 = หายสนิท
    private Coroutine routine;
    private bool endRequested;
    private NPCController trackedNPC;

    // ---------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------

    /// <summary>เรียกตอนเริ่ม Interaction — Player จะ Fade หาย</summary>
    public static void BeginInteraction(NPCController npc)
    {
        if (Instance != null) Instance.StartGhostSequence(npc);
    }

    /// <summary>เรียกตอน Interaction จบ (ไม่บังคับ — มีระบบตรวจจับอัตโนมัติอยู่แล้ว)</summary>
    public static void EndInteraction()
    {
        if (Instance != null) Instance.endRequested = true;
    }

    /// <summary>คืนค่า Player กลับมาทึบปกติทันที (เช่น ตอนเปลี่ยนวัน/โหลดฉาก)</summary>
    public static void ResetInstant()
    {
        if (Instance != null) Instance.ResetToFull();
    }

    // ---------------------------------------------------------------
    // Unity
    // ---------------------------------------------------------------

    void Awake()
    {
        Instance = this;
        RefreshRenderers();
    }

    void OnDisable()
    {
        ResetToFull();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // PlayerController2D เปิด/ปิด sprite ของไอเทมที่ถืออยู่ตลอด — ย้ำค่า alpha ทุกเฟรมระหว่างจาง
    void LateUpdate()
    {
        if (currentFactor < 0.999f) ApplyFactor(currentFactor);
    }

    // ---------------------------------------------------------------
    // Core
    // ---------------------------------------------------------------

    void StartGhostSequence(NPCController npc)
    {
        trackedNPC = npc;
        endRequested = false;

        if (routine != null) StopCoroutine(routine);
        RefreshRenderers();
        routine = StartCoroutine(GhostRoutine());
    }

    IEnumerator GhostRoutine()
    {
        // 1) Fade หายไป
        yield return FadeTo(0f, fadeOutDuration);

        // 2) รอจน Interaction จบ
        float startTime = Time.unscaledTime;
        float unlockedFor = 0f;

        while (!endRequested)
        {
            if (trackedNPC == null) break;

            var st = trackedNPC.currentState;
            if (st != NPCController.NPCState.AtInteractionZone &&
                st != NPCController.NPCState.Performing) break;

            if (Time.unscaledTime - startTime > startGrace)
            {
                if (!PlayerController2D.IsLocked && !trackedNPC.IsPerforming)
                    unlockedFor += Time.unscaledDeltaTime;
                else
                    unlockedFor = 0f;

                if (unlockedFor >= unlockDebounce) break;
            }

            yield return null;
        }

        trackedNPC = null;

        // 3) กลับมาเป็นร่างลางๆ
        yield return FadeTo(ghostAlpha, ghostFadeInDuration);

        // 4) (Optional) คงไว้ชั่วครู่ แล้วกลับมาทึบปกติ
        if (restoreToFull)
        {
            float t = 0f;
            while (t < ghostHoldSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return FadeTo(1f, restoreDuration);
        }

        routine = null;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float from = currentFactor;

        if (duration <= 0f)
        {
            currentFactor = target;
            ApplyFactor(currentFactor);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ใช้ unscaled เพราะเกมอาจ Pause (timeScale = 0) ระหว่าง Interaction
            currentFactor = Mathf.Lerp(from, target, Mathf.Clamp01(t / duration));
            ApplyFactor(currentFactor);
            yield return null;
        }

        currentFactor = target;
        ApplyFactor(currentFactor);
    }

    void ResetToFull()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        trackedNPC = null;
        endRequested = false;
        currentFactor = 1f;
        ApplyFactor(1f);
    }

    // ---------------------------------------------------------------
    // Renderer helpers
    // ---------------------------------------------------------------

    void RefreshRenderers()
    {
        renderers.Clear();
        GetComponentsInChildren(true, renderers);

        foreach (var sr in renderers)
        {
            // จำ alpha ดั้งเดิมไว้ครั้งเดียวตอนที่ยังไม่โดนแตะ
            if (sr != null && !baseAlpha.ContainsKey(sr))
                baseAlpha[sr] = sr.color.a;
        }
    }

    void ApplyFactor(float factor)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var sr = renderers[i];
            if (sr == null) continue;

            float original = baseAlpha.TryGetValue(sr, out float a) ? a : 1f;
            Color c = sr.color;
            c.a = original * factor;
            sr.color = c;
        }
    }
}