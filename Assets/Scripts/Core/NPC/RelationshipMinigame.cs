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
        if (currentInteraction >= maxInteraction)
            return;

        currentInteraction++;

        var npc = RelationshipManager.Instance.GetCurrentNPC();

        if (npc != null)
        {
            // 🔥 ดึง component ความสัมพันธ์
            var relation = npc.GetComponent<NPCRelationship>();

            if (relation != null)
            {
                relation.relationship += relationshipGain;
            }
        }

        Debug.Log("❤️ Relationship Increased! (" + currentInteraction + "/" + maxInteraction + ")");

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