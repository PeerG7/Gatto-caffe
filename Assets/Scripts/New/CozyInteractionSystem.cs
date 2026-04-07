using UnityEngine;

public class CozyInteractionSystem : MonoBehaviour
{
    void Update()
    {
        var npc = NPCInteract.selectedNPC;

        if (npc == null) return;

        if (npc.currentState != NPCController.NPCState.Sitting)
            return;

        switch (InteractionManager.Instance.currentMode)
        {
            case InteractionMode.Pet:
                HandlePet(npc);
                break;

            case InteractionMode.Feed:
                HandleFeed(npc);
                break;

            case InteractionMode.Play:
                HandlePlay(npc);
                break;
        }
    }

    void HandlePet(NPCController npc)
    {
        if (Input.GetMouseButton(0))
        {
            if (IsMouseOnNPC(npc))
            {
                npc.AddRelationship(0.1f);
            }
        }
    }

    void HandleFeed(NPCController npc)
    {
        float distance = Vector3.Distance(npc.transform.position, GetMouseWorldPos());

        if (distance < 2f)
        {
            npc.AddRelationship(Time.deltaTime);
        }
    }

    void HandlePlay(NPCController npc)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOnNPC(npc))
            {
                npc.AddRelationship(0.5f);
            }
        }
    }

    bool IsMouseOnNPC(NPCController npc)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.GetComponent<NPCController>() == npc;
        }

        return false;
    }

    Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, 0);

        if (plane.Raycast(ray, out float dist))
        {
            return ray.GetPoint(dist);
        }

        return Vector3.zero;
    }
}