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

    // ✔ ทำเฉพาะตัวหน้าคิว
    if (frontNPC != npc) return;

    if (npc.currentState == NPCController.NPCState.InQueue)
    {
        npc.GoToSeat();
    }
    else if (npc.currentState == NPCController.NPCState.Sitting)
    {
        npc.LeaveSeat();
    }
    }
    public void RelationShip()
    {
        if (npc == null) return;

        // ❗ จำ NPC ตัวนี้ไว้
        RelationshipManager.Instance.SetCurrentNPC(npc);

        SceneLoader.Instance.LoadRelationshipScene();
    }
}