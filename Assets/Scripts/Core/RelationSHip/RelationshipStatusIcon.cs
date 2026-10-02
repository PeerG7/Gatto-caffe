using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RelationshipStatusIcon : MonoBehaviour
{
    [Header("Target Fill Bar")]
    [Tooltip("ลาก GameObject ของหลอด Bar (Image) ที่ต้องการตรวจจับค่า fillAmount มาใส่")]
    public Image targetFillBar;

    [Header("Status Sprites")]
    public Sprite iconLow;   // ภาพที่ 1: Bar < 50%
    public Sprite iconHalf;  // ภาพที่ 2: Bar 50% - 99%
    public Sprite iconFull;  // ภาพที่ 3: Bar 100% (Full)

    private Image myIconImage;

    void Awake()
    {
        // ดึง Image Component จาก GameObject ตัวเองทันที ไม่ต้องลากใส่ Inspector
        myIconImage = GetComponent<Image>();
    }

    void Update()
    {
        if (targetFillBar == null || myIconImage == null) return;

        // อ่านค่า fillAmount จากหลอด Bar ตรงๆ
        float fill = targetFillBar.fillAmount;
        Sprite targetSprite = null;

        if (fill >= 0.999f)
        {
            targetSprite = iconFull;
        }
        else if (fill >= 0.5f)
        {
            targetSprite = iconHalf;
        }
        else
        {
            targetSprite = iconLow;
        }

        // ปรับเปลี่ยน Sprite
        if (targetSprite != null)
        {
            if (myIconImage.sprite != targetSprite)
            {
                myIconImage.sprite = targetSprite;
            }

            if (!myIconImage.enabled) myIconImage.enabled = true;

            // บังคับค่า Alpha ให้แสดงผล (กันกรณีรูปโปร่งใส)
            Color c = myIconImage.color;
            if (c.a < 1f)
            {
                c.a = 1f;
                myIconImage.color = c;
            }
        }
        else
        {
            myIconImage.enabled = false;
        }
    }
}