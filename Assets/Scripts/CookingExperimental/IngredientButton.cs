using UnityEngine;
using UnityEngine.UI;

// =====================================================================
// IngredientButton — v3 (7 Slots, 0-based index 0-6)[cite: 1]
// =====================================================================
public class IngredientButton : MonoBehaviour
{
    [Header("Ingredient Data")]
    public string ingredientName; //[cite: 1]
    public Sprite ingredientSprite; //[cite: 1]

    [Header("Hotkey Slot")]
    [Tooltip("ตำแหน่งปุ่มใน canvas 0-6 (0 = กด 1, ... 6 = กด 7)")] //[cite: 1]
    [Range(0, 6)] //[cite: 1]
    public int hotkeySlotIndex = 0; //[cite: 1]

    private CookingManager cookingManager; //[cite: 1]

    void Start()
    {
        cookingManager = FindObjectOfType<CookingManager>(); //[cite: 1]

        Button btn = GetComponent<Button>(); //[cite: 1]
        if (btn != null)
            btn.onClick.AddListener(OnClick); //[cite: 1]

        if (cookingManager != null)
            cookingManager.RegisterIngredientButton(this, hotkeySlotIndex); //[cite: 1]
        else
            Debug.LogWarning($"[IngredientButton] '{ingredientName}' ไม่พบ CookingManager ใน scene"); //[cite: 1]
    }

    void OnClick() => TriggerIngredient(); //[cite: 1]

    public void TriggerIngredient()
    {
        if (cookingManager != null)
            cookingManager.AddIngredient(ingredientName, ingredientSprite); //[cite: 1]
    }
}