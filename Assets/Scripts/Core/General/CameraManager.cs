using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    private Camera cam;
    private float defaultSize;
    private Vector3 defaultPos;

    [Header("Zoom Settings")]
    public float zoomSize = 3f; // ปรับค่านี้เพื่อกำหนดความใกล้
    public float smoothSpeed = 5f;
    private bool isZooming = false;
    private Transform targetTransform;

    void Awake()
    {
        if (Instance == null) Instance = this;
        cam = GetComponent<Camera>();
        defaultSize = cam.orthographicSize;
        defaultPos = transform.position;
    }

    void LateUpdate()
    {
        if (isZooming && targetTransform != null)
        {
            Vector3 targetPos = new Vector3(targetTransform.position.x, targetTransform.position.y, -10f);
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomSize, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, defaultPos, smoothSpeed * Time.deltaTime);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, defaultSize, smoothSpeed * Time.deltaTime);
        }
    }

    public void ZoomIn(Transform target) { targetTransform = target; isZooming = true; }
    public void ZoomOut() { isZooming = false; }
}