using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fade จอดำ ข้าม Scene ได้ — ไม่ต้องสร้าง Object / Canvas เอง
/// เรียกใช้: ScreenFader.Ensure().FadeOut(0.8f, callback);
/// </summary>
public class ScreenFader : MonoBehaviour
{
    static ScreenFader instance;

    CanvasGroup group;
    Coroutine running;

    // ถ้า true จะ Fade จากดำเป็นใสเอง เมื่อโหลด Scene ใหม่เสร็จ
    bool fadeInOnNextScene;
    float nextSceneFadeDuration = 1f;

    public static ScreenFader Ensure()
    {
        if (instance != null) return instance;

        var go = new GameObject("ScreenFader");
        instance = go.AddComponent<ScreenFader>();
        DontDestroyOnLoad(go);
        instance.Build();
        return instance;
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;   // อยู่บนสุดของทุก UI

        gameObject.AddComponent<CanvasScaler>();

        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var imgObj = new GameObject("Black");
        imgObj.transform.SetParent(transform, false);
        var img = imgObj.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        if (!fadeInOnNextScene) return;

        fadeInOnNextScene = false;
        FadeIn(nextSceneFadeDuration);
    }

    // ── Public API ───────────────────────────────────────

    /// <summary>ตั้งความดำทันที (0 = ใส, 1 = ดำสนิท)</summary>
    public void SetAlpha(float a)
    {
        StopRunning();
        group.alpha = a;
        group.blocksRaycasts = a > 0.01f;
    }

    /// <summary>ใสเป็นดำ</summary>
    public void FadeOut(float duration, System.Action onComplete = null)
    {
        Run(FadeRoutine(1f, duration, onComplete));
    }

    /// <summary>ดำเป็นใส</summary>
    public void FadeIn(float duration, System.Action onComplete = null)
    {
        Run(FadeRoutine(0f, duration, onComplete));
    }

    /// <summary>สั่งให้ Fade จากดำเป็นใสอัตโนมัติ เมื่อ Scene ถัดไปโหลดเสร็จ</summary>
    public void FadeInOnNextScene(float duration)
    {
        fadeInOnNextScene = true;
        nextSceneFadeDuration = duration;
    }

    // ── Internal ─────────────────────────────────────────

    void Run(IEnumerator routine)
    {
        StopRunning();
        running = StartCoroutine(routine);
    }

    void StopRunning()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }

    IEnumerator FadeRoutine(float target, float duration, System.Action onComplete)
    {
        float start = group.alpha;
        float t = 0f;
        group.blocksRaycasts = true;   // กันกดปุ่มระหว่าง Fade

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }

        group.alpha = target;
        group.blocksRaycasts = target > 0.01f;
        running = null;
        onComplete?.Invoke();
    }
}