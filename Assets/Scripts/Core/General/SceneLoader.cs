using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [SerializeField] private string relationshipSceneName = "RelationshipScene";

    void Awake()
    {
        relationshipSceneName = "RelationshipScene";

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
        }
    }
}