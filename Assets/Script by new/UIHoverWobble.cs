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

    [Header("Hover SFX")]
    [Tooltip("เสียงตอนเอาเมาส์ไปชี้ (ลาก mouse_chee_ui_sfx.wav มาใส่)")]
    [SerializeField] private AudioClip hoverSfx;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    // AudioSource กลาง ใช้ร่วมกันทุกปุ่ม (ใช้ตัวเดียวกับ UIHoverBounce ถ้าชื่อเดียวกันจะแยกกันคนละ static)
    private static AudioSource sharedSource;

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
        PlayHoverSfx();

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

    private void PlayHoverSfx()
    {
        if (hoverSfx == null) return;
        GetSharedSource().PlayOneShot(hoverSfx, sfxVolume);
    }

    private static AudioSource GetSharedSource()
    {
        if (sharedSource == null)
        {
            GameObject go = new GameObject("UIHoverWobbleSfxPlayer");
            DontDestroyOnLoad(go);
            sharedSource = go.AddComponent<AudioSource>();
            sharedSource.playOnAwake = false;
            sharedSource.spatialBlend = 0f; // 2D sound
        }
        return sharedSource;
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