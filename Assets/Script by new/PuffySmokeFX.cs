using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PuffySmokeFX : MonoBehaviour
{
    [Header("UI Image Target")]
    [SerializeField] private Image smokeImage;

    [Header("Frame Animation")]
    [Tooltip("Drag all your smoke animation sprite frames here in order.")]
    [SerializeField] private Sprite[] animationFrames;

    [Tooltip("Frames per second (e.g., 12 or 24 FPS).")]
    [SerializeField] private float frameRate = 12f;

    private Coroutine animRoutine;
    private int currentFrame = 0;

    void OnEnable()
    {
        StartSmoke();
    }

    void OnDisable()
    {
        StopSmoke();
    }

    public void StartSmoke()
    {
        if (smokeImage != null)
            smokeImage.gameObject.SetActive(true);

        if (animRoutine != null)
            StopCoroutine(animRoutine);

        if (animationFrames != null && animationFrames.Length > 0)
        {
            currentFrame = 0;
            animRoutine = StartCoroutine(AnimateFrames());
        }
    }

    public void StopSmoke()
    {
        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }

        if (smokeImage != null)
            smokeImage.gameObject.SetActive(false);
    }

    private IEnumerator AnimateFrames()
    {
        float frameDuration = 1f / Mathf.Max(1f, frameRate);

        while (true)
        {
            if (smokeImage != null && animationFrames.Length > 0)
            {
                smokeImage.sprite = animationFrames[currentFrame];
                currentFrame = (currentFrame + 1) % animationFrames.Length;
            }
            yield return new WaitForSeconds(frameDuration);
        }
    }

    // Kept for backwards compatibility with external calls
    public void ClearAllPuffs() => StopSmoke();
}