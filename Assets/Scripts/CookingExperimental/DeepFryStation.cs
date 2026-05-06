using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeepFryStation : MonoBehaviour
{
    [Header("UI Elements")]
    public Image fillImage;
    public Button fryButton;

    [Header("Settings")]
    public float fryDuration = 4f;
    public string foodName = "Tempura";
    public Sprite foodSprite;

    public GameObject stationFryCanvas;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip frySoundClip;
    public AudioClip completeSoundClip;  // ✅ เสียงเมื่อหลอดเต็ม (override AudioManager.sfxComplete)

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

        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();

        // ✅ เล่นเสียง complete เมื่อหลอดเต็ม
        PlayCompleteSound();

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
            player.PickUpItem(foodName, foodSprite);

        ResetStation();

        if (stationFryCanvas != null)
            stationFryCanvas.SetActive(false);

        PlayerController2D.IsLocked = false;
    }

    /// <summary>เล่นเสียง complete — ใช้ completeSoundClip ถ้ามี ไม่งั้นใช้ AudioManager</summary>
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
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }
}