using UnityEngine;

public class GameIntroTrigger : MonoBehaviour
{
    [Header("บทสนทนาเปิดฉากใน GameScene")]
    [TextArea(2, 5)]
    public string[] startDialogueLines = new string[]
    {
        "ยินดีต้อนรับสู่ร้านคาเฟ่แมวของคุณ!",
        "ดูเหมือนร้านจะยังโล่งอยู่เลย ลองสำรวจรอบๆ แล้วเริ่มจัดการร้านกันเถอะ"
    };

    void Start()
    {
        // หน่วงเวลาเล็กน้อยให้ฉากโหลดเสร็จสมบูรณ์ แล้วสั่งเปิด Dialogue ทันที
        Invoke("TriggerIntro", 0.5f);
    }

    void TriggerIntro()
    {
        if (DialogueController.Instance != null)
        {
            DialogueController.Instance.StartDialogue(startDialogueLines);
        }
        else
        {
            Debug.LogWarning("⚠️ ไม่พบ DialogueController ใน GameScene!");
        }
    }
}