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

    private FurnitureObject selectedFurniture;

    void Awake() => Instance = this;

    void Update()
    {
        if (!isBuildMode || currentGhost == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector3 snapPos = new Vector3(
            Mathf.Round(mousePos.x / gridSize) * gridSize,
            Mathf.Round(mousePos.y / gridSize) * gridSize,
            0f
        );
        currentGhost.transform.position = snapPos;

        if (ghostCollider != null) ghostCollider.enabled = false;

        string myFurnitureID = PlacementBlocker.GetFurnitureID(currentPrefab);

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(snapPos, new Vector2(gridSize * 0.7f, gridSize * 0.7f), 0f, interactableLayer);
        isValid = true;
        foreach (Collider2D col in overlaps)
        {
            if (col == null) continue;

            PlacementBlocker blocker = col.GetComponentInParent<PlacementBlocker>();
            if (blocker != null && !blocker.Blocks(myFurnitureID)) continue;

            isValid = false;
            break;
        }

        if (ghostCollider != null) ghostCollider.enabled = true;

        if (ghostRenderer != null)
        {
            ghostRenderer.color = isValid ? new Color(0, 1, 0, 0.6f) : new Color(1, 0, 0, 0.6f);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (isValid)
            {
                Place();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }

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
        AudioManager.instance?.PlayPlaceFurniture();
        UINotificationManager.Instance?.ShowNotification("NOTI_FURNITURE_PLACED");
        RebakeNavMesh();
        Cancel();
    }

    public void Cancel()
    {
        if (currentGhost != null) Destroy(currentGhost);
        currentGhost = null;
        isBuildMode = false;
        selectedFurniture = null;

        PlayerController2D.SetLock(false);
        CameraController2D.SetPanMode(false);
    }

    public void RebakeNavMesh()
    {
        var surface = FindObjectOfType<NavMeshSurface>();
        if (surface != null) surface.BuildNavMesh();
    }

    public void SelectPlacedFurniture(FurnitureObject furniture)
    {
        if (isBuildMode) return;

        selectedFurniture = furniture;
        Debug.Log($"เลือกเฟอร์นิเจอร์: {furniture.furnitureID}");
    }

    public void MoveSelectedFurniture()
    {
        if (selectedFurniture == null) return;

        GameObject prefabToMove = selectedFurniture.furniturePrefab != null ? selectedFurniture.furniturePrefab : selectedFurniture.gameObject;

        Destroy(selectedFurniture.gameObject);
        selectedFurniture = null;
        RebakeNavMesh();

        StartPlacement(prefabToMove);
    }

    public void StoreSelectedFurniture()
    {
        if (selectedFurniture == null) return;

        AudioManager.instance?.PlayCollectFurniture();
        UINotificationManager.Instance?.ShowNotification("NOTI_FURNITURE_STORED");
        Debug.Log($"เก็บ {selectedFurniture.furnitureID} เรียบร้อยแล้ว");

        Destroy(selectedFurniture.gameObject);
        selectedFurniture = null;
        RebakeNavMesh();
    }
}