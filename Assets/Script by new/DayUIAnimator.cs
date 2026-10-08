using System.Collections;
using UnityEngine;
using TMPro;

public class DayUIAnimator : MonoBehaviour
{
    public enum AnimationStyle
    {
        SmoothEase,
        CozyBounce
    }

    [Header("Main Panel Reference")]
    [SerializeField] private RectTransform boarderPanelRect;

    [Header("Text Reference (Optional)")]
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("Positions (Anchored Position)")]
    [SerializeField] private Vector2 hiddenPosition = new Vector2(-700f, 650f);
    [SerializeField] private Vector2 visiblePosition = new Vector2(-700f, 370f);

    [Header("Animation Settings")]
    [SerializeField] private AnimationStyle style = AnimationStyle.CozyBounce;
    [SerializeField] private float transitionDuration = 0.65f;
    [SerializeField] private float startDelay = 0.5f;

    [Header("Day & Night Sync")]
    [SerializeField] private bool syncWithDayNightManager = true;
    [SerializeField] private float endOfDayHour = 22f;
    [SerializeField] private float startOfDayHour = 6f;

    private int currentDayNumber = 1;
    private bool isDayEnded = false;
    private Coroutine activeTransition;

    void Awake()
    {
        if (boarderPanelRect == null)
            boarderPanelRect = GetComponent<RectTransform>();

        if (boarderPanelRect != null)
            boarderPanelRect.anchoredPosition = hiddenPosition;
    }

    void Start()
    {
        StartCoroutine(InitialEntryRoutine());
    }

    private IEnumerator InitialEntryRoutine()
    {
        yield return new WaitForSeconds(startDelay);
        SlideIn();
    }

    void Update()
    {
        if (!syncWithDayNightManager || DayNightManager.Instance == null) return;

        float currentHour = DayNightManager.Instance.GetCurrentGameHour24();

        if (!isDayEnded && currentHour >= endOfDayHour)
        {
            isDayEnded = true;
            SlideOut();
        }
        else if (isDayEnded && currentHour >= startOfDayHour && currentHour < endOfDayHour)
        {
            isDayEnded = false;
            currentDayNumber++;
            StartNewDay(currentDayNumber);
        }
    }

    public void SlideIn()
    {
        PlayTransition(visiblePosition, style);
    }

    public void SlideOut()
    {
        PlayTransition(hiddenPosition, AnimationStyle.SmoothEase);
    }

    public void StartNewDay(int newDay)
    {
        currentDayNumber = newDay;
        if (dayText != null)
        {
            dayText.text = "Days " + currentDayNumber;
        }

        SlideIn();
    }

    private void PlayTransition(Vector2 targetPos, AnimationStyle animStyle)
    {
        if (activeTransition != null)
            StopCoroutine(activeTransition);

        activeTransition = StartCoroutine(AnimateRoutine(targetPos, animStyle));
    }

    private IEnumerator AnimateRoutine(Vector2 targetPos, AnimationStyle animStyle)
    {
        Vector2 startPos = boarderPanelRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float curve;

            if (animStyle == AnimationStyle.CozyBounce)
            {
                float overshoot = 1.3f;
                curve = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                boarderPanelRect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curve);
            }
            else
            {
                curve = Mathf.SmoothStep(0f, 1f, t);
                boarderPanelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curve);
            }

            yield return null;
        }

        boarderPanelRect.anchoredPosition = targetPos;
    }
}