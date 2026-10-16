using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverWobble : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Wobble Settings")]
    [Tooltip("Maximum tilt angle in degrees")]
    [SerializeField] private float tiltAngle = 8f;

    [Tooltip("Wobble animation speed multiplier")]
    [SerializeField] private float wobbleSpeed = 15f;

    private Quaternion defaultRotation;
    private Coroutine wobbleRoutine;

    void Awake()
    {
        defaultRotation = transform.localRotation;
    }

    void OnDisable()
    {
        if (wobbleRoutine != null)
        {
            StopCoroutine(wobbleRoutine);
            wobbleRoutine = null;
        }
        transform.localRotation = defaultRotation;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (wobbleRoutine != null) StopCoroutine(wobbleRoutine);
        wobbleRoutine = StartCoroutine(AnimateWobble());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (wobbleRoutine != null)
        {
            StopCoroutine(wobbleRoutine);
            wobbleRoutine = null;
        }
        transform.localRotation = defaultRotation;
    }

    private IEnumerator AnimateWobble()
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * wobbleSpeed;
            float zRot = Mathf.Sin(time) * tiltAngle;
            transform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, zRot);
            yield return null;
        }
    }
}