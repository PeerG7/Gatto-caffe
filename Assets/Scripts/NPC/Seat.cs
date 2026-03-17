using UnityEngine;

public class Seat : MonoBehaviour
{
    public bool isOccupied = false;
    public NPCController currentNPC;

    public bool IsAvailable()
    {
        return !isOccupied;
    }

    public void Occupy(NPCController npc)
    {
        isOccupied = true;
        currentNPC = npc;
    }

    public void Leave()
    {
        isOccupied = false;
        currentNPC = null;
    }
}