using UnityEngine;

public class CatIdentity : MonoBehaviour
{
    public CatRelationshipData catData;

    public string CatID => catData != null ? catData.catID : "";

    /// <summary>
    /// ตรวจสอบว่าเป็น DATACAT 1 (AIKow) หรือไม่[cite: 5]
    /// </summary>
    public bool IsDataCat1()
    {
        return catData != null && (catData.catID == "AIKow" || catData.name.Contains("DATACAT 1"));
    }
}