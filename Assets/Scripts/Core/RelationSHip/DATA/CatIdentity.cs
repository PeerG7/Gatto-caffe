using UnityEngine;

// =====================================================================
// Add component นี้บน NPC Prefab แทน CatRelationshipData
// แล้วลาก CatRelationshipData asset มาใส่ใน catData field
// =====================================================================
public class CatIdentity : MonoBehaviour
{
    public CatRelationshipData catData;

    public string CatID => catData != null ? catData.catID : "";
}