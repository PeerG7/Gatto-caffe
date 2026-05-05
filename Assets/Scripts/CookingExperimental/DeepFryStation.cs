using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeepFryStation : MonoBehaviour
{
    [Header("UI Elements")]
    public Image fillImage;         // Fill image showing frying progress (e.g. a golden-brown overlay)
    public Button fryButton;        // Button the player clicks to start frying

    [Header("Settings")]
    public float fryDuration = 4f;  // How long the frying takes (seconds)
    public string foodName = "Tempura";
    public Sprite foodSprite;

    public GameObject stationFryCanvas;

    [Header("SFX")]
    public AudioSource sfxSource;       // Separate AudioSource (not BGM)
    public AudioClip frySoundClip;      // Drag your sizzling/frying sound clip here

    private Coroutine fryCoroutine = null;
    private bool isProcessing = false;

    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }

    public void OpenCanvas()
    {
        if (stationFryCanvas != null)
            stationFryCanvas.SetActive(true);

        PlayerController2D.IsLocked = true;
    }

    public void CloseCanvas()
    {
        if (stationFryCanvas != null)
            stationFryCanvas.SetActive(false);

        if (isProcessing)
        {
            if (fryCoroutine != null)
            {
                StopCoroutine(fryCoroutine);
                fryCoroutine = null;
            }

            // Stop sizzle sound if canvas is closed mid-fry
            if (sfxSource != null && sfxSource.isPlaying)
                sfxSource.Stop();

            ResetStation();
        }

        PlayerController2D.IsLocked = false;
    }

    public void OnFryButtonClicked()
    {
        if (isProcessing) return;
        PlayerController2D.IsLocked = true;
        fryCoroutine = StartCoroutine(FryCoroutine());
    }

    private IEnumerator FryCoroutine()
    {
        isProcessing = true;
        if (fryButton != null) fryButton.interactable = false;

        if (fillImage != null)
        {
            fillImage.gameObject.SetActive(true);
            fillImage.fillAmount = 0;
        }

        // Play sizzling sound as soon as frying starts
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
            yield return null;
        }

        fryCoroutine = null;

        // Give the fried item to the player
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
            player.PickUpItem(foodName, foodSprite);

        ResetStation();

        if (stationFryCanvas != null)
            stationFryCanvas.SetActive(false);

        PlayerController2D.IsLocked = false;
    }

    void ResetStation()
    {
        isProcessing = false;
        if (fryButton != null) fryButton.interactable = true;
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }
}