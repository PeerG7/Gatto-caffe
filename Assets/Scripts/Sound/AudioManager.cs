using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("SFX Source (แยกจาก BGM)")]
    public AudioSource sfxSource;   // ✅ ใช้เล่น SFX ทั่วไป (สร้างอัตโนมัติถ้าไม่ใส่)

    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("SFX Clips")]
    public AudioClip sfxMeow;           // 1. แมวร้องตอนเรียกเข้าร้าน
    public AudioClip sfxAngry;          // 2. แมวโกรธออกร้านโดยไม่ได้รับอาหาร
    public AudioClip sfxSitDown;        // 3. แมวมาถึงเก้าอี้แล้วนั่งลง
    public AudioClip sfxComplete;       // 4. หลอดอาหาร / เครื่องดื่มเต็ม
    public AudioClip sfxCoin;           // 5. ได้รับเงินจากแมว
    public AudioClip sfxEndOfDay;       // 6. ประกาศจบวัน

    [Header("Fade Settings")]
    [Range(0.1f, 3f)] public float fadeDuration = 1f;
    [Range(0f, 1f)]   public float maxVolume    = 1f;

    [Header("End-of-Day Fade Settings")]
    [Tooltip("ความนานของ BGM fade-out ตอนใกล้จบวัน (วินาที)")]
    [Range(1f, 15f)] public float endOfDayFadeDuration = 5f;

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

            // ✅ สร้าง sfxSource อัตโนมัติถ้าไม่ได้ลากใส่ใน Inspector
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop        = false;
                sfxSource.playOnAwake = false;
            }
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
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        if (scene.name == mainMenuSceneName)
        {
            if (audioSource.clip != menuMusic)
                CrossfadeTo(menuMusic);
            else if (!audioSource.isPlaying)
                PlayMusicWithFadeIn(menuMusic);
        }
        else if (scene.name == gameSceneName)
        {
            if (audioSource.clip != gameMusic)
                CrossfadeTo(gameMusic);
            else if (!audioSource.isPlaying)
                PlayMusicWithFadeIn(gameMusic);
        }
    }

    // ── Public BGM API ───────────────────────────────────

    public void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip) return;
        audioSource.clip   = clip;
        audioSource.loop   = true;
        audioSource.volume = maxVolume;
        audioSource.Play();
    }

    public void PlayMusicWithFadeIn(AudioClip clip)
    {
        if (audioSource.clip == clip && audioSource.isPlaying) return;
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine(clip));
    }

    public void CrossfadeTo(AudioClip clip, System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(CrossfadeRoutine(clip, onComplete));
    }

    public void StopWithFadeOut(System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine(onComplete));
    }

    /// <summary>
    /// เรียกเมื่อใกล้จบวัน — BGM ค่อยๆ fade out ตาม endOfDayFadeDuration
    /// จากนั้นเล่น sfxEndOfDay อัตโนมัติ
    /// </summary>
    public void PlayEndOfDaySequence()
    {
        StopAllCoroutines();
        StartCoroutine(EndOfDayRoutine());
    }

    /// <summary>เรียกเมื่อกด Next Day — BGM เพลงเกมกลับมา fade in</summary>
    public void ResumeGameMusic()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine(gameMusic));
    }

    // ── Public SFX API ───────────────────────────────────

    /// <summary>แมวร้องตอนถูกเรียกเข้าร้าน</summary>
    public void PlayMeow()     => PlaySFX(sfxMeow);

    /// <summary>แมวโกรธออกร้านโดยไม่ได้รับอาหาร</summary>
    public void PlayAngry()    => PlaySFX(sfxAngry);

    /// <summary>แมวมาถึงเก้าอี้แล้วนั่งลง</summary>
    public void PlaySitDown()  => PlaySFX(sfxSitDown);

    /// <summary>หลอดอาหาร / เครื่องดื่มเต็ม</summary>
    public void PlayComplete() => PlaySFX(sfxComplete);

    /// <summary>ได้รับเงินจากแมว</summary>
    public void PlayCoin()     => PlaySFX(sfxCoin);

    /// <summary>เสียงประกาศจบวัน</summary>
    public void PlayEndOfDay() => PlaySFX(sfxEndOfDay);

    void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
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
        float startVolume = audioSource.volume;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        audioSource.Stop();

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

    // ✅ Fade BGM ออกช้าๆ ด้วย endOfDayFadeDuration แล้วเล่น sfxEndOfDay
    IEnumerator EndOfDayRoutine()
    {
        float startVolume = audioSource.volume;
        float t = 0f;
        while (t < endOfDayFadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / endOfDayFadeDuration);
            yield return null;
        }
        audioSource.volume = 0f;
        audioSource.Stop();

        // เล่นเสียงจบวัน
        PlayEndOfDay();
    }
}