using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCInteract : MonoBehaviour
{
    public string relationshipSceneName = "RelationshipScene";
    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            EnterRelationshipScene();
        }
        if (RelationshipManager.Instance.CurrentNPC.CanInteract())
        {
            RelationshipManager.Instance.CurrentNPC.IncreaseInteraction();
        }
    }

    void EnterRelationshipScene()
    {
        // ส่งข้อมูล NPC ไป Scene ใหม่
        RelationshipManager.Instance.SetCurrentNPC(gameObject);
        SceneManager.LoadScene(relationshipSceneName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}