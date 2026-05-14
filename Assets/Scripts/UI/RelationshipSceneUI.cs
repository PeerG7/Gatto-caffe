using UnityEngine;
using System.Collections;

// =====================================================================
// RelationshipUI — v2 (Canvas-based, ไม่ใช้ SceneLoader แล้ว)
//
// ของเดิม: เรียก SceneLoader.Instance.CloseRelationshipScene()
// แก้ใหม่: ปิด Canvas ตัวเอง + ResumeGame() ผ่าน DayNightManager
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

        yield return new WaitForSeconds(0.3f);  // FadeOut เดิม

        // ✅ ปิด Canvas แทน SceneLoader
        if (relationshipCanvas != null)
            relationshipCanvas.SetActive(false);
        else
            gameObject.SetActive(false); // fallback: ปิด GameObject ที่ script นี้ติดอยู่

        // ✅ Resume เวลา
        DayNightManager.Instance?.ResumeGame();

        isEnding = false; // reset เผื่อเปิดใหม่ในรอบถัดไป
    }
}