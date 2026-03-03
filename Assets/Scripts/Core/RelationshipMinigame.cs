using UnityEngine;
using UnityEngine.SceneManagement;

public class RelationshipMinigame : MonoBehaviour
{
    public int maxInteraction = 3;
    private int currentInteraction = 0;

    public float relationshipGain = 10f;

    public void OnClickCat()
    {
        if (currentInteraction >= maxInteraction)
            return;

        currentInteraction++;

        RelationshipManager.Instance.CurrentNPC.relationship += relationshipGain;

        Debug.Log("Relationship Increased!");

        if (currentInteraction >= maxInteraction)
        {
            Debug.Log("Interaction Limit Reached");
        }
    }

    public void BackToShop()
    {
        SceneManager.LoadScene("ShopScene");
    }
}