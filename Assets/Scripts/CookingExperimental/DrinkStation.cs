using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrinkStation : MonoBehaviour
{
    [Header("UI Elements")]
    public Image fillImage; // Image ที่ตั้งเป็น Filled (Radial 360)
    public Button drinkButton; // ลากปุ่มกดน้ำมาใส่ที่นี่

    [Header("Settings")]
    public float fillDuration = 3f; // เวลาในการกดน้ำ 3 วินาที
    public string drinkName = "Milk";
    public Sprite drinkSprite;

    private bool isProcessing = false;
    public GameObject stationDrinksCanvas; // ลาก Canvas ทำอาหาร หรือ Canvas ตู้กดน้ำ มาใส่ที่นี่


    public void CloseCanvas()
    {
        if (stationDrinksCanvas != null)
        {
            stationDrinksCanvas.SetActive(false);
        }
    }
    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false); // ซ่อนหลอดโหลดไว้ก่อน
        }
    }

    // ฟังก์ชันสำหรับผูกกับ OnClick ของ Button
    public void OnDrinkButtonClicked()
    {
        if (!isProcessing)
        {
            StartCoroutine(FillDrinkCoroutine());
        }
    }

    private IEnumerator FillDrinkCoroutine()
    {
        isProcessing = true;
        drinkButton.interactable = false; // ปิดปุ่มระหว่างทำงาน

        if (fillImage != null)
        {
            fillImage.gameObject.SetActive(true);
            fillImage.fillAmount = 0;
        }

        float elapsedTime = 0f;
        while (elapsedTime < fillDuration)
        {
            elapsedTime += Time.deltaTime;
            if (fillImage != null)
            {
                fillImage.fillAmount = elapsedTime / fillDuration; // อัปเดตหลอดโหลด
            }
            yield return null;
        }

        CompleteDrink();
    }

    void CompleteDrink()
    {
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
        {
            player.PickUpItem(drinkName, drinkSprite); // ส่งไอเทมให้ผู้เล่น
        }

        ResetStation();
    }

    void ResetStation()
    {
        isProcessing = false;
        drinkButton.interactable = true;
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }
}