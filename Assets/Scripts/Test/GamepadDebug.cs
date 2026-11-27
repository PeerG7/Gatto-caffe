using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

// =====================================================================
// GamepadDebug — กล่องข้อมูลบนจอ เอาไว้ดูว่า "เกมเห็นจอยเพื่อนหรือไม่ / เห็นปุ่มไหน"
//
//  • สร้างตัวเองตอนเริ่มเกม (ไม่ต้องวางใน Scene) ใช้ได้ทั้งใน Editor และ Build
//  • F9 = ซ่อน/แสดงกล่อง
//  • ทดสอบเสร็จแล้วลบไฟล์นี้ทิ้งได้เลย ไม่มีสคริปต์อื่นพึ่งพามัน
// =====================================================================
public class GamepadDebug : MonoBehaviour
{
    public bool show = true;

    private static GamepadDebug instance;
    private static readonly KeyCode[] allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    private int padAPresses;
    private int legacyAPresses;
    private int mouseClicks;
    private string lastKey = "-";

    private GUIStyle style;
    private readonly StringBuilder sb = new StringBuilder();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("GamepadDebug");
        DontDestroyOnLoad(go);
        go.AddComponent<GamepadDebug>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F9)) show = !show;
        if (Input.GetKeyDown(KeyCode.JoystickButton0)) legacyAPresses++;
        if (Input.GetMouseButtonDown(0)) mouseClicks++;

        foreach (KeyCode k in allKeys)
        {
            if (k == KeyCode.None) continue;
            if (Input.GetKeyDown(k)) lastKey = k.ToString();
        }
#endif
        if (GameInput.PadConfirmDown) padAPresses++;
    }

#if ENABLE_INPUT_SYSTEM
    static string P(ButtonControl b) => b != null && b.isPressed ? "●" : "○";
#endif

    void OnGUI()
    {
        if (!show) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = Mathf.Max(12, Screen.height / 55);
            style.normal.textColor = Color.white;
            style.wordWrap = false;
        }

        sb.Length = 0;
        sb.AppendLine("== GAMEPAD DEBUG (F9 ซ่อน) ==");

#if ENABLE_INPUT_SYSTEM
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            sb.AppendLine("Input System Gamepad: " + pad.displayName + "  [" + pad.layout + "]");
            sb.AppendLine("A:" + P(pad.buttonSouth) + " B:" + P(pad.buttonEast) + " X:" + P(pad.buttonWest) + " Y:" + P(pad.buttonNorth)
                        + "  LB:" + P(pad.leftShoulder) + " RB:" + P(pad.rightShoulder)
                        + "  LT:" + P(pad.leftTrigger) + " RT:" + P(pad.rightTrigger)
                        + "  Start:" + P(pad.startButton) + " Select:" + P(pad.selectButton));
            sb.AppendLine("L-stick " + pad.leftStick.ReadValue().ToString("F2") + "   R-stick " + pad.rightStick.ReadValue().ToString("F2"));
        }
        else
        {
            sb.AppendLine("Input System Gamepad: NONE  <-- Input System ไม่เห็นจอยเป็น Gamepad");
        }

        sb.Append("Input System devices: ");
        foreach (InputDevice d in InputSystem.devices) sb.Append(d.layout).Append(" | ");
        sb.AppendLine();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        string[] joys = Input.GetJoystickNames();
        sb.AppendLine("Legacy joysticks: " + (joys.Length == 0 ? "none" : string.Join(" | ", joys)));

        float hx = 0f, vy = 0f;
        try { hx = Input.GetAxisRaw("Horizontal"); vy = Input.GetAxisRaw("Vertical"); } catch (Exception) { }
        sb.AppendLine("Legacy Horizontal/Vertical: " + hx.ToString("F2") + ", " + vy.ToString("F2"));

        sb.Append("Legacy JoystickButton held: ");
        bool any = false;
        for (int i = 0; i < 12; i++)
        {
            if (Input.GetKey((KeyCode)((int)KeyCode.JoystickButton0 + i))) { sb.Append(i).Append(' '); any = true; }
        }
        sb.AppendLine(any ? "" : "-");

        sb.AppendLine("Mouse (legacy): " + Input.mousePosition.ToString("F0") + "  L:" + (Input.GetMouseButton(0) ? "●" : "○"));
        sb.AppendLine("Last keyboard/mouse key seen: " + lastKey);
#endif

        sb.AppendLine("A presses -> InputSystem/GameInput: " + padAPresses + " | legacy Joy0: " + legacyAPresses + " | mouse clicks: " + mouseClicks);

#if ENABLE_INPUT_SYSTEM
        sb.AppendLine("GamepadCursor: " + (GamepadCursor.Instance != null ? "running" : "NOT running") + "  padMode=" + GamepadCursor.IsPadMode);
#endif
        EventSystem es = EventSystem.current;
        string module = es != null && es.currentInputModule != null ? es.currentInputModule.GetType().Name : "none";
        sb.AppendLine("EventSystem module: " + module + "   Player locked: " + PlayerController2D.IsLocked);

        sb.AppendLine("Active device: " + InputDeviceMonitor.Current + "   Pad style: " + InputDeviceMonitor.Style
                      + "   Interact label: [" + InputGlyphs.Label(GameAction.Interact) + "]");

        sb.AppendLine("UI context: " + (GameInput.UIContext ? "YES" : "no") + "   UIContextTag active: " + UIContextTag.Count
                      + "   A = " + (GameInput.ClickMode ? "CLICK" : (GameInput.UIContext ? "QTE hold" : "Interact (E)")));

        string text = sb.ToString();
        Vector2 size = style.CalcSize(new GUIContent(text));
        GUI.Box(new Rect(8f, 8f, size.x + 16f, size.y + 12f), text, style);
    }
}