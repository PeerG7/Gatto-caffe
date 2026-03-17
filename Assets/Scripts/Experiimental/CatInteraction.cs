using UnityEngine;

// [System.Serializable] ทำให้ตัวแปรกลุ่มนี้ไปโชว์ในหน้า Inspector ของ Unity
[System.Serializable]
public class CatInteraction
{
    [Header("ข้อมูลพื้นฐาน")]
    public string actionName; // ชื่อการกระทำ เช่น "Pet", "Hug", "Play"
    public QTEType qteType;   // ประเภทของ QTE (กดครั้งเดียว, ค้างไว้, รัวปุ่ม)

    [Header("เงื่อนไขของแมวตัวนี้")]
    public KeyCode preferredKey = KeyCode.Space; // ปุ่มเฉพาะที่แมวตัวนี้ต้องการ
    public float timeLimit = 3.0f;               // เวลาที่จำกัดในการทำ QTE ให้สำเร็จ
    public float reputationChange = 0.1f;        // ค่าความสัมพันธ์ (Reputation) ที่ได้/ลด
}