using UnityEngine;
using UnityEngine.UI;

public class IngredientButton : MonoBehaviour
{
    [Header("Ingredient Data")]
    public string ingredientName; // ใส่ชื่อ เช่น Meat, Veggie
    public Sprite ingredientSprite; // ลากรูปวัตถุดิบมาใส่ตรงนี้

    private CookingManager cookingManager;

    void Start()
    {
        // 1. ค้นหา CookingManager ในฉากอัตโนมัติ
        cookingManager = FindObjectOfType<CookingManager>();

        // 2. สั่งให้ปุ่ม UI ผูกการทำงานกับฟังก์ชัน OnClick เมื่อเริ่มเกม
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClick);
        }
    }

    void OnClick()
    {
        if (cookingManager != null)
        {
            // ส่งข้อมูลชื่อและรูปภาพไปให้ CookingManager
            cookingManager.AddIngredient(ingredientName, ingredientSprite);
        }
    }
}