using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCRelationship npcRelationship;

    void Awake()
    {
        npcRelationship = GetComponent<NPCRelationship>();
    }

    public void Interact()
    {
        RelationshipManager.Instance.SetCurrentNPC(npcRelationship);

        SceneLoader.Instance.LoadRelationshipScene();
    }
}