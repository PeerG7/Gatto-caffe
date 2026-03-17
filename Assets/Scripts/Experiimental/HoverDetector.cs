using UnityEngine;
using UnityEngine.EventSystems;

public class HoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool isHovering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        Debug.Log("เริ่มลูบแมว...");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        Debug.Log("หยุดลูบแมว...");
    }

    // รีเซ็ตค่าเมื่อเปิดใช้งานใหม่
    void OnEnable() { isHovering = false; }
}