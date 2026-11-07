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
    public Transform interactionZoneTarget;

    [Header("Animation Offset")]
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0);
    public float floatSpeed = 5f;
    public float floatHeight = 15f;

    [Header("Refresh")]
    [Tooltip("ความถี่ในการเช็กว่าควรชี้ที่ไหน/ควรซ่อนหรือไม่ (วินาที)")]
    public float refreshInterval = 0.25f;

    private Camera mainCamera;
    private Vector3 baseLocalPosition;
    private Transform _currentTarget;
    private float _refreshTimer = 0f;
    private CanvasGroup _selfCanvasGroup; // ใช้ซ่อนด้วย alpha ในกรณีที่สคริปต์นี้อยู่บน Canvas ของ Pointer เอง

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

        // 🟢 FIX: เช็กเป้าหมายเป็นระยะ และ "ซ่อน" Pointer เมื่อไม่มีเป้าหมายแล้ว (เช่น เควสจบ)
        // เดิม Update() จะ return ทันทีเมื่อไม่มีเป้าหมาย ทำให้ Pointer ค้างอยู่บนหน้าจอ
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = refreshInterval;
            RefreshTarget();
        }

        if (_currentTarget == null || !IsPointerVisible()) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || pointerRectTransform == null) return;

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(_currentTarget.position + worldOffset);

        if (screenPoint.z > 0)
        {
            float yOffset = Mathf.Sin(Time.unscaledTime * floatSpeed) * floatHeight;
            pointerRectTransform.position = screenPoint + new Vector3(0, yOffset, 0);
        }
    }

    Transform GetCurrentTargetTransform()
    {
        if (MainQuestManager.Instance == null) return null;

        var step = MainQuestManager.Instance.currentQuest;
        switch (step)
        {
            case MainQuestManager.QuestStep.BuyFurniture:
                return computerDeskTarget;

            case MainQuestManager.QuestStep.InviteCat1:
            case MainQuestManager.QuestStep.InviteCat2:
                return queueAreaTarget;

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
                return stoveTarget;

            case MainQuestManager.QuestStep.ServeFood:
                foreach (var table in FindObjectsOfType<CustomerTable>())
                {
                    if (table != null && table.HasAnyNPCWaiting())
                    {
                        return table.transform;
                    }
                }
                return servingTableTarget;

            case MainQuestManager.QuestStep.CarryCat2ToZone:
                if (interactionZoneTarget != null) return interactionZoneTarget;

                InteractionZone zone = FindObjectOfType<InteractionZone>();
                if (zone != null) return zone.transform;
                return null;

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
        RefreshTarget();
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