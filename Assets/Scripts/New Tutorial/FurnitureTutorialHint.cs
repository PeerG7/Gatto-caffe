using UnityEngine;

// =====================================================================
// FurnitureTutorialHint — แสดง/ซ่อนสัญลักษณ์สอน "กด B เปิดเมนู Furniture" ตอนขั้นเควส PlaceFurniture
//
// สคริปต์นี้ทำหน้าที่ "เปิด/ปิด" ชุดรูป (HintRoot) อย่างเดียว:
//   - การขยับ (เช่น UI Hover Bounce) ให้ใช้ Component ของคุณเองบนรูปที่ต้องการ
//   - ตำแหน่งรูป จัดวางเองใน Editor ได้เลย
//
// แสดงเมื่อ : เควสอยู่ขั้น PlaceFurniture และหน้าร้านค้า (คอมพิวเตอร์) ไม่ได้เปิดอยู่
// ซ่อนเมื่อ : ผู้เล่นกด B เปิดเมนู Furniture แล้ว / กำลังถือเฟอร์นิเจอร์เพื่อวาง
//            (ถ้าปิดเมนูโดยยังไม่ได้วาง สัญลักษณ์จะโผล่กลับมา)
//
// ห้ามแปะสคริปต์นี้บน HintRoot เอง (แปะบน GameObject อื่นที่เปิดอยู่เสมอ เช่น MainQuestManager)
// =====================================================================
public class FurnitureTutorialHint : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("กลุ่มรูปที่จะแสดง (เช่น HintRoot ที่มี Button + Hand) — ต้องไม่ใช่ GameObject เดียวกับสคริปต์นี้")]
    public GameObject hintRoot;

    [Header("Behaviour")]
    [Tooltip("เปิด = Fade เข้า/ออกด้วย CanvasGroup (รูปยังเปิดอยู่ตลอด) | ปิด = SetActive เปิด/ปิดตรง ๆ (Hover Bounce จะเริ่มใหม่ทุกครั้งที่โผล่)")]
    public bool useFade = true;
    public float fadeSpeed = 6f;

    [Tooltip("เปิด = ซ่อนสัญลักษณ์ตอนเปิดเมนู Furniture / กำลังวางของ")]
    public bool hideWhileEditMode = true;

    private CanvasGroup group;

    void Awake()
    {
        if (hintRoot == null || hintRoot == gameObject)
        {
            Debug.LogError("[FurnitureTutorialHint] ต้องลาก Hint Root มาใส่ และห้ามเป็น GameObject เดียวกับสคริปต์นี้", this);
            enabled = false;
            return;
        }

        if (useFade)
        {
            group = hintRoot.GetComponent<CanvasGroup>();
            if (group == null) group = hintRoot.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false; // เป็นแค่ภาพ ไม่บังการคลิก
            group.interactable = false;
        }
        else
        {
            hintRoot.SetActive(false);
        }
    }

    void Update()
    {
        bool visible = ShouldShow();

        if (!useFade)
        {
            if (hintRoot.activeSelf != visible) hintRoot.SetActive(visible);
            return;
        }

        // ใช้ unscaled เพราะช่วง Tutorial เกมถูก Pause อยู่
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
    }

    bool ShouldShow()
    {
        bool questActive = MainQuestManager.Instance != null &&
                           MainQuestManager.Instance.currentQuest == MainQuestManager.QuestStep.PlaceFurniture;
        if (!questActive) return false;

        // ร้านค้าเปิดอยู่ปุ่ม B ใช้ไม่ได้ → ยังไม่ต้องโชว์
        if (ComputerStation.IsShopOpen) return false;

        if (hideWhileEditMode)
        {
            FurnitureManager fm = FurnitureManager.Instance;
            if (fm != null && (fm.isEditMode || fm.IsPlacingFurniture)) return false;
        }

        return true;
    }
}