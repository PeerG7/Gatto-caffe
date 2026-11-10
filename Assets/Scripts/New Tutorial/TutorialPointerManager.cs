using UnityEngine;
using UnityEngine.UI;

public class TutorialPointerManager : MonoBehaviour
{
    public static TutorialPointerManager Instance;

    [Header("UI References")]
    public GameObject pointerCanvasObject;
    public RectTransform pointerRectTransform;

    [Header("Target Stations (กำหนดตำแหน่งของสถานีต่างๆ ในฉาก)")]
    public Transform computerDeskTarget;
    public Transform queueAreaTarget;
    public Transform stoveTarget;
    public Transform servingTableTarget;
    [Tooltip("(ไม่ใช้แล้ว) ระบบจะหา InteractionZone ที่ว่างให้เองอัตโนมัติ — เก็บช่องนี้ไว้เฉยๆ ไม่ต้องลากใส่")]
    public Transform interactionZoneTarget;

    [Header("Interaction Zone (ขั้นอุ้มแมวไปวาง)")]
    [Tooltip("true = ชี้ไปโซนว่างที่ใกล้ผู้เล่นที่สุด / false = ชี้โซนว่างตัวแรกใน list")]
    public bool pointToNearestFreeZone = true;

    [Header("Animation Offset")]
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0);
    public float floatSpeed = 5f;
    public float floatHeight = 15f;

    [Header("Refresh")]
    [Tooltip("ความถี่ในการเช็กว่าควรชี้ที่ไหน/ควรซ่อนหรือไม่ (วินาที)")]
    public float refreshInterval = 0.25f;

    [Header("Timing (เว้นช่วงก่อนโชว์ Icon)")]
    [Tooltip("หลังแมวมายืนรอที่คิวแล้ว ให้รออีกกี่วินาทีก่อนโชว์ Icon (0 = โชว์ทันที)")]
    public float queueArrivalDelay = 0.5f;
    [Tooltip("หลังแมวสั่งอาหารแล้ว ให้รออีกกี่วินาทีก่อนชี้ไปที่เตา (0 = โชว์ทันที)")]
    public float orderPlacedDelay = 0.5f;
    [Tooltip("ขั้น ServeFood: ตอนที่ผู้เล่นยังไม่ได้ถืออาหาร ให้ชี้ไปที่ servingTableTarget (จุดรับอาหาร) ไหม")]
    public bool pointToServingCounterWhenNotHolding = true;
    [Tooltip("ขั้น ServeFood: ตอนที่ผู้เล่นยังไม่ได้ถืออาหาร ให้ชี้ไปที่ TraySlot ที่มีอาหารวางอยู่จริง (ใกล้ผู้เล่นที่สุด) — ถ้าไม่มี Tray ไหนมีอาหาร จะ fallback ไป servingTableTarget")]
    public bool pointToTrayWithFood = true;

    [Header("Carry Cat (ขั้นอุ้มแมว)")]
    [Tooltip("ระยะยกเพิ่มจาก worldOffset ตอนชี้ที่ตัวแมว (ปรับให้ Icon ไม่บังหัวแมว)")]
    public Vector3 carryCatExtraOffset = new Vector3(0, 0.5f, 0);

    private Camera mainCamera;
    private Vector3 baseLocalPosition;
    private Transform _currentTarget;
    private float _refreshTimer = 0f;
    private CanvasGroup _selfCanvasGroup; // ใช้ซ่อนด้วย alpha ในกรณีที่สคริปต์นี้อยู่บน Canvas ของ Pointer เอง

    // เวลาที่เริ่มเห็นแมวยืนรอที่คิว (-1 = ยังไม่มีแมวรอ)
    private float _queueReadySince = -1f;
    // เวลาที่เริ่มเห็นแมวสั่งอาหารแล้ว (-1 = ยังไม่มีแมวสั่ง)
    private float _orderReadySince = -1f;

    private Vector3 _currentExtraOffset = Vector3.zero; // offset เพิ่มเฉพาะเป้าหมายปัจจุบัน (เช่น ตัวแมว)
    private PlayerInventory _playerInv;
    private PlayerController2D _playerCtrl;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (pointerRectTransform != null)
        {
            baseLocalPosition = pointerRectTransform.localPosition;
        }
        UpdatePointerState();
    }

    void Update()
    {
        if (pointerCanvasObject == null) return;

        // เช็กเป้าหมายเป็นระยะ และซ่อน Pointer เมื่อยังไม่ถึงเวลา / ไม่มีเป้าหมาย
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = refreshInterval;
            RefreshTarget();
        }

        UpdatePointerPosition();
    }

    void UpdatePointerPosition()
    {
        if (_currentTarget == null || !IsPointerVisible()) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || pointerRectTransform == null) return;

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(_currentTarget.position + worldOffset + _currentExtraOffset);

        if (screenPoint.z > 0)
        {
            float yOffset = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatHeight;
            pointerRectTransform.position = screenPoint + new Vector3(0, yOffset, 0);
        }
    }

    // 🟢 เช็กว่ามีแมวยืนรอที่คิวแล้ว (และรอครบ delay) หรือยัง
    bool IsQueueReady()
    {
        bool hasCat = QueueManager.Instance != null && QueueManager.Instance.HasNPCWaitingInQueue();

        if (!hasCat)
        {
            _queueReadySince = -1f; // รีเซ็ต ถ้าแมวหายไปแล้วมาใหม่จะนับ delay ใหม่
            return false;
        }

        if (_queueReadySince < 0f) _queueReadySince = Time.unscaledTime;
        return (Time.unscaledTime - _queueReadySince) >= queueArrivalDelay;
    }

    // 🟢 เช็กว่ามีแมวที่โต๊ะสั่งอาหารแล้ว (Order Icon โผล่แล้ว) และรอครบ delay หรือยัง
    bool IsOrderReady()
    {
        bool hasOrder = false;
        foreach (var table in FindObjectsOfType<CustomerTable>())
        {
            if (table != null && table.HasAnyNPCOrdered())
            {
                hasOrder = true;
                break;
            }
        }

        if (!hasOrder)
        {
            _orderReadySince = -1f;
            return false;
        }

        if (_orderReadySince < 0f) _orderReadySince = Time.unscaledTime;
        return (Time.unscaledTime - _orderReadySince) >= orderPlacedDelay;
    }

    // ── Player / Cat helpers ───────────────────────────────────────
    PlayerInventory GetPlayerInventory()
    {
        if (_playerInv == null) _playerInv = FindObjectOfType<PlayerInventory>();
        return _playerInv;
    }

    PlayerController2D GetPlayerController()
    {
        if (_playerCtrl == null) _playerCtrl = FindObjectOfType<PlayerController2D>();
        return _playerCtrl;
    }

    // หาโต๊ะที่แมวสั่งอาหารตรงกับที่ผู้เล่นถืออยู่
    Transform FindServeTableForHeldItem(string heldItem)
    {
        foreach (var table in FindObjectsOfType<CustomerTable>())
        {
            if (table != null && table.CanServeItem(heldItem))
                return table.transform;
        }
        return null;
    }

    // ✅ หา TraySlot ที่มีอาหารวางอยู่จริง (เลือกตัวที่ใกล้ผู้เล่นที่สุด)
    Transform FindTrayWithFood()
    {
        PlayerController2D pc = GetPlayerController();
        TraySlot best = null;
        float bestDist = Mathf.Infinity;

        foreach (var slot in FindObjectsOfType<TraySlot>())
        {
            if (slot == null || !slot.HasFood()) continue;

            float d = pc != null
                ? Vector2.Distance(pc.transform.position, slot.transform.position)
                : 0f;
            if (d < bestDist) { bestDist = d; best = slot; }
        }
        return best != null ? best.transform : null;
    }

    // หาแมวที่กินเสร็จแล้วและรอให้อุ้ม (เลือกตัวที่ใกล้ผู้เล่นที่สุด)
    Transform FindCatToCarry()
    {
        PlayerController2D pc = GetPlayerController();
        NPCController best = null;
        float bestDist = Mathf.Infinity;

        foreach (var npc in FindObjectsOfType<NPCController>())
        {
            if (npc == null) continue;
            if (!npc.hasFinishedEating || !npc.needsToBeCarried) continue;
            if (npc.currentState != NPCController.NPCState.Sitting) continue;

            float d = pc != null
                ? Vector2.Distance(pc.transform.position, npc.transform.position)
                : 0f;
            if (d < bestDist) { bestDist = d; best = npc; }
        }
        return best != null ? best.transform : null;
    }

    // ✅ FIX: หา InteractionZone ที่ "ว่างจริง" เอง (ไม่ล็อกที่จุดเดียวอีกต่อไป)
    // ทุกโซนเต็ม → return null (Icon จะซ่อนจนกว่าจะมีโซนว่าง)
    Transform GetInteractionZoneTransform()
    {
        // รวบรวมโซนทั้งหมด: ใช้ list ของ Manager ก่อน ถ้าไม่มีก็ค้นหาในซีนเอง
        InteractionZone[] allZones;
        if (InteractionZoneManager.Instance != null && InteractionZoneManager.Instance.zones.Count > 0)
            allZones = InteractionZoneManager.Instance.zones.ToArray();
        else
            allZones = FindObjectsOfType<InteractionZone>();

        PlayerController2D pc = pointToNearestFreeZone ? GetPlayerController() : null;

        InteractionZone best = null;
        float bestDist = Mathf.Infinity;

        foreach (var zone in allZones)
        {
            if (zone == null || !zone.IsAvailable()) continue;

            // ไม่ต้องการเรียงตามระยะ → เอาโซนว่างตัวแรกเลย
            if (pc == null) { best = zone; break; }

            Vector3 zp = zone.point != null ? zone.point.position : zone.transform.position;
            float d = Vector2.Distance(pc.transform.position, zp);
            if (d < bestDist) { bestDist = d; best = zone; }
        }

        if (best == null) return null;
        return best.point != null ? best.point : best.transform;
    }

    Transform GetCurrentTargetTransform()
    {
        _currentExtraOffset = Vector3.zero;

        if (MainQuestManager.Instance == null) return null;

        var step = MainQuestManager.Instance.currentQuest;

        // ออกจากขั้นเชิญแมวแล้ว → รีเซ็ตตัวนับ delay
        if (step != MainQuestManager.QuestStep.InviteCat1 &&
            step != MainQuestManager.QuestStep.InviteCat2)
            _queueReadySince = -1f;

        // ออกจากขั้นทำอาหารแล้ว → รีเซ็ตตัวนับ delay
        if (step != MainQuestManager.QuestStep.CookFood)
            _orderReadySince = -1f;

        switch (step)
        {
            case MainQuestManager.QuestStep.BuyFurniture:
                return computerDeskTarget;

            case MainQuestManager.QuestStep.InviteCat1:
            case MainQuestManager.QuestStep.InviteCat2:
                // 🟢 ชี้ที่หน้าร้านก็ต่อเมื่อมีแมวมายืนรอที่คิวแล้วเท่านั้น
                return IsQueueReady() ? queueAreaTarget : null;

            case MainQuestManager.QuestStep.GuideCat2ToTable:
                // ค้นหาโต๊ะที่ว่างและปลดล็อกแล้วให้นิ้วชี้ไปชี้
                foreach (var table in FindObjectsOfType<CustomerTable>())
                {
                    if (table != null)
                    {
                        FurnitureObject furniture = table.GetComponent<FurnitureObject>();
                        if (furniture == null) furniture = table.GetComponentInChildren<FurnitureObject>();
                        bool isUnlocked = (furniture == null || furniture.isUnlocked);

                        if (isUnlocked && !table.isOccupied)
                        {
                            return table.transform;
                        }
                    }
                }
                return null;

            case MainQuestManager.QuestStep.CookFood:
                // 🟢 FIX: ชี้ที่เตาก็ต่อเมื่อแมวนั่งโต๊ะและ "สั่งอาหารแล้ว" เท่านั้น
                return IsOrderReady() ? stoveTarget : null;

            case MainQuestManager.QuestStep.ServeFood:
                {
                    // 🟢 FIX: เช็กว่าผู้เล่นถืออะไรอยู่ แล้วชี้ไปโต๊ะที่แมวสั่งอาหารนั้นจริงๆ
                    PlayerInventory inv = GetPlayerInventory();
                    if (inv != null && inv.HasItem())
                        return FindServeTableForHeldItem(inv.GetCurrentItemName()); // ถืออาหารผิด = ไม่ชี้

                    // ✅ ยังไม่ได้ถืออาหาร → ชี้ไป TraySlot ที่มีอาหารวางอยู่จริงก่อน
                    if (pointToTrayWithFood)
                    {
                        Transform tray = FindTrayWithFood();
                        if (tray != null) return tray;
                    }

                    // ไม่มี Tray ไหนมีอาหาร → ชี้ไปจุดรับอาหารทั่วไป (ถ้าเปิดไว้)
                    return pointToServingCounterWhenNotHolding ? servingTableTarget : null;
                }

            case MainQuestManager.QuestStep.CarryCat2ToZone:
                {
                    // 🟢 อุ้มแมวอยู่จริง → ชี้ไป InteractionZone ที่ "ว่าง"
                    PlayerController2D pc = GetPlayerController();
                    if (pc != null && pc.isHoldingCat)
                        return GetInteractionZoneTransform();

                    // ยังไม่ได้อุ้ม → ชี้ที่ตัวแมวที่ต้องการให้อุ้ม (ถ้าแมวยังกินไม่เสร็จ/ยังไม่ขอให้อุ้ม = ไม่โชว์)
                    Transform cat = FindCatToCarry();
                    if (cat != null) _currentExtraOffset = carryCatExtraOffset;
                    return cat;
                }

            case MainQuestManager.QuestStep.Completed:
                return null;
            default:
                return null;
        }
    }

    void RefreshTarget()
    {
        _currentTarget = GetCurrentTargetTransform();
        SetPointerVisible(_currentTarget != null);
    }

    /// <summary>
    /// เรียกทันทีเมื่อเควสเปลี่ยนขั้น (MainQuestManager.AdvanceQuest เรียกให้)
    /// </summary>
    public void UpdatePointerState()
    {
        _refreshTimer = refreshInterval;
        _queueReadySince = -1f; // เริ่มนับ delay ใหม่ทุกครั้งที่เปลี่ยนขั้น
        _orderReadySince = -1f;
        RefreshTarget();
        UpdatePointerPosition(); // กัน Icon แวบที่ตำแหน่งเก่า
    }

    // ── Visibility helpers ─────────────────────────────────────────
    bool IsPointerVisible()
    {
        if (pointerCanvasObject == null) return false;
        if (_selfCanvasGroup != null) return _selfCanvasGroup.alpha > 0f;
        return pointerCanvasObject.activeSelf;
    }

    void SetPointerVisible(bool visible)
    {
        if (pointerCanvasObject == null) return;

        // ถ้าสคริปต์นี้อยู่บน/ใต้ Canvas ของ Pointer เอง การ SetActive(false) จะทำให้ Update() หยุด
        // และ Pointer จะไม่กลับมาแสดงอีก → ใช้ CanvasGroup alpha แทน
        bool selfInside = transform.IsChildOf(pointerCanvasObject.transform);
        if (selfInside)
        {
            if (_selfCanvasGroup == null)
            {
                _selfCanvasGroup = pointerCanvasObject.GetComponent<CanvasGroup>();
                if (_selfCanvasGroup == null) _selfCanvasGroup = pointerCanvasObject.AddComponent<CanvasGroup>();
            }
            _selfCanvasGroup.alpha = visible ? 1f : 0f;
            return;
        }

        if (pointerCanvasObject.activeSelf != visible)
            pointerCanvasObject.SetActive(visible);
    }
}