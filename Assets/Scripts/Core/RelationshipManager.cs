using UnityEngine;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance;

    public NPCRelationship CurrentNPC;

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

    public void SetCurrentNPC(NPCRelationship npc)
    {
        CurrentNPC = npc;
    }
}