using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";
    public int gameSceneIndex = 1;
    public bool loadByName = true;

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public Image loadingProgressBar;

    [Header("Screen Fade (จอดำ)")]
    [Tooltip("เวลา Fade จากดำเป็นใส ตอนเปิดหน้าเมนู")]
    public float menuFadeInDuration = 1.2f;
    [Tooltip("เวลา Fade เป็นจอดำ ตอนกดเล่น")]
    public float fadeToBlackDuration = 1f;
    [Tooltip("เวลา Fade จากดำเป็นใส ตอนเข้าหน้าเกม")]
    public float gameFadeInDuration = 1.5f;

    [Header("Cat Relationship Data (สำหรับสั่ง New Game)")]
    [Tooltip("ลาก ScriptableObject CatData ของแมวทุกตัวในเกมมาใส่ เพื่อให้รู้ Key ที่ต้องรีเซ็ตตอนกด New Game")]
    public System.Collections.Generic.List<CatRelationshipData> allCats;

    void Start()
    {
        // เปิดหน้าเมนู: เริ่มจากจอดำ แล้วค่อยๆ ใสขึ้นจนเห็นหน้าเมนูและข้อความ
        // ถ้าหน้านี้ใช้ MainMenuIntro ให้ตัวนั้นจัดการ Fade จากดำเอง (กัน Fade ซ้อน)
        if (MainMenuIntro.Exists) return;

        var fader = ScreenFader.Ensure();
        fader.SetAlpha(1f);
        fader.FadeIn(menuFadeInDuration);
    }

    /// <summary>
    /// ผูกปุ่ม New Game บน UI เข้ากับฟังก์ชันนี้
    /// </summary>
    public void OnNewGamePressed()
    {
        ResetAllRelationshipData();
        OnPlayPressed();
    }

    /// <summary>
    /// ฟังก์ชันลบค่า Relationship Progression ใน PlayerPrefs ของแมวทุกตัว
    /// </summary>
    private void ResetAllRelationshipData()
    {
        // 1. ถ้า RelationshipManager มีอยู่ในฉาก ให้เรียกใช้ ResetAllRelationships() โดยตรง
        if (RelationshipManager.Instance != null)
        {
            RelationshipManager.Instance.ResetAllRelationships();
            return;
        }

        // 2. ถ้าอยู่หน้า MainMenu (ยังไม่มี RelationshipManager) ให้ทำการลบ PlayerPrefs ตาม catID
        if (allCats != null && allCats.Count > 0)
        {
            foreach (var cat in allCats)
            {
                if (cat != null && !string.IsNullOrEmpty(cat.catID))
                {
                    PlayerPrefs.DeleteKey("rel_" + cat.catID);
                }
            }
            PlayerPrefs.Save();
            Debug.Log("✅ [MainMenu] Reset Relationship Progression เรียบร้อยแล้ว");
        }
        else
        {
            // Fallback: หากไม่ได้ใส่ allCats ใน Inspector
            Debug.LogWarning("⚠️ [MainMenu] ไม่พบรายการ allCats! แนะนำให้ลาก CatData ใส่ใน Inspector ของ MainMenuManager");
        }
    }

    bool isStarting = false;

    public void OnPlayPressed()
    {
        if (isStarting) return;   // กันกดซ้ำระหว่าง Fade
        isStarting = true;

        // จอค่อยๆ ดำ พร้อมกับเพลง Crossfade
        ScreenFader.Ensure().FadeOut(fadeToBlackDuration);

        if (AudioManager.instance != null)
        {
            // ✅ สุ่มเพลงเกมตั้งแต่ตอนกดปุ่ม แล้วค่อยโหลด Scene
            AudioManager.instance.CrossfadeToRandomGameMusic(
                onComplete: () => StartCoroutine(LoadGameAsync())
            );
        }
        else
        {
            StartCoroutine(LoadGameAfterDelay());
        }
    }

    IEnumerator LoadGameAfterDelay()
    {
        yield return new WaitForSecondsRealtime(fadeToBlackDuration);
        yield return LoadGameAsync();
    }

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnBackToMainMenuPressed()
    {
        if (AudioManager.instance != null && AudioManager.instance.menuMusic != null)
        {
            AudioManager.instance.CrossfadeTo(
                AudioManager.instance.menuMusic,
                onComplete: () => SceneManager.LoadScene(0)
            );
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    IEnumerator LoadGameAsync()
    {
        var fader = ScreenFader.Ensure();

        // จอดำสนิทแล้ว → เปิด Loading Screen (ถ้ามี) แล้วเอาจอดำออกชั่วคราวให้เห็น Loading
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            fader.SetAlpha(0f);
        }

        AsyncOperation op = loadByName
            ? SceneManager.LoadSceneAsync(gameSceneName)
            : SceneManager.LoadSceneAsync(gameSceneIndex);

        op.allowSceneActivation = false;
        bool activating = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (loadingProgressBar != null)
                loadingProgressBar.fillAmount = progress;

            if (op.progress >= 0.9f && !activating)
            {
                activating = true;
                yield return new WaitForSecondsRealtime(0.2f);

                // ดำอีกรอบก่อนสลับ Scene แล้วสั่งให้ใสขึ้นเองเมื่อหน้าเกมโหลดเสร็จ
                fader.FadeInOnNextScene(gameFadeInDuration);
                bool done = false;
                fader.FadeOut(0.3f, () => done = true);
                while (!done) yield return null;

                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}