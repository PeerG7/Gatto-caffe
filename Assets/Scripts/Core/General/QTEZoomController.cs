using UnityEngine;
using Unity.Cinemachine; // Unity 6 Cinemachine 3.x namespace

public class QTEZoomController : MonoBehaviour
{
    public static QTEZoomController Instance;

    [Header("Camera References")]
    [Tooltip("Drag your Unity 6 Cinemachine Camera here")]
    public CinemachineCamera cinemachineCam;
    [Tooltip("Main Camera in scene")]
    public Camera main2DCamera;

    [Header("Targeting")]
    [Tooltip("The empty GameObject assigned as Follow target in Cinemachine")]
    public Transform cameraFollowTarget;
    public Transform playerTransform;

    [Header("Responsiveness Settings")]
    [Tooltip("How fast the camera follows the player during normal movement (Lower = tighter/faster, Higher = floatier)")]
    public float followDampTime = 0.1f;
    [Tooltip("Transition speed when zooming in/out for QTE (in seconds)")]
    public float zoomTransitionTime = 0.25f;

    [Header("Zoom Settings")]
    [Tooltip("Default top-down game camera zoom size (e.g. 5)")]
    public float defaultOrthoSize = 5f;
    [Tooltip("Zoomed-in size during QTE (smaller number = closer zoom, e.g. 2.5)")]
    public float qteOrthoSize = 2.5f;

    private bool isQTEActive = false;
    private Vector3 currentTargetPos;
    private float currentOrthoSize;
    private float zoomVelocity;
    private Vector3 posVelocity;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (main2DCamera == null) main2DCamera = Camera.main;
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        currentOrthoSize = defaultOrthoSize;

        if (cameraFollowTarget != null && playerTransform != null)
        {
            // Ensure target is detached from hierarchy to prevent recursive motion wobble
            cameraFollowTarget.SetParent(null);
            cameraFollowTarget.position = playerTransform.position;
            currentTargetPos = playerTransform.position;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null || cameraFollowTarget == null) return;

        // 1. Determine target position (Player position normally, Midpoint during QTE)
        Vector3 targetPos = isQTEActive ? currentTargetPos : playerTransform.position;

        // 2. Smoothly follow target position (Uses tighter damp time for normal tracking)
        float currentDamp = isQTEActive ? zoomTransitionTime : followDampTime;
        cameraFollowTarget.position = Vector3.SmoothDamp(
            cameraFollowTarget.position,
            targetPos,
            ref posVelocity,
            currentDamp
        );

        // 3. Smoothly adjust orthographic zoom size
        float targetSize = isQTEActive ? qteOrthoSize : defaultOrthoSize;
        currentOrthoSize = Mathf.SmoothDamp(
            currentOrthoSize,
            targetSize,
            ref zoomVelocity,
            zoomTransitionTime
        );

        ApplyOrthoSize(currentOrthoSize);
    }

    /// <summary>
    /// Smoothly focuses camera halfway between Player and NPC, and zooms in.
    /// </summary>
    public void ZoomInToQTE(Transform npcTransform)
    {
        if (playerTransform == null || npcTransform == null) return;

        isQTEActive = true;
        // Calculate midpoint between Player and NPC
        currentTargetPos = (playerTransform.position + npcTransform.position) * 0.5f;
    }

    /// <summary>
    /// Smoothly zooms back out and restores camera focus onto Player.
    /// </summary>
    public void ZoomOutToGameplay()
    {
        isQTEActive = false;
    }

    private void ApplyOrthoSize(float size)
    {
        // 1. Apply to Cinemachine 3.x Camera Lens
        if (cinemachineCam != null)
        {
            var lens = cinemachineCam.Lens;
            lens.OrthographicSize = size;
            cinemachineCam.Lens = lens;
        }

        // 2. Direct fallback on Main Camera
        if (main2DCamera != null)
        {
            main2DCamera.orthographicSize = size;
        }
    }
}