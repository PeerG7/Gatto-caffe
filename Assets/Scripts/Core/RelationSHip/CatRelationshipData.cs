using UnityEngine;

// =====================================================================
// ScriptableObject ข้อมูลพื้นฐานของแมวแต่ละตัว
// สร้างโดย: Right Click in Project → Create → CatData → Cat Relationship Data
// =====================================================================
[CreateAssetMenu(fileName = "NewCatData", menuName = "CatData/Cat Relationship Data")]
public class CatRelationshipData : ScriptableObject
{
    [Header("Cat Identity")]
    public string catName;
    public Sprite catPortrait;

    // ID unique ของแมวแต่ละตัว — ใช้เป็น key สำหรับ save
    // ตั้งชื่อแบบ snake_case ห้ามซ้ำกัน เช่น "cat_mimi", "cat_neko"
    public string catID;

    [Header("Relationship Settings")]
    public float maxRelationship = 100f;
}