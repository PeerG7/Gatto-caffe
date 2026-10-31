using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Chat bubble for a cat. Keeps your original fields (bubbleCanvas, iconImage,
/// chatIcons, displayDuration) so existing Inspector assignments still work.
/// New: icon swaps randomly while talking, and the bubble mirrors toward the
/// cat being talked to (the cat sprite uses flipX, which never flips children).
/// </summary>
public class CatChatBubble : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bubbleCanvas;      // Canvas/Panel รูปก้อนเมฆ
    public Image iconImage;              // Image ตรงกลางก้อนเมฆ
    public Sprite[] chatIcons;           // สุ่มรูปไอคอนสนทนา
    public float displayDuration = 3.5f; // ใช้กับ ShowChatBubble() แบบเดิม

    [Header("Random Icon")]
    [Tooltip("เปลี่ยนไอคอนใหม่ทุกๆ กี่วินาทีระหว่างคุย")]
    public float iconSwapInterval = 0.55f;
    public bool avoidRepeatingIcon = true;

    [Header("Direction / Flip")]
    [Tooltip("ติ๊กถ้าหางก้อนเมฆในรูปชี้ไปทาง 'ขวา' ไม่ติ๊กถ้าชี้ไป 'ซ้าย'")]
    public bool artTailPointsRight = true;
    [Tooltip("สลับตำแหน่ง X ของก้อนเมฆตามทิศที่แมวหันไป")]
    public bool mirrorPosition = true;

    [Header("Animation")]
    public float popDuration = 0.15f;
    public float bobAmount = 0.03f;
    public float bobSpeed = 6f;

    private Coroutine routine;
    private Vector3 baseCanvasScale = Vector3.one;
    private Vector3 baseCanvasPos;
    private Vector3 baseIconScale = Vector3.one;
    private int lastIconIndex = -1;
    private bool cached = false;

    private void CacheBase()
    {
        if (cached || bubbleCanvas == null) return;
        cached = true;

        Transform ct = bubbleCanvas.transform;
        baseCanvasScale = ct.localScale;
        baseCanvasScale.x = Mathf.Abs(baseCanvasScale.x);
        baseCanvasPos = ct.localPosition;

        if (iconImage != null)
        {
            baseIconScale = iconImage.transform.localScale;
            baseIconScale.x = Mathf.Abs(baseIconScale.x);
        }
    }

    void Start()
    {
        CacheBase();
        if (bubbleCanvas != null) bubbleCanvas.SetActive(false);
    }

    /// <summary>แบบเดิม: อ่านทิศจาก flipX ของแมวเอง</summary>
    public void ShowChatBubble()
    {
        bool faceRight = true;
        NPCController npc = GetComponentInParent<NPCController>();
        if (npc != null && npc.bodySpriteRenderer != null)
            faceRight = !npc.bodySpriteRenderer.flipX; // flipX = true คือแมวหันซ้าย

        ShowChatBubble(faceRight, displayDuration);
    }

    /// <summary>faceRight = แมวตัวนี้คุยไปทางขวาของจอ</summary>
    public void ShowChatBubble(bool faceRight, float duration)
    {
        if (bubbleCanvas == null) return;
        CacheBase();

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(BubbleRoutine(faceRight, duration));
    }

    public void HideChatBubble()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (bubbleCanvas != null) bubbleCanvas.SetActive(false);
    }

    private IEnumerator BubbleRoutine(bool faceRight, float duration)
    {
        Transform ct = bubbleCanvas.transform;

        // sign = +1 เมื่อทิศแมวตรงกับทิศหางในรูป, -1 เมื่อต้องกลับด้าน
        float sign = (faceRight == artTailPointsRight) ? 1f : -1f;

        Vector3 basePos = baseCanvasPos;
        if (mirrorPosition) basePos.x = baseCanvasPos.x * sign;

        // กลับด้านทั้งก้อนเมฆ แล้วกลับไอคอนอีกที ไอคอนจะได้ไม่กลับหลัง
        if (iconImage != null)
        {
            Vector3 s = baseIconScale;
            s.x *= sign;
            iconImage.transform.localScale = s;
        }

        PickRandomIcon();
        ct.localPosition = basePos;
        ct.localScale = Vector3.zero;
        bubbleCanvas.SetActive(true);

        float t = 0f;
        float swapTimer = 0f;

        while (t < duration)
        {
            float dt = Time.deltaTime;
            t += dt;
            swapTimer += dt;

            float pop = popDuration > 0f ? Mathf.Clamp01(t / popDuration) : 1f;
            float k = pop < 1f ? pop * (1f + 0.15f * Mathf.Sin(pop * Mathf.PI)) : 1f;

            ct.localScale = new Vector3(baseCanvasScale.x * sign * k, baseCanvasScale.y * k, baseCanvasScale.z * k);
            ct.localPosition = basePos + Vector3.up * (Mathf.Sin(t * bobSpeed) * bobAmount);

            if (iconSwapInterval > 0f && swapTimer >= iconSwapInterval)
            {
                swapTimer = 0f;
                PickRandomIcon();
            }
            yield return null;
        }

        bubbleCanvas.SetActive(false);
        routine = null;
    }

    private void PickRandomIcon()
    {
        if (chatIcons == null || chatIcons.Length == 0 || iconImage == null) return;

        int index = Random.Range(0, chatIcons.Length);
        if (avoidRepeatingIcon && chatIcons.Length > 1)
        {
            int guard = 0;
            while (index == lastIconIndex && guard++ < 10)
                index = Random.Range(0, chatIcons.Length);
        }
        lastIconIndex = index;
        iconImage.sprite = chatIcons[index];
    }
}