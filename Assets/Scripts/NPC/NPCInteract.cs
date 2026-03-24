using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;
    public bool isInStore = false;

    void Awake()
    {
        npc = GetComponent<NPCController>();
    }

    public bool CanInteract()
    {
        return npc != null && npc.currentState == NPCController.NPCState.Sitting;
    }

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
            npc.GoToSeat();
        }
    }

    // ⭐ แก้ตรงนี้
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

        // ❌ ลบ SceneLoader
        // SceneLoader.Instance.LoadRelationshipScene();

        // ✅ ใช้ UI แทน
        UIManager.Instance.OpenRelationshipPanel();
    }
}