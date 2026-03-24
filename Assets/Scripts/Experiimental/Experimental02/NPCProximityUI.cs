using UnityEngine;

public class NPCProximityUI : MonoBehaviour
{
    public GameObject iconUI;

    private NPCInteract npcInteract;
    private bool playerInRange = false;

    void Awake()
    {
        npcInteract = GetComponent<NPCInteract>();
        iconUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;

        if (npcInteract.CanInteract())
        {
            iconUI.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                npcInteract.RelationShip();
            }
        }
        else
        {
            iconUI.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            iconUI.SetActive(false);
        }
    }
}