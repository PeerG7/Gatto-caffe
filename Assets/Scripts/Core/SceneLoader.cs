using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [SerializeField] private string relationshipSceneName = "Experimental Method";

    void Awake()
    {
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
        if (!SceneManager.GetSceneByName(relationshipSceneName).isLoaded)
        {
            SceneManager.LoadScene(relationshipSceneName, LoadSceneMode.Additive);
        }
    }

    public void CloseRelationshipScene()
    {
        SceneManager.UnloadSceneAsync(relationshipSceneName);
    }
}