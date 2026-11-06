using UnityEngine;
using UnityEngine.UI;

public class TutorialPointerManager : MonoBehaviour
{
    public static TutorialPointerManager Instance;

    [Header("UI References")]
    public GameObject pointerCanvasObject; // ลาก GameObject ของนิ้วชี้มาใส่ (หรือตัว Canvas)
    public RectTransform pointerRectTransform; // RectTransform ของรูปนิ้วชี้

    [Header("Target Stations (กำหนดตำแหน่งของสถานีต่างๆ ในฉาก)")]
    public Transform computerDeskTarget;    // สำหรับสเต็ป BuyFurniture (โต๊ะคอมพิวเตอร์)
    public Transform queueAreaTarget;       // สำหรับสเต็ป InviteCat1 / InviteCat2 (จุดยืนรอคิวหน้าร้าน)
    public Transform stoveTarget;           // สำหรับสเต็ป CookFood (เตาทำอาหาร)
    public Transform servingTableTarget;    // สำหรับสเต็ป ServeFood (โต๊ะอาหารที่มีแมวรอรับอาหาร)
    public Transform interactionZoneTarget; // 🌟 สำหรับสเต็ป CarryCat2ToZone (โซนสำหรับอุ้มแมวไปส่ง)

    [Header("Animation Offset")]
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0); // ให้ชี้ลอยเหนือหัววัตถุเล็กน้อย
    public float floatSpeed = 5f;
    public float floatHeight = 15f; // ขยับขึ้นลงนิดหน่อยให้ดูมีชีวิตชีวา

    private Camera mainCamera;
    private Vector3 baseLocalPosition;

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
        if (pointerCanvasObject == null || !pointerCanvasObject.activeSelf) return;

        Transform currentTarget = GetCurrentTargetTransform();
        if (currentTarget != null)
        {
            // แปลงพิกัด World Position ของ Station ให้เป็นพิกัดหน้าจอ (Screen Point)
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(currentTarget.position + worldOffset);

            // เช็คว่าเป้าหมายอยู่หน้ากล้องหรือไม่
            if (screenPoint.z > 0)
            {
                // ขยับนิ้วชี้ไปตามตำแหน่งจอ + ทำอนิเมชันขยับขึ้นลงเบาๆ
                float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
                pointerRectTransform.position = screenPoint + new Vector3(0, yOffset, 0);
            }
        }
    }

    Transform GetCurrentTargetTransform()
    {
        if (MainQuestManager.Instance == null) return null;

        // 🌟 ประกาศและดึงข้อมูลโต๊ะทั้งหมดไว้ตรงนี้ที่เดียว เพื่อให้ทุกเคสเรียกใช้ได้อย่างปลอดภัย
        CustomerTable[] allTables = FindObjectsOfType<CustomerTable>();

        var step = MainQuestManager.Instance.currentQuest;
        switch (step)
        {
            case MainQuestManager.QuestStep.BuyFurniture:
                return computerDeskTarget;

            case MainQuestManager.QuestStep.InviteCat1:
            case MainQuestManager.QuestStep.InviteCat2:
                return queueAreaTarget;

            case MainQuestManager.QuestStep.GuideCat2ToTable:
                // 🌟 ค้นหาโต๊ะที่ว่างและปลดล็อกแล้วในฉาก เพื่อให้นิ้วชี้ไปที่โต๊ะสำหรับพาแมวไปส่ง
                foreach (var table in allTables)
                {
                    if (table != null)
                    {
                        FurnitureObject furniture = table.GetComponent<FurnitureObject>();
                        if (furniture == null) furniture = table.GetComponentInChildren<FurnitureObject>();
                        bool isUnlocked = (furniture == null || furniture.isUnlocked);

                        if (isUnlocked && !table.isOccupied)
                        {
                            return table.transform; // นิ้วชี้จะพุ่งไปชี้โต๊ะที่ว่างทันที
                        }
                    }
                }
                return null;

            case MainQuestManager.QuestStep.CookFood:
                return stoveTarget;

            case MainQuestManager.QuestStep.ServeFood:
                // 🌟 ระบบอัจฉริยะ: ค้นหาโต๊ะที่ผู้เล่นวางเอง ที่มีแมวรอรับอาหารอยู่จริง ๆ ในฉาก
                foreach (var table in allTables)
                {
                    if (table != null && table.HasAnyNPCWaiting())
                    {
                        return table.transform; // นิ้วชี้จะวิ่งไปชี้โต๊ะตัวที่แมวกำลังรออาหารทันที!
                    }
                }
                return servingTableTarget;

            case MainQuestManager.QuestStep.CarryCat2ToZone:
                // 🌟 เมื่อถึงสเต็ปอุ้มแมวไปส่งโซน ให้ชี้ไปที่ Interaction Zone
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

    public void UpdatePointerState()
    {
        Transform target = GetCurrentTargetTransform();
        if (pointerCanvasObject != null)
        {
            pointerCanvasObject.SetActive(target != null);
        }
    }
}