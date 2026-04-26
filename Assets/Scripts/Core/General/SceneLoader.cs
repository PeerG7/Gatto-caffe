using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [SerializeField] private string relationshipSceneName = "RelationshipScene"; // ❗ เปลี่ยนชื่อไม่มี space

    void Awake()
    {
        relationshipSceneName = "RelationshipScene"; // 🔥 force ค่าใหม่

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadRelationshipScene()
    {
        Scene scene = SceneManager.GetSceneByName(relationshipSceneName);

        if (!scene.isLoaded)
        {
            Debug.Log("🔥 Loading Scene: " + relationshipSceneName);
            SceneManager.LoadScene(relationshipSceneName, LoadSceneMode.Additive);
            TimeManager.Instance.PauseGame(); // 🔥 ใช้ตัวนี้แทน timeScale
        }
        else
        {
            Debug.Log("⚠ Scene already loaded");
        }
    }

    public void CloseRelationshipScene()
    {
        Scene scene = SceneManager.GetSceneByName(relationshipSceneName);

        if (scene.isLoaded)
        {
            Debug.Log("🧹 Unloading Scene");
            SceneManager.UnloadSceneAsync(relationshipSceneName);
            TimeManager.Instance.ResumeGame();
        }
    }
}