using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// DeepFryStation — v5 (With Dynamic Sprite State & UI Shake Effect)
// =====================================================================
public class DeepFryStation : MonoBehaviour
{
    [Header("UI Elements")]
    public Image fillImage;
    public Button fryButton;

    [Header("Station Visuals")]
    public Image stationImage;       // UI Image component for the station
    public Sprite emptySprite;       // Sprite when station is idle
    public Sprite fryingSprite;      // Sprite when deep-frying is in progress

    [Header("Shake Settings (New)")]
    [Tooltip("Target RectTransform to shake. Leaves empty to automatically shake stationImage.")]
    public RectTransform shakeTarget;
    [Tooltip("Strength of the shake in pixels.")]
    public float shakeIntensity = 3f;

    [Header("Settings")]
    public float fryDuration = 4f;
    public string foodName = "Tempura";
    public Sprite foodSprite;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip frySoundClip;
    public AudioClip completeSoundClip;

    private Coroutine fryCoroutine = null;
    private bool isProcessing = false;
    private Vector2 originalPosition;

    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }

        // Default shakeTarget to stationImage if not assigned
        if (shakeTarget == null && stationImage != null)
        {
            shakeTarget = stationImage.rectTransform;
        }

        if (shakeTarget != null)
        {
            originalPosition = shakeTarget.anchoredPosition;
        }

        UpdateVisualState(false);
    }

    public void OpenCanvas() { /* Managed by CookingManager */ }
    public void CloseCanvas() { /* Managed by CookingManager */ }

    public void OnFryButtonClicked()
    {
        if (isProcessing) return;
        fryCoroutine = StartCoroutine(FryCoroutine());
    }

    private IEnumerator FryCoroutine()
    {
        isProcessing = true;
        if (fryButton != null) fryButton.interactable = false;

        // Store original position prior to shaking
        if (shakeTarget != null)
            originalPosition = shakeTarget.anchoredPosition;

        UpdateVisualState(true);

        if (fillImage != null)
        {
            fillImage.gameObject.SetActive(true);
            fillImage.fillAmount = 0;
        }

        if (sfxSource != null && frySoundClip != null)
        {
            sfxSource.clip = frySoundClip;
            sfxSource.loop = false;
            sfxSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < fryDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
                fillImage.fillAmount = elapsed / fryDuration;

            // Apply slight random offset each frame
            if (shakeTarget != null)
            {
                shakeTarget.anchoredPosition = originalPosition + (Random.insideUnitCircle * shakeIntensity);
            }

            yield return null;
        }

        fryCoroutine = null;

        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();

        PlayCompleteSound();

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
            player.PickUpItem(foodName, foodSprite);

        ResetStation();
    }

    void PlayCompleteSound()
    {
        if (sfxSource != null && completeSoundClip != null)
            sfxSource.PlayOneShot(completeSoundClip);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayComplete();
    }

    void ResetStation()
    {
        isProcessing = false;
        if (fryButton != null) fryButton.interactable = true;

        // Reset anchored position back to exact original coordinates
        if (shakeTarget != null)
        {
            shakeTarget.anchoredPosition = originalPosition;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }

        UpdateVisualState(false);
    }

    void UpdateVisualState(bool isFrying)
    {
        if (stationImage != null)
        {
            if (isFrying && fryingSprite != null)
            {
                stationImage.sprite = fryingSprite;
            }
            else if (!isFrying && emptySprite != null)
            {
                stationImage.sprite = emptySprite;
            }
        }
    }

    public bool IsProcessing => isProcessing;
}