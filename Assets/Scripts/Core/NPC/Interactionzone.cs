using UnityEngine;

// =====================================================================
// InteractionZone — Prompt Icon Handler based purely on Zone/Cat State
// =====================================================================
public class InteractionZone : MonoBehaviour
{
    [Tooltip("จุดยืนจริงของแมว (ต้องอยู่บน NavMesh) — ถ้าไม่ใส่จะใช้ transform ของตัวเองแทน")]
    public Transform point;

    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public NPCController currentNPC;

    [Header("Interaction & Placement Icons")]
    [Tooltip("ขึ้นตอนมีแมวนั่งอยู่ที่จุดนี้ — ใช้แสดงปุ่มกด เช่น [E] หรือ Icon กิจกรรม")]
    public GameObject catPresentIcon;

    [Tooltip("ขึ้นตอนผู้เล่นอุ้มแมวอยู่ และจุดนี้ว่าง — บอกตำแหน่งที่วางแมวได้")]
    public GameObject freeSpotIcon;

    [Tooltip("เปิด = ซ่อน Icon ตอนผู้เล่นถูกล็อก (กำลังเล่น QTE / อยู่ในคัตซีน) กัน Icon บัง UI")]
    public bool hideCatIconWhilePlayerLocked = true;

    private PlayerController2D player;
    private float nextFindPlayerTime = 0f;

    [Header("Icon Sorting")]
    [Tooltip("เปิด = ตั้ง Order in Layer ของ Icon ให้สูง เพื่อให้อยู่หน้าตัวแมว")]
    public bool forceIconOnTop = true;
    public string iconSortingLayer = "";
    public int iconSortingOrder = 500;

    [Header("Debug")]
    public bool debugLog = false;

    private bool catIconUsable = false;
    private bool freeIconUsable = false;
    private string lastDebugMsg = "";

    void Reset()
    {
        point = transform;
    }

    void Awake()
    {
        if (point == null) point = transform;
    }

    void Start()
    {
        catIconUsable = ValidateIcon(catPresentIcon, "Cat Present / Interaction Key Icon");
        freeIconUsable = ValidateIcon(freeSpotIcon, "Free Spot Icon");

        if (catIconUsable) { ApplyIconSorting(catPresentIcon); catPresentIcon.SetActive(false); }
        if (freeIconUsable) { ApplyIconSorting(freeSpotIcon); freeSpotIcon.SetActive(false); }
    }

    void ApplyIconSorting(GameObject icon)
    {
        if (!forceIconOnTop || icon == null) return;

        foreach (var sr in icon.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!string.IsNullOrEmpty(iconSortingLayer)) sr.sortingLayerName = iconSortingLayer;
            sr.sortingOrder = iconSortingOrder;
        }

        foreach (var cv in icon.GetComponentsInChildren<Canvas>(true))
        {
            if (cv.isRootCanvas && cv.renderMode != RenderMode.WorldSpace) continue;
            if (!cv.isRootCanvas) cv.overrideSorting = true;
            if (!string.IsNullOrEmpty(iconSortingLayer)) cv.sortingLayerName = iconSortingLayer;
            cv.sortingOrder = iconSortingOrder;
        }
    }

    bool ValidateIcon(GameObject icon, string label)
    {
        if (icon == null) return false;

        if (!icon.scene.IsValid())
        {
            Debug.LogWarning($"[InteractionZone] {name}: {label} เป็น Prefab ใน Project ให้ลากตัวใน Scene มาใส่แทน", this);
            return false;
        }

        if (transform.IsChildOf(icon.transform))
        {
            Debug.LogWarning($"[InteractionZone] {name}: {label} เป็น GameObject ของสคริปต์นี้เอง ให้สร้าง Child แยกต่างหาก", this);
            return false;
        }

        return true;
    }

    void Update()
    {
        bool showCatIcon = false;
        bool showFreeIcon = false;

        if (isOccupied && currentNPC != null)
        {
            // เมื่อแมวเดินถึงจุดและเข้าสู่สถานะพร้อมทำกิจกรรม -> เปิดแสดงปุ่ม Keybind ทันที (ไม่เช็คระยะผู้เล่น)
            bool seated = currentNPC.currentState == NPCController.NPCState.AtInteractionZone;
            bool hiddenByLock = hideCatIconWhilePlayerLocked && PlayerController2D.IsLocked;

            showCatIcon = seated && !hiddenByLock;
        }
        else if (!isOccupied)
        {
            // เมื่อผู้เล่นอุ้มแมวอยู่และจุดนี้ว่าง -> แสดง Icon จุดวางแมว
            PlayerController2D pc = GetPlayer();
            showFreeIcon = pc != null && pc.isHoldingCat;
        }

        if (catIconUsable) SetIcon(catPresentIcon, showCatIcon);
        if (freeIconUsable) SetIcon(freeSpotIcon, showFreeIcon);

        if (debugLog)
        {
            PlayerController2D dpc = GetPlayer();
            string npcInfo = currentNPC != null ? currentNPC.name + ":" + currentNPC.currentState : "none";
            string msg = $"occupied={isOccupied}, npc={npcInfo}, playerLocked={PlayerController2D.IsLocked}, " +
                         $"showCatIcon={showCatIcon}, showFreeIcon={showFreeIcon}";
            if (msg != lastDebugMsg)
            {
                lastDebugMsg = msg;
                Debug.Log($"[InteractionZone] {name}: {msg}", this);
            }
        }
    }

    PlayerController2D GetPlayer()
    {
        if (player == null && Time.unscaledTime >= nextFindPlayerTime)
        {
            player = FindObjectOfType<PlayerController2D>();
            nextFindPlayerTime = Time.unscaledTime + 1f;
        }
        return player;
    }

    static void SetIcon(GameObject icon, bool visible)
    {
        if (icon != null && icon.activeSelf != visible)
            icon.SetActive(visible);
    }

    public bool IsAvailable() => !isOccupied;

    public void Occupy(NPCController npc)
    {
        isOccupied = true;
        currentNPC = npc;
    }

    public void Release()
    {
        isOccupied = false;
        currentNPC = null;
    }

    void OnDrawGizmos()
    {
        Vector3 p = point != null ? point.position : transform.position;
        Gizmos.color = isOccupied ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawWireSphere(p, 0.3f);
        Gizmos.DrawLine(p + Vector3.up * 0.3f, p + Vector3.up * 0.6f);
    }
}