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
                Debug.Log("Interacting with NPC");
                currentNPC.Interact();
            }
        }
        //if (Input.GetKeyDown(KeyCode.E) && currentNPC != null)
        //{
        //    currentNPC.Interact();
        //}
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