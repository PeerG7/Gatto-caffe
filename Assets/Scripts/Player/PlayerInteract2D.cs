using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    private NPCInteract currentNPC;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Pressed E");

            if (currentNPC != null)
            {
                Debug.Log("Invite NPC To Store");
                currentNPC.Interact();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Pressed R");

            if (currentNPC != null)
            {
                Debug.Log("Interacting with NPC");
                currentNPC.RelationShip();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        NPCInteract npc = other.GetComponent<NPCInteract>();

        if (npc != null)
        {
            currentNPC = npc;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        NPCInteract npc = other.GetComponent<NPCInteract>();

        if (npc == currentNPC)
        {
            currentNPC = null;
        }
    }
}