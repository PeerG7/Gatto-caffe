using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CookingManager : MonoBehaviour, IGamepadNavigable //[cite: 2]
{
    [Header("Recipe Data")]
    public List<RecipeSO> allRecipes; //[cite: 2]
    public RecipeSO failedDishRecipe; //[cite: 2]

    [Header("Ingredients & Board")]
    public List<string> currentIngredients = new List<string>(); //[cite: 2]
    public Image[] boardSlots; //[cite: 2]

    [Header("UI Controls")]
    public Button cookButton; //[cite: 2]
    public GameObject cookingVisuals; //[cite: 2]
    public Image cookingProgressBar; //[cite: 2]

    [Header("Settings")]
    public float cookingDuration = 3f; //[cite: 2]

    public GameObject stationCookingCanvas; //[cite: 2]

    [Header("Deep Fry")]
    public DeepFryStation deepFryStation; //[cite: 2]
    public Button deepFryButton; //[cite: 2]

    [Header("SFX")]
    public AudioSource sfxSource; //[cite: 2]
    public AudioClip cookingSoundClip; //[cite: 2]
    public AudioClip completeSoundClip; //[cite: 2]

    [Header("Gamepad / Keyboard Navigation")]
    public GameObject firstSelectedButton; //[cite: 2]

#if ENABLE_INPUT_SYSTEM
    public UnityEngine.InputSystem.InputActionReference backAction; //[cite: 2]
#endif

    private IngredientButton[] _registeredIngredients = new IngredientButton[7]; //[cite: 2]

    public void RegisterIngredientButton(IngredientButton btn, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 7)
            _registeredIngredients[slotIndex] = btn; //[cite: 2]
    }

    private static readonly KeyCode[] _ingredientHotkeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7
    }; //[cite: 2]

    private static readonly KeyCode[] _confirmKeys = {
        KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space
    }; //[cite: 2]

    private const KeyCode DEEPFRY_KEY = KeyCode.F; //[cite: 2]

    private RecipeSO currentOutput; //[cite: 2]
    private bool isFailed = false; //[cite: 2]
    private Coroutine cookCoroutine = null; //[cite: 2]
    private bool isCooking = false; //[cite: 2]

    void Start()
    {
        if (cookButton != null) cookButton.interactable = false; //[cite: 2]
        ResetBoardVisuals(); //[cite: 2]
        if (cookingVisuals != null) cookingVisuals.SetActive(false); //[cite: 2]
        if (cookingProgressBar != null) cookingProgressBar.gameObject.SetActive(false); //[cite: 2]
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable() { if (backAction != null) backAction.action.Enable(); } //[cite: 2]
    void OnDisable() { if (backAction != null) backAction.action.Disable(); } //[cite: 2]
#endif

    void Update()
    {
        if (stationCookingCanvas == null || !stationCookingCanvas.activeSelf) return; //[cite: 2]
        if (isCooking) return; //[cite: 2]

        bool backPressed = Input.GetKeyDown(KeyCode.Escape); //[cite: 2]
#if ENABLE_INPUT_SYSTEM
        if (!backPressed && backAction != null && backAction.action.WasPressedThisFrame())
            backPressed = true; //[cite: 2]
#endif
        if (backPressed) { CloseCanvas(); return; } //[cite: 2]

        // --- INGREDIENT REMOVAL HOTKEYS ---
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RemoveLastIngredient();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            ClearAllIngredients();
            return;
        }

        for (int i = 0; i < _ingredientHotkeys.Length; i++)
            if (Input.GetKeyDown(_ingredientHotkeys[i])) { TryAddIngredientBySlot(i); return; } //[cite: 2]

        if (Input.GetKeyDown(DEEPFRY_KEY)) { TryTriggerDeepFry(); return; } //[cite: 2]

        foreach (KeyCode key in _confirmKeys)
            if (Input.GetKeyDown(key))
            {
                if (cookButton != null && cookButton.interactable) OnCookButtonClicked(); //[cite: 2]
                return;
            }
    }

    void TryAddIngredientBySlot(int i)
    {
        IngredientButton btn = _registeredIngredients[i]; //[cite: 2]
        if (btn == null) { Debug.LogWarning($"[CookingManager] ไม่มี IngredientButton ที่ slot {i + 1}"); return; } //[cite: 2]
        btn.TriggerIngredient(); //[cite: 2]
    }

    void TryTriggerDeepFry()
    {
        if (deepFryButton != null && !deepFryButton.interactable) return; //[cite: 2]
        if (deepFryStation != null) deepFryStation.OnFryButtonClicked(); //[cite: 2]
        else Debug.LogWarning("[CookingManager] ไม่ได้ผูก DeepFryStation"); //[cite: 2]
    }

    public void OnConfirm() { if (cookButton != null && cookButton.interactable && !isCooking) OnCookButtonClicked(); } //[cite: 2]
    public void OnBack() => CloseCanvas(); //[cite: 2]
    public void OnNavigate(Vector2 direction) { } //[cite: 2]

    public void OpenCanvas()
    {
        if (stationCookingCanvas != null) stationCookingCanvas.SetActive(true); //[cite: 2]
        PlayerController2D.IsLocked = true; //[cite: 2]
        PlayerInteract2D.RegisterActiveCanvas(this); //[cite: 2]
        DayNightManager.Instance?.PauseGame(); //[cite: 2]
        SetInitialFocus(); //[cite: 2]
    }

    public void CloseCanvas()
    {
        if (stationCookingCanvas != null) stationCookingCanvas.SetActive(false); //[cite: 2]
        PlayerInteract2D.UnregisterActiveCanvas(this); //[cite: 2]

        if (isCooking)
        {
            if (cookCoroutine != null) { StopCoroutine(cookCoroutine); cookCoroutine = null; } //[cite: 2]
            if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop(); //[cite: 2]
            isCooking = false; //[cite: 2]
            if (cookingVisuals != null) cookingVisuals.SetActive(false); //[cite: 2]
            if (cookingProgressBar != null) { cookingProgressBar.fillAmount = 0; cookingProgressBar.gameObject.SetActive(false); } //[cite: 2]
            currentIngredients.Clear(); //[cite: 2]
            ResetBoardVisuals(); //[cite: 2]
            CheckRecipe(); //[cite: 2]
        }

        PlayerController2D.IsLocked = false; //[cite: 2]
        DayNightManager.Instance?.ResumeGame(); //[cite: 2]
    }

    void SetInitialFocus()
    {
        if (EventSystem.current == null) return; //[cite: 2]
        GameObject target = firstSelectedButton != null ? firstSelectedButton
            : (cookButton != null ? cookButton.gameObject : null); //[cite: 2]
        if (target != null) EventSystem.current.SetSelectedGameObject(target); //[cite: 2]
    }

    public void AddIngredient(string ingredientName, Sprite ingredientSprite)
    {
        if (isCooking) return; //[cite: 2]
        if (currentIngredients.Count < 3) //[cite: 2]
        {
            currentIngredients.Add(ingredientName); //[cite: 2]
            int index = currentIngredients.Count - 1; //[cite: 2]
            if (index < boardSlots.Length) { boardSlots[index].sprite = ingredientSprite; boardSlots[index].enabled = true; } //[cite: 2]
            CheckRecipe(); //[cite: 2]
        }
    }

    // =====================================================================
    // REMOVAL FUNCTIONS
    // =====================================================================

    /// <summary>
    /// Removes the last added ingredient (Backspace hotkey).
    /// </summary>
    public void RemoveLastIngredient()
    {
        if (isCooking || currentIngredients.Count == 0) return;
        RemoveIngredientAt(currentIngredients.Count - 1);
    }

    /// <summary>
    /// Removes an ingredient at a specific board slot index (0, 1, or 2) and shifts sprites left.
    /// </summary>
    public void RemoveIngredientAt(int index)
    {
        if (isCooking || index < 0 || index >= currentIngredients.Count) return;

        currentIngredients.RemoveAt(index);

        // Shift remaining visual slot images to the left
        for (int i = index; i < boardSlots.Length - 1; i++)
        {
            if (boardSlots[i] != null && boardSlots[i + 1] != null)
            {
                boardSlots[i].sprite = boardSlots[i + 1].sprite;
                boardSlots[i].enabled = boardSlots[i + 1].enabled;
            }
        }

        // Clear the last visual slot
        int clearIndex = currentIngredients.Count;
        if (clearIndex >= 0 && clearIndex < boardSlots.Length && boardSlots[clearIndex] != null)
        {
            boardSlots[clearIndex].sprite = null;
            boardSlots[clearIndex].enabled = false;
        }

        CheckRecipe();
    }

    /// <summary>
    /// Clears all ingredients from the board at once (Delete hotkey).
    /// </summary>
    public void ClearAllIngredients()
    {
        if (isCooking || currentIngredients.Count == 0) return;

        currentIngredients.Clear();
        ResetBoardVisuals();
        CheckRecipe();
    }

    // =====================================================================

    void CheckRecipe()
    {
        isFailed = false; //[cite: 2]
        currentOutput = null; //[cite: 2]
        foreach (var recipe in allRecipes) //[cite: 2]
        {
            if (IsMatch(recipe)) { currentOutput = recipe; if (cookButton != null) cookButton.interactable = true; return; } //[cite: 2]
        }
        if (currentIngredients.Count == 3) { currentOutput = failedDishRecipe; isFailed = true; if (cookButton != null) cookButton.interactable = true; } //[cite: 2]
        else { if (cookButton != null) cookButton.interactable = false; } //[cite: 2]
    }

    bool IsMatch(RecipeSO recipe)
    {
        if (recipe.requiredIngredients.Count != currentIngredients.Count) return false; //[cite: 2]
        List<string> playerInput = new List<string>(currentIngredients); //[cite: 2]
        foreach (string required in recipe.requiredIngredients) //[cite: 2]
        {
            string found = playerInput.Find(x => x.Equals(required, System.StringComparison.OrdinalIgnoreCase)); //[cite: 2]
            if (found != null) playerInput.Remove(found); else return false; //[cite: 2]
        }
        return playerInput.Count == 0; //[cite: 2]
    }

    public void OnCookButtonClicked()
    {
        if (currentOutput == null || isCooking) return; //[cite: 2]
        if (TrayManager.instance != null && !TrayManager.instance.HasEmptySlot()) { Debug.LogWarning("[CookingManager] Tray เต็ม!"); return; } //[cite: 2]
        PlayerController2D.IsLocked = true; //[cite: 2]
        cookCoroutine = StartCoroutine(PerformCookingCoroutine()); //[cite: 2]
    }

    private IEnumerator PerformCookingCoroutine()
    {
        isCooking = true; //[cite: 2]
        if (cookButton != null) cookButton.interactable = false; //[cite: 2]
        ResetBoardVisuals(); //[cite: 2]
        if (cookingVisuals != null) cookingVisuals.SetActive(true); //[cite: 2]
        if (cookingProgressBar != null) { cookingProgressBar.gameObject.SetActive(true); cookingProgressBar.fillAmount = 0f; } //[cite: 2]
        if (sfxSource != null && cookingSoundClip != null) { sfxSource.clip = cookingSoundClip; sfxSource.loop = false; sfxSource.Play(); } //[cite: 2]

        float elapsed = 0f; //[cite: 2]
        while (elapsed < cookingDuration) //[cite: 2]
        {
            elapsed += Time.deltaTime; //[cite: 2]
            if (cookingProgressBar != null) cookingProgressBar.fillAmount = elapsed / cookingDuration; //[cite: 2]
            yield return null; //[cite: 2]
        }

        cookCoroutine = null; //[cite: 2]
        isCooking = false; //[cite: 2]
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop(); //[cite: 2]
        PlayCompleteSound(); //[cite: 2]

        if (TrayManager.instance != null) //[cite: 2]
        {
            bool placed = TrayManager.instance.ReceiveFood(currentOutput.recipeName, currentOutput.finalDishSprite); //[cite: 2]
            if (!placed) Debug.LogWarning("[CookingManager] Tray เต็ม"); //[cite: 2]
        }
        else
        {
            PlayerInventory player = FindObjectOfType<PlayerInventory>(); //[cite: 2]
            if (player != null) player.PickUpItem(currentOutput.recipeName, currentOutput.finalDishSprite); //[cite: 2]
        }

        if (cookingVisuals != null) cookingVisuals.SetActive(false); //[cite: 2]
        if (cookingProgressBar != null) cookingProgressBar.gameObject.SetActive(false); //[cite: 2]
        currentIngredients.Clear(); //[cite: 2]
        CheckRecipe(); //[cite: 2]

        if (stationCookingCanvas != null) stationCookingCanvas.SetActive(false); //[cite: 2]
        PlayerInteract2D.UnregisterActiveCanvas(this); //[cite: 2]
        PlayerController2D.IsLocked = false; //[cite: 2]
        DayNightManager.Instance?.ResumeGame(); //[cite: 2]
    }

    void PlayCompleteSound()
    {
        if (sfxSource != null && completeSoundClip != null) sfxSource.PlayOneShot(completeSoundClip); //[cite: 2]
        else if (AudioManager.instance != null) AudioManager.instance.PlayComplete(); //[cite: 2]
    }

    void ResetBoardVisuals()
    {
        foreach (var slot in boardSlots)
            if (slot != null) { slot.sprite = null; slot.enabled = false; } //[cite: 2]
    }
}