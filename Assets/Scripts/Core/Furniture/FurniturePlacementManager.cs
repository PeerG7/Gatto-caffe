using UnityEngine;
using UnityEngine.EventSystems;
using Unity.AI.Navigation;

public class FurniturePlacementManager : MonoBehaviour
{
    public static FurniturePlacementManager Instance;

    [Header("Settings")]
    public LayerMask interactableLayer;
    public float gridSize = 0.5f;

    [Header("UI Reference")]
    public GameObject computerShopCanvas;

    private GameObject currentGhost;
    private GameObject currentPrefab;
    private SpriteRenderer ghostRenderer;
    private Collider2D ghostCollider;
    private bool isBuildMode = false;
    private bool isValid = false;

    void Awake() => Instance = this;

    void Update()
    {
        if (!isBuildMode || currentGhost == null) return;

        // 1. ตำแหน่ง Snap เข้า Grid ตามเมาส์
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector3 snapPos = new Vector3(
            Mathf.Round(mousePos.x / gridSize) * gridSize,
            Mathf.Round(mousePos.y / gridSize) * gridSize,
            0f
        );
        currentGhost.transform.position = snapPos;

        // 2. ตรวจสอบพื้นที่วาง
        if (ghostCollider != null) ghostCollider.enabled = false;

        Collider2D overlap = Physics2D.OverlapBox(snapPos, new Vector2(gridSize * 0.7f, gridSize * 0.7f), 0f, interactableLayer);
        isValid = (overlap == null);

        if (ghostCollider != null) ghostCollider.enabled = true;

        // เปลี่ยนสี Ghost พรีวิว
        if (ghostRenderer != null)
        {
            ghostRenderer.color = isValid ? new Color(0, 1, 0, 0.6f) : new Color(1, 0, 0, 0.6f);
        }

        // 3. คลิกซ้ายวางวัตถุ
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (isValid)
            {
                Place();
            }
        }

        // ✅ ยกเลิกการวางด้วยการกด ESC เท่านั้น (เอาคลิกขวาออก เพื่อให้คลิกขวาลากเลื่อนกล้องได้)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }

        // กด R เพื่อหมุน
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentGhost.transform.Rotate(0, 0, 90f);
        }
    }

    public void StartPlacement(GameObject prefab)
    {
        Cancel();
        currentPrefab = prefab;
        isBuildMode = true;

        // ✅ เปิด Pan Mode และ Lock Player ไว้ต่อเนื่องขณะอยู่ในโหมดวาง
        PlayerController2D.SetLock(true);
        CameraController2D.SetPanMode(true);

        if (computerShopCanvas != null) computerShopCanvas.SetActive(false);

        currentGhost = Instantiate(prefab);

        var furnitureComp = currentGhost.GetComponent<FurnitureObject>();
        if (furnitureComp != null) Destroy(furnitureComp);

        Collider2D[] ghostColliders = currentGhost.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in ghostColliders)
        {
            col.enabled = false;
        }

        Canvas[] ghostCanvases = currentGhost.GetComponentsInChildren<Canvas>();
        foreach (Canvas c in ghostCanvases)
        {
            c.enabled = false;
        }

        ghostRenderer = currentGhost.GetComponentInChildren<SpriteRenderer>();
    }

    void Place()
    {
        Instantiate(currentPrefab, currentGhost.transform.position, currentGhost.transform.rotation);
        RebakeNavMesh();
        Cancel();
    }

    public void Cancel()
    {
        if (currentGhost != null) Destroy(currentGhost);
        currentGhost = null;
        isBuildMode = false;

        // ✅ คืนค่ากล้องกลับไปตาม Player และปลดล็อกเมื่อวางเสร็จ/ยกเลิกวาง
        PlayerController2D.SetLock(false);
        CameraController2D.SetPanMode(false);
    }

    public void RebakeNavMesh()
    {
        var surface = FindObjectOfType<NavMeshSurface>();
        if (surface != null) surface.BuildNavMesh();
    }
}