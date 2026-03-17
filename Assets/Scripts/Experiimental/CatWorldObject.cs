using UnityEngine;
using UnityEngine.SceneManagement;

public class CatWorldObject : MonoBehaviour
{
    public CatProfile myProfile; // ลากไฟล์ Profile ของแมวตัวนี้มาใส่

    // เมื่อผู้เล่นเดินมาชน หรือคลิกที่ตัวแมว
    void OnMouseDown()
    {
        // 1. บันทึกข้อมูลแมวตัวนี้ลงในตัวกลาง
        GameManager.SelectedCatProfile = myProfile;

        // 2. ย้ายไปยังฉากโต้ตอบ (สมมติว่าชื่อ InteractionScene)
        SceneManager.LoadScene("Experimental Method");
    }
}