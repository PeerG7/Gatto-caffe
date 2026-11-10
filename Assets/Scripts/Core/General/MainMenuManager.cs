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

    [Header("Massive Cat Flood Transition Settings")]
    [Tooltip("ลาก Prefab แอนิเมชันแมว 3 แบบมาใส่ตรงนี้")]
    public GameObject[] transitionPrefabs;
    [Tooltip("Canvas หลักที่จะให้ Effect ไปเกิดทับหน้าจอ UI")]
    public Transform canvasTransform;
    [Tooltip("จำนวนแมวที่จะถูกสเปนออกมาวิ่งถล่มจอพร้อมกันจากขอบซ้าย")]
    public int spawnCount = 25;
    [Tooltip("เวลารอให้ฝูงแมววิ่งเต็มจอก่อนจะยอมสลับไปฉากเกมจริง")]
    public float transitionAnimationDelay = 2.0f;

    [Header("Cat Relationship Data (สำหรับสั่ง New Game)")]
    [Tooltip("ลาก ScriptableObject CatData ของแมวทุกตัวในเกมมาใส่ เพื่อให้รู้ Key ที่ต้องรีเซ็ตตอนกด New Game")]
    public System.Collections.Generic.List<CatRelationshipData> allCats;

    [Header("Continue Button (ปิดปุ่มเมื่อไม่มีเซฟ)")]
    [Tooltip("ลากปุ่ม Continue ในเมนูมาใส่ — ถ้าไม่มีข้อมูลเซฟ ปุ่มจะกดไม่ได้และจางลง")]
    public Button continueButton;
    [Range(0f, 1f)]
    [Tooltip("ความโปร่งใสของปุ่ม Continue ตอนกดไม่ได้")]
    public float continueDisabledAlpha = 0.35f;

    // 🔑 Key นี้ถูกเขียนทุกครั้งที่เซฟ Checkpoint (จบ Tutorial / ขึ้นวันใหม่) และถูกลบตอน New Game
    private const string DaySaveKey = "SavedGameDay";

    /// <summary>มีข้อมูลเซฟให้ Continue หรือไม่</summary>
    public static bool HasSaveData()
    {
        return PlayerPrefs.HasKey(DaySaveKey);
    }

    /// <summary>เปิด/ปิดปุ่ม Continue ตามว่ามีเซฟหรือไม่</summary>
    public void RefreshContinueButton()
    {
        if (continueButton == null) return;

        bool hasSave = HasSaveData();
        continueButton.interactable = hasSave;

        // ใช้ CanvasGroup ทำให้ปุ่มจางลงและไม่รับเมาส์เลย (กัน Hover Effect / เสียงชี้ ทำงานบนปุ่มที่กดไม่ได้)
        CanvasGroup cg = continueButton.GetComponent<CanvasGroup>();
        if (cg == null) cg = continueButton.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = hasSave ? 1f : continueDisabledAlpha;
        cg.blocksRaycasts = hasSave;
        cg.interactable = hasSave;
    }

    void Start()
    {
        RefreshContinueButton();

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
        PlayerPrefs.DeleteKey("SavedFurniture"); // 🟢 รีเซ็ตเฟอร์นิเจอร์ด้วยตอนกด New Game
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

        // 🌟 สั่งระเบิดสเปนฝูงแมวโถมจากฝั่งซ้ายพร้อมกันทันที
        StartCoroutine(TriggerCatFloodRoutine());

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

    /// <summary>
    /// สเปนกองทัพแมวกระจุกโถมออกมาจากทางขอบซ้ายของหน้าจอพุ่งไปทางขวา
    /// </summary>
    IEnumerator TriggerCatFloodRoutine()
    {
        if (transitionPrefabs == null || transitionPrefabs.Length == 0)
        {
            yield break;
        }

        // หา Canvas หลัก
        Transform targetParent = canvasTransform;
        if (targetParent == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null) targetParent = canvas.transform;
        }

        RectTransform canvasRect = targetParent as RectTransform;
        if (canvasRect == null) yield break;

        float halfWidth = canvasRect.rect.width * 0.5f;
        float halfHeight = canvasRect.rect.height * 0.5f;

        // สเปนแมวทุกตัวพร้อมกันทันทีในเฟรมเดียว
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject selectedPrefab = transitionPrefabs[Random.Range(0, transitionPrefabs.Length)];
            if (selectedPrefab == null) continue;

            GameObject spawnedEffect = Instantiate(selectedPrefab, targetParent);
            RectTransform rt = spawnedEffect.GetComponent<RectTransform>();

            if (rt != null)
            {
                // 🌟 กำหนดจุดเกิดให้อยู่ทางฝั่งซ้ายเท่านั้น
                float spawnX = -halfWidth - Random.Range(100f, 450f);
                float spawnY = Random.Range(-halfHeight - 50f, halfHeight + 50f);

                rt.anchoredPosition = new Vector2(spawnX, spawnY);
            }
        }

        yield return null;
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

        // 🌟 หน่วงเวลาให้ฝูงแมวโถมตัดหน้าจอจนเต็ม ก่อนจะเปิดฉากเกมจริง
        yield return new WaitForSecondsRealtime(transitionAnimationDelay);

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
        // กันกรณีถูกเรียกจากที่อื่นโดยไม่มีเซฟ
        if (!HasSaveData())
        {
            Debug.Log("[MainMenu] ไม่มีข้อมูลเซฟ — Continue ใช้ไม่ได้");
            return;
        }

        // โหลดเกมต่อจากเซฟเดิมโดยตรงทันที ไม่ล้างข้อมูลเซฟ
        OnPlayPressed();
    }
}