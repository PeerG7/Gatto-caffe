using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CookingManager : MonoBehaviour, IGamepadNavigable
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

    [Header("Deep Fry")]
    public DeepFryStation deepFryStation;
    public Button deepFryButton;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip cookingSoundClip;
    public AudioClip completeSoundClip;

    [Header("Gamepad / Keyboard Navigation")]
    public GameObject firstSelectedButton;

#if ENABLE_INPUT_SYSTEM
    public UnityEngine.InputSystem.InputActionReference backAction;
#endif

    private IngredientButton[] _registeredIngredients = new IngredientButton[7];

    public void RegisterIngredientButton(IngredientButton btn, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 7)
            _registeredIngredients[slotIndex] = btn;
    }

    private static readonly KeyCode[] _ingredientHotkeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7
    };
    private static readonly KeyCode[] _confirmKeys = {
        KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space
    };
    private const KeyCode DEEPFRY_KEY = KeyCode.F;

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

#if ENABLE_INPUT_SYSTEM
    void OnEnable() { if (backAction != null) backAction.action.Enable(); }
    void OnDisable() { if (backAction != null) backAction.action.Disable(); }
#endif

    void Update()
    {
        if (stationCookingCanvas == null || !stationCookingCanvas.activeSelf) return;
        if (isCooking) return;

        bool backPressed = Input.GetKeyDown(KeyCode.Escape);
#if ENABLE_INPUT_SYSTEM
        if (!backPressed && backAction != null && backAction.action.WasPressedThisFrame())
            backPressed = true;
#endif
        if (backPressed) { CloseCanvas(); return; }

        for (int i = 0; i < _ingredientHotkeys.Length; i++)
            if (Input.GetKeyDown(_ingredientHotkeys[i])) { TryAddIngredientBySlot(i); return; }

        if (Input.GetKeyDown(DEEPFRY_KEY)) { TryTriggerDeepFry(); return; }

        foreach (KeyCode key in _confirmKeys)
            if (Input.GetKeyDown(key))
            {
                if (cookButton != null && cookButton.interactable) OnCookButtonClicked();
                return;
            }
    }

    void TryAddIngredientBySlot(int i)
    {
        IngredientButton btn = _registeredIngredients[i];
        if (btn == null) { Debug.LogWarning($"[CookingManager] ไม่มี IngredientButton ที่ slot {i + 1}"); return; }
        btn.TriggerIngredient();
    }

    void TryTriggerDeepFry()
    {
        if (deepFryButton != null && !deepFryButton.interactable) return;
        if (deepFryStation != null) deepFryStation.OnFryButtonClicked();
        else Debug.LogWarning("[CookingManager] ไม่ได้ผูก DeepFryStation");
    }

    public void OnConfirm() { if (cookButton != null && cookButton.interactable && !isCooking) OnCookButtonClicked(); }
    public void OnBack() => CloseCanvas();
    public void OnNavigate(Vector2 direction) { }

    public void OpenCanvas()
    {
        if (stationCookingCanvas != null) stationCookingCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;
        PlayerInteract2D.RegisterActiveCanvas(this);
        DayNightManager.Instance?.PauseGame();  // ✅ pause
        SetInitialFocus();
    }

    public void CloseCanvas()
    {
        if (stationCookingCanvas != null) stationCookingCanvas.SetActive(false);
        PlayerInteract2D.UnregisterActiveCanvas(this);

        if (isCooking)
        {
            if (cookCoroutine != null) { StopCoroutine(cookCoroutine); cookCoroutine = null; }
            if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
            isCooking = false;
            if (cookingVisuals != null) cookingVisuals.SetActive(false);
            if (cookingProgressBar != null) { cookingProgressBar.fillAmount = 0; cookingProgressBar.gameObject.SetActive(false); }
            currentIngredients.Clear();
            ResetBoardVisuals();
            CheckRecipe();
        }

        PlayerController2D.IsLocked = false;
        DayNightManager.Instance?.ResumeGame();  // ✅ resume
    }

    void SetInitialFocus()
    {
        if (EventSystem.current == null) return;
        GameObject target = firstSelectedButton != null ? firstSelectedButton
            : (cookButton != null ? cookButton.gameObject : null);
        if (target != null) EventSystem.current.SetSelectedGameObject(target);
    }

    public void AddIngredient(string ingredientName, Sprite ingredientSprite)
    {
        if (isCooking) return;
        if (currentIngredients.Count < 3)
        {
            currentIngredients.Add(ingredientName);
            int index = currentIngredients.Count - 1;
            if (index < boardSlots.Length) { boardSlots[index].sprite = ingredientSprite; boardSlots[index].enabled = true; }
            CheckRecipe();
        }
    }

    void CheckRecipe()
    {
        isFailed = false;
        currentOutput = null;
        foreach (var recipe in allRecipes)
        {
            if (IsMatch(recipe)) { currentOutput = recipe; if (cookButton != null) cookButton.interactable = true; return; }
        }
        if (currentIngredients.Count == 3) { currentOutput = failedDishRecipe; isFailed = true; if (cookButton != null) cookButton.interactable = true; }
        else { if (cookButton != null) cookButton.interactable = false; }
    }

    bool IsMatch(RecipeSO recipe)
    {
        if (recipe.requiredIngredients.Count != currentIngredients.Count) return false;
        List<string> playerInput = new List<string>(currentIngredients);
        foreach (string required in recipe.requiredIngredients)
        {
            string found = playerInput.Find(x => x.Equals(required, System.StringComparison.OrdinalIgnoreCase));
            if (found != null) playerInput.Remove(found); else return false;
        }
        return playerInput.Count == 0;
    }

    public void OnCookButtonClicked()
    {
        if (currentOutput == null || isCooking) return;
        if (TrayManager.instance != null && !TrayManager.instance.HasEmptySlot()) { Debug.LogWarning("[CookingManager] Tray เต็ม!"); return; }
        PlayerController2D.IsLocked = true;
        cookCoroutine = StartCoroutine(PerformCookingCoroutine());
    }

    private IEnumerator PerformCookingCoroutine()
    {
        isCooking = true;
        if (cookButton != null) cookButton.interactable = false;
        ResetBoardVisuals();
        if (cookingVisuals != null) cookingVisuals.SetActive(true);
        if (cookingProgressBar != null) { cookingProgressBar.gameObject.SetActive(true); cookingProgressBar.fillAmount = 0f; }
        if (sfxSource != null && cookingSoundClip != null) { sfxSource.clip = cookingSoundClip; sfxSource.loop = false; sfxSource.Play(); }

        float elapsed = 0f;
        while (elapsed < cookingDuration)
        {
            elapsed += Time.deltaTime;
            if (cookingProgressBar != null) cookingProgressBar.fillAmount = elapsed / cookingDuration;
            yield return null;
        }

        cookCoroutine = null;
        isCooking = false;
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
        PlayCompleteSound();

        if (TrayManager.instance != null)
        {
            bool placed = TrayManager.instance.ReceiveFood(currentOutput.recipeName, currentOutput.finalDishSprite);
            if (!placed) Debug.LogWarning("[CookingManager] Tray เต็ม");
        }
        else
        {
            PlayerInventory player = FindObjectOfType<PlayerInventory>();
            if (player != null) player.PickUpItem(currentOutput.recipeName, currentOutput.finalDishSprite);
        }

        if (cookingVisuals != null) cookingVisuals.SetActive(false);
        if (cookingProgressBar != null) cookingProgressBar.gameObject.SetActive(false);
        currentIngredients.Clear();
        CheckRecipe();

        if (stationCookingCanvas != null) stationCookingCanvas.SetActive(false);
        PlayerInteract2D.UnregisterActiveCanvas(this);
        PlayerController2D.IsLocked = false;
        DayNightManager.Instance?.ResumeGame();  // ✅ resume หลังทำอาหารเสร็จ
    }

    void PlayCompleteSound()
    {
        if (sfxSource != null && completeSoundClip != null) sfxSource.PlayOneShot(completeSoundClip);
        else if (AudioManager.instance != null) AudioManager.instance.PlayComplete();
    }

    void ResetBoardVisuals()
    {
        foreach (var slot in boardSlots)
            if (slot != null) { slot.sprite = null; slot.enabled = false; }
    }
}