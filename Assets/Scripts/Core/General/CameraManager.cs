using UnityEngine;
using Unity.Cinemachine; // หรือ Unity.Cinemachine สำหรับเวอร์ชันใหม่ 2023+

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Cinemachine Settings")]
    public CinemachineCamera vCam; // ลาก Virtual Camera มาใส่ที่นี่
    public float zoomSize = 3f;
    public float defaultSize = 5f;
    public float smoothSpeed = 5f;

    private Transform targetTransform;
    private bool isZooming = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // ถ้าต้องการให้เงินติดตัวไปทุก Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }
    

    void Update()
    {
        float targetSize = isZooming ? zoomSize : defaultSize;
        // ปรับ Lens.OrthographicSize ของ Cinemachine
        vCam.Lens.OrthographicSize = Mathf.Lerp(vCam.Lens.OrthographicSize, targetSize, Time.deltaTime * smoothSpeed);

        if (isZooming && targetTransform != null)
        {
            // ให้กล้องติดตาม NPC
            vCam.Follow = targetTransform;
        }
        else
        {
            vCam.Follow = null; // หรือกลับไปหา Player
        }
    }

    public void ZoomToNPC(Transform target) { targetTransform = target; isZooming = true; }
    public void ResetCamera() { isZooming = false; targetTransform = null; }
}