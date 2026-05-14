using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenuButton : MonoBehaviour
{
    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";

    public void OnClick()
    {
        // ✅ Reset pause ก่อนออก ป้องกัน Time.timeScale ค้าง
        DayNightManager.Instance?.ForceResume();

        if (AudioManager.instance != null && AudioManager.instance.menuMusic != null)
        {
            AudioManager.instance.CrossfadeTo(
                AudioManager.instance.menuMusic,
                onComplete: () => SceneManager.LoadScene(mainMenuSceneName)
            );
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}