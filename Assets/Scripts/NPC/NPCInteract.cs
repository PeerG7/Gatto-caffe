using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCRelationship npcRelationship;
    private NPCController npc;

    void Awake()
    {
        npcRelationship = GetComponent<NPCRelationship>();
        npc = GetComponent<NPCController>();
    }

    public void Interact()
    {
        NPCController frontNPC = QueueManager.Instance.GetFrontNPC();
        NPCController npc = GetComponent<NPCController>();

        if (frontNPC != null)
        {
            frontNPC.GoToSeat();
        }
        if (npc.currentState == NPCController.NPCState.GoingToSeat) return;

        if (npc == null) return;

        // 🟢 ถ้ายังไม่นั่ง → ให้ไปนั่ง
        if (npc.currentState == NPCController.NPCState.InQueue)
        {
            npc.GoToSeat();
        }
        // 🟢 ถ้านั่งอยู่ → ค่อยให้ออก
        else if (npc.currentState == NPCController.NPCState.Sitting)
        {
            npc.LeaveSeat();
        }
    }
    public void RelationShip()
    {
        NPCController npc = GetComponent<NPCController>();

        if (npc.currentState == NPCController.NPCState.GoingToSeat) return;

        if (npc == null) return;

        RelationshipManager.Instance.SetCurrentNPC(npcRelationship);

        SceneLoader.Instance.LoadRelationshipScene();
    }
}