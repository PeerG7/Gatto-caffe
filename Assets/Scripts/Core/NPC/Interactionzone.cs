using UnityEngine;

// =====================================================================
// InteractionZone — จุด (หรือโซน) ที่แมวจะเดินมายืน/นั่งเพื่อทำ Interaction
// แยกออกจากโต๊ะกินข้าว (CustomerTable.seatPoint) โดยเด็ดขาด
//
// Setup:
//   1. สร้าง Empty GameObject วางตำแหน่งในพื้นที่สีน้ำเงินตามภาพ
//      ต้องอยู่ "บน NavMesh" ที่ bake แล้ว (เดินไปถึงได้จริง)
//   2. ใส่ component นี้ (ถ้าไม่ลาก point ไว้ จะใช้ transform ตัวเองอัตโนมัติ)
//   3. ถ้าต้องการหลายจุด (รองรับแมวหลายตัวพร้อมกัน) ให้สร้างหลาย GameObject
//      แบบนี้ แล้วลากทุกตัวไปใส่ list ที่ InteractionZoneManager
//   4. (Icon) วางรูปที่ต้องการไว้บนจุดนี้เอง (เป็น Child GameObject อะไรก็ได้ เช่น Sprite/UI Image)
//      แล้วลากมาใส่ช่อง Cat Present Icon / Free Spot Icon — สคริปต์นี้แค่เปิด/ปิดให้ตามสถานะ
//        - Cat Present Icon : ขึ้นตอนมีแมวนั่งอยู่ที่จุดนี้
//        - Free Spot Icon   : ขึ้นตอนผู้เล่น "อุ้มแมวอยู่" และจุดนี้ว่าง (บอกว่าเอาไปวางตรงไหนได้)
//      ห้ามใช้ GameObject ที่แปะสคริปต์นี้เป็น Icon เอง (ไม่งั้นพอปิดแล้วสคริปต์จะหยุดทำงาน)
// =====================================================================
public class InteractionZone : MonoBehaviour
{
    [Tooltip("จุดยืนจริงของแมว (ต้องอยู่บน NavMesh) — ถ้าไม่ใส่จะใช้ transform ของตัวเองแทน")]
    public Transform point;

    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public NPCController currentNPC;

    [Header("Icons (ลากรูป/GameObject ที่วางไว้บนจุดนี้มาใส่เอง — ไม่ใส่ = ไม่แสดง)")]
    [Tooltip("ขึ้นตอนมีแมวนั่งอยู่ที่จุดนี้ (อยู่ในสถานะ AtInteractionZone)")]
    public GameObject catPresentIcon;

    [Tooltip("ขึ้นตอนผู้เล่นอุ้มแมวอยู่ และจุดนี้ว่าง — บอกตำแหน่งที่วางแมวได้")]
    public GameObject freeSpotIcon;

    [Tooltip("เปิด = ซ่อน Cat Present Icon ตอนผู้เล่นถูกล็อก (กำลังเลือกกิจกรรม/เล่น QTE/อนิเมชัน) กัน Icon บัง UI")]
    public bool hideCatIconWhilePlayerLocked = true;

    private PlayerController2D player;
    private float nextFindPlayerTime = 0f;

    [Header("Icon Sorting (กัน Icon ไปอยู่หลังตัวแมว/พื้น)")]
    [Tooltip("เปิด = ตั้ง Order in Layer ของ SpriteRenderer/Canvas ที่อยู่ใน Icon ให้สูง เพื่อให้อยู่หน้าตัวแมว")]
    public bool forceIconOnTop = true;
    [Tooltip("ไม่ว่าง = เปลี่ยน Sorting Layer ของ Icon เป็นชื่อนี้ / เว้นว่าง = ใช้ Layer เดิมของ Icon")]
    public string iconSortingLayer = "";
    public int iconSortingOrder = 500;

    [Header("Debug")]
    [Tooltip("เปิด = พิมพ์สถานะใน Console ทุกครั้งที่ค่าเปลี่ยน (ใช้หาสาเหตุที่ Icon ไม่ขึ้น)")]
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
        catIconUsable = ValidateIcon(catPresentIcon, "Cat Present Icon");
        freeIconUsable = ValidateIcon(freeSpotIcon, "Free Spot Icon");

        // จัด Sorting ให้ Icon อยู่หน้าตัวแมว แล้วซ่อนทั้งคู่ (กัน Icon ที่วางไว้ใน Scene โผล่ค้างตอนเริ่มเกม)
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
            // Canvas ราก แบบ Screen Space ไม่แตะ — เฉพาะ World Space หรือ Canvas ซ้อน
            if (cv.isRootCanvas && cv.renderMode != RenderMode.WorldSpace) continue;
            if (!cv.isRootCanvas) cv.overrideSorting = true;
            if (!string.IsNullOrEmpty(iconSortingLayer)) cv.sortingLayerName = iconSortingLayer;
            cv.sortingOrder = iconSortingOrder;
        }
    }

    // เช็กว่า Icon ที่ลากมาใส่ใช้งานได้จริงไหม — ถ้าไม่ได้จะไม่แตะมันเลย และแจ้งสาเหตุใน Console
    bool ValidateIcon(GameObject icon, string label)
    {
        if (icon == null) return false;

        // ลาก Prefab จาก Project window มาใส่ = ไม่ใช่ตัวใน Scene → เปิด/ปิดแล้วไม่เห็นอะไร
        if (!icon.scene.IsValid())
        {
            Debug.LogWarning($"[InteractionZone] {name}: {label} เป็น Prefab ใน Project ไม่ใช่ตัวที่อยู่ใน Scene — ให้ลากตัวที่วางอยู่ใน Hierarchy มาใส่แทน", this);
            return false;
        }

        // Icon คือ GameObject ของสคริปต์นี้เอง หรือเป็นแม่ของมัน → พอปิดแล้วสคริปต์จะหยุดทำงานทั้งหมด
        if (transform.IsChildOf(icon.transform))
        {
            Debug.LogWarning($"[InteractionZone] {name}: {label} เป็น GameObject ของสคริปต์นี้เอง (หรือตัวแม่) — ให้สร้าง Child แยกต่างหากแล้วลากมาใส่แทน", this);
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
            // มีแมวนั่งอยู่ที่จุดนี้จริง (ถึงจุดแล้ว) — ตอนแมวกำลังเดินมา/กำลัง Perform/ออกไปแล้ว จะไม่ขึ้น
            bool seated = currentNPC.currentState == NPCController.NPCState.AtInteractionZone;
            bool hiddenByLock = hideCatIconWhilePlayerLocked && PlayerController2D.IsLocked;
            showCatIcon = seated && !hiddenByLock;
        }
        else if (!isOccupied)
        {
            // จุดว่าง + ผู้เล่นกำลังอุ้มแมวอยู่ → บอกว่าวางตรงนี้ได้ (เงื่อนไขเดียวกับ PlayerInteract2D / TutorialPointerManager)
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
                         $"playerHoldingCat={(dpc != null && dpc.isHoldingCat)} → showCatIcon={showCatIcon} (usable={catIconUsable}), " +
                         $"showFreeIcon={showFreeIcon} (usable={freeIconUsable})";
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
            nextFindPlayerTime = Time.unscaledTime + 1f; // ไม่มีผู้เล่นใน Scene ก็ค้นแค่วินาทีละครั้ง
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

    // ช่วยให้เห็นตำแหน่งจุดชัดเจนใน Scene view ตอนแก้ level
    void OnDrawGizmos()
    {
        Vector3 p = point != null ? point.position : transform.position;
        Gizmos.color = isOccupied ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.6f, 1f);
        Gizmos.DrawWireSphere(p, 0.3f);
        Gizmos.DrawLine(p + Vector3.up * 0.3f, p + Vector3.up * 0.6f);
    }
}