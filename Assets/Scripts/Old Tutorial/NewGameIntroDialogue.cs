using UnityEngine;

public class NewGameIntroDialogue : MonoBehaviour
{
    [Header("References")]
    public MainMenuManager mainMenuManager; // ลาก MainMenuManager มาใส่ใน Inspector

    [Header("Intro Lines (บทสนทนาเปิดเกมตอนกด New Game)")]
    [TextArea(2, 5)]
    public string[] introSentences = new string[]
    {
        "คุณได้รับมรดกเป็นร้านคาเฟ่แมวเก่าๆ แห่งหนึ่ง...",
        "ถึงเวลาเริ่มต้นชีวิตใหม่ และสร้างคาเฟ่ในฝันของคุณแล้ว!",
        "ลองเข้าไปสำรวจข้างในกันเถอะ..."
    };

    /// <summary>
    /// ผูกฟังก์ชันนี้เข้ากับปุ่ม New Game บนหน้า Main Menu
    /// </summary>
    public void OnNewGameButtonClicked()
    {
        // 🌟 1. สั่งล้างค่าความสัมพันธ์, เควส และวันที่ทั้งหมดใหม่ทันทีเมื่อกด New Game
        ResetRelationships();

        // 2. เรียกเปิดระบบ DialogueBox เพื่อรันบทสนทนาเปิดฉาก
        if (DialogueController.Instance != null)
        {
            DialogueController.Instance.StartDialogue(introSentences);
        }
        else
        {
            Debug.LogWarning("⚠️ ไม่พบ DialogueController ในฉาก! กำลังเข้าเกมแบบปกติ...");
            if (mainMenuManager != null) mainMenuManager.OnPlayPressed();
        }
    }

    void ResetRelationships()
    {
        // 🌟 ล้างข้อมูลเซฟเควสและวันที่ใน PlayerPrefs ทันที
        PlayerPrefs.DeleteKey("SavedMainQuestStep");
        PlayerPrefs.DeleteKey("SavedGameDay");
        PlayerPrefs.DeleteKey("SavedMoney"); // 🟢 รีเซ็ตเงินด้วยตอนกด New Game
        PlayerPrefs.Save();

        // วิธีที่ 1: เรียกผ่าน RelationshipManager โดยตรง (ถ้ามีอยู่ในฉาก Main Menu)
        if (RelationshipManager.Instance != null)
        {
            RelationshipManager.Instance.ResetAllRelationships();
            Debug.Log("✅ [NewGameIntro] ล้างค่าความสัมพันธ์, เควส และวัน เรียบร้อย");
            return;
        }

        // วิธีที่ 2: ถ้าไม่มี RelationshipManager ให้ลบผ่าน MainMenuManager หรือ PlayerPrefs โดยตรง
        if (mainMenuManager != null && mainMenuManager.allCats != null)
        {
            foreach (var cat in mainMenuManager.allCats)
            {
                if (cat != null && !string.IsNullOrEmpty(cat.catID))
                {
                    PlayerPrefs.DeleteKey("rel_" + cat.catID);
                }
            }
            PlayerPrefs.Save();
            Debug.Log("✅ [NewGameIntro] ล้างค่าความสัมพันธ์, เควส และวัน ผ่าน PlayerPrefs เรียบร้อย");
        }
    }
}