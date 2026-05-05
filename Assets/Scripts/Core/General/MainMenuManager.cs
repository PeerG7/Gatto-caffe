using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName  = "GameScene";
    public int    gameSceneIndex = 1;
    public bool   loadByName     = true;

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public Image      loadingProgressBar;

    // ── Start ────────────────────────────────────────────
    // ✅ ไม่ต้องจัดการเพลงที่นี่แล้ว — AudioManager.OnSceneLoaded จัดการให้อัตโนมัติ
    // ทุกครั้งที่ MainMenu scene โหลด AudioManager จะเล่น menuMusic เองเลย

    public void OnPlayPressed()
    {
        if (AudioManager.instance != null && AudioManager.instance.gameMusic != null)
        {
            // Crossfade ไปเพลง Game แล้วค่อย Load scene
            // ✅ OnSceneLoaded จะ detect ว่า clip == gameMusic อยู่แล้ว → ไม่เล่นซ้ำ
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