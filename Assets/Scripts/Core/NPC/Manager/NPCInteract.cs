using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;

    public bool isInStore = false;

    void Awake()
    {
        npc = GetComponent<NPCController>();
    }

    // 🎯 ใช้เช็คเปิด QTE
    public bool CanInteract()
    {
        return npc != null && npc.currentState == NPCController.NPCState.Sitting;
    }

    // 🤖 Invite
    public void Interact()
    {
        if (npc == null) return;

        NPCController frontNPC = QueueManager.Instance.GetFrontNPC();

        if (frontNPC != npc)
        {
            Debug.Log("❌ Not front of queue");
            return;
        }

        if (npc.currentState == NPCController.NPCState.InQueue)
        {
            Debug.Log("✅ Invite NPC");
            isInStore = true;
            npc.GoToTableDirectly();
        }
    }
    public void GoToTableDirectly()
    {
        // ค้นหา NPCController ที่อยู่ในตัวแมวตัวนี้แล้วสั่งให้หาโต๊ะ
        NPCController controller = GetComponent<NPCController>();
        if (controller != null)
        {
            controller.GoToTableDirectly();
        }
    }

    // ❤️ QTE
    public void RelationShip()
    {
        if (npc == null) return;

        if (npc.currentState != NPCController.NPCState.Sitting)
        {
            Debug.Log("❌ NPC not sitting");
            return;
        }

        Debug.Log("❤️ OPEN RELATIONSHIP");

        RelationshipManager.Instance.SetCurrentNPC(npc);
        SceneLoader.Instance.LoadRelationshipScene();
    }
}