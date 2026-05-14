using UnityEngine;
using UnityEngine.UI;

// =====================================================================
// IngredientButton — v3 (7 Slots, 0-based index 0-6)
//
// ของเดิม: mouse click ยังทำงานปกติ
// เพิ่มใหม่:
//   - hotkeySlotIndex 0-6 (0 = key 1, ... 6 = key 7)
//   - RegisterIngredientButton() แจ้ง CookingManager ตอน Start()
//   - TriggerIngredient() public method สำหรับ hotkey
// =====================================================================
public class IngredientButton : MonoBehaviour
{
    [Header("Ingredient Data")]
    public string ingredientName;
    public Sprite ingredientSprite;

    [Header("Hotkey Slot (ใหม่)")]
    [Tooltip("ตำแหน่งปุ่มใน canvas 0-6 (0 = กด 1, 1 = กด 2, ... 6 = กด 7)")]
    [Range(0, 6)]
    public int hotkeySlotIndex = 0;

    private CookingManager cookingManager;

    void Start()
    {
        cookingManager = FindObjectOfType<CookingManager>();

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClick);

        if (cookingManager != null)
            cookingManager.RegisterIngredientButton(this, hotkeySlotIndex);
        else
            Debug.LogWarning($"[IngredientButton] '{ingredientName}' ไม่พบ CookingManager ใน scene");
    }

    void OnClick() => TriggerIngredient();

    public void TriggerIngredient()
    {
        if (cookingManager != null)
            cookingManager.AddIngredient(ingredientName, ingredientSprite);
    }
}