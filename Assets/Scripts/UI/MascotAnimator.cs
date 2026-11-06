using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MascotAnimator : MonoBehaviour
{
    private Image mascotImage;

    [Header("Mascot Sprites")]
    public Sprite idleSprite;          // รูปตอนปิดปาก/ปกติ
    public Sprite[] talkingSprites;    // อาเรย์รูปขยับปากพูด (Frame 1, Frame 2, etc.)
    public float frameRate = 8.0f;     // ความเร็วในการเปลี่ยนเฟรมภาพ (fps)

    private bool isTalking = false;
    private float timer = 0f;
    private int currentFrame = 0;

    void Awake()
    {
        mascotImage = GetComponent<Image>();
    }

    void Update()
    {
        if (!isTalking || talkingSprites == null || talkingSprites.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= (1f / frameRate))
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % talkingSprites.Length;
            mascotImage.sprite = talkingSprites[currentFrame];
        }
    }

    public void StartTalking()
    {
        isTalking = true;
        currentFrame = 0;
        timer = 0f;
    }

    public void StopTalking()
    {
        isTalking = false;
        if (idleSprite != null && mascotImage != null)
        {
            mascotImage.sprite = idleSprite; // กลับมาเป็นรูปปกติเมื่อพูดจบ
        }
    }
}