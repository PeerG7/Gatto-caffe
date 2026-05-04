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

    // Flag ป้องกันไม่ให้ Start() เปลี่ยนเพลงหลังกด Play
    private static bool isLoadingGame = false;

    void Start()
    {
        if (AudioManager.instance == null) return;

        if (isLoadingGame)
        {
            // กำลังจะไป GameScene อยู่ ไม่ต้องแตะเพลง
            isLoadingGame = false;
            return;
        }

        // เปิดหน้า Main Menu ครั้งแรก หรือกลับมาจากเกม → เล่นเพลง Menu
        if (AudioManager.instance.audioSource.clip != AudioManager.instance.menuMusic)
            AudioManager.instance.CrossfadeTo(AudioManager.instance.menuMusic);
        else if (!AudioManager.instance.audioSource.isPlaying)
            AudioManager.instance.PlayMusicWithFadeIn(AudioManager.instance.menuMusic);
    }

    public void OnPlayPressed()
    {
        isLoadingGame = true; // ✅ เซต Flag ก่อน Load

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