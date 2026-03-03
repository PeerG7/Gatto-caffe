using UnityEngine;

public class NPCRelationship : MonoBehaviour
{
    public float relationship = 0f;
    public float relationshipPerInteract = 10f;

    public int maxInteractionPerVisit = 3;
    private int currentInteractionCount = 0;
    public int interactionThisVisit = 0;
    public int maxPerVisit = 3;

    private bool isPlayerNearby = false;

    void Update()
    {
        if (isPlayerNearby && Input.GetMouseButtonDown(0))
        {
            TryIncreaseRelationship();
        }
    }
    public bool CanInteract()
    {
        return interactionThisVisit < maxPerVisit;
    }
    public void IncreaseInteraction()
    {
        interactionThisVisit++;
    }
    public void ResetVisit()
    {
        interactionThisVisit = 0;
    }
    void TryIncreaseRelationship()
    {
        if (currentInteractionCount >= maxInteractionPerVisit)
        {
            Debug.Log("Interaction limit reached!");
            return;
        }

        relationship += relationshipPerInteract;
        currentInteractionCount++;

        Debug.Log("Relationship: " + relationship);
    }

    public void ResetInteraction()
    {
        currentInteractionCount = 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Cursor"))
        {
            isPlayerNearby = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Cursor"))
        {
            isPlayerNearby = false;
        }
    }
}