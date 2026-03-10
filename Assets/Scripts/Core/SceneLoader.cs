using UnityEngine;
using UnityEngine.SceneManagement;

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
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadRelationshipScene()
    {
        SceneManager.LoadScene("Experimental Method", LoadSceneMode.Additive);
    }

    public void CloseRelationshipScene()
    {
        //SceneManager.LoadScene("ShopScene");
        SceneManager.UnloadSceneAsync("Experimental Method");
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneLoader.Instance.CloseRelationshipScene();
        }
    }
}