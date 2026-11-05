using UnityEngine;

public class CatIdentity : MonoBehaviour
{
    public CatRelationshipData catData;

    public string CatID => catData != null ? catData.catID : "";

    public bool IsDataCat1()
    {
        return catData != null && (catData.catID == "AIKow" || catData.name.Contains("DATACAT 1"));
    }

    // 🌟 เมธอดใหม่: ตรวจสอบว่าเป็น Data Cat ทุกตัวหรือไม่ (เช่น AIKow, AIDum, AISom ที่ขึ้นต้นด้วย "AI")
    public bool IsDataCat()
    {
        return catData != null && (!string.IsNullOrEmpty(catData.catID) && catData.catID.StartsWith("AI"));
    }
}