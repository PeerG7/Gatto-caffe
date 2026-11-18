using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// =====================================================================
// FurnitureEditUI — Dock คลังเฟอร์นิเจอร์ที่ขอบล่างจอ
//
// สถานะของกรอบ (DockState):
//   Hidden    : ซ่อนพ้นจอ (ไม่ได้อยู่ใน Edit Mode และไม่ใช่ช่วง Tutorial)
//   Peek      : โผล่ขึ้นมานิดเดียวที่ขอบล่างจอ (เห็นขอบกรอบ + ปุ่ม B)
//   HoverOpen : เอาเมาส์ไปชี้ที่ขอบล่าง → กรอบเด้งขึ้นมาชั่วคราว (เมาส์ออกก็กลับไป Peek)
//   Open      : เปิดค้างไว้ (คลิกซ้ายตอน HoverOpen / เปิด Edit Mode ด้วยปุ่ม B / วางเสร็จ)
//
// การทำงาน:
//   - Tutorial ขั้น PlaceFurniture : กรอบโผล่ Peek ให้เห็นปุ่ม B (คลิกที่กรอบ = เข้า Edit Mode เหมือนกดปุ่ม B)
//   - กด B เข้า Edit Mode           : กรอบเด้งขึ้นเปิดค้าง (Open)
//   - เลือกเฟอร์นิเจอร์             : กรอบหุบลงเป็น Peek เพื่อให้วางของได้เต็มจอ
//   - ชี้เมาส์ที่ขอบล่าง             : เด้งขึ้นมา (HoverOpen) → คลิกซ้าย 1 ครั้ง = เปิดค้าง (Open)
//   - ปิดกรอบที่เปิดค้างอยู่         : คลิกตรงไหนก็ได้ที่เป็นพื้นที่ว่างหรือกรอบ (ยกเว้นพื้นที่รายการเฟอร์นิเจอร์)
//   - คลิกที่ Dock ถูกนับเป็น "คลิกของ UI" → FurnitureManager จะไม่เอาไปวางเฟอร์นิเจอร์
//
// ✅ เพิ่มใหม่: กล้องค้างระยะไกลไว้ตลอด Edit Mode (เลือกโต๊ะ → วาง → เปิดกระเป๋าอีกรอบ)
//   แล้วค่อยซูมกลับตอนออกจาก Edit Mode จริง ๆ (ผ่าน CameraController2D.SetHoldZoom)
// =====================================================================
[DefaultExecutionOrder(-50)] // ให้ Update ของ Dock ทำงานก่อน FurnitureManager (ใช้ ClickConsumedThisFrame)
public class FurnitureEditUI : MonoBehaviour
{
    public static FurnitureEditUI Instance;

    public enum DockState { Hidden, Peek, HoverOpen, Open }

    [Header("UI Canvas & Panels")]
    public GameObject editUIPanel;             // Panel หลักของ Edit Mode (กรอบ Dock) — จะเปิดค้างไว้เสมอ แล้วเลื่อนเข้า/ออกจอแทน
    public Transform container;                // Content Transform ใน ScrollView
    public GameObject furnitureItemButtonPrefab; // Prefab ปุ่มไอเท็มในคลัง

    [Header("Scroll & Position Mechanics")]
    public ScrollRect furnitureScrollRect;     // ลาก ScrollRect ของ UI เข้ามาใส่
    public RectTransform uiPanelRect;          // ลาก RectTransform ของ Scroll View / Panel เข้ามาใส่
    [Tooltip("พื้นที่รายการเฟอร์นิเจอร์ที่ 'คลิกแล้วไม่ปิดกรอบ' — ว่าง = ใช้ Viewport ของ Scroll Rect")]
    public RectTransform inventoryArea;

    [Header("Button Guide Panel Mechanics (Top Center)")]
    public GameObject buttonGuidePanel;        // Panel กลุ่มรูปภาพและข้อความแนะนำปุ่มกด (วางไว้ตรงกลางด้านบน)
    public RectTransform buttonGuideRect;      // RectTransform ของ Button Guide Panel
    public float guideShownPosY = -30f;        // ตำแหน่ง Y ตอนโผล่มาแสดง
    public float guideHiddenPosY = 150f;       // ตำแหน่ง Y ตอนสไลด์ขึ้นไปซ่อนพ้นจอข้างบน

    [Header("Y Position Settings (Anchored Pos Y ของกรอบ)")]
    [Tooltip("Open: ตำแหน่ง Y ตอนเปิดแสดงคลังเต็ม")]
    public float normalPosY = -406f;
    [Tooltip("Peek: ตำแหน่ง Y ตอนหุบ — ให้เห็นแค่ขอบกรอบ + ปุ่ม B นิดเดียว (ค่าเดิมคือ -640)")]
    public float minimisedPosY = -640f;
    [Tooltip("Hidden: ซ่อนพ้นจอ = ตำแหน่ง Peek ลงไปอีกอย่างน้อยเท่านี้ (ใช้ความสูงกรอบถ้ามากกว่า)")]
    public float hiddenExtraDrop = 500f;

    [Header("Dock Behaviour")]
    [Tooltip("เปิด = ชี้เมาส์ที่ขอบล่างแล้วกรอบเด้งขึ้นมา (ต้องคลิกซ้ายเพื่อเปิดค้าง)")]
    public bool hoverToPeekOpen = true;
    [Tooltip("ความสูง (พิกเซลจากขอบล่างจอ) ของโซนที่เมาส์ชี้แล้วกรอบเด้งขึ้น")]
    public float hoverStripPixels = 60f;
    [Tooltip("เมาส์ออกจากกรอบแล้วรอกี่วินาทีก่อนหุบกลับ")]
    public float hoverCloseDelay = 0.2f;
    [Tooltip("เปิด = โชว์ Peek ตลอดเวลาแม้ไม่อยู่ใน Edit Mode | ปิด = โชว์เฉพาะช่วง Tutorial ขั้น PlaceFurniture")]
    public bool showPeekOutsideEditMode = false;
    [Tooltip("เปิด = คลิกที่ Peek (นอก Edit Mode) เพื่อเข้า Edit Mode เหมือนกดปุ่ม B")]
    public bool clickPeekToEnterEditMode = true;
    [Tooltip("เปิด = ปิดกรอบแล้วเวลาต้องเดินทันที ถ้ายังหยุดอยู่ (มี PauseGame ค้าง) จะสั่ง ForceResume ให้ | ปิด = แค่เตือนใน Console")]
    public bool forceTimeRunWhenClosed = true;

    [Header("Bounce (สปริงตอนเด้งขึ้น/ลง)")]
    [Tooltip("ยิ่งมากยิ่งเร็ว")]
    public float springStiffness = 220f;
    [Tooltip("ยิ่งน้อยยิ่งเด้งเยอะ (ประมาณ 12-24 กำลังดี)")]
    public float springDamping = 17f;

    public float moveSpeed = 12f;              // (ไม่ได้ใช้เมื่อเปิด guideFollowsDock)

    [Header("Button Guide Sync")]
    [Tooltip("เปิด = แผงด้านบนเลื่อน/เด้งตามกรอบล่างด้วยระยะเท่ากันเป๊ะ (Guide Shown Pos Y = ตำแหน่งตอนกรอบ Open) และเฟดเข้า/ออกแทนการเลื่อนหนี | ปิด = ใช้สปริงแยกแบบเดิม")]
    public bool guideFollowsDock = true;

    private DockState dockState = DockState.Hidden;
    private bool isEditActive = false;

    private float currentY;
    private float velY;
    private bool hasOverride = false;
    private float overrideY;

    private float hoverLeaveTimer = 0f;
    private bool hoverArmed = true;      // false = เพิ่งหุบ ต้องให้เมาส์ออกจากโซนก่อน ถึงจะเด้งขึ้นใหม่ (กันเด้งซ้ำหลังเลือกของ)
    private int clickConsumedFrame = -1;

    private float targetGuidePosY;
    private float guideY;
    private float guideVelY;
    private CanvasGroup guideGroup;
    private bool playerLockedByDock = false;
    private bool pausedByDock = false;
    private readonly Vector3[] corners = new Vector3[4];

    public DockState State => dockState;

    /// <summary>true = เฟรมนี้มีการคลิกซ้ายที่ Dock ใช้ไปแล้ว (เปิดค้าง/ปิดกรอบ/คลิกบนกรอบ) — ห้ามเอาไปวางเฟอร์นิเจอร์ซ้ำ</summary>
    public bool ClickConsumedThisFrame => clickConsumedFrame == Time.frameCount;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (uiPanelRect == null && editUIPanel != null)
            uiPanelRect = editUIPanel.GetComponent<RectTransform>();

        if (buttonGuideRect == null && buttonGuidePanel != null)
            buttonGuideRect = buttonGuidePanel.GetComponent<RectTransform>();

        if (buttonGuidePanel != null)
        {
            guideGroup = buttonGuidePanel.GetComponent<CanvasGroup>();
            if (guideGroup == null) guideGroup = buttonGuidePanel.AddComponent<CanvasGroup>();
        }

        targetGuidePosY = guideHiddenPosY;
        guideY = guideHiddenPosY;
        guideVelY = 0f;

        // กรอบต้องเปิดอยู่เสมอ (ใช้เลื่อนเข้า/ออกจอแทนการปิด) เพื่อให้โชว์ Peek ได้
        if (editUIPanel != null) editUIPanel.SetActive(true);
        if (furnitureScrollRect != null) furnitureScrollRect.gameObject.SetActive(true);

        ChangeState(DockState.Hidden);
        currentY = GetStateY(DockState.Hidden);
        ApplyPanelY();
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f); // Edit Mode เกมถูก Pause อยู่ จึงใช้เวลาจริง

        UpdateRestingState();
        HandleDockInput(dt);
        UpdateSpring(dt);
        UpdateGuide(dt);
    }

    // =====================================================================
    //  State
    // =====================================================================
    private bool WantsPeekOutsideEditMode()
    {
        // กรอบล่างโชว์ (Peek) ตลอดเวลา ยกเว้นตอนเปิดหน้าร้านคอมพิวเตอร์
        // (showPeekOutsideEditMode ไม่ได้ใช้แล้ว)
        return !ComputerStation.IsShopOpen;
    }

    // นอก Edit Mode: สลับระหว่าง Hidden / Peek ตามเงื่อนไข
    private void UpdateRestingState()
    {
        if (isEditActive) return;
        if (dockState != DockState.Hidden && dockState != DockState.Peek) return;

        DockState desired = WantsPeekOutsideEditMode() ? DockState.Peek : DockState.Hidden;
        if (desired != dockState) ChangeState(desired);
    }

    private void ChangeState(DockState newState)
    {
        dockState = newState;
        hasOverride = false;
        hoverLeaveTimer = 0f;

        if (newState == DockState.Peek || newState == DockState.Hidden)
            hoverArmed = false; // ต้องให้เมาส์ออกจากโซนก่อน ถึงจะเด้งขึ้นอีก

        // แผงแนะนำปุ่มด้านบน โชว์เฉพาะตอนกรอบเปิดอยู่
        SetButtonGuideActive(newState == DockState.Open || newState == DockState.HoverOpen);

        UpdatePlayerLock();
    }

    // ตอนกรอบ "เปิดค้าง" (Open) เท่านั้น: ล็อกผู้เล่น + กล้องโหมด Pan + หยุดเวลา
    // กรอบหุบเป็น Peek/Hidden เมื่อไหร่: ผู้เล่นขยับได้ + กล้องตามผู้เล่น + เวลาเดินต่อ
    private void UpdatePlayerLock()
    {
        bool shouldLock = isEditActive && dockState == DockState.Open;
        if (shouldLock == playerLockedByDock) return;

        playerLockedByDock = shouldLock;
        PlayerController2D.SetLock(shouldLock);
        if (CameraController2D.Instance != null)
            CameraController2D.SetPanMode(shouldLock);

        SetTimePaused(shouldLock);
    }

    // Pause/Resume แบบจับคู่กันเสมอ (ใช้ flag ของตัวเอง) กัน pauseCount ค้างจนเวลาไม่เดิน
    private void SetTimePaused(bool pause)
    {
        if (pause == pausedByDock) return;

        DayNightManager dn = DayNightManager.Instance;
        if (dn == null) return;

        pausedByDock = pause;
        if (pause)
        {
            dn.PauseGame();
        }
        else
        {
            dn.ResumeGame();

            // ตรวจว่าปิดกรอบแล้วเวลาเดินจริง — ถ้ายังหยุด แปลว่ามี PauseGame() ค้างอยู่ (ไม่ใช่ของกรอบนี้)
            // ช่วง Tutorial (เควสยังไม่ Completed) MainQuestManager จงใจ Pause ไว้ → ห้ามบังคับให้เวลาเดิน
            bool tutorialHoldsPause = MainQuestManager.Instance != null &&
                                      MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;

            if (dn.isPaused && !ComputerStation.IsShopOpen && !tutorialHoldsPause)
            {
                if (forceTimeRunWhenClosed)
                {
                    dn.ForceResume();
                    Debug.LogWarning("[FurnitureEditUI] ปิดกรอบแล้วแต่เวลายังหยุดค้าง (pauseCount เหลือจากที่อื่น) → สั่ง ForceResume ให้แล้ว");
                }
                else
                {
                    Debug.LogWarning("[FurnitureEditUI] ปิดกรอบแล้วแต่เวลายังหยุดอยู่ — มีระบบอื่นเรียก PauseGame() ค้างไว้");
                }
            }
        }
    }

    private void OnDisable()
    {
        // กันผู้เล่น/กล้อง/เวลาค้างถ้า UI นี้ถูกปิด/ทำลายตอนกรอบยังเปิดอยู่
        if (playerLockedByDock)
        {
            playerLockedByDock = false;
            PlayerController2D.SetLock(false);
            if (CameraController2D.Instance != null)
                CameraController2D.SetPanMode(false);
        }
        SetTimePaused(false);

        // ✅ เพิ่มใหม่: กันกล้องค้างระยะไกลถ้า UI ถูกปิดตอนยังอยู่ใน Edit Mode
        if (CameraController2D.Instance != null)
            CameraController2D.SetHoldZoom(false);
    }

    private float GetStateY(DockState state)
    {
        switch (state)
        {
            case DockState.Hidden:
                float drop = hiddenExtraDrop;
                if (uiPanelRect != null) drop = Mathf.Max(drop, uiPanelRect.rect.height);
                return minimisedPosY - drop;
            case DockState.Peek:
                return minimisedPosY;
            default:
                return normalPosY;
        }
    }

    // =====================================================================
    //  Input (Hover / Click)
    // =====================================================================
    private void HandleDockInput(float dt)
    {
        if (uiPanelRect == null) return;

        bool mouseDown = Input.GetMouseButtonDown(0);
        bool overPanel = IsPointerOverPanel();
        bool inZone = overPanel || IsPointerInBottomStrip();

        // ── นอก Edit Mode: คลิกที่ Peek = เข้า Edit Mode (เหมือนกดปุ่ม B) ──
        if (!isEditActive)
        {
            if (dockState == DockState.Peek && clickPeekToEnterEditMode && mouseDown && overPanel &&
                !ComputerStation.IsShopOpen && FurnitureManager.Instance != null)
            {
                clickConsumedFrame = Time.frameCount;
                FurnitureManager.Instance.ToggleEditMode(true);
            }
            return;
        }

        switch (dockState)
        {
            case DockState.Peek:
                if (!inZone)
                {
                    hoverArmed = true;
                }
                else if (mouseDown)
                {
                    clickConsumedFrame = Time.frameCount; // คลิกที่ขอบกรอบ ไม่เอาไปวางของ
                    if (hoverArmed) ChangeState(DockState.Open);
                }
                // (ไม่มี Hover เด้งขึ้นแล้ว — กรอบเปิดด้วยการคลิกเท่านั้น)
                break;

            case DockState.HoverOpen:
                if (mouseDown && overPanel)
                {
                    clickConsumedFrame = Time.frameCount;
                    ChangeState(DockState.Open); // คลิกซ้ายครั้งเดียว = เปิดค้าง
                }
                else if (inZone)
                {
                    hoverLeaveTimer = 0f;
                }
                else
                {
                    hoverLeaveTimer += dt;
                    if (hoverLeaveTimer >= hoverCloseDelay) ChangeState(DockState.Peek);
                }
                break;

            case DockState.Open:
                if (mouseDown)
                {
                    clickConsumedFrame = Time.frameCount;

                    // คลิกบนรายการเฟอร์นิเจอร์ = ใช้งานปกติ / คลิกที่อื่น (กรอบ, พื้นที่ว่าง) = ปิดกรอบ
                    // ปิดกรอบด้วยการคลิก = ออกจาก Edit Mode จริงๆ (isEditMode = false) เพื่อให้ทุกระบบรู้ว่าออกแล้ว
                    // ผู้เล่นขยับได้ กล้องตามผู้เล่น และเวลาเดินต่อ
                    // (ส่วนตอนเลือกเฟอร์นิเจอร์มาวาง กรอบหุบเป็น Peek แต่ยังอยู่ใน Edit Mode เพื่อให้วางของต่อได้)
                    if (!IsPointerOverInventoryArea())
                    {
                        if (FurnitureManager.Instance != null && FurnitureManager.Instance.isEditMode)
                            FurnitureManager.Instance.ToggleEditMode(false);
                        else
                            ChangeState(DockState.Peek);
                    }
                }
                break;
        }
    }

    private Camera GetUICamera()
    {
        if (uiPanelRect == null) return null;
        Canvas c = uiPanelRect.GetComponentInParent<Canvas>();
        if (c == null) return null;
        c = c.rootCanvas;
        return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
    }

    private bool IsPointerOverPanel()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(uiPanelRect, Input.mousePosition, GetUICamera());
    }

    // โซนแถบบางที่ขอบล่างจอ ตามแนวกว้างของกรอบ — ชี้ตรงนี้แล้วกรอบจะเด้งขึ้น
    private bool IsPointerInBottomStrip()
    {
        Vector2 m = Input.mousePosition;
        if (m.y < 0f || m.y > hoverStripPixels) return false;

        Camera cam = GetUICamera();
        uiPanelRect.GetWorldCorners(corners);
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            if (s.x < minX) minX = s.x;
            if (s.x > maxX) maxX = s.x;
        }
        return m.x >= minX && m.x <= maxX;
    }

    private bool IsPointerOverInventoryArea()
    {
        RectTransform area = inventoryArea;
        if (area == null && furnitureScrollRect != null)
            area = furnitureScrollRect.viewport != null ? furnitureScrollRect.viewport : furnitureScrollRect.GetComponent<RectTransform>();

        return area != null && RectTransformUtility.RectangleContainsScreenPoint(area, Input.mousePosition, GetUICamera());
    }

    // =====================================================================
    //  Motion
    // =====================================================================
    private void UpdateSpring(float dt)
    {
        if (uiPanelRect == null) return;

        float target = hasOverride ? overrideY : GetStateY(dockState);

        // สปริงแบบมี Overshoot นิดหน่อย = เด้ง
        float accel = (target - currentY) * springStiffness - velY * springDamping;
        velY += accel * dt;
        currentY += velY * dt;

        if (Mathf.Abs(target - currentY) < 0.05f && Mathf.Abs(velY) < 0.5f)
        {
            currentY = target;
            velY = 0f;
        }

        ApplyPanelY();
    }

    private void ApplyPanelY()
    {
        if (uiPanelRect == null) return;
        Vector2 p = uiPanelRect.anchoredPosition;
        p.y = currentY;
        uiPanelRect.anchoredPosition = p;
    }

    private void UpdateGuide(float dt)
    {
        if (buttonGuideRect == null) return;

        if (guideFollowsDock)
        {
            // ระยะเลื่อนเท่ากรอบล่างเป๊ะ → ความเร็ว + การเด้งเท่ากันทุกเฟรม
            Vector2 gp = buttonGuideRect.anchoredPosition;
            gp.y = guideShownPosY + (currentY - normalPosY);
            buttonGuideRect.anchoredPosition = gp;

            // โผล่/หายด้วยการเฟด ตามความคืบหน้าของกรอบล่าง (Peek/Hidden = 0, Open = 1)
            if (guideGroup != null)
            {
                float a = Mathf.InverseLerp(minimisedPosY, normalPosY, currentY);
                guideGroup.alpha = Mathf.Clamp01(a);
            }
            return;
        }

        // สปริงชุดเดียวกับกรอบล่าง (stiffness/damping เท่ากัน, dt เท่ากัน, เริ่มพร้อมกันตอนเปลี่ยน State)
        // → จังหวะ/จำนวนครั้งที่เด้งเหมือนกันเป๊ะ ต่างกันแค่ระยะทางที่ตั้งไว้ใน Inspector
        float accel = (targetGuidePosY - guideY) * springStiffness - guideVelY * springDamping;
        guideVelY += accel * dt;
        guideY += guideVelY * dt;

        if (Mathf.Abs(targetGuidePosY - guideY) < 0.05f && Mathf.Abs(guideVelY) < 0.5f)
        {
            guideY = targetGuidePosY;
            guideVelY = 0f;
        }

        Vector2 p = buttonGuideRect.anchoredPosition;
        p.y = guideY;
        buttonGuideRect.anchoredPosition = p;
    }

    // =====================================================================
    //  Public API (เดิม + ใหม่)
    // =====================================================================

    /// <summary>
    /// เปิด / ปิด UI โหมดแก้ไขร้าน (เรียกจาก FurnitureManager.ToggleEditMode)
    /// เปิด = กรอบเด้งขึ้นเปิดค้าง | ปิด = กลับไป Peek (ช่วง Tutorial) หรือซ่อน
    /// </summary>
    public void ToggleEditUI(bool active)
    {
        isEditActive = active;

        if (active)
        {
            // ✅ เพิ่มใหม่: ค้างกล้องไกลไว้ตลอด Edit Mode (เลือกโต๊ะ/วางของ ก็ยังไกลอยู่)
            CameraController2D.SetHoldZoom(true);

            RefreshUI();
            ChangeState(DockState.Open);
            ScrollToTop();
        }
        else
        {
            // ✅ เพิ่มใหม่: ออกจาก Edit Mode จริงแล้ว → ปล่อยให้กล้องซูมกลับระยะปกติ
            CameraController2D.SetHoldZoom(false);

            ChangeState(WantsPeekOutsideEditMode() ? DockState.Peek : DockState.Hidden);
        }
    }

    /// <summary>
    /// สั่งสไลด์แผงแนะนำปุ่มกดขึ้นหรือลง (โดย GameObject จะยังคง Active อยู่ตลอดเวลา)
    /// </summary>
    public void SetButtonGuideActive(bool show)
    {
        if (buttonGuidePanel == null) return;

        // ให้มั่นใจว่าตัว Panel เปิด Active ไว้เสมอไม่เคยปิด
        if (!buttonGuidePanel.activeSelf)
            buttonGuidePanel.SetActive(true);

        targetGuidePosY = show ? guideShownPosY : guideHiddenPosY;
    }

    public void RefreshUI()
    {
        if (container == null || furnitureItemButtonPrefab == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        if (FurnitureInventory.Instance != null)
        {
            List<GroupedFurnitureData> groupedItems = FurnitureInventory.Instance.GetGroupedInventory();

            foreach (var group in groupedItems)
            {
                if (group == null || group.itemData == null || group.instances.Count == 0) continue;

                GameObject btnObj = Instantiate(furnitureItemButtonPrefab, container);

                FurnitureSlotUI slotUI = btnObj.GetComponent<FurnitureSlotUI>();
                if (slotUI != null)
                {
                    slotUI.SetupSlot(group);
                }

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        OnSelectGroupedFurniture(group);
                    });
                }
            }
        }
    }

    public void OnSelectGroupedFurniture(GroupedFurnitureData group)
    {
        if (group == null || group.instances.Count == 0) return;

        // หุบกรอบลงเป็น Peek (เห็นแค่ขอบ + ปุ่ม B) เพื่อให้ผู้เล่นวางเฟอร์นิเจอร์ได้เต็มจอ
        // (แผงแนะนำด้านบนจะหนีขึ้นไปซ่อนเองตอนเปลี่ยนสถานะ)
        ChangeState(DockState.Peek);
        ScrollToBottom();

        FurnitureInstanceData instanceToPlace = group.instances[0];

        if (FurnitureManager.Instance != null && instanceToPlace != null)
        {
            FurnitureManager.Instance.StartPlacingFurnitureInstance(instanceToPlace);
        }
    }

    /// <summary>บังคับตำแหน่ง Y ของกรอบชั่วคราว (จะถูกยกเลิกเมื่อสถานะ Dock เปลี่ยน) — เก็บไว้เพื่อความเข้ากันได้กับโค้ดเดิม</summary>
    public void SetPanelPositionY(float targetY)
    {
        hasOverride = true;
        overrideY = targetY;
    }

    /// <summary>เรียกจาก FurnitureManager หลังวางเสร็จ/ยกเลิก → กรอบกลับมาเปิดค้าง (ถ้ายังอยู่ใน Edit Mode)</summary>
    public void RestorePanelPosition()
    {
        if (isEditActive) ChangeState(DockState.Open);
    }

    public void ScrollToBottom()
    {
        StartCoroutine(ScrollToBottomRoutine());
    }

    private IEnumerator ScrollToBottomRoutine()
    {
        yield return new WaitForEndOfFrame();

        if (furnitureScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            furnitureScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ScrollToTop()
    {
        if (furnitureScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            furnitureScrollRect.verticalNormalizedPosition = 1f;
        }
    }
}