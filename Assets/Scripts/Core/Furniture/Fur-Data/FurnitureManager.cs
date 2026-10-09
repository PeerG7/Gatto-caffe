using UnityEngine;
using UnityEngine.EventSystems;

public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager Instance;

    public bool isEditMode = false;

    [Header("Grid Snap Settings")]
    public float gridSize = 0.5f;
    public Vector3 gridOffset = Vector3.zero;

    [Header("Layer & Placement Settings")]
    public LayerMask furnitureLayer;
    [Tooltip("Layer ที่ห้ามวางทับ (เช่น Wall, Furniture, Obstacle)")]
    public LayerMask unplaceableLayer;

    [Header("Preview Colors")]
    public Color validColor = new Color(0f, 1f, 0f, 0.6f);   // สีเขียวโปร่งแสง (วางได้)
    public Color invalidColor = new Color(1f, 0f, 0f, 0.6f); // สีแดงโปร่งแสง (วางไม่ได้)

    private GameObject currentPlacingObject;
    private FurnitureInstanceData currentPlacingInstance;
    private SpriteRenderer[] currentRenderers;
    private BoxCollider2D previewCollider;
    private bool isValidPosition = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (ComputerStation.IsShopOpen) return;
            ToggleEditMode(!isEditMode);
        }

        if (currentPlacingObject != null)
        {
            // 1. Snap เข้า Grid
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 snapPos = new Vector3(
                Mathf.Round((mousePos.x - gridOffset.x) / gridSize) * gridSize + gridOffset.x,
                Mathf.Round((mousePos.y - gridOffset.y) / gridSize) * gridSize + gridOffset.y,
                0f
            );
            currentPlacingObject.transform.position = snapPos;

            // 2. หมุนวัตถุ (ปุ่ม R)
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentPlacingObject.transform.Rotate(0f, 0f, -90f);
            }

            // 3. ตรวจสอบพื้นที่วาง (เช็คการซ้อนทับ)
            CheckPlacementValidity();

            // 4. คลิกซ้ายวางวัตถุ
            if (Input.GetMouseButtonDown(0))
            {
                if (isValidPosition)
                {
                    ConfirmPlacement();
                }
            }

            // ยกเลิกด้วย ESC หรือ คลิกขวา
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
            }
        }
        else if (isEditMode)
        {
            HandleFurniturePickup();
        }
    }

    public void StartPlacingFurnitureInstance(FurnitureInstanceData instance)
    {
        if (instance == null || instance.itemData == null || instance.itemData.prefab == null) return;

        currentPlacingInstance = instance;
        Vector3 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        spawnPos.z = 0f;

        currentPlacingObject = Instantiate(instance.itemData.prefab, spawnPos, Quaternion.identity);

        // ซ่อน LockedVisual ชั่วคราวขณะเป็น Preview และเปิด Visual ปกติขึ้นมา
        FurnitureObject furnObj = currentPlacingObject.GetComponent<FurnitureObject>();
        if (furnObj != null)
        {
            if (furnObj.lockedVisual != null) furnObj.lockedVisual.SetActive(false);
            if (furnObj.woodVisual != null) furnObj.woodVisual.SetActive(true);
            if (furnObj.woodChairVisual != null) furnObj.woodChairVisual.SetActive(true);
        }

        // เก็บ SpriteRenderer ทั้งหมดในตัวลูกเพื่อย้อมสีทั้งชุด
        currentRenderers = currentPlacingObject.GetComponentsInChildren<SpriteRenderer>(true);
        previewCollider = currentPlacingObject.GetComponent<BoxCollider2D>();

        SetObjectCollidersTrigger(currentPlacingObject, true);
    }

    private void CheckPlacementValidity()
    {
        if (previewCollider == null)
        {
            isValidPosition = true;
            SetPreviewColor(validColor);
            return;
        }

        // 1. คำนวณจุดศูนย์กลางและขนาด (ย่อขนาดลงเหลือ 85% ป้องกันขอบ Collider ไปแตะขอบกำแพงเกินจริง)
        Vector2 center = (Vector2)currentPlacingObject.transform.position + previewCollider.offset;
        Vector2 size = previewCollider.size * currentPlacingObject.transform.lossyScale * 0.85f;

        // 2. ยิง OverlapBoxAll เพื่อดึง Collider ทั้งหมดในบริเวณนั้นมาเช็ก
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            center,
            size,
            currentPlacingObject.transform.eulerAngles.z,
            unplaceableLayer
        );

        // 3. กรอง Collider ของตัวมันเอง และ Child ออกจากการเช็ก
        bool isBlocked = false;
        foreach (var col in overlaps)
        {
            if (col != null && !col.transform.IsChildOf(currentPlacingObject.transform))
            {
                isBlocked = true;
                break;
            }
        }

        isValidPosition = !isBlocked;

        // เปลี่ยนสีพรีวิว
        SetPreviewColor(isValidPosition ? validColor : invalidColor);
    }

    private void SetPreviewColor(Color color)
    {
        if (currentRenderers == null) return;
        foreach (var rend in currentRenderers)
        {
            if (rend != null)
            {
                rend.color = color;
            }
        }
    }

    private void ConfirmPlacement()
    {
        if (FurnitureInventory.Instance != null && FurnitureInventory.Instance.RemoveInstance(currentPlacingInstance))
        {
            // คืนค่าสี Sprite เป็นสีปกติ
            SetPreviewColor(Color.white);

            SetObjectCollidersTrigger(currentPlacingObject, false);

            PlacedFurniture placedScript = currentPlacingObject.GetComponent<PlacedFurniture>();
            if (placedScript == null)
            {
                placedScript = currentPlacingObject.AddComponent<PlacedFurniture>();
            }

            placedScript.Setup(currentPlacingInstance);

            currentPlacingObject = null;
            currentPlacingInstance = null;
            currentRenderers = null;
            previewCollider = null;

            // 🟢 ยกตำแหน่ง UI กลับมาที่ Pos Y = -406 และอัปเดตรายการในคลัง
            if (FurnitureEditUI.Instance != null)
            {
                FurnitureEditUI.Instance.RestorePanelPosition();
                FurnitureEditUI.Instance.RefreshUI();
            }
        }
        else
        {
            CancelPlacement();
        }
    }

    private void CancelPlacement()
    {
        if (currentPlacingObject != null)
        {
            Destroy(currentPlacingObject);
            currentPlacingObject = null;
            currentPlacingInstance = null;
            currentRenderers = null;
            previewCollider = null;
        }

        // 🟢 ยกตำแหน่ง UI กลับมาที่ Pos Y = -406 เมื่อกดยกเลิก
        if (FurnitureEditUI.Instance != null)
        {
            FurnitureEditUI.Instance.RestorePanelPosition();
        }
    }

    private void HandleFurniturePickup()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            RaycastHit2D hit = (furnitureLayer.value != 0)
                ? Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, furnitureLayer)
                : Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                PlacedFurniture furniture = hit.collider.GetComponentInParent<PlacedFurniture>();
                if (furniture != null)
                {
                    furniture.PickupFurniture();
                }
            }
        }
    }

    private void SetObjectCollidersTrigger(GameObject obj, bool isTrigger)
    {
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            col.isTrigger = isTrigger;
        }
    }

    public void ToggleEditMode(bool active)
    {
        isEditMode = active;
        PlayerController2D.SetLock(isEditMode);

        if (CameraController2D.Instance != null)
            CameraController2D.SetPanMode(isEditMode);

        if (DayNightManager.Instance != null)
        {
            if (isEditMode) DayNightManager.Instance.PauseGame();
            else DayNightManager.Instance.ResumeGame();
        }

        if (!isEditMode && currentPlacingObject != null) CancelPlacement();

        if (FurnitureEditUI.Instance != null)
            FurnitureEditUI.Instance.ToggleEditUI(active);
    }
}