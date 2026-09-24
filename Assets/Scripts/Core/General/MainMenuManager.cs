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

    [Header("Cat Relationship Data (สำหรับสั่ง New Game)")]
    [Tooltip("ลาก ScriptableObject CatData ของแมวทุกตัวในเกมมาใส่ เพื่อให้รู้ Key ที่ต้องรีเซ็ตตอนกด New Game")]
    public System.Collections.Generic.List<CatRelationshipData> allCats;

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

    public void OnPlayPressed()
    {
        if (AudioManager.instance != null && AudioManager.instance.gameMusic != null)
        {
            AudioManager.instance.CrossfadeTo(
                AudioManager.instance.gameMusic,
                onComplete: () => StartCoroutine(LoadGameAsync())
            );
        }
        else
        {
            StartCoroutine(LoadGameAsync());
        }
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
        if (loadingScreen != null) loadingScreen.SetActive(true);

        AsyncOperation op = loadByName
            ? SceneManager.LoadSceneAsync(gameSceneName)
            : SceneManager.LoadSceneAsync(gameSceneIndex);

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (loadingProgressBar != null)
                loadingProgressBar.fillAmount = progress;

            if (op.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.2f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}