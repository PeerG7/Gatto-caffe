using UnityEngine;

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

    public static bool IsInShopPanMode = false;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
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
        IsInShopPanMode = enable;

        if (Instance == null) return;

        // ถ้าเข้าโหมด Pan ให้ปิด QTE Zoom Controller ไม่ให้มันดึงกล้องกลับ
        if (Instance.qteZoomController != null)
        {
            Instance.qteZoomController.enabled = !enable;
        }
    }
}