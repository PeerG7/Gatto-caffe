using UnityEngine;

public class NPCRelationship : MonoBehaviour
{
    public string npcName;
    public float relationship = 0;

    public void AddRelationship(float amount)
    {
        relationship += amount;
    }
}