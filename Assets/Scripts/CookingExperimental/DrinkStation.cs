using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DrinkStation : MonoBehaviour
{
    [Header("UI Elements")]
    public Image fillImage;
    public Button drinkButton;

    [Header("Settings")]
    public float fillDuration = 3f;
    public string drinkName = "Milk";
    public Sprite drinkSprite;

    public GameObject stationDrinksCanvas;

    [Header("SFX")]
    public AudioSource sfxSource;        // AudioSource แยกต่างหาก (ไม่ใช่ BGM)
    public AudioClip pourSoundClip;      // ลาก pour_water_FIX_6sec มาใส่

    private Coroutine fillCoroutine = null;
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
        if (stationDrinksCanvas != null)
            stationDrinksCanvas.SetActive(true);

        PlayerController2D.IsLocked = true;
    }

    public void CloseCanvas()
    {
        if (stationDrinksCanvas != null)
            stationDrinksCanvas.SetActive(false);

        if (isProcessing)
        {
            if (fillCoroutine != null)
            {
                StopCoroutine(fillCoroutine);
                fillCoroutine = null;
            }

            // หยุดเสียงถ้าปิด canvas กลางคัน
            if (sfxSource != null && sfxSource.isPlaying)
                sfxSource.Stop();

            ResetStation();
        }

        PlayerController2D.IsLocked = false;
    }

    public void OnDrinkButtonClicked()
    {
        if (isProcessing) return;
        PlayerController2D.IsLocked = true;
        fillCoroutine = StartCoroutine(FillDrinkCoroutine());
    }

    private IEnumerator FillDrinkCoroutine()
    {
        isProcessing = true;
        if (drinkButton != null) drinkButton.interactable = false;

        if (fillImage != null)
        {
            fillImage.gameObject.SetActive(true);
            fillImage.fillAmount = 0;
        }

        // เล่นเสียงเทน้ำพร้อมกับเริ่ม fill
        if (sfxSource != null && pourSoundClip != null)
        {
            sfxSource.clip = pourSoundClip;
            sfxSource.loop = false;
            sfxSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
                fillImage.fillAmount = elapsed / fillDuration;
            yield return null;
        }

        fillCoroutine = null;

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
            player.PickUpItem(drinkName, drinkSprite);

        ResetStation();

        if (stationDrinksCanvas != null)
            stationDrinksCanvas.SetActive(false);

        PlayerController2D.IsLocked = false;
    }

    void ResetStation()
    {
        isProcessing = false;
        if (drinkButton != null) drinkButton.interactable = true;
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }
}