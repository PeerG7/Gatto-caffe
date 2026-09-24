using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

    private static readonly KeyCode[] _choiceHotkeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3
    };

    private bool isHoldingViaKey = false;

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

    void HandleChoiceHotkey()
    {
        if (choiceIndex < _choiceHotkeys.Length)
        {
            if (Input.GetKeyDown(_choiceHotkeys[choiceIndex]))
                FireChoice();
        }
    }

    void FireChoice()
    {
        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.StartQTE(choiceIndex);
    }

    void HandleQTEInput()
    {
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mode == ButtonMode.QTEInput)
            SendPress();
        else
            FireChoice();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mode == ButtonMode.QTEInput)
            SendRelease();
    }

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

    public static void PlayMeowOnCurrentNPC()
    {
        NPCController npc = RelationshipManager.Instance?.GetCurrentNPC();
        if (npc == null) return;
        NPCInteract interact = npc.GetComponent<NPCInteract>();
        if (interact != null) interact.PlayMeow();
    }
}