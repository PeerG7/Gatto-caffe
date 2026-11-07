using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [SerializeField] private string relationshipSceneName = "RelationshipScene";

    void Awake()
    {
        relationshipSceneName = "RelationshipScene";

        // 🟢 FIX (ต้นเหตุหลัก): เอา DontDestroyOnLoad ออก
        // เดิม GameManager ทั้งก้อน (DayNightManager, PauseMenuManager, FurnitureInventory ฯลฯ) ค้างข้ามฉาก
        // พอกลับเข้า ShopScene อีกรอบ GameManager ตัวใหม่ในฉากถูก Destroy ทิ้ง เหลือตัวเก่าที่ชี้ Light/UI ของฉากเก่าที่ตายไปแล้ว
        // → จอมืด (Light ไม่ถูกอัปเดต), Timer นับต่อ, ESC เปิด Pause ไม่ได้, เฟอร์นิเจอร์/เงินเพี้ยน
        // ตอนนี้ GameManager เกิดใหม่พร้อม reference ของฉากทุกครั้ง ส่วนข้อมูลที่ต้องเก็บ (วัน/เงิน/เฟอร์นิเจอร์) เซฟลง PlayerPrefs แทน
        Instance = this;
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