using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// =====================================================================
// UIFocusHelper — ย้าย "ปุ่มที่ถูกเลือก" (D-pad) ไปที่ปุ่มที่ต้องการตอนเปิดหน้า UI ใหม่
//
//  ใช้ตอนเปิดหน้าที่ซ้อนทับหน้าอื่น (เช่น หน้ายืนยันออกเกม, หน้าสรุปวัน)
//  ถ้าไม่ย้าย ปุ่มที่ถูกเลือกค้างอยู่ที่หน้าเดิม D-pad จะวิ่งไปปุ่มของหน้าที่อยู่ข้างหลัง
//
//  ทำงานเฉพาะตอนผู้เล่นใช้จอยอยู่ (ผู้เล่นเมาส์ไม่ต้องเห็นปุ่มถูกไฮไลต์)
// =====================================================================
public static class UIFocusHelper
{
    public static void Select(Selectable target)
    {
        if (target == null || !target.gameObject.activeInHierarchy || !target.IsInteractable()) return;

#if ENABLE_INPUT_SYSTEM
        if (!InputDeviceMonitor.UsingGamepad) return;
#else
        return;
#endif

#if ENABLE_INPUT_SYSTEM
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target.gameObject);

        // แจ้ง GamepadUINavigator ว่านี่คือการเลือกที่ตั้งใจ (กันถูกมองว่าเป็น D-pad พาหลุดไปหน้าอื่นแล้วเด้งกลับ)
        if (GamepadUINavigator.Instance != null)
            GamepadUINavigator.Instance.AcceptSelection(target.gameObject);

        // เคอร์เซอร์จอยกระโดดไปอยู่บนปุ่มด้วย → กด A (คลิกซ้าย) แล้วโดนปุ่มนั้นพอดี
        RectTransform rt = target.transform as RectTransform;
        if (rt != null && GamepadCursor.Instance != null)
        {
            Canvas canvas = target.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null)
            {
                Canvas root = canvas.rootCanvas;
                if (root.renderMode != RenderMode.ScreenSpaceOverlay)
                    cam = root.worldCamera != null ? root.worldCamera : Camera.main;
            }

            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
            GamepadCursor.Instance.MoveTo(sp);
        }
#endif
    }
}