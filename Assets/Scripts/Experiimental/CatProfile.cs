using UnityEngine;

// บรรทัดนี้ทำให้คุณสามารถคลิกขวาในโฟลเดอร์ของ Unity เพื่อสร้างโปรไฟล์แมวตัวใหม่ได้
[CreateAssetMenu(fileName = "NewCatProfile", menuName = "CatSystem/Cat Profile")]
public class CatProfile : ScriptableObject
{
    public string catName; // ชื่อแมว (เช่น "Calico Cat", "Stray Cat")
    
    // กำหนดให้แมวแต่ละตัวมีการโต้ตอบเฉพาะตัว 3 รูปแบบ
    public CatInteraction[] interactions = new CatInteraction[3]; 
    
    public GameObject catModelPrefab; // โมเดลหรือรูปลักษณ์ของแมวตัวนี้
}