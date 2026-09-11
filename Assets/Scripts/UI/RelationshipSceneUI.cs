using UnityEngine;
using System.Collections;

// =====================================================================
// RelationshipUI — v4 (Player Lock Fix + Fail-safe)
//
// v3: PlayerController2D.IsLocked = false ตอนจบ Interaction ผ่าน FinishInteraction()
//
// v4 (ใหม่): เพิ่ม OnDisable() เป็น fail-safe —
//   กันกรณีที่ relationshipCanvas ถูกปิดจาก "ที่อื่น" โดยไม่ผ่าน FinishInteraction()
//   (เช่น ถูก SetActive(false) ตรงๆ จากสคริปต์อื่น, ถูกปิดกลางคันตอนเปลี่ยนซีน,
//    หรือมีปุ่ม Close อีกปุ่มที่ไม่ได้เรียก FinishInteraction())
//   ถ้าเกิดกรณีแบบนี้ IsLocked / pauseCount จะไม่ถูกปลดเลย ทำให้ Player ค้างตลอดไป
//   OnDisable() จะเป็นตาข่ายรองรับสุดท้ายเสมอ ไม่ว่าทางปิดจะมาจากไหน
//
// ⚠️ หมายเหตุสำคัญ: ถ้าในโปรเจกต์มีอีกเส้นทางปิดหน้าความสัมพันธ์ที่ไม่ผ่าน
//    GameObject นี้เลย (เช่น RelationshipMinigame.BackToShop() ที่ปิดผ่าน
//    SceneLoader.CloseRelationshipScene() ในซีนแยกต่างหาก) OnDisable() ของสคริปต์นี้
//    จะไม่ถูกเรียก — ต้องไปเพิ่ม PlayerController2D.IsLocked = false และ
//    DayNightManager.Instance?.ForceResume() ในเส้นทางนั้นด้วยเช่นกัน
//
// Setup: ปุ่ม Close / Finish ใน Relationship Canvas → เรียก FinishInteraction()
// =====================================================================
public class RelationshipUI : MonoBehaviour
{
    [Header("Canvas Reference")]
    [Tooltip("ลาก RelationshipCanvas (root GameObject) มาใส่ตรงนี้")]
    public GameObject relationshipCanvas;

    private bool isEnding = false;

    public void FinishInteraction()
    {
        if (isEnding) return;
        isEnding = true;
        StartCoroutine(FinishRoutine());
    }

    IEnumerator FinishRoutine()
    {
        var npc = RelationshipManager.Instance?.GetCurrentNPC();
        if (npc != null)
            npc.GoExit();

        RelationshipManager.Instance?.ClearNPC();

        yield return new WaitForSeconds(0.3f);

        if (relationshipCanvas != null)
            relationshipCanvas.SetActive(false);
        else
            gameObject.SetActive(false);

        // ✅ ForceResume แทน ResumeGame เพื่อ reset pauseCount เป็น 0 ทันที
        DayNightManager.Instance?.ForceResume();

        // ✅ Fix บั๊ก: ปลดล็อก Player คู่กับตอนที่ NPCInteract.RelationShip() ล็อกไว้
        PlayerController2D.IsLocked = false;

        isEnding = false;
    }

    // ── ใหม่ v4: Fail-safe ──────────────────────────────────────────
    // ทำงานทุกครั้งที่ GameObject นี้ถูกปิด ไม่ว่าจะปิดผ่าน FinishInteraction()
    // (ซึ่งปลดล็อกไปแล้วตามปกติ — เรียกซ้ำไม่เป็นไร ไม่มีผลข้างเคียง)
    // หรือถูกปิดจากที่อื่นโดยไม่ผ่าน FinishInteraction() เลย (เคสที่เคยพลาด)
    // การันตีว่า Player และเกมจะไม่ค้างล็อกอีก
    void OnDisable()
    {
        DayNightManager.Instance?.ForceResume();
        PlayerController2D.IsLocked = false;
    }
}