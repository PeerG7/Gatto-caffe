using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void LoadRelationshipScene()
    {
        StartCoroutine(LoadAdditive());
    }

    IEnumerator LoadAdditive()
    {
        yield return SceneManager.LoadSceneAsync("RelationshipScene", LoadSceneMode.Additive);
    }

    public void UnloadRelationshipScene()
    {
        SceneManager.UnloadSceneAsync("RelationshipScene");
    }
}