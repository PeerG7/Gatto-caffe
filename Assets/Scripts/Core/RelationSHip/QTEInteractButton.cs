using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// QTEInteractButton — v2 (Keyboard + Gamepad Added)
//
// ของเดิม: IPointerDownHandler / IPointerUpHandler (mouse/touch) ยังครบ
// เพิ่มใหม่: รับ input จาก keyboard/gamepad ผ่าน Update()
//
// Setup ใน Inspector:
//   - ไม่ต้องลบ Button component เดิม — ยังใช้ mouse click ได้
//   - ถ้าใช้ New Input System: ลาก InputActionReference
//     (action เดียวกับที่ใช้ใน PlayerInteract2D) มาใส่ช่อง qteAction
//   - ถ้าใช้ Legacy Input: ใส่ keyCode ที่ต้องการ (default = Space)
// =====================================================================
public class QTEInteractButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Legacy Fallback Key (ยังใช้งานได้ปกติ)")]
    [Tooltip("ปุ่ม keyboard สำหรับ QTE — default Space เพื่อไม่ชนกับ E (interact)")]
    public KeyCode qteKey = KeyCode.Space;

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System (optional)")]
    [Tooltip("ลาก InputActionReference เดียวกับ Interact Action ของ Player มาใส่")]
    public UnityEngine.InputSystem.InputActionReference qteAction;
#endif

    // ── track hold state สำหรับ keyboard/gamepad ──────────────────
    private bool isHoldingViaKey = false;

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (qteAction != null) qteAction.action.Enable();
    }

    void OnDisable()
    {
        if (qteAction != null) qteAction.action.Disable();
    }
#endif

    void Update()
    {
        // ── Legacy KeyCode ─────────────────────────────────────────
        HandleLegacyKey();

#if ENABLE_INPUT_SYSTEM
        // ── New Input System ───────────────────────────────────────
        HandleNewInputSystem();
#endif
    }

    void HandleLegacyKey()
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
    }

#if ENABLE_INPUT_SYSTEM
    void HandleNewInputSystem()
    {
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
    }
#endif

    // ── ของเดิม: pointer events (mouse / touch) ────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        SendPress();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SendRelease();
    }

    // ── shared helpers ─────────────────────────────────────────────
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

    // ── static helper (ของเดิม — ยังคงไว้) ────────────────────────
    public static void PlayMeowOnCurrentNPC()
    {
        NPCController npc = RelationshipManager.Instance?.GetCurrentNPC();
        if (npc == null) return;

        NPCInteract interact = npc.GetComponent<NPCInteract>();
        if (interact != null)
            interact.PlayMeow();
    }
}