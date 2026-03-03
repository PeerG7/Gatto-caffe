using UnityEngine;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance;

    public NPCRelationship CurrentNPC;
    public GameObject[] hearts;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCurrentNPC(GameObject npcObject)
    {
        CurrentNPC = npcObject.GetComponent<NPCRelationship>();
    }
    public void LeaveShop()
    {
        GetComponent<NPCRelationship>().ResetInteraction();
    }
    //public void UpdateHearts()
    //{
    //    for (int i = 0; i < hearts.Length; i++)
    //    {
    //        hearts[i].SetActive(i < currentInteraction);
    //    }
    //}
}