using UnityEngine;
using UnityEngine.EventSystems;
using Unity.AI.Navigation;

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

    [Header("Spacing (กันเฟอร์นิเจอร์ชิดกันจนแมวเดิน/นั่งไม่ได้)")]
    [Tooltip("ระยะห่างขั้นต่ำระหว่างเฟอร์นิเจอร์ด้วยกัน (หน่วย Unity) ควรมากกว่าความกว้างตัวแมวเล็กน้อย — 0 = ไม่เช็ก")]
    public float minFurnitureGap = 0.6f;

    [Header("NavMesh")]
    [Tooltip("สั่ง Bake NavMesh ใหม่ทุกครั้งที่วางหรือเก็บเฟอร์นิเจอร์")]
    public bool rebakeNavMeshOnChange = true;

    [Header("Preview Colors")]
    public Color validColor = new Color(0f, 1f, 0f, 0.6f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.6f);

    private GameObject currentPlacingObject;

    public bool IsPlacingFurniture => currentPlacingObject != null;
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
        if (Input.GetKeyDown(KeyCode.B) || GameInput.PadEditModeDown)
        {
            if (ComputerStation.IsShopOpen) return;
            ToggleEditMode(!isEditMode);
        }

        if (currentPlacingObject != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(GameInput.PointerPosition);
            Vector3 snapPos = new Vector3(
                Mathf.Round((mousePos.x - gridOffset.x) / gridSize) * gridSize + gridOffset.x,
                Mathf.Round((mousePos.y - gridOffset.y) / gridSize) * gridSize + gridOffset.y,
                0f
            );
            currentPlacingObject.transform.position = snapPos;

            if (Input.GetKeyDown(KeyCode.R) || GameInput.PadNextDown)
            {
                currentPlacingObject.transform.Rotate(0f, 0f, -90f);
            }

            CheckPlacementValidity();

            if (GameInput.PointerDown)
            {
                bool clickUsedByDock = FurnitureEditUI.Instance != null && FurnitureEditUI.Instance.ClickConsumedThisFrame;

                if (!clickUsedByDock && isValidPosition)
                {
                    ConfirmPlacement();
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) || GameInput.PointerRightDown || GameInput.PadBackDown)
            {
                CancelPlacement();
            }
        }
        else if (isEditMode)
        {
            if (GameInput.PadBackDown) { ToggleEditMode(false); return; }
            HandleFurniturePickup();
        }
    }

    public void StartPlacingFurnitureInstance(FurnitureInstanceData instance)
    {
        if (instance == null || instance.itemData == null || instance.itemData.prefab == null) return;

        currentPlacingInstance = instance;
        Vector3 spawnPos = Camera.main.ScreenToWorldPoint(GameInput.PointerPosition);
        spawnPos.z = 0f;

        currentPlacingObject = Instantiate(instance.itemData.prefab, spawnPos, Quaternion.identity);

        FurnitureObject furnObj = currentPlacingObject.GetComponent<FurnitureObject>();
        if (furnObj != null)
        {
            if (furnObj.lockedVisual != null) furnObj.lockedVisual.SetActive(false);
            if (furnObj.woodVisual != null) furnObj.woodVisual.SetActive(true);
            if (furnObj.woodChairVisual != null) furnObj.woodChairVisual.SetActive(true);
        }

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

        Vector2 center = (Vector2)currentPlacingObject.transform.position + previewCollider.offset;
        Vector2 size = previewCollider.size * currentPlacingObject.transform.lossyScale * 0.85f;

        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            center,
            size,
            currentPlacingObject.transform.eulerAngles.z,
            unplaceableLayer
        );

        string myFurnitureID = PlacementBlocker.GetFurnitureID(currentPlacingObject);

        bool isBlocked = false;
        foreach (var col in overlaps)
        {
            if (col == null) continue;
            if (col.transform.IsChildOf(currentPlacingObject.transform)) continue;

            PlacementBlocker blocker = col.GetComponentInParent<PlacementBlocker>();
            if (blocker != null && !blocker.Blocks(myFurnitureID)) continue;

            isBlocked = true;
            break;
        }

        if (!isBlocked && minFurnitureGap > 0f)
        {
            foreach (var other in FindObjectsByType<PlacedFurniture>(FindObjectsSortMode.None))
            {
                if (other == null || other.gameObject == currentPlacingObject) continue;

                BoxCollider2D otherBox = other.GetComponent<BoxCollider2D>();
                if (otherBox == null || !otherBox.enabled) continue;

                ColliderDistance2D d = previewCollider.Distance(otherBox);
                if (d.isValid && d.distance < minFurnitureGap) { isBlocked = true; break; }
            }
        }

        isValidPosition = !isBlocked;
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
            SetPreviewColor(Color.white);

            SetObjectCollidersTrigger(currentPlacingObject, false);
            RequestNavMeshRebake();

            PlacedFurniture placedScript = currentPlacingObject.GetComponent<PlacedFurniture>();
            if (placedScript == null)
            {
                placedScript = currentPlacingObject.AddComponent<PlacedFurniture>();
            }

            placedScript.Setup(currentPlacingInstance);

            AudioManager.instance?.PlayPlaceFurniture();

            UINotificationManager.Instance?.ShowNotification("NOTI_FURNITURE_PLACED");

            StartCoroutine(AnimatePlacementBounce(currentPlacingObject.transform));

            currentPlacingObject = null;
            currentPlacingInstance = null;
            currentRenderers = null;
            previewCollider = null;

            if (FurnitureEditUI.Instance != null)
            {
                FurnitureEditUI.Instance.RestorePanelPosition();
                FurnitureEditUI.Instance.RefreshUI();
            }

            if (MainQuestManager.Instance != null && MainQuestManager.Instance.currentQuest == MainQuestManager.QuestStep.PlaceFurniture)
            {
                MainQuestManager.Instance.AdvanceQuest();
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

        if (FurnitureEditUI.Instance != null)
        {
            FurnitureEditUI.Instance.RestorePanelPosition();
        }
    }

    private void HandleFurniturePickup()
    {
        if (Input.GetKeyDown(KeyCode.V) || GameInput.PadRemoveDown)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(GameInput.PointerPosition);

            RaycastHit2D hit = (furnitureLayer.value != 0)
                ? Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, furnitureLayer)
                : Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                PlacedFurniture furniture = hit.collider.GetComponentInParent<PlacedFurniture>();
                if (furniture != null)
                {
                    RemoveCatsFromFurniture(furniture);

                    AudioManager.instance?.PlayCollectFurniture();

                    UINotificationManager.Instance?.ShowNotification("NOTI_FURNITURE_STORED");

                    StartCoroutine(AnimatePickupShrink(furniture));
                }
            }
        }
    }

    private int RemoveCatsFromFurniture(PlacedFurniture furniture)
    {
        if (furniture == null) return 0;
        return furniture.EvictCats();
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

        if (!isEditMode && currentPlacingObject != null) CancelPlacement();

        if (FurnitureEditUI.Instance != null)
            FurnitureEditUI.Instance.ToggleEditUI(active);
    }

    private System.Collections.IEnumerator AnimatePlacementBounce(Transform targetTransform)
    {
        if (targetTransform == null) yield break;

        Vector3 originalScale = targetTransform.localScale;
        Vector3 squashScale = new Vector3(originalScale.x * 1.18f, originalScale.y * 0.82f, originalScale.z);
        Vector3 stretchScale = new Vector3(originalScale.x * 0.92f, originalScale.y * 1.08f, originalScale.z);

        float duration = 0.07f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (targetTransform == null) yield break;
            elapsed += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(originalScale, squashScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            if (targetTransform == null) yield break;
            elapsed += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(squashScale, stretchScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            if (targetTransform == null) yield break;
            elapsed += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(stretchScale, originalScale, elapsed / duration);
            yield return null;
        }

        if (targetTransform != null)
            targetTransform.localScale = originalScale;
    }

    private System.Collections.IEnumerator AnimatePickupShrink(PlacedFurniture furniture)
    {
        if (furniture == null) yield break;

        Transform targetTransform = furniture.transform;
        Vector3 originalScale = targetTransform.localScale;
        Vector3 popScale = originalScale * 1.2f;

        float duration = 0.08f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (targetTransform == null) yield break;
            elapsed += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(originalScale, popScale, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        duration = 0.1f;
        while (elapsed < duration)
        {
            if (targetTransform == null) yield break;
            elapsed += Time.deltaTime;
            targetTransform.localScale = Vector3.Lerp(popScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        if (furniture != null)
        {
            furniture.PickupFurniture();
        }
    }

    private Coroutine _rebakeRoutine;

    public void RequestNavMeshRebake()
    {
        if (!rebakeNavMeshOnChange) return;
        if (_rebakeRoutine != null) StopCoroutine(_rebakeRoutine);
        _rebakeRoutine = StartCoroutine(RebakeNavMeshNextFrame());
    }

    private System.Collections.IEnumerator RebakeNavMeshNextFrame()
    {
        yield return null;
        yield return null;

        NavMeshSurface surface = FindFirstObjectByType<NavMeshSurface>();
        if (surface != null) surface.BuildNavMesh();
        _rebakeRoutine = null;
    }
}