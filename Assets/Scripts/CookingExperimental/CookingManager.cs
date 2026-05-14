using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// CookingManager — v2 (Gamepad Canvas Navigation Added)
//
// ของเดิม: mouse click / IngredientButton ยังทำงานปกติ
// เพิ่มใหม่:
//   - implement IGamepadNavigable → PlayerInteract2D รู้ว่า canvas เปิดอยู่
//   - OpenCanvas() register / CloseCanvas() unregister
//   - กด B / Escape เพื่อปิด canvas ได้
//   - EventSystem.SetSelectedGameObject ตั้ง focus อัตโนมัติตอนเปิด
//
// Setup ใน Inspector (เพิ่มจากเดิม):
//   - firstSelectedButton: ลาก button แรกใน canvas มาใส่
//     (ปกติคือ ingredient ตัวแรก หรือ cook button)
//   - backAction (optional): InputActionReference สำหรับปุ่ม B/Escape
// =====================================================================
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

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip cookingSoundClip;
    public AudioClip completeSoundClip;

    // ── Gamepad / Keyboard Navigation (ใหม่) ──────────────────────
    [Header("Gamepad Navigation (ใหม่)")]
    [Tooltip("ปุ่มแรกที่ควร focus เมื่อ canvas เปิด — ลาก ingredient button แรกมาใส่")]
    public GameObject firstSelectedButton;

#if ENABLE_INPUT_SYSTEM
    [Tooltip("InputActionReference สำหรับปุ่ม Back/Cancel (B button / Escape)")]
    public UnityEngine.InputSystem.InputActionReference backAction;
#endif

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
        // ── ตรวจ Back input ขณะ canvas เปิดอยู่ ──────────────────
        if (stationCookingCanvas == null || !stationCookingCanvas.activeSelf) return;
        if (isCooking) return; // ห้ามปิดขณะกำลัง cook

        bool backPressed = false;

        // Legacy
        if (Input.GetKeyDown(KeyCode.Escape)) backPressed = true;

#if ENABLE_INPUT_SYSTEM
        if (!backPressed && backAction != null && backAction.action.WasPressedThisFrame())
            backPressed = true;
#endif

        if (backPressed) CloseCanvas();
    }

    // ── IGamepadNavigable ──────────────────────────────────────────
    public void OnConfirm()
    {
        // confirm ขณะ canvas เปิด → กด cook ถ้า interactable
        if (cookButton != null && cookButton.interactable && !isCooking)
            OnCookButtonClicked();
    }

    public void OnBack() => CloseCanvas();

    public void OnNavigate(Vector2 direction)
    {
        // EventSystem จัดการ navigation ระหว่างปุ่มให้อัตโนมัติ
        // ไม่ต้องทำอะไรเพิ่ม — ใส่ไว้เพื่อ satisfy interface
    }

    // ── OpenCanvas / CloseCanvas ───────────────────────────────────
    public void OpenCanvas()
    {
        if (stationCookingCanvas != null)
            stationCookingCanvas.SetActive(true);

        PlayerController2D.IsLocked = true;

        // ✅ ใหม่: register เพื่อให้ PlayerInteract2D รู้ว่า canvas เปิดอยู่
        PlayerInteract2D.RegisterActiveCanvas(this);

        // ✅ ใหม่: ตั้ง initial focus ให้ gamepad/keyboard navigate ได้ทันที
        SetInitialFocus();
    }

    public void CloseCanvas()
    {
        if (stationCookingCanvas != null)
            stationCookingCanvas.SetActive(false);

        // ✅ ใหม่: unregister
        PlayerInteract2D.UnregisterActiveCanvas(this);

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

    void SetInitialFocus()
    {
        if (EventSystem.current == null) return;

        // ใช้ firstSelectedButton ถ้ามี ไม่งั้น fallback ไปที่ cookButton
        GameObject target = firstSelectedButton != null
            ? firstSelectedButton
            : (cookButton != null ? cookButton.gameObject : null);

        if (target != null)
            EventSystem.current.SetSelectedGameObject(target);
    }

    // ── ของเดิมทั้งหมด (ไม่มีการแก้ไข) ───────────────────────────
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

        if (TrayManager.instance != null && !TrayManager.instance.HasEmptySlot())
        {
            Debug.LogWarning("[CookingManager] Tray เต็มทุก slot! รอ player หยิบอาหารก่อน");
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

        // ✅ unregister เมื่อ canvas ปิดหลัง cook เสร็จ
        PlayerInteract2D.UnregisterActiveCanvas(this);

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