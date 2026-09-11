using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

// =====================================================================
// CatSystemManager — v3 (Interaction Zone — ทำงานกับ NPCController ตรงๆ)
//
// เปลี่ยนจาก v2: ของเดิมผูกกับ CustomerTable (currentTable) เพราะ Interaction
//   เกิดที่โต๊ะ ตอนนี้ Interaction ย้ายไปเกิดที่ InteractionZone แทน (แยกจากโต๊ะ
//   เด็ดขาด โต๊ะถูกปลดว่างไปตั้งแต่ก่อนแมวเดินมาถึงโซนแล้ว) จึงอ้างอิง NPCController
//   (currentNPC) โดยตรงแทนที่จะอ้อมผ่านโต๊ะ
//
// Safety-net เดิม (ForceResume / IsLocked ปลดล็อก) ยังเก็บไว้เหมือนเดิมทั้งหมด
// เผื่อมีจุดอื่นเรียก PauseGame()/Lock ไว้แล้วลืมปลด
// =====================================================================
public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;
    private NPCController currentNPC;
    private bool isInteracting = false;

    [Header("QTE UI")]
    public GameObject qtePanel;
    public Image qteProgressBarFill;

    [Header("QTE Settings")]
    public float interactionWindow = 3.0f;
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;

    // input flags — set โดย QTEInteractButton บน NPC prefab เท่านั้น
    private bool _pressReceived = false;
    private bool _holdActive = false;

    public void NotifyPress() { if (isInteracting) _pressReceived = true; _holdActive = true; }
    public void NotifyRelease() { _holdActive = false; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        if (qtePanel != null) qtePanel.SetActive(false);
    }

    /// <summary>เรียกจาก NPCController ตอนไปถึง InteractionZone แล้ว (แทนที่การเรียกจาก CustomerTable เดิม)</summary>
    public void StartInteraction(NPCController npc)
    {
        currentNPC = npc;
        isInteracting = false;
        StopAllCoroutines();
        ResetInputFlags();
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);
    }

    public void StartQTE(int typeIndex)
    {
        if (!isInteracting)
        {
            if (currentNPC != null)
                currentNPC.OpenQTECanvas();
            ResetInputFlags();
            StartCoroutine(RunInteractionQTE((QTEType)typeIndex));
        }
    }

    void ResetInputFlags()
    {
        _pressReceived = false;
        _holdActive = false;
    }

    IEnumerator RunInteractionQTE(QTEType qteType)
    {
        isInteracting = true;
        if (qtePanel != null) qtePanel.SetActive(true);
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;

        float timeLeft = interactionWindow;
        bool success = false;
        float currentHoldTime = 0f;
        int currentClicks = 0;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            switch (qteType)
            {
                case QTEType.SinglePress:
                    if (qteProgressBarFill != null)
                        qteProgressBarFill.fillAmount = timeLeft / interactionWindow;
                    if (_pressReceived) { success = true; timeLeft = 0; }
                    break;

                case QTEType.Hold:
                    if (_holdActive)
                    {
                        currentHoldTime += Time.deltaTime;
                        if (qteProgressBarFill != null)
                            qteProgressBarFill.fillAmount = currentHoldTime / holdRequiredTime;
                        if (currentHoldTime >= holdRequiredTime) { success = true; timeLeft = 0; }
                    }
                    else
                    {
                        currentHoldTime = 0f;
                        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
                    }
                    break;

                case QTEType.ButtonMash:
                    if (_pressReceived)
                    {
                        currentClicks++;
                        if (qteProgressBarFill != null)
                            qteProgressBarFill.fillAmount = (float)currentClicks / mashRequiredClicks;
                        if (currentClicks >= mashRequiredClicks) { success = true; timeLeft = 0; }
                    }
                    break;
            }

            _pressReceived = false;
            yield return null;
        }

        // ส่งผลไปยัง RelationshipManager ก่อน close UI
        ApplyResult(success);

        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);
        isInteracting = false;
        ResetInputFlags();

        // ✅ Safety-net: บังคับรีเซ็ต pause กลับเป็น 0 ทุกครั้งที่ QTE จบ
        // กันกรณีมีจุดอื่น (เช่น QTEInteractButton ตอนกด Mash/Hold) เรียก PauseGame()
        // ไว้แล้วลืม/พลาดเรียก ResumeGame() คืนให้ครบ ทำให้เกมค้าง Pause ค้างตลอดไป
        DayNightManager.Instance?.ForceResume();

        // ✅ ปลดล็อก Player คู่กับตอนที่ NPCInteract.RelationShip() ล็อกไว้ตอนเปิด relationshipCanvas
        PlayerController2D.IsLocked = false;

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;
            npcToClose.FinishInteractionAtZone();
        }
    }

    void ApplyResult(bool success)
    {
        // QTE สำเร็จ +10, ล้มเหลว -5
        float change = success ? 10f : -5f;

        if (currentNPC == null) return;

        // ✅ เล่นเสียงแมวเฉพาะตอน QTE สำเร็จ
        if (success)
        {
            NPCInteract interact = currentNPC.GetComponent<NPCInteract>();
            if (interact != null) interact.PlayMeow();
        }

        // ดึง catID จาก CatRelationshipData ที่ผูกไว้บน NPC prefab
        CatIdentity identity = currentNPC.GetComponent<CatIdentity>();
        if (identity == null || identity.catData == null)
        {
            Debug.LogWarning("CatSystemManager: NPC ไม่มี CatIdentity component หรือไม่ได้ผูก catData");
            return;
        }

        if (RelationshipManager.Instance != null)
            RelationshipManager.Instance.AddRelationship(identity.CatID, change);
    }

    public void ForceCloseSystem()
    {
        isInteracting = false;
        StopAllCoroutines();
        ResetInputFlags();
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);

        // ✅ Safety-net เดียวกับตอน QTE จบปกติ — กันเกมค้าง Pause ถ้าถูกบังคับปิดกลางทาง
        DayNightManager.Instance?.ForceResume();

        // ✅ ปลดล็อก Player — จุดที่ ExitButton (GroupOfInteraction/QTEPanel) เรียกใช้จริง
        PlayerController2D.IsLocked = false;

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;
            npcToClose.FinishInteractionAtZone();
        }
    }
}