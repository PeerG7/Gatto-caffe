using UnityEngine;
using UnityEngine.EventSystems;

// ====================================================================
// ใส่ script นี้บนปุ่มทุกปุ่มใน qteCanvasInPrefab บน NPC prefab
// แทน Button component ปกติ — รองรับทั้ง Click, Hold, และ Mash
//
// Setup ใน Inspector:
//   - ลบ Button component ออก (ถ้ามี)
//   - Add Component → QTEInteractButton
//   - ปุ่มจะส่ง event ไปหา CatSystemManager.Instance โดยอัตโนมัติ
// ====================================================================
public class QTEInteractButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.NotifyPress();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.NotifyRelease();
    }
}