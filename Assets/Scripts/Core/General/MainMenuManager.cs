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
    /// ฟังก์ชันลบค่า Progression ทั้งหมด (Relationship, Quest Step, Day) เมื่อกด New Game
    /// </summary>
    private void ResetAllRelationshipData()
    {
        // 🌟 ล้างข้อมูลเซฟเควสและวันที่ใน PlayerPrefs ทันที
        PlayerPrefs.DeleteKey("SavedMainQuestStep");
        PlayerPrefs.DeleteKey("SavedGameDay");
        PlayerPrefs.DeleteKey("SavedMoney"); // 🟢 รีเซ็ตเงินด้วยตอนกด New Game
        PlayerPrefs.Save();

        // 1. ถ้า RelationshipManager มีอยู่ในฉาก ให้เรียกใช้ ResetAllRelationships() โดยตรง
        if (RelationshipManager.Instance != null)
        {
            RelationshipManager.Instance.ResetAllRelationships();
            Debug.Log("✅ [MainMenu] Reset Quest, Day และ Relationships เรียบร้อยแล้ว");
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
            Debug.Log("✅ [MainMenu] Reset Relationship Progression, Quest และ Day เรียบร้อยแล้ว");
        }
        else
        {
            Debug.LogWarning("⚠️ [MainMenu] ไม่พบรายการ allCats! แนะนำให้ลาก CatData ใส่ใน Inspector ของ MainMenuManager");
        }
    }

    bool isStarting = false;

    public void OnPlayPressed()
    {
        if (isStarting) return;
        isStarting = true;

        ScreenFader.Ensure().FadeOut(fadeToBlackDuration);

        if (AudioManager.instance != null)
        {
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

                fader.FadeInOnNextScene(gameFadeInDuration);
                bool done = false;
                fader.FadeOut(0.3f, () => done = true);
                while (!done) yield return null;

                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    public void OnContinuePressed()
    {
        // โหลดเกมต่อจากเซฟเดิมโดยตรงทันที ไม่ล้างข้อมูลเซฟ
        OnPlayPressed();
    }
}