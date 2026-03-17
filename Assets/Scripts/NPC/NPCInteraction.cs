using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    private NPCController npc;
    private bool playerInRange = false;

    void Awake()
    {
        npc = GetComponent<NPCController>();
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void Interact()
    {
        NPCController frontNPC = QueueManager.Instance.GetFrontNPC();

        // ทำเฉพาะตัวหน้าคิว
        if (frontNPC != null && frontNPC == npc)
        {
            if (npc.currentState == NPCController.NPCState.InQueue)
            {
                npc.GoToSeat();
            }
            else if (npc.currentState == NPCController.NPCState.Sitting)
            {
                npc.LeaveSeat();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}