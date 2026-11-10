using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpriteSheetAnimationUI : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Drag your sequential sprite frames here.")]
    public Sprite[] spriteFrames;

    [Tooltip("Playback speed in frames per second.")]
    [Range(1, 120)]
    public float frameRate = 12.0f;

    [Tooltip("Should the animation loop continuously?")]
    public bool isLooping = true;

    [Tooltip("Should the animation start playing automatically on start?")]
    public bool playOnAwake = true;

    private Image uiImage;
    private int currentFrame;
    private float timer;
    private bool isPlaying;

    void Awake()
    {
        uiImage = GetComponent<Image>();
    }

    void Start()
    {
        if (playOnAwake)
        {
            Play();
        }
    }

    void Update()
    {
        if (!isPlaying || spriteFrames == null || spriteFrames.Length == 0)
            return;

        // Accumulate time based on frame rate
        timer += Time.deltaTime;
        float frameDuration = 1.0f / frameRate;

        if (timer >= frameDuration)
        {
            // Reset timer, accounting for any slight overage
            timer -= frameDuration;
            currentFrame++;

            if (currentFrame >= spriteFrames.Length)
            {
                if (isLooping)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = spriteFrames.Length - 1;
                    isPlaying = false; // Stop at the final frame
                }
            }

            // Apply the sprite to the UI Image component
            if (uiImage != null && spriteFrames[currentFrame] != null)
            {
                uiImage.sprite = spriteFrames[currentFrame];
            }
        }
    }

    /// <summary>
    /// Starts or resumes the animation.
    /// </summary>
    public void Play()
    {
        if (spriteFrames != null && spriteFrames.Length > 0)
        {
            isPlaying = true;
        }
    }

    /// <summary>
    /// Pauses the animation at the current frame.
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// Stops the animation and resets it to the first frame.
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        currentFrame = 0;
        if (uiImage != null && spriteFrames != null && spriteFrames.Length > 0)
        {
            uiImage.sprite = spriteFrames[0];
        }
    }
}