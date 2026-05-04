using UnityEngine;

public class RelationshipMinigame : MonoBehaviour
{
    public int maxInteraction = 3;
    private int currentInteraction = 0;

    public float relationshipGain = 10f;

    void OnEnable()
    {
        currentInteraction = 0; // 🔥 รีเซ็ตทุกครั้งที่เปิด
    }

public void OnClickCat()
{
    if (currentInteraction >= maxInteraction) return;

    currentInteraction++;
    
    // ดึง NPC ที่เรากำลังคุยด้วยจาก Manager
    var npc = RelationshipManager.Instance.GetCurrentNPC(); 

    if (npc != null)
    {
        // 1. เพิ่มค่าความสัมพันธ์
        var relation = npc.GetComponent<NPCRelationship>();
        if (relation != null) relation.relationship += relationshipGain;

        // 2. สั่งให้แมวส่งเสียง (ต้องดึง Component NPCInteract มาสั่ง Play)
        var interactScript = npc.GetComponent<NPCInteract>();
        if (interactScript != null)
        {
            interactScript.PlayMeow(); // เรียก Method ที่คุณเขียนไว้ใน NPCInteract
        }
    }



    Debug.Log("❤️ Relationship Increased & Meow! (" + currentInteraction + "/" + maxInteraction + ")");

    if (currentInteraction >= maxInteraction)
    {
        Debug.Log("✅ Interaction Limit Reached");
    }
}

    public void BackToShop()
    {
        var npc = RelationshipManager.Instance.GetCurrentNPC();

        if (npc != null)
        {
            npc.GoExit(); // 🔥 NPC เดินออกจริง
        }

        RelationshipManager.Instance.ClearNPC();

        SceneLoader.Instance.CloseRelationshipScene(); // ✔ ใช้ตัวนี้
    }
}