using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CookingManager : MonoBehaviour
{
    [Header("Recipe Data")]
    public List<RecipeSO> allRecipes;
    public RecipeSO failedDishRecipe;

    [Header("Ingredients & Board")]
    public List<string> currentIngredients = new List<string>();
    public Image[] boardSlots;

    [Header("UI Controls")]
    public Button cookButton;
    public GameObject cookingVisuals;
    public Image cookingProgressBar;

    [Header("Settings")]
    public float cookingDuration = 3f;

    public GameObject stationCookingCanvas;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip cookingSoundClip;
    public AudioClip completeSoundClip;

    private RecipeSO currentOutput;
    private bool isFailed = false;
    private Coroutine cookCoroutine = null;
    private bool isCooking = false;

    void Start()
    {
        if (cookButton != null) cookButton.interactable = false;
        ResetBoardVisuals();
        if (cookingVisuals != null) cookingVisuals.SetActive(false);
        if (cookingProgressBar != null) cookingProgressBar.gameObject.SetActive(false);
    }

    public void OpenCanvas()
    {
        if (stationCookingCanvas != null)
            stationCookingCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;
    }

    public void CloseCanvas()
    {
        if (stationCookingCanvas != null)
            stationCookingCanvas.SetActive(false);

        if (isCooking)
        {
            if (cookCoroutine != null) { StopCoroutine(cookCoroutine); cookCoroutine = null; }
            if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();

            isCooking = false;
            if (cookingVisuals != null) cookingVisuals.SetActive(false);
            if (cookingProgressBar != null)
            {
                cookingProgressBar.fillAmount = 0;
                cookingProgressBar.gameObject.SetActive(false);
            }

            currentIngredients.Clear();
            ResetBoardVisuals();
            CheckRecipe();
        }

        PlayerController2D.IsLocked = false;
    }

    public void AddIngredient(string ingredientName, Sprite ingredientSprite)
    {
        if (isCooking) return;
        if (currentIngredients.Count < 3)
        {
            currentIngredients.Add(ingredientName);
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

        foreach (var recipe in allRecipes)
        {
            if (IsMatch(recipe))
            {
                currentOutput = recipe;
                if (cookButton != null) cookButton.interactable = true;
                return;
            }
        }

        if (currentIngredients.Count == 3)
        {
            currentOutput = failedDishRecipe;
            isFailed = true;
            if (cookButton != null) cookButton.interactable = true;
        }
        else
        {
            if (cookButton != null) cookButton.interactable = false;
        }
    }

    bool IsMatch(RecipeSO recipe)
    {
        if (recipe.requiredIngredients.Count != currentIngredients.Count) return false;
        List<string> playerInput = new List<string>(currentIngredients);
        foreach (string required in recipe.requiredIngredients)
        {
            string found = playerInput.Find(x => x.Equals(required, System.StringComparison.OrdinalIgnoreCase));
            if (found != null) playerInput.Remove(found);
            else return false;
        }
        return playerInput.Count == 0;
    }

    public void OnCookButtonClicked()
    {
        if (currentOutput == null || isCooking) return;

        // ✅ เช็ค Tray ว่างก่อน Cook — ถ้าเต็มทุก slot ให้ block
        if (TrayManager.instance != null && !TrayManager.instance.HasEmptySlot())
        {
            Debug.LogWarning("[CookingManager] Tray เต็มทุก slot! รอ player หยิบอาหารก่อน");
            // TODO: แสดง UI แจ้งเตือน "Tray เต็ม" ให้ player รู้
            return;
        }

        PlayerController2D.IsLocked = true;
        cookCoroutine = StartCoroutine(PerformCookingCoroutine());
    }

    private IEnumerator PerformCookingCoroutine()
    {
        isCooking = true;
        if (cookButton != null) cookButton.interactable = false;
        ResetBoardVisuals();

        if (cookingVisuals != null) cookingVisuals.SetActive(true);
        if (cookingProgressBar != null)
        {
            cookingProgressBar.gameObject.SetActive(true);
            cookingProgressBar.fillAmount = 0f;
        }

        if (sfxSource != null && cookingSoundClip != null)
        {
            sfxSource.clip = cookingSoundClip;
            sfxSource.loop = false;
            sfxSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < cookingDuration)
        {
            elapsed += Time.deltaTime;
            if (cookingProgressBar != null)
                cookingProgressBar.fillAmount = elapsed / cookingDuration;
            yield return null;
        }

        cookCoroutine = null;
        isCooking = false;

        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
        PlayCompleteSound();

        // ✅ ส่งอาหารไป TrayManager แทนที่จะส่งตรงไป PlayerInventory
        if (TrayManager.instance != null)
        {
            bool placed = TrayManager.instance.ReceiveFood(
                currentOutput.recipeName,
                currentOutput.finalDishSprite
            );
            if (!placed)
                Debug.LogWarning("[CookingManager] วางลง Tray ไม่ได้ — Tray เต็ม");
        }
        else
        {
            // Fallback: ถ้าไม่มี TrayManager ให้ส่งตรงๆ แบบเดิม
            Debug.LogWarning("[CookingManager] ไม่พบ TrayManager — ใช้ระบบเดิม");
            PlayerInventory player = FindObjectOfType<PlayerInventory>();
            if (player != null)
                player.PickUpItem(currentOutput.recipeName, currentOutput.finalDishSprite);
        }

        if (isFailed)
            Debug.Log("Cooking_Failure: " + string.Join(", ", currentIngredients));

        if (cookingVisuals != null) cookingVisuals.SetActive(false);
        if (cookingProgressBar != null) cookingProgressBar.gameObject.SetActive(false);

        currentIngredients.Clear();
        CheckRecipe();

        if (stationCookingCanvas != null)
            stationCookingCanvas.SetActive(false);

        PlayerController2D.IsLocked = false;
    }

    void PlayCompleteSound()
    {
        if (sfxSource != null && completeSoundClip != null)
            sfxSource.PlayOneShot(completeSoundClip);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayComplete();
    }

    void ResetBoardVisuals()
    {
        foreach (var slot in boardSlots)
            if (slot != null) { slot.sprite = null; slot.enabled = false; }
    }
}