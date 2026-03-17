using UnityEngine;

public class RelationshipManager : MonoBehaviour
{
    public static RelationshipManager Instance;

    private NPCController currentNPC;

    void Awake()
    {
        Instance = this;
    }

    public void SetCurrentNPC(NPCController npc)
    {
        currentNPC = npc;
    }

    public NPCController GetCurrentNPC()
    {
        return currentNPC;
    }

    public void ClearNPC()
    {
        currentNPC = null;
    }
}