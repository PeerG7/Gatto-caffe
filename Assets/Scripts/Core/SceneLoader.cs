using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;
    // เพิ่มตัวแปรเก็บโปรไฟล์แมวที่กำลังคุยด้วย
    public CatProfile currentCatInConversation;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    // แก้ไขฟังก์ชันให้รับข้อมูลแมวเข้ามาด้วย
    public void LoadRelationshipScene(CatProfile catToTalkTo)
    {
        currentCatInConversation = catToTalkTo;
        SceneManager.LoadScene("Experimental Method", LoadSceneMode.Additive);
    }

    public void CloseRelationshipScene()
    {
        SceneManager.UnloadSceneAsync("Experimental Method");
        currentCatInConversation = null; // เคลียร์ข้อมูลเมื่อปิด
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseRelationshipScene();
        }
    }
}