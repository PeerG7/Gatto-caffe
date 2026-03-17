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

        // โหลดฉากแบบ Additive
        AsyncOperation loadScene = SceneManager.LoadSceneAsync("Experimental Method", LoadSceneMode.Additive);

        // เมื่อโหลดเสร็จแล้ว ให้ส่งข้อมูลแมวไปให้ Manager ในฉากนั้นทันที
        loadScene.completed += (op) => {
            CatSystemManager manager = FindObjectOfType<CatSystemManager>();
            if (manager != null)
            {
                manager.SetupCat(currentCatInConversation);
            }
        };
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