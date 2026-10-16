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
    public Transform cameraFollowTarget;
    public Transform playerTransform;

    [Header("Responsiveness Settings")]
    [Tooltip("Transition speed when zooming in/out (seconds)")]
    public float zoomTransitionTime = 0.65f;

    [Header("Zoom Settings")]
    public float defaultOrthoSize = 5f;
    public float qteOrthoSize = 2.5f;

    private bool isQTEActive = false;
    private float currentOrthoSize;
    private float zoomVelocity;
    private Vector3 posVelocity;

    private Transform activeTargetTransform;
    private bool isLockedInQTE = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (main2DCamera == null) main2DCamera = Camera.main;
        if (cinemachineCam == null) cinemachineCam = FindFirstObjectByType<CinemachineCamera>();
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
            cameraFollowTarget.SetParent(null);
            cameraFollowTarget.position = playerTransform.position;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null || cameraFollowTarget == null) return;

        Vector3 targetPos;

        if (isQTEActive && activeTargetTransform != null)
        {
            targetPos = (playerTransform.position + activeTargetTransform.position) * 0.5f;

            cameraFollowTarget.position = Vector3.SmoothDamp(
                cameraFollowTarget.position,
                targetPos,
                ref posVelocity,
                zoomTransitionTime
            );
        }
        else
        {
            cameraFollowTarget.position = Vector3.SmoothDamp(
                cameraFollowTarget.position,
                playerTransform.position,
                ref posVelocity,
                zoomTransitionTime
            );
        }

        float targetSize = isQTEActive ? qteOrthoSize : defaultOrthoSize;
        currentOrthoSize = Mathf.SmoothDamp(
            currentOrthoSize,
            targetSize,
            ref zoomVelocity,
            zoomTransitionTime
        );

        ApplyOrthoSize(currentOrthoSize);
    }

    public void ZoomInToQTE(Transform targetTransform, bool lockZoom = false)
    {
        if (targetTransform == null) return;

        activeTargetTransform = targetTransform;
        isQTEActive = true;
        isLockedInQTE = lockZoom;
    }

    public void ZoomOutToGameplay(bool force = false)
    {
        if (isLockedInQTE && !force) return;

        isQTEActive = false;
        isLockedInQTE = false;
        activeTargetTransform = null;
    }

    public void UnlockQTEZoom()
    {
        isLockedInQTE = false;
    }

    private void ApplyOrthoSize(float size)
    {
        if (cinemachineCam != null)
        {
            var lens = cinemachineCam.Lens;
            lens.OrthographicSize = size;
            cinemachineCam.Lens = lens;
        }

        if (main2DCamera != null)
        {
            main2DCamera.orthographicSize = size;
        }
    }
}