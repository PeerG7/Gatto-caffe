using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// อนิเมชันมือสลับ 2 เฟรม (ปล่อย / กด) ตามตัวเลือก QTE ที่ผู้เล่นเลือก
///   Tap  = กดครั้งเดียว  : ปล่อย → กดสั้นๆ → ปล่อย (พัก) แล้ววนซ้ำ
///   Hold = กดค้าง        : ปล่อย → กดค้างนาน → ปล่อย แล้ววนซ้ำ
///   Mash = กดรัวๆ        : สลับ ปล่อย/กด เร็วๆ ต่อเนื่อง
///
/// วางบน GameObject ที่มี Image (UI) หรือ SpriteRenderer รูปมือ
/// </summary>
public class QTEHandAnimator : MonoBehaviour
{
    public enum HandPattern { Tap, Hold, Mash }

    // ---------- ตัวเลือกล่าสุดที่ผู้เล่นกด (QTEInteractButton ส่งมาให้) ----------
    public static int LastChoice = 0;
    public static event System.Action<int> OnChoiceSelected;

    public static void NotifyChoice(int choiceIndex)
    {
        LastChoice = choiceIndex;
        OnChoiceSelected?.Invoke(choiceIndex);
    }

    // ---------- Inspector ----------
    [Header("Sprites (2 เฟรม)")]
    [Tooltip("รูปมือตอนปล่อย")]
    public Sprite releasedSprite;
    [Tooltip("รูปมือตอนกด")]
    public Sprite pressedSprite;

    [Header("Pattern ตามตัวเลือก (index 0 = ปุ่ม 1, 1 = ปุ่ม 2, 2 = ปุ่ม 3)")]
    public HandPattern[] patternByChoice = { HandPattern.Tap, HandPattern.Hold, HandPattern.Mash };

    [Tooltip("-1 = ตามตัวเลือกที่ผู้เล่นเลือก | 0/1/2 = เล่น pattern ของปุ่มนั้นเสมอ (ใช้วางมือบนปุ่มแต่ละปุ่ม)")]
    public int fixedChoice = -1;

    [Header("Tap (กดครั้งเดียว)")]
    public float tapReleaseTime = 0.8f;
    public float tapPressTime = 0.2f;

    [Header("Hold (กดค้าง)")]
    public float holdReleaseTime = 0.5f;
    public float holdPressTime = 1.5f;

    [Header("Mash (กดรัว)")]
    [Tooltip("เวลาต่อ 1 เฟรม ยิ่งน้อยยิ่งรัว (0.08 ≈ 6 ครั้ง/วินาที)")]
    public float mashInterval = 0.08f;

    [Header("Options")]
    public bool playOnEnable = true;
    [Tooltip("ใช้เวลาจริง เล่นต่อได้แม้ Time.timeScale = 0")]
    public bool useUnscaledTime = true;

    private Image uiImage;
    private SpriteRenderer spriteRenderer;
    private Coroutine routine;

    private void Awake()
    {
        uiImage = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        OnChoiceSelected += HandleChoiceSelected;
        if (playOnEnable) PlayChoice(fixedChoice >= 0 ? fixedChoice : LastChoice);
    }

    private void OnDisable()
    {
        OnChoiceSelected -= HandleChoiceSelected;
        Stop();
    }

    private void HandleChoiceSelected(int choiceIndex)
    {
        if (fixedChoice >= 0) return; // ตัวนี้ล็อกไว้กับปุ่มใดปุ่มหนึ่ง ไม่ตามที่ผู้เล่นเลือก
        PlayChoice(choiceIndex);
    }

    // ---------- Public API ----------
    public void PlayChoice(int choiceIndex)
    {
        if (patternByChoice == null || patternByChoice.Length == 0) return;
        int i = Mathf.Clamp(choiceIndex, 0, patternByChoice.Length - 1);
        Play(patternByChoice[i]);
    }

    public void Play(HandPattern pattern)
    {
        Stop();
        if (!isActiveAndEnabled) return;
        routine = StartCoroutine(Run(pattern));
    }

    public void Stop()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        SetPressed(false);
    }

    // ---------- Internals ----------
    private IEnumerator Run(HandPattern pattern)
    {
        while (true)
        {
            switch (pattern)
            {
                case HandPattern.Tap:
                    SetPressed(false);
                    yield return Wait(tapReleaseTime);
                    SetPressed(true);
                    yield return Wait(tapPressTime);
                    break;

                case HandPattern.Hold:
                    SetPressed(false);
                    yield return Wait(holdReleaseTime);
                    SetPressed(true);
                    yield return Wait(holdPressTime);
                    break;

                case HandPattern.Mash:
                    SetPressed(false);
                    yield return Wait(mashInterval);
                    SetPressed(true);
                    yield return Wait(mashInterval);
                    break;
            }
        }
    }

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void SetPressed(bool pressed)
    {
        Sprite s = pressed ? pressedSprite : releasedSprite;
        if (s == null) return;

        if (uiImage != null) uiImage.sprite = s;
        if (spriteRenderer != null) spriteRenderer.sprite = s;
    }
}