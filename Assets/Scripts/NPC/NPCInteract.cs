using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;
    public static NPCController selectedNPC;

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
            npc.GoToSeat();
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

        Debug.Log("❤️ SELECT NPC");

        selectedNPC = npc;

        // ❌ ปิดของเดิม
        // SceneLoader.Instance.LoadRelationshipScene();

        // ✅ เปิด UI แทน
        UIManager.Instance.ShowInteractionUI(npc);
    }
    public void OnCloseUI()
    {
        UIManager.Instance.HideInteractionUI();
        NPCInteract.selectedNPC = null;
    }
}