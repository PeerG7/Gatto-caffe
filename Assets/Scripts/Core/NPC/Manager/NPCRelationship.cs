using UnityEngine;

public class NPCRelationship : MonoBehaviour
{
    [Tooltip("ต้องตรงกับ catID ใน RelationshipManager > allCats")]
    public string npcName;  // ใช้เป็น catID ในการ lookup RelationshipManager

    // ✅ relationship อ่านค่าจาก RelationshipManager เสมอ (ข้อมูล sync กัน)
    public float relationship
    {
        get => RelationshipManager.Instance != null
            ? RelationshipManager.Instance.GetRelationship(npcName)
            : 0f;
    }

    // ✅ เพิ่มค่าผ่าน RelationshipManager (บันทึก PlayerPrefs + clamp อัตโนมัติ)
    public void AddRelationship(float amount)
    {
        if (RelationshipManager.Instance != null)
            RelationshipManager.Instance.AddRelationship(npcName, amount);
    }
}