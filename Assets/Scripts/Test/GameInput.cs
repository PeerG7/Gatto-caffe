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

    const float NavDeadzone = 0.2f;

    public static bool PadConnected => Pad != null;

    // สำรอง: ถ้า Input System ไม่เห็นจอยเป็น Gamepad แต่ Input แบบเก่าเห็น (เช่นจอย DirectInput ทั่วไป)
    // ใช้การจับคู่ปุ่มแบบ Xbox บน Windows: 0=A 1=B 2=X 3=Y 4=LB 5=RB 6=Back 7=Start
    static bool LegacyDown(int i)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Pad == null && Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i));
#else
        return false;
#endif
    }

    static bool LegacyUp(int i)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Pad == null && Input.GetKeyUp((KeyCode)((int)KeyCode.JoystickButton0 + i));
#else
        return false;
#endif
    }

    // ---- ปุ่มหน้า (ชื่อแบบ Xbox / PlayStation = Cross, Circle, Square, Triangle) ----
    public static bool PadConfirmDown => (Pad != null && Pad.buttonSouth.wasPressedThisFrame) || LegacyDown(0);   // A
    public static bool PadConfirmUp => (Pad != null && Pad.buttonSouth.wasReleasedThisFrame) || LegacyUp(0);  // A (ปล่อย)
    public static bool PadBackDown => (Pad != null && Pad.buttonEast.wasPressedThisFrame) || LegacyDown(1);    // B
    public static bool PadRemoveDown => (Pad != null && Pad.buttonWest.wasPressedThisFrame) || LegacyDown(2);    // X  (ลบวัตถุดิบ / เก็บเฟอร์นิเจอร์)
    public static bool PadActionDown => (Pad != null && Pad.buttonNorth.wasPressedThisFrame) || LegacyDown(3);   // Y  (ทอด / อัปเกรด)

    // ---- ไหล่ / เมนู ----
    public static bool PadPrevDown => (Pad != null && Pad.leftShoulder.wasPressedThisFrame) || LegacyDown(4);  // LB (สมุด: ก่อนหน้า)
    public static bool PadNextDown => (Pad != null && Pad.rightShoulder.wasPressedThisFrame) || LegacyDown(5); // RB (สมุด: ถัดไป / หมุนเฟอร์นิเจอร์)
    public static bool PadClearDown => (Pad != null && Pad.leftShoulder.wasPressedThisFrame) || LegacyDown(4);  // LB (ครัว: ล้างทั้งหมด)
    public static bool PadPauseDown => PadStartDown;   // Start = Esc บนคีย์บอร์ด (เมนู Pause)
    public static bool PadCookDown => (Pad != null && Pad.rightShoulder.wasPressedThisFrame) || LegacyDown(5);  // RB = Space/Enter (ครัว: Cook)
    public static bool PadStartDown => (Pad != null && Pad.startButton.wasPressedThisFrame) || LegacyDown(7);   // Start (ครัว: Cook)
    public static bool PadEditModeDown => (Pad != null && Pad.selectButton.wasPressedThisFrame) || LegacyDown(6);  // Select/View (= ปุ่ม B บนคีย์บอร์ด)

    // QTE choice 0/1/2 -> X / Y / B (ซ้ายไปขวาบนจอย)
    public static bool PadChoiceDown(int index)
    {
        if (Pad == null)
            return index == 0 ? LegacyDown(2) : index == 1 ? LegacyDown(3) : index == 2 && LegacyDown(1);
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
            if (d.sqrMagnitude > 0.01f) return d;                       // D-pad ก่อน
            Vector2 st = Pad.leftStick.ReadValue();
            return st.sqrMagnitude > NavDeadzone * NavDeadzone ? st : Vector2.zero;   // กันก้านซ้ายดริฟต์ทำให้ตัวละครเดินเอง
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

    /// <summary>A กำลังเป็น "คลิกซ้าย" อยู่ (เคอร์เซอร์จอยทำงาน + อยู่ในหน้า UI) → สคริปต์ที่อ่าน PointerDown อยู่แล้วไม่ต้องอ่าน A ซ้ำ</summary>
    public static bool AIsClick => GamepadCursor.IsPadMode && ClickMode;

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
    public static bool PadPauseDown => false;
    public static bool PadCookDown => false;
    public static bool PadEditModeDown => false;
    public static bool PadChoiceDown(int index) => false;
    public static Vector2 Navigate => Vector2.zero;
    public static Vector2 PointerPosition => Input.mousePosition;
    public static bool PointerDown => Input.GetMouseButtonDown(0);
    public static bool PointerRightDown => Input.GetMouseButtonDown(1);
    public static bool AIsClick => false;
#endif

    // =================================================================
    //  บริบท: ตอนนี้อยู่ "หน้า UI" หรือ "โลกปกติ"
    //    โลกปกติ  → A = E (โต้ตอบ)
    //    หน้า UI  → A = คลิกซ้ายตรงตำแหน่งเคอร์เซอร์ (เลือก / กดปุ่ม / วางเฟอร์นิเจอร์)
    // =================================================================
    /// <summary>เปิดไว้ = เวลากด A แล้วถูกบล็อกเพราะ "อยู่หน้า UI" จะ log สาเหตุใน Console (ปิดเมื่อแก้เสร็จ)</summary>
    public static bool DebugUIContext = true;

    /// <summary>คืนชื่อสาเหตุที่ทำให้ถือว่าอยู่หน้า UI (null = อยู่โลกปกติ)</summary>
    public static string UIContextReason()
    {
        if (UIContextTag.Count > 0) return "UIContextTag: " + UIContextTag.Describe();   // Canvas ที่แปะ UIContextTag อยู่
        if (PlayerController2D.IsLocked) return "PlayerController2D.IsLocked";            // Cooking / Drink / สมุด / ร้านคอม / แมว+QTE
        if (ScreenSmudgeManager.IsInterrupting) return "ScreenSmudgeManager.IsInterrupting";   // แมว/ซอสบังจอ
        if (ComputerStation.IsShopOpen) return "ComputerStation.IsShopOpen";
        if (FurnitureManager.Instance != null && FurnitureManager.Instance.isEditMode) return "FurnitureManager.isEditMode";
        return null;
    }

    public static bool UIContext => UIContextReason() != null;

    /// <summary>ปุ่ม QTE ใหญ่กำลังโชว์ → ตอนนี้ A = กดค้าง/รัว QTE (เหมือน Space) ไม่ใช่คลิก</summary>
    public static bool QTEButtonShown
    {
        get
        {
            CatSystemManager cat = CatSystemManager.Instance;
            return cat != null && cat.qteInteractButton != null && cat.qteInteractButton.activeInHierarchy;
        }
    }

    /// <summary>true = ตอนนี้ A ควรทำหน้าที่คลิกซ้าย</summary>
    public static bool ClickMode => UIContext && !QTEButtonShown;

    /// <summary>A = โต้ตอบ (เหมือน E) — ใช้ในโลกปกติเท่านั้น ในหน้า UI ปุ่ม A เป็นคลิกแทน</summary>
    private static int _lastDebugFrame = -1;

    public static bool PadInteractDown
    {
        get
        {
            if (!PadConfirmDown) return false;

            string reason = UIContextReason();
            if (reason == null) return true;

            if (DebugUIContext && _lastDebugFrame != Time.frameCount)
            {
                _lastDebugFrame = Time.frameCount;
                Debug.Log("[GameInput] กด A แล้วแต่ไม่ Interact เพราะเกมคิดว่าอยู่หน้า UI → " + reason);
            }
            return false;
        }
    }

    // =================================================================
    //  Esc / B ถูกใช้ปิดหน้าต่าง (หรือยกเลิกอะไรบางอย่าง) ไปแล้วในเฟรมนี้หรือยัง
    //  → เมนู Pause เช็กธงนี้ จะได้ไม่เปิดซ้ำจาก Esc ครั้งเดียวกัน
    // =================================================================
    private static int _backHandledFrame = -1;

    /// <summary>เรียกตอนใช้ Esc / B ปิดหน้าต่างหรือยกเลิกอะไรสักอย่าง</summary>
    public static void MarkBackHandled() { _backHandledFrame = Time.frameCount; }

    public static bool BackHandledThisFrame => _backHandledFrame == Time.frameCount;
}