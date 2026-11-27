using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// InputDeviceMonitor — ตรวจว่า "ผู้เล่นใช้อุปกรณ์ไหนล่าสุด" แบบ Realtime
//
//   ขยับเมาส์ / กดคีย์บอร์ด  → InputScheme.KeyboardMouse
//   กดปุ่ม / ขยับก้านจอย     → InputScheme.Gamepad  (+ รู้ว่าเป็น Xbox / PlayStation / Nintendo)
//
//  • สร้างตัวเองตอนเริ่มเกม ไม่ต้องวางใน Scene
//  • ใช้ผ่านตัวแปร static:  InputDeviceMonitor.Current / .Style / .Changed (event)
//  • ปกติไม่ต้องเขียนโค้ดเอง: แปะ InputIconSwitcher (รูป) หรือ InputLabel (ข้อความ) บน UI ได้เลย
//
//  หมายเหตุ: เมาส์ตรวจจากระบบ Input เก่า (Input.mousePosition) เพราะเคอร์เซอร์จอยของ
//  GamepadCursor ฉีดค่าเข้า Mouse ของ Input System — ถ้าใช้ตัวนั้นตรวจจะนึกว่าเป็นเมาส์จริงตลอด
// =====================================================================
public enum InputScheme { KeyboardMouse, Gamepad }
public enum PadStyle { Xbox, PlayStation, Nintendo, Generic }

/// <summary>การกระทำในเกม — ใช้เลือกชื่อปุ่มที่จะแสดงให้ตรงกับอุปกรณ์</summary>
public enum GameAction { Interact, Upgrade, PickUp, EditMode, Rotate, Back, Click, Pause, Cook, Move }

[DefaultExecutionOrder(-32001)]
public class InputDeviceMonitor : MonoBehaviour
{
    public static InputScheme Current { get; private set; } = InputScheme.KeyboardMouse;
    public static PadStyle Style { get; private set; } = PadStyle.Xbox;
    public static bool UsingGamepad => Current == InputScheme.Gamepad;

    /// <summary>ยิงเมื่ออุปกรณ์เปลี่ยน (คีย์บอร์ด/เมาส์ ↔ จอย) หรือยี่ห้อจอยเปลี่ยน</summary>
    public static event Action Changed;

    [Tooltip("ก้านต้องเอียงเกินค่านี้ถึงนับว่า 'ขยับ' (กันก้านเอียงเอง)")]
    public float stickThreshold = 0.35f;
    [Tooltip("เมาส์ต้องขยับเกินกี่พิกเซลต่อเฟรมถึงนับว่า 'ขยับ' (ปรับเพิ่มเพื่อกันเมาส์สะเทือนดึงกลับจากจอย)")]
    public float mouseMovePixels = 12f;

    private static InputDeviceMonitor instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Current = InputScheme.KeyboardMouse;
        Style = PadStyle.Xbox;
        Changed = null;
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        new GameObject("InputDeviceMonitor").AddComponent<InputDeviceMonitor>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }

#if ENABLE_INPUT_SYSTEM
    private uint prevPadMask;
    private bool prevKeyHeld;
    private Vector3 lastMousePos;
    private bool mouseInit;

    void Update()
    {
        if (!Application.isFocused) return;   // ตอนสลับไปหน้าต่างอื่น ไม่นับ

        bool padFresh = PollGamepad();
        bool kmFresh = PollKeyboardMouse();

        // ถ้าสองอย่างเกิดพร้อมกันในเฟรมเดียว ให้จอยชนะ
        if (padFresh) SetScheme(InputScheme.Gamepad);
        else if (kmFresh) SetScheme(InputScheme.KeyboardMouse);

        if (Current == InputScheme.Gamepad) RefreshStyle();
    }

    bool PollGamepad()
    {
        Gamepad p = Gamepad.current;
        if (p == null) { prevPadMask = 0; return false; }

        uint m = 0;
        if (p.buttonSouth.isPressed) m |= 1u << 0;
        if (p.buttonEast.isPressed) m |= 1u << 1;
        if (p.buttonWest.isPressed) m |= 1u << 2;
        if (p.buttonNorth.isPressed) m |= 1u << 3;
        if (p.leftShoulder.isPressed) m |= 1u << 4;
        if (p.rightShoulder.isPressed) m |= 1u << 5;
        if (p.leftStickButton.isPressed) m |= 1u << 6;
        if (p.rightStickButton.isPressed) m |= 1u << 7;
        if (p.startButton.isPressed) m |= 1u << 8;
        if (p.selectButton.isPressed) m |= 1u << 9;
        if (p.dpad.up.isPressed) m |= 1u << 10;
        if (p.dpad.down.isPressed) m |= 1u << 11;
        if (p.dpad.left.isPressed) m |= 1u << 12;
        if (p.dpad.right.isPressed) m |= 1u << 13;
        if (p.leftTrigger.isPressed) m |= 1u << 14;
        if (p.rightTrigger.isPressed) m |= 1u << 15;

        float t2 = stickThreshold * stickThreshold;
        if (p.leftStick.ReadValue().sqrMagnitude > t2) m |= 1u << 16;
        if (p.rightStick.ReadValue().sqrMagnitude > t2) m |= 1u << 17;

        // "ใหม่" = มีบิตที่เพิ่งติดขึ้นมาเทียบกับเฟรมก่อน (ถือก้านค้างไว้ไม่นับซ้ำ)
        bool fresh = (m & ~prevPadMask) != 0;
        prevPadMask = m;
        return fresh;
    }

    bool PollKeyboardMouse()
    {
        bool active = false;

        Keyboard kb = Keyboard.current;
        bool held = kb != null && kb.anyKey.isPressed;
        if (kb != null && (kb.anyKey.wasPressedThisFrame || (held && !prevKeyHeld))) active = true;
        prevKeyHeld = held;

#if ENABLE_LEGACY_INPUT_MANAGER
        Vector3 mp = Input.mousePosition;
        if (mouseInit && (mp - lastMousePos).sqrMagnitude > mouseMovePixels * mouseMovePixels) active = true;
        lastMousePos = mp;
        mouseInit = true;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) active = true;
        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.01f) active = true;
#else
        Mouse mouse = Mouse.current;
        if (mouse != null && !GamepadCursor.IsPadMode)
        {
            if (mouse.delta.ReadValue().sqrMagnitude > mouseMovePixels * mouseMovePixels) active = true;
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) active = true;
        }
#endif
        return active;
    }

    void RefreshStyle()
    {
        Gamepad p = Gamepad.current;
        if (p == null) return;
        PadStyle s = DetectStyle(p);
        if (s == Style) return;
        Style = s;
        Changed?.Invoke();
    }

    static PadStyle DetectStyle(Gamepad p)
    {
        string d = p.description.product + " " + p.description.manufacturer;
        string s = (p.layout + " " + p.displayName + " " + d).ToLowerInvariant();

        if (s.Contains("dualshock") || s.Contains("dualsense") || s.Contains("playstation") || s.Contains("sony") || s.Contains("wireless controller"))
            return PadStyle.PlayStation;
        if (s.Contains("switch") || s.Contains("nintendo") || s.Contains("pro controller"))
            return PadStyle.Nintendo;
        if (s.Contains("xinput") || s.Contains("xbox") || s.Contains("microsoft"))
            return PadStyle.Xbox;
        return PadStyle.Generic;
    }
#endif

    static void SetScheme(InputScheme scheme)
    {
        if (Current == scheme) return;
        Current = scheme;
        Changed?.Invoke();
    }
}

// =====================================================================
// InputGlyphs — ตารางชื่อปุ่ม (เอาไปใช้ในโค้ดเองได้: InputGlyphs.Label(GameAction.Interact))
// ปุ่มจอยแมปตามที่ตั้งไว้ใน GameInput.cs (A=Interact, Y=Upgrade, X=PickUp, ...)
// Nintendo ปุ่มล่างติดป้าย "B" (ไม่ใช่ "A") ชื่อจึงต่างจาก Xbox ทั้งที่เป็นปุ่มตำแหน่งเดียวกัน
// =====================================================================
public static class InputGlyphs
{
    public static string Label(GameAction action)
        => Label(action, InputDeviceMonitor.Current, InputDeviceMonitor.Style);

    public static string Label(GameAction action, InputScheme scheme, PadStyle style)
    {
        if (scheme == InputScheme.KeyboardMouse)
        {
            switch (action)
            {
                case GameAction.Interact: return "E";
                case GameAction.Upgrade: return "F";
                case GameAction.PickUp: return "V";
                case GameAction.EditMode: return "B";
                case GameAction.Rotate: return "R";
                case GameAction.Back: return "Esc";
                case GameAction.Click: return "Click";
                case GameAction.Pause: return "Esc";
                case GameAction.Cook: return "Space";
                case GameAction.Move: return "WASD";
            }
            return "";
        }

        switch (style)
        {
            case PadStyle.PlayStation:
                switch (action)
                {
                    case GameAction.Interact: return "Cross";
                    case GameAction.Upgrade: return "Triangle";
                    case GameAction.PickUp: return "Square";
                    case GameAction.EditMode: return "Create";
                    case GameAction.Rotate: return "R1";
                    case GameAction.Back: return "Circle";
                    case GameAction.Click: return "Cross";
                    case GameAction.Pause: return "Options";
                    case GameAction.Cook: return "R1";
                    case GameAction.Move: return "Left Stick";
                }
                break;

            case PadStyle.Nintendo:
                switch (action)
                {
                    case GameAction.Interact: return "B";
                    case GameAction.Upgrade: return "X";
                    case GameAction.PickUp: return "Y";
                    case GameAction.EditMode: return "-";
                    case GameAction.Rotate: return "R";
                    case GameAction.Back: return "A";
                    case GameAction.Click: return "B";
                    case GameAction.Pause: return "+";
                    case GameAction.Cook: return "R";
                    case GameAction.Move: return "Left Stick";
                }
                break;

            default: // Xbox / Generic
                switch (action)
                {
                    case GameAction.Interact: return "A";
                    case GameAction.Upgrade: return "Y";
                    case GameAction.PickUp: return "X";
                    case GameAction.EditMode: return "View";
                    case GameAction.Rotate: return "RB";
                    case GameAction.Back: return "B";
                    case GameAction.Click: return "A";
                    case GameAction.Pause: return "Menu";
                    case GameAction.Cook: return "RB";
                    case GameAction.Move: return "Left Stick";
                }
                break;
        }
        return "";
    }
}