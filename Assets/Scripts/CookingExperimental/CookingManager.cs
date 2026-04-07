using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CookingManager : MonoBehaviour
{
    [Header("Recipe Data")]
    public List<RecipeSO> allRecipes;
    public RecipeSO failedDishRecipe; // สูตรสำหรับอาหารขยะ (Method 1)

    [Header("Ingredients & Board")]
    public List<string> currentIngredients = new List<string>();
    public Image[] boardSlots; // สล็อต 3 ช่องบนเขียง 

    [Header("UI Controls")]
    public Button cookButton;
    public GameObject cookingVisuals; // ภาพตอนอาหารกำลังสุกในกระทะ
    public Image cookingProgressBar;  // หลอดเวลา (Fill Amount)

    [Header("Settings")]
    public float cookingDuration = 3f; // เวลาทำอาหาร 3 วินาทีตาม Storyboard 

    private RecipeSO currentOutput;
    private bool isFailed = false;

    public GameObject stationCookingCanvas; // ลาก Canvas ทำอาหาร หรือ Canvas ตู้กดน้ำ มาใส่ที่นี่


    public void CloseCanvas()
    {
        if (stationCookingCanvas != null)
        {
            stationCookingCanvas.SetActive(false);
        }
    }
    void Start()
    {
        cookButton.interactable = false;
        ResetBoardVisuals();

        // ปิด UI ทำอาหารไว้ก่อนตอนเริ่มเกม
        if (cookingVisuals != null) cookingVisuals.SetActive(false);
        if (cookingProgressBar != null) cookingProgressBar.gameObject.SetActive(false);
    }

    // ฟังก์ชันรับวัตถุดิบ (ถูกเรียกจากสคริปต์ IngredientButton)
    public void AddIngredient(string ingredientName, Sprite ingredientSprite)
    {
        if (currentIngredients.Count < 3)
        {
            currentIngredients.Add(ingredientName);

            // แสดงผลภาพบนเขียงตามลำดับ
            int index = currentIngredients.Count - 1;
            if (index < boardSlots.Length)
            {
                boardSlots[index].sprite = ingredientSprite;
                boardSlots[index].enabled = true;
            }

            CheckRecipe();
        }
    }

    void CheckRecipe()
    {
        isFailed = false;
        currentOutput = null;

        // 1. ตรวจสอบว่าตรงกับสูตรไหนไหม
        foreach (var recipe in allRecipes)
        {
            if (IsMatch(recipe))
            {
                currentOutput = recipe;
                cookButton.interactable = true;
                return;
            }
        }

        // 2. ถ้าใส่ครบ 3 อย่างแต่ไม่ตรงสูตรเลย -> ได้อาหารขยะ (Method 1)
        if (currentIngredients.Count == 3)
        {
            currentOutput = failedDishRecipe;
            isFailed = true;
            cookButton.interactable = true;
        }
        else
        {
            cookButton.interactable = false;
        }
    }

    bool IsMatch(RecipeSO recipe)
    {
        // 1. ตรวจสอบก่อนว่าจำนวนวัตถุดิบเท่ากันไหม (ต้องเป็น 3 เท่ากัน)
        if (recipe.requiredIngredients.Count != currentIngredients.Count) return false;

        // 2. สร้างสำเนาของวัตถุดิบที่ผู้เล่นเลือกมา
        List<string> playerInput = new List<string>(currentIngredients);

        // 3. ไล่เช็คทีละอย่างที่สูตรต้องการ
        foreach (string requiredItem in recipe.requiredIngredients)
        {
            // ค้นหาวัตถุดิบในมือผู้เล่นที่ตรงกับสูตร (ไม่สนตัวเล็กตัวใหญ่)
            string found = playerInput.Find(x => x.Equals(requiredItem, System.StringComparison.OrdinalIgnoreCase));

            if (found != null)
            {
                // ถ้าเจอ ให้ลบออกจากลิสต์จำลอง (เพื่อไม่ให้ตัวเดิมถูกนับซ้ำ)
                playerInput.Remove(found);
            }
            else
            {
                // ถ้าหาไม่เจอแม้แต่อันเดียว แสดงว่าผิดสูตร
                return false;
            }
        }

        // 4. ถ้าเช็คครบทุกตัวแล้วเหลือ playerInput เป็น 0 แสดงว่าตรงกันเป๊ะ
        return playerInput.Count == 0;
    }

    // ฟังก์ชันเมื่อคลิกปุ่ม COOK
    public void OnCookButtonClicked()
    {
        if (currentOutput != null)
        {
            StartCoroutine(PerformCookingCoroutine());
        }
    }

    // Coroutine สำหรับลูปลำปรุงอาหาร
    private IEnumerator PerformCookingCoroutine()
    {
        cookButton.interactable = false; // ปิดปุ่มระหว่างทำอาหาร

        // ซ่อนวัตถุดิบบนเขียง
        ResetBoardVisuals();

        // เปิด Visuals และหลอดเวลาในกระทะ
        if (cookingVisuals != null) cookingVisuals.SetActive(true);
        if (cookingProgressBar != null)
        {
            cookingProgressBar.gameObject.SetActive(true);
            cookingProgressBar.fillAmount = 0f;
        }

        // ลูปลำปรุงอาหารตามเวลาที่กำหนด
        float elapsedTime = 0f;
        while (elapsedTime < cookingDuration)
        {
            elapsedTime += Time.deltaTime;
            if (cookingProgressBar != null)
            {
                cookingProgressBar.fillAmount = elapsedTime / cookingDuration;
            }
            yield return null;
        }

        // ทำอาหารเสร็จสมบูรณ์
        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
        {
            player.PickUpItem(currentOutput.recipeName, currentOutput.finalDishSprite);

            if (isFailed)
            {
                Debug.Log("Cooking_Failure: ผู้เล่นผสมพลาดด้วย " + string.Join(", ", currentIngredients));
            }
        }

        // รีเซ็ตสถานะหน้าต่างครัว
        if (cookingVisuals != null) cookingVisuals.SetActive(false);
        if (cookingProgressBar != null) cookingProgressBar.gameObject.SetActive(false);

        currentIngredients.Clear();
        CheckRecipe();
    }

    void ResetBoardVisuals()
    {
        foreach (var slot in boardSlots)
        {
            if (slot != null)
            {
                slot.sprite = null;
                slot.enabled = false;
            }
        }
    }
}