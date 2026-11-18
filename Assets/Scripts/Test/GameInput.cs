using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// GameInput — gamepad layer for a mouse/keyboard project
//
// Your keyboard code stays exactly where it is. At each call site you only add
//     || GameInput.PadXxx
// All button mapping lives HERE, so remapping later is a one-file change.
// No InputActionReference / no Enable()/Disable() => nothing to wire in the
// Inspector and nothing that one screen can switch off for another.
//
// Requires Active Input Handling = "Both" (Player Settings), which your
// project already seems to use (Input.GetKeyDown + ENABLE_INPUT_SYSTEM).
// =====================================================================
public static class GameInput
{
#if ENABLE_INPUT_SYSTEM
    static Gamepad Pad => Gamepad.current;

    public static bool PadConnected => Pad != null;

    // ---- Face buttons (Xbox names; PlayStation = Cross / Circle / Square / Triangle) ----
    public static bool PadConfirmDown   => Pad != null && Pad.buttonSouth.wasPressedThisFrame;   // A
    public static bool PadConfirmUp     => Pad != null && Pad.buttonSouth.wasReleasedThisFrame;  // A (release, for QTE hold)
    public static bool PadBackDown      => Pad != null && Pad.buttonEast.wasPressedThisFrame;    // B
    public static bool PadRemoveDown    => Pad != null && Pad.buttonWest.wasPressedThisFrame;    // X  (remove last ingredient)
    public static bool PadActionDown    => Pad != null && Pad.buttonNorth.wasPressedThisFrame;   // Y  (deep fry / furniture upgrade)

    // ---- Shoulders / menu buttons ----
    public static bool PadPrevDown      => Pad != null && Pad.leftShoulder.wasPressedThisFrame;  // LB (book: previous)
    public static bool PadNextDown      => Pad != null && Pad.rightShoulder.wasPressedThisFrame; // RB (book: next)
    public static bool PadClearDown     => Pad != null && Pad.leftShoulder.wasPressedThisFrame;  // LB (cooking: clear all)
    public static bool PadStartDown     => Pad != null && Pad.startButton.wasPressedThisFrame;   // Start (cooking: Cook!)
    public static bool PadEditModeDown  => Pad != null && Pad.selectButton.wasPressedThisFrame;  // Select/View (furniture edit mode, = keyboard B)

    // QTEInteractButton choice mode: keys 1/2/3 -> X / Y / B (left-to-right on the pad)
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

    // D-pad wins over left stick when both are touched
    public static Vector2 Navigate
    {
        get
        {
            if (Pad == null) return Vector2.zero;
            Vector2 d = Pad.dpad.ReadValue();
            return d.sqrMagnitude > 0.01f ? d : Pad.leftStick.ReadValue();
        }
    }

    // ---- Pointer (replaces Input.mousePosition / Input.GetMouseButtonDown(0) in UI code) ----
    // Reads Mouse.current, which also follows a VirtualMouseInput cursor if you add one later.
    public static Vector2 PointerPosition =>
        Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

    public static bool PointerDown =>
        Input.GetMouseButtonDown(0) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

#else
    // Input System package not active: everything pad-related is simply "not pressed".
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
#endif
}
