using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3 (Unity 6)

public class CameraController2D : MonoBehaviour
{
    // Singleton Instance สำหรับอ้างอิงจาก static method
    public static CameraController2D Instance;

    [Header("Cinemachine Target")]
    public Transform cameraFollowTarget;

    [Header("Target & Follow")]
    public Transform playerTarget;
    public float followSpeed = 10f;

    [Header("Pan Mode Settings")]
    public float panSpeed = 0.5f;

    [Header("Pan Mode Zoom Out")]
    [Tooltip("ลาก Cinemachine Camera มาใส่ (ว่าง = หาให้อัตโนมัติ)")]
    public CinemachineCamera virtualCamera;
    [Tooltip("ซูมออกกี่เท่าตอนเปิดกระเป๋า (1.5 = กว้างขึ้น 50%)")]
    public float panZoomMultiplier = 1.5f;
    [Tooltip("ความเร็วการซูม ยิ่งมากยิ่งเร็ว")]
    public float zoomSpeed = 6f;

    [Header("Camera Bounds")]
    public bool enableBounds = true;
    public float minX = -20f;
    public float maxX = 20f;
    public float minY = -20f;
    public float maxY = 20f;

    [Header("References")]
    public MonoBehaviour qteZoomController; // ลากตัว QTE Zoom Controller มาใส่ช่องนี้

    private Vector3 lastMousePosition;
    private Camera cam;

    // ตัวแปรสำหรับซูม
    private bool isZooming = false;
    private bool isZoomedOut = false;   // ✅ เพิ่มใหม่: ตอนนี้กล้องอยู่ในสถานะซูมออกอยู่ไหม
    private float sizeBeforePan;
    private float targetSize;

    public static bool IsInShopPanMode = false;

    // ✅ เพิ่มใหม่: true = ห้ามซูมกลับ (ใช้ตอนเลือกโต๊ะแล้วยังไม่วาง)
    public static bool HoldZoom = false;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;

        if (virtualCamera == null)
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();
    }

    void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }
    }

    void Update()
    {
        UpdateZoom();

        if (cameraFollowTarget == null) return;

        if (IsInShopPanMode)
        {
            HandlePanMovement();
        }
        else
        {
            HandleFollowPlayer();
        }
    }

    // ซูมแบบนุ่ม ๆ (ใช้ unscaledDeltaTime เผื่อเกมถูก Pause ตอนเปิดกระเป๋า)
    void UpdateZoom()
    {
        if (!isZooming) return;

        float current = GetOrthoSize();
        float next = Mathf.Lerp(current, targetSize, zoomSpeed * Time.unscaledDeltaTime);

        if (Mathf.Abs(next - targetSize) < 0.01f)
        {
            next = targetSize;
            isZooming = false;

            // ซูมกลับเสร็จแล้ว → คืนให้ QTE Zoom Controller ทำงานต่อ
            if (!IsInShopPanMode && !isZoomedOut && qteZoomController != null)
                qteZoomController.enabled = true;
        }

        SetOrthoSize(next);
    }

    float GetOrthoSize()
    {
        if (virtualCamera != null) return virtualCamera.Lens.OrthographicSize;
        return cam != null ? cam.orthographicSize : 5f;
    }

    void SetOrthoSize(float size)
    {
        if (virtualCamera != null)
        {
            // Lens เป็น struct ต้องก๊อปออกมาแก้แล้วใส่กลับ
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = size;
            virtualCamera.Lens = lens;
        }
        else if (cam != null)
        {
            cam.orthographicSize = size;
        }
    }

    void HandleFollowPlayer()
    {
        if (playerTarget == null) return;

        Vector3 targetPos = playerTarget.position;

        if (enableBounds)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        targetPos.z = 0f;
        cameraFollowTarget.position = Vector3.Lerp(cameraFollowTarget.position, targetPos, followSpeed * Time.deltaTime);
    }

    void HandlePanMovement()
    {
        // 1. กดคลิกขวา -> จำตำแหน่งเมาส์บนหน้าจอ
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        // 2. คลิกขวาค้างไว้ -> คำนวณ delta การลากเมาส์
        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            // แปลงระยะลากหน้าจอมาขยับ Target ใน World Space
            Vector3 move = new Vector3(-delta.x, -delta.y, 0f) * (panSpeed * Time.deltaTime);
            Vector3 targetPos = cameraFollowTarget.position + move;

            if (enableBounds)
            {
                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
            }

            targetPos.z = 0f;
            cameraFollowTarget.position = targetPos;

            lastMousePosition = Input.mousePosition;
        }
    }

    public static void SetPanMode(bool enable)
    {
        bool changed = IsInShopPanMode != enable;

        IsInShopPanMode = enable;

        if (Instance == null) return;

        if (changed) Instance.BeginZoom(enable);

        // ถ้าเข้าโหมด Pan ให้ปิด QTE Zoom Controller ไม่ให้มันดึงกล้องกลับ
        // (ตอนออก ถ้ายังซูมออกค้างอยู่หรือกำลังซูมกลับ จะเปิดคืนให้เองใน UpdateZoom)
        if (Instance.qteZoomController != null)
        {
            if (enable) Instance.qteZoomController.enabled = false;
            else if (!Instance.isZoomedOut && !Instance.isZooming) Instance.qteZoomController.enabled = true;
        }
    }

    // ✅ เพิ่มใหม่: สั่งให้กล้องค้างระยะไกลไว้ / ปล่อยให้ซูมกลับ
    public static void SetHoldZoom(bool hold)
    {
        HoldZoom = hold;

        if (Instance == null) return;

        // ปล่อยค้าง และไม่ได้อยู่ใน Pan Mode แล้ว → ซูมกลับเลย
        if (!hold && !IsInShopPanMode)
            Instance.BeginZoom(false);
    }

    void BeginZoom(bool zoomOut)
    {
        if (zoomOut)
        {
            if (isZoomedOut) return; // ซูมออกอยู่แล้ว ไม่ต้องคูณซ้ำ

            sizeBeforePan = GetOrthoSize();
            targetSize = sizeBeforePan * panZoomMultiplier;
            isZoomedOut = true;
        }
        else
        {
            if (HoldZoom) return;       // ยังต้องค้างไกลไว้
            if (!isZoomedOut) return;   // ไม่ได้ซูมออกอยู่ ไม่มีอะไรต้องกลับ

            targetSize = sizeBeforePan;
            isZoomedOut = false;
        }

        isZooming = true;
    }
}