using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("Fade Settings")]
    [Range(0.1f, 3f)] public float fadeDuration = 1f;
    [Range(0f, 1f)]   public float maxVolume    = 1f;

    [Header("Scene Names (ต้องตรงกับใน Build Settings)")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName     = "GameScene";

    // ── Singleton ────────────────────────────────────────
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Auto-play เพลงตาม Scene ──────────────────────────
    /// <summary>
    /// เรียกอัตโนมัติทุกครั้งที่ scene โหลดเสร็จ
    /// ครอบคลุมทั้ง: กด Play ใน Editor, กลับ Main Menu, โหลด GameScene
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ไม่แตะเพลงถ้าโหลดแบบ Additive (เช่น RelationshipScene)
        if (mode == LoadSceneMode.Additive) return;

        if (scene.name == mainMenuSceneName)
        {
            // กลับ Main Menu → เล่นเพลง Menu
            if (audioSource.clip != menuMusic)
                CrossfadeTo(menuMusic);
            else if (!audioSource.isPlaying)
                PlayMusicWithFadeIn(menuMusic);
        }
        else if (scene.name == gameSceneName)
        {
            // โหลด GameScene (รวมถึงกด Play ใน Editor ตรงๆ) → เล่นเพลง Game
            if (audioSource.clip != gameMusic)
                CrossfadeTo(gameMusic);
            else if (!audioSource.isPlaying)
                PlayMusicWithFadeIn(gameMusic);
        }
    }

    // ── Public API ───────────────────────────────────────

    /// <summary>เล่นเพลงทันที (ไม่ fade)</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip) return;

        audioSource.clip   = clip;
        audioSource.loop   = true;
        audioSource.volume = maxVolume;
        audioSource.Play();
    }

    /// <summary>Fade in เพลงใหม่ (หยุดเพลงเก่าก่อน)</summary>
    public void PlayMusicWithFadeIn(AudioClip clip)
    {
        if (audioSource.clip == clip && audioSource.isPlaying) return;
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine(clip));
    }

    /// <summary>Fade out เพลงเก่า → Fade in เพลงใหม่</summary>
    public void CrossfadeTo(AudioClip clip, System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(CrossfadeRoutine(clip, onComplete));
    }

    /// <summary>Fade out แล้วหยุด</summary>
    public void StopWithFadeOut(System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine(onComplete));
    }

    // ── Coroutines ───────────────────────────────────────

    IEnumerator FadeInRoutine(AudioClip clip)
    {
        audioSource.clip   = clip;
        audioSource.loop   = true;
        audioSource.volume = 0f;
        audioSource.Play();

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, maxVolume, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = maxVolume;
    }

    IEnumerator FadeOutRoutine(System.Action onComplete)
    {
        float startVolume = audioSource.volume;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        onComplete?.Invoke();
    }

    IEnumerator CrossfadeRoutine(AudioClip newClip, System.Action onComplete)
    {
        // Fade out
        float startVolume = audioSource.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.Stop();

        // Swap clip & fade in
        audioSource.clip   = newClip;
        audioSource.loop   = true;
        audioSource.volume = 0f;
        audioSource.Play();

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, maxVolume, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = maxVolume;
        onComplete?.Invoke();
    }
}