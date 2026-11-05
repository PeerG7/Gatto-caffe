using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Intro หน้า Main Menu
///   1) ตัวอักษรชื่อเกมตกลงมาทีละตัว
///   2) ตกครบแล้ว Background ค่อยๆ Fade ขึ้นมา (ระหว่างนี้ปุ่มยังซ่อนและกดไม่ได้)
///   3) Background ขึ้นครบแล้ว ปุ่มค่อยๆ ปรากฏและกดได้
/// </summary>
public class MainMenuIntro : MonoBehaviour
{
    // ให้ MainMenuManager เช็กได้ว่าหน้านี้ใช้ Intro อยู่ (จะได้ไม่ Fade จอดำซ้อน)
    public static bool Exists { get; private set; }

    [Header("Title (TextMeshPro)")]
    public TMP_Text titleText;

    [Header("Background")]
    [Tooltip("Object พื้นหลัง (UI Image) — จะเพิ่ม CanvasGroup ให้เองถ้ายังไม่มี")]
    public GameObject background;

    [Header("Buttons")]
    [Tooltip("ลากปุ่มทั้งหมดมาใส่ (เช่น 3 ปุ่ม) — จะเพิ่ม CanvasGroup ให้เองถ้ายังไม่มี")]
    public GameObject[] buttons;

    [Header("Fade From Black (จอดำตอนเปิดหน้า)")]
    [Tooltip("เวลาที่จอค่อยๆ ใสขึ้นจากดำ ตอนเปิดหน้าเมนู")]
    public float blackFadeDuration = 0.8f;
    [Tooltip("ตั้งสีพื้นหลัง Camera เป็นดำ เพื่อให้ระหว่างรอ Background ขึ้น หน้าจอเป็นสีดำจริงๆ")]
    public bool forceBlackCamera = true;

    [Header("Title Drop Settings")]
    public float startDelay = 0.6f;
    [Tooltip("ระยะห่างระหว่างตัวอักษรแต่ละตัว (วินาที)")]
    public float letterDelay = 0.09f;
    [Tooltip("เวลาที่ตัวอักษร 1 ตัวใช้ตกลงมา")]
    public float letterDropDuration = 0.45f;
    [Tooltip("ความสูงที่ตัวอักษรตกลงมา (หน่วยของ Text)")]
    public float dropHeight = 200f;
    public bool bounce = true;

    [Header("Fade Settings")]
    [Tooltip("พักหลังตัวอักษรตกครบ ก่อนเริ่ม Fade Background")]
    public float pauseBeforeBackground = 0.3f;
    public float backgroundFadeDuration = 1.2f;
    public float buttonFadeDuration = 0.4f;
    [Tooltip("หน่วงระหว่างปุ่มแต่ละปุ่ม (0 = ขึ้นพร้อมกันทั้งหมด)")]
    public float buttonStagger = 0f;

    CanvasGroup bgGroup;
    CanvasGroup[] buttonGroups;

    void Awake()
    {
        Exists = true;

        // เริ่มจากจอดำสนิททันที (ก่อนเฟรมแรกจะถูกวาด)
        ScreenFader.Ensure().SetAlpha(1f);

        if (forceBlackCamera && Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }

        // ซ่อนทุกอย่างตั้งแต่เฟรมแรก
        if (titleText != null) titleText.alpha = 0f;

        bgGroup = GetOrAddGroup(background);
        if (bgGroup != null) bgGroup.alpha = 0f;

        int n = buttons != null ? buttons.Length : 0;
        buttonGroups = new CanvasGroup[n];
        for (int i = 0; i < n; i++)
            buttonGroups[i] = GetOrAddGroup(buttons[i]);
        LockButtons();
    }

    void OnDestroy()
    {
        Exists = false;
    }

    void Start()
    {
        StartCoroutine(IntroRoutine());
    }

    CanvasGroup GetOrAddGroup(GameObject go)
    {
        if (go == null) return null;
        var g = go.GetComponent<CanvasGroup>();
        if (g == null) g = go.AddComponent<CanvasGroup>();
        return g;
    }

    // ซ่อนและล็อกทุกปุ่ม
    void LockButtons()
    {
        foreach (var g in buttonGroups)
        {
            if (g == null) continue;
            g.alpha = 0f;
            g.interactable = false;
            g.blocksRaycasts = false;
        }
    }

    IEnumerator ShowButton(CanvasGroup g)
    {
        yield return FadeGroup(g, 1f, buttonFadeDuration);
        g.interactable = true;
        g.blocksRaycasts = true;
    }

    IEnumerator IntroRoutine()
    {
        // จอค่อยๆ ใสขึ้นจากดำ (ตัวอักษรเริ่มตกระหว่างที่จอกำลังใสขึ้น)
        ScreenFader.Ensure().FadeIn(blackFadeDuration);

        yield return new WaitForSecondsRealtime(startDelay);

        // 1) ตัวอักษรตกลงมาทีละตัว
        if (titleText != null)
            yield return DropTitle();

        yield return new WaitForSecondsRealtime(pauseBeforeBackground);

        // 2) Background Fade ขึ้นมา (ปุ่มยังล็อกอยู่)
        if (bgGroup != null)
            yield return FadeGroup(bgGroup, 1f, backgroundFadeDuration);

        // 3) Background ครบแล้ว → ปุ่มกลับมา
        for (int i = 0; i < buttonGroups.Length; i++)
        {
            if (buttonGroups[i] == null) continue;
            StartCoroutine(ShowButton(buttonGroups[i]));
            if (buttonStagger > 0f)
                yield return new WaitForSecondsRealtime(buttonStagger);
        }
    }

    IEnumerator FadeGroup(CanvasGroup g, float target, float duration)
    {
        float start = g.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        g.alpha = target;
    }

    IEnumerator DropTitle()
    {
        titleText.alpha = 1f;
        titleText.ForceMeshUpdate();

        // นับเฉพาะตัวที่มองเห็น (ข้ามช่องว่าง) เพื่อให้จังหวะตกสม่ำเสมอ
        int visibleCount = 0;
        for (int i = 0; i < titleText.textInfo.characterCount; i++)
            if (titleText.textInfo.characterInfo[i].isVisible) visibleCount++;

        if (visibleCount == 0) yield break;

        float total = (visibleCount - 1) * letterDelay + letterDropDuration;
        float t = 0f;

        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            // ForceMeshUpdate จะคืนค่าตำแหน่งเดิมของทุกตัวอักษร แล้วเราขยับทับอีกที
            titleText.ForceMeshUpdate();
            TMP_TextInfo info = titleText.textInfo;

            int v = 0;   // ลำดับตัวที่มองเห็น
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo ci = info.characterInfo[i];
                if (!ci.isVisible) continue;

                float local = Mathf.Clamp01((t - v * letterDelay) / letterDropDuration);
                v++;

                float eased = bounce ? EaseOutBounce(local) : 1f - Mathf.Pow(1f - local, 3f);
                Vector3 offset = new Vector3(0f, (1f - eased) * dropHeight, 0f);
                float alpha = Mathf.Clamp01(local * 4f);   // โผล่เร็วๆ ตอนเริ่มตก

                int mi = ci.materialReferenceIndex;
                int vi = ci.vertexIndex;
                Vector3[] verts = info.meshInfo[mi].vertices;
                Color32[] cols = info.meshInfo[mi].colors32;

                for (int j = 0; j < 4; j++)
                {
                    verts[vi + j] += offset;
                    Color32 c = cols[vi + j];
                    c.a = (byte)(c.a * alpha);
                    cols[vi + j] = c;
                }
            }

            titleText.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
            yield return null;
        }

        // คืนค่าปกติ
        titleText.ForceMeshUpdate();
    }

    static float EaseOutBounce(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (x < 1f / d1) return n1 * x * x;
        if (x < 2f / d1) { x -= 1.5f / d1; return n1 * x * x + 0.75f; }
        if (x < 2.5f / d1) { x -= 2.25f / d1; return n1 * x * x + 0.9375f; }
        x -= 2.625f / d1;
        return n1 * x * x + 0.984375f;
    }
}