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

    // StationInteract หรือ E-key เรียก method นี้
    public void OpenCanvas()
    {
        if (stationDrinksCanvas != null)
            stationDrinksCanvas.SetActive(true);

        PlayerController2D.IsLocked = true;
    }

    // ปุ่ม Close / Leave ใน canvas เรียก method นี้
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
            ResetStation();
        }

        PlayerController2D.IsLocked = false;
    }

    public void OnDrinkButtonClicked()
    {
        if (isProcessing) return;
        // lock ซ้ำตรงนี้ด้วยเพื่อกัน edge case ที่ OpenCanvas ถูก bypass
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

        // unlock เฉพาะเมื่อ process จบสำเร็จ
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