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

    private static readonly KeyCode[] _choiceHotkeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3
    };

    private bool isHoldingViaKey = false;

    private static List<QTEInteractButton> _activeChoiceButtons = new List<QTEInteractButton>();

    void OnEnable()
    {
        if (mode == ButtonMode.InteractChoice)
            _activeChoiceButtons.Add(this);
    }

    void OnDisable()
    {
        isHoldingViaKey = false;
        _activeChoiceButtons.Remove(this);
    }

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
            // 🎮 choice 0/1/2 = X / Y / B
            if (Input.GetKeyDown(_choiceHotkeys[choiceIndex]) || GameInput.PadChoiceDown(choiceIndex))
                FireChoice();
        }
    }

    void FireChoice()
    {
        // บอกมือ (QTEHandAnimator) ว่าเลือกปุ่มไหน — ส่งก่อน StartQTE เพื่อให้ Panel เปิดมาแล้วเล่นถูก pattern
        QTEHandAnimator.NotifyChoice(choiceIndex);

        if (CatSystemManager.Instance != null)
            CatSystemManager.Instance.StartQTE(choiceIndex);
    }

    void HandleQTEInput()
    {
        // 🎮 A = กดค้าง QTE (เหมือน Space) | RT คลิกปุ่มบนจอด้วยเคอร์เซอร์ก็ได้
        if (Input.GetKeyDown(qteKey) || GameInput.PadConfirmDown)
        {
            isHoldingViaKey = true;
            SendPress();
        }
        else if ((Input.GetKeyUp(qteKey) || GameInput.PadConfirmUp) && isHoldingViaKey)
        {
            isHoldingViaKey = false;
            SendRelease();
        }
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

        // ✅ Interact สำเร็จ — ร้อง meow + เสียงมีความสุข (VIP ใช้เสียงเฉพาะตัวถ้าตั้งไว้)
        NPCInteract interact = npc.GetComponent<NPCInteract>();
        if (interact != null)
        {
            interact.PlayMeow();
            interact.PlayHappy();
        }
        else if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayMeow();
            AudioManager.instance.Cathappy();
        }
    }
}