using UnityEngine;

// =====================================================================
// RelationshipMinigame — v2 (Player Lock Fix)
//
// บั๊กที่แก้: BackToShop() ปิดฉาก RelationshipScene (ผ่าน SceneLoader) แต่ไม่เคย
//   ปลด PlayerController2D.IsLocked หรือ ForceResume() DayNightManager เลย
//   ทำให้ Player ค้างขยับไม่ได้ตลอดไปหลังจบ minigame ความสัมพันธ์
//   (ต้นทางล็อกอยู่ที่ NPCInteract.RelationShip() ตอนเปิด relationshipCanvas
//    แต่ RelationshipMinigame อยู่คนละฉากที่โหลดแบบ Additive ผ่าน SceneLoader
//    จึงไม่มีทางรับรู้/ปลดล็อกให้กันเองถ้าไม่เขียนตรงๆ ตรงนี้)
// =====================================================================
public class RelationshipMinigame : MonoBehaviour
{
    public int maxInteraction = 3;
    private int currentInteraction = 0;
    public float relationshipGain = 10f;

    // 🔈 เสียงตอนคลิกแมว เล่นผ่าน NPCInteract.PlayHappy() แทนแล้ว (ดูใน OnClickCat)
    //    เพื่อให้แมว VIP ใช้เสียงเฉพาะตัวได้ — ไม่ต้องลากไฟล์เสียงใส่ในนี้อีกต่อไป

    void OnEnable()
    {
        currentInteraction = 0;
    }

    public void OnClickCat()
    {
        if (currentInteraction >= maxInteraction) return;

        currentInteraction++;

        // ✅ เพิ่มค่าความสัมพันธ์ผ่าน RelationshipManager
        //    เพื่อให้บันทึก PlayerPrefs และผ่าน maxRelationship clamp ด้วย
        var npc = RelationshipManager.Instance.GetCurrentNPC();
        if (npc != null)
        {
            var relation = npc.GetComponent<NPCRelationship>();
            if (relation != null)
                RelationshipManager.Instance.AddRelationship(relation.npcName, relationshipGain);

            // ✅ Interact สำเร็จ — เล่นเสียงมีความสุข (VIP ใช้เสียงเฉพาะตัวถ้าตั้งไว้)
            NPCInteract interact = npc.GetComponent<NPCInteract>();
            if (interact != null)
                interact.PlayHappy();
            else if (AudioManager.instance != null)
                AudioManager.instance.Cathappy();
        }

        Debug.Log($"❤️ +{relationshipGain} Relationship | ({currentInteraction}/{maxInteraction})");

        if (currentInteraction >= maxInteraction)
            Debug.Log("✅ Interaction Limit Reached");
    }

    public void BackToShop()
    {
        var npc = RelationshipManager.Instance.GetCurrentNPC();
        if (npc != null) npc.GoExit();

        RelationshipManager.Instance.ClearNPC();
        SceneLoader.Instance.CloseRelationshipScene();

        // ✅ Fix บั๊ก: ปลดล็อก Player + resume เกม ให้คู่กับตอนที่
        //    NPCInteract.RelationShip() ล็อกไว้ตอนเปิด relationshipCanvas
        //    (จุดนี้เป็นคนละฉากกับ RelationshipUI จึงต้องปลดเองตรงๆ ที่นี่)
        DayNightManager.Instance?.ForceResume();
        PlayerController2D.IsLocked = false;
    }
}