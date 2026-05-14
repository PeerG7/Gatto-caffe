using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// QTEInteractButton — v3 (Interact Choice Hotkeys + QTE Input)
//
// script นี้ทำงาน 2 หน้าที่ในหนึ่งเดียว:
//
// [หน้าที่ 1] เลือก QTE Type (ใส่บน 3 ปุ่มใน interactionCanvas)
//   - ตั้ง mode = InteractChoice
//   - ตั้ง choiceIndex: 0 = กด 1, 1 = กด 2, 2 = กด 3
//   - กด keyboard 1/2/3 → เรียก StartQTE(choiceIndex) อัตโนมัติ
//   - mouse click ยังทำงานปกติผ่าน Button component เดิม
//
// [หน้าที่ 2] QTE Input (ใส่บนปุ่มใน qteCanvasInPrefab)
//   - ตั้ง mode = QTEInput
//   - กด Space / A button → NotifyPress / NotifyRelease
//   - IPointerDownHandler / IPointerUpHandler ยังทำงานปกติ
//
// Setup ใน Inspector:
//   interactionCanvas ปุ่ม 1: mode = InteractChoice, choiceIndex = 0
//   interactionCanvas ปุ่ม 2: mode = InteractChoice, choiceIndex = 1
//   interactionCanvas ปุ่ม 3: mode = InteractChoice, choiceIndex = 2
//   qteCanvasInPrefab ปุ่ม:   mode = QTEInput
// =====================================================================
public class QTEInteractButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public enum ButtonMode { QTEInput, InteractChoice }

    [Header("Mode")]
    [Tooltip("QTEInput = ปุ่มกด QTE | InteractChoice = ปุ่มเลือก interact type")]
    public ButtonMode mode = ButtonMode.QTEInput;

    [Header("InteractChoice Settings (ใช้เมื่อ mode = InteractChoice)")]
    [Tooltip("0 = กด 1, 1 = กด 2, 2 = กด 3")]
    [Range(0, 2)]
    public int choiceIndex = 0;

    [Header("QTEInput Settings (ใช้เมื่อ mode = QTEInput)")]
    [Tooltip("ปุ่ม keyboard สำหรับ QTE — default Space")]
    public KeyCode qteKey = KeyCode.Space;

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System (optional)")]
    [Tooltip("ลาก InputActionReference (Interact Action) มาใส่")]
    public UnityEngine.InputSystem.InputActionReference qteAction;
#endif

    // ── hotkeys สำหรับ InteractChoice ─────────────────────────────
    private static readonly KeyCode[] _choiceHotkeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3
    };

    // ── track hold state ───────────────────────────────────────────
    private bool isHoldingViaKey = false;

    // ── ตัวติดตาม active choice buttons ───────────────────────────
    // เก็บ list ของ InteractChoice buttons ที่ active อยู่
    // เพื่อให้แต่ละตัว listen hotkey ได้โดยไม่ต้องมี manager กลาง
    private static List<QTEInteractButton> _activeChoiceButtons = new List<QTEInteractButton>();

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (qteAction != null) qteAction.action.Enable();
        if (mode == ButtonMode.InteractChoice)
            _activeChoiceButtons.Add(this);
    }

    void OnDisable()
    {
        if (qteAction != null) qteAction.action.Disable();
        _activeChoiceButtons.Remove(this);
    }
#else
    void OnEnable()
    {
        if (mode == ButtonMode.InteractChoice)
            _activeChoiceButtons.Add(this);
    }

    void OnDisable()
    {
        _activeChoiceButtons.Remove(this);
    }
#endif

    void Update()
    {
        if (mode == ButtonMode.InteractChoice)
            HandleChoiceHotkey();
        else
            HandleQTEInput();
    }

    // ── InteractChoice: กด 1/2/3 เลือก QTE type ──────────────────
    void HandleChoiceHotkey()
    {
        // ตรวจแค่ปุ่มที่ตรงกับ choiceIndex ของตัวเอง
        if (choiceIndex < _choiceHotkeys.Length)
        {
            if (Input.GetKeyDown(_choiceHotkeys[choiceIndex]))
                FireChoice();
        }
    }

    void FireChoice()
    {
        // เรียก StartQTE โดยตรง — ไม่ต้องผ่าน mouse click
        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.StartQTE(choiceIndex);
    }

    // ── QTEInput: กด/ปล่อย Space สำหรับ QTE ─────────────────────
    void HandleQTEInput()
    {
        // Legacy KeyCode
        if (Input.GetKeyDown(qteKey))
        {
            isHoldingViaKey = true;
            SendPress();
        }
        else if (Input.GetKeyUp(qteKey) && isHoldingViaKey)
        {
            isHoldingViaKey = false;
            SendRelease();
        }

#if ENABLE_INPUT_SYSTEM
        if (qteAction == null) return;

        if (qteAction.action.WasPressedThisFrame())
        {
            isHoldingViaKey = true;
            SendPress();
        }
        else if (qteAction.action.WasReleasedThisFrame() && isHoldingViaKey)
        {
            isHoldingViaKey = false;
            SendRelease();
        }
#endif
    }

    // ── IPointerDownHandler / IPointerUpHandler (ของเดิม) ─────────
    public void OnPointerDown(PointerEventData eventData)
    {
        if (mode == ButtonMode.QTEInput)
            SendPress();
        else
            FireChoice(); // กด mouse บน choice button
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mode == ButtonMode.QTEInput)
            SendRelease();
    }

    // ── helpers ────────────────────────────────────────────────────
    void SendPress()
    {
        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.NotifyPress();
    }

    void SendRelease()
    {
        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.NotifyRelease();
    }

    // ── static helper (ของเดิม) ────────────────────────────────────
    public static void PlayMeowOnCurrentNPC()
    {
        NPCController npc = RelationshipManager.Instance?.GetCurrentNPC();
        if (npc == null) return;
        NPCInteract interact = npc.GetComponent<NPCInteract>();
        if (interact != null) interact.PlayMeow();
    }
}