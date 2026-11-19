using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// GameInput — ชั้นเดียวที่รวมการอ่านจอย + เมาส์ (รวมเมาส์เสมือนจาก GamepadCursor)
//
//  • โค้ดคีย์บอร์ดเดิมของคุณไม่ต้องแตะ  แค่ต่อท้ายด้วย   || GameInput.PadXxx
//  • แทนที่ Input.mousePosition / Input.GetMouseButtonDown(0/1)
//    ด้วย GameInput.PointerPosition / PointerDown / PointerRightDown
//    (เพราะ Input.* แบบเก่า "มองไม่เห็น" เคอร์เซอร์ที่ขยับด้วยก้านขวา)
//
//  ต้องตั้ง Player Settings > Active Input Handling = Both
// =====================================================================
public static class GameInput
{
#if ENABLE_INPUT_SYSTEM
    static Gamepad Pad => Gamepad.current;

    public static bool PadConnected => Pad != null;

    // ---- ปุ่มหน้า (ชื่อแบบ Xbox / PlayStation = Cross, Circle, Square, Triangle) ----
    public static bool PadConfirmDown => Pad != null && Pad.buttonSouth.wasPressedThisFrame;   // A
    public static bool PadConfirmUp => Pad != null && Pad.buttonSouth.wasReleasedThisFrame;  // A (ปล่อย)
    public static bool PadBackDown => Pad != null && Pad.buttonEast.wasPressedThisFrame;    // B
    public static bool PadRemoveDown => Pad != null && Pad.buttonWest.wasPressedThisFrame;    // X  (ลบวัตถุดิบ / เก็บเฟอร์นิเจอร์)
    public static bool PadActionDown => Pad != null && Pad.buttonNorth.wasPressedThisFrame;   // Y  (ทอด / อัปเกรด)

    // ---- ไหล่ / เมนู ----
    public static bool PadPrevDown => Pad != null && Pad.leftShoulder.wasPressedThisFrame;  // LB (สมุด: ก่อนหน้า)
    public static bool PadNextDown => Pad != null && Pad.rightShoulder.wasPressedThisFrame; // RB (สมุด: ถัดไป / หมุนเฟอร์นิเจอร์)
    public static bool PadClearDown => Pad != null && Pad.leftShoulder.wasPressedThisFrame;  // LB (ครัว: ล้างทั้งหมด)
    public static bool PadStartDown => Pad != null && Pad.startButton.wasPressedThisFrame;   // Start (ครัว: Cook)
    public static bool PadEditModeDown => Pad != null && Pad.selectButton.wasPressedThisFrame;  // Select/View (= ปุ่ม B บนคีย์บอร์ด)

    // QTE choice 0/1/2 -> X / Y / B (ซ้ายไปขวาบนจอย)
    public static bool PadChoiceDown(int index)
    {
        if (Pad == null) return false;
        switch (index)
        {
            case 0: return Pad.buttonWest.wasPressedThisFrame;
            case 1: return Pad.buttonNorth.wasPressedThisFrame;
            case 2: return Pad.buttonEast.wasPressedThisFrame;
            default: return false;
        }
    }

    // D-pad (ถ้ากด) ไม่งั้นใช้ก้านซ้าย — ใช้เดินได้ทุกยี่ห้อจอย (ไม่พึ่ง Input Manager เก่า)
    public static Vector2 Navigate
    {
        get
        {
            if (Pad == null) return Vector2.zero;
            Vector2 d = Pad.dpad.ReadValue();
            return d.sqrMagnitude > 0.01f ? d : Pad.leftStick.ReadValue();
        }
    }

    // ---- Pointer: เมาส์จริง + เมาส์เสมือน (GamepadCursor ฉีดค่าเข้า Mouse.current) ----
    public static Vector2 PointerPosition
    {
        get
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }
    }

    public static bool PointerDown
    {
        get
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }
    }

    public static bool PointerRightDown
    {
        get
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(1);
#else
            return false;
#endif
        }
    }

#else
    public static bool PadConnected => false;
    public static bool PadConfirmDown => false;
    public static bool PadConfirmUp => false;
    public static bool PadBackDown => false;
    public static bool PadRemoveDown => false;
    public static bool PadActionDown => false;
    public static bool PadPrevDown => false;
    public static bool PadNextDown => false;
    public static bool PadClearDown => false;
    public static bool PadStartDown => false;
    public static bool PadEditModeDown => false;
    public static bool PadChoiceDown(int index) => false;
    public static Vector2 Navigate => Vector2.zero;
    public static Vector2 PointerPosition => Input.mousePosition;
    public static bool PointerDown => Input.GetMouseButtonDown(0);
    public static bool PointerRightDown => Input.GetMouseButtonDown(1);
#endif
}