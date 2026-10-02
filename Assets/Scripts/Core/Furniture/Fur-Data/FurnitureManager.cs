using UnityEngine;

public class FurnitureManager : MonoBehaviour
{
    public static FurnitureManager Instance;

    public bool isEditMode = false;
    private GameObject currentPlacingObject;
    private FurnitureInstanceData currentPlacingInstance;

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
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            currentPlacingObject.transform.position = mousePos;

            if (Input.GetMouseButtonDown(0)) ConfirmPlacement();
            if (Input.GetMouseButtonDown(1)) CancelPlacement();
        }
        else if (isEditMode)
        {
            HandleFurniturePickup();
        }
    }

    public void ToggleEditMode(bool active)
    {
        isEditMode = active;
        PlayerController2D.SetLock(isEditMode);

        if (CameraController2D.Instance != null)
            CameraController2D.SetPanMode(isEditMode);

        // ⏸ หยุดเวลาเมื่อเข้า Edit Mode และ ▶ เดินเวลาต่อเมื่อออกจาก Edit Mode
        if (DayNightManager.Instance != null)
        {
            if (isEditMode)
            {
                DayNightManager.Instance.PauseGame();
            }
            else
            {
                DayNightManager.Instance.ResumeGame();
            }
        }

        if (!isEditMode && currentPlacingObject != null) CancelPlacement();

        if (FurnitureEditUI.Instance != null)
            FurnitureEditUI.Instance.ToggleEditUI(active);
    }

    public void StartPlacingFurnitureInstance(FurnitureInstanceData instance)
    {
        if (instance == null || instance.itemData == null || instance.itemData.prefab == null) return;

        currentPlacingInstance = instance;
        Vector3 spawnPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        spawnPos.z = 0;

        currentPlacingObject = Instantiate(instance.itemData.prefab, spawnPos, Quaternion.identity);
    }

    private void ConfirmPlacement()
    {
        if (FurnitureInventory.Instance.RemoveInstance(currentPlacingInstance))
        {
            PlacedFurniture placedScript = currentPlacingObject.GetComponent<PlacedFurniture>();
            if (placedScript == null)
            {
                placedScript = currentPlacingObject.AddComponent<PlacedFurniture>();
            }

            // นำ InstanceData (จำ Level/State ล่าสุด) ยัดคืนกลับไปที่วัตถุในฉาก
            placedScript.Setup(currentPlacingInstance);

            currentPlacingObject = null;
            currentPlacingInstance = null;

            if (FurnitureEditUI.Instance != null)
                FurnitureEditUI.Instance.RefreshUI();
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
        }
    }

    private void HandleFurniturePickup()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                PlacedFurniture furniture = hit.collider.GetComponent<PlacedFurniture>();
                if (furniture != null)
                {
                    furniture.PickupFurniture();
                }
            }
        }
    }
}