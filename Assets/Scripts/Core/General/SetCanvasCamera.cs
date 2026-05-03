using UnityEngine;

public class SetCanvasCamera : MonoBehaviour
{
    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        SetCamera();
    }

    void Start()
    {
        SetCamera();
    }

    void SetCamera()
    {
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            canvas.worldCamera = Camera.main;
    }
}