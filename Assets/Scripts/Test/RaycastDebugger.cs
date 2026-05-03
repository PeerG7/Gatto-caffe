using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RaycastDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(
                new PointerEventData(EventSystem.current)
                { position = Input.mousePosition },
                results
            );
            foreach (var r in results)
                Debug.Log("🎯 Hit UI: " + r.gameObject.name + " | Layer: " + LayerMask.LayerToName(r.gameObject.layer));
        }
    }
}