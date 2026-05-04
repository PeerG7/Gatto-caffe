using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "GameScene";
    public int gameSceneIndex = 1;
    public bool loadByName = true;

    [Header("Loading Screen")]
    public GameObject loadingScreen;
    public Image loadingProgressBar;

    // ── Buttons ──────────────────────────────────────────
    public void OnPlayPressed()
    {
        StartCoroutine(LoadGameAsync());
    }

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Async Load ────────────────────────────────────────
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