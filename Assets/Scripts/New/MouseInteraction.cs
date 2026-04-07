using UnityEngine;

public class MouseInteraction : MonoBehaviour
{
    void Update()
    {
        var selected = NPCInteract.selectedNPC;

        if (selected == null) return;

        switch (InteractionManager.Instance.currentMode)
        {
            case InteractionMode.Pet:
                HandlePet(selected);
                break;

            case InteractionMode.Feed:
                HandleFeed(selected);
                break;

            case InteractionMode.Play:
                HandlePlay(selected);
                break;
        }
    }

    void HandlePet(NPCController cat)
    {
        if (Input.GetMouseButton(0))
        {
            if (IsMouseOnCat(cat))
            {
                cat.AddRelationship(0.1f);
            }
        }
    }

    void HandleFeed(NPCController cat)
    {
        float distance = Vector3.Distance(cat.transform.position, GetMouseWorldPos());

        if (distance < 2f)
        {
            cat.AddRelationship(Time.deltaTime);
        }
    }

    void HandlePlay(NPCController cat)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseOnCat(cat))
            {
                cat.AddRelationship(0.5f);
            }
        }
    }

    bool IsMouseOnCat(NPCController cat)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.GetComponent<NPCController>() == cat;
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