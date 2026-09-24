using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

// =====================================================================
// CatSystemManager — v3 (Interaction Zone — ทำงานกับ NPCController ตรงๆ)
// =====================================================================
public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;
    private NPCController currentNPC;
    private bool isInteracting = false;

    [Header("QTE UI")]
    public GameObject qtePanel;
    public Image qteProgressBarFill;

    [Header("Interaction Choice UI (Global — ใช้ตัวเดียวร่วมกันทั้งร้าน ไม่ต้องสร้างซ้ำต่อ NPC)")]
    [Tooltip("ลาก 'ButtonGroup' ที่มีอยู่แล้วใน GroupOfInteraction > QTEPanel มาใส่ตรงนี้ — ตัวนี้อยู่ในซีนแล้ว ไม่ใช่ Prefab จึงลากใส่ตรงนี้ได้ปกติ")]
    public GameObject interactionButtonGroup;

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

    /// <summary>เรียกจาก NPCController ตอนไปถึง InteractionZone แล้ว</summary>
    public void StartInteraction(NPCController npc)
    {
        currentNPC = npc;
        isInteracting = false;
        StopAllCoroutines();
        ResetInputFlags();
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);
    }

    /// <summary>เปิดแผงเลือกประเภท QTE (ButtonGroup ตัวกลาง) ให้ NPC ตัวนี้</summary>
    public void ShowInteractionChoice(NPCController npc)
    {
        StartInteraction(npc);

        if (qtePanel != null)
            qtePanel.SetActive(true);

        if (interactionButtonGroup != null)
        {
            interactionButtonGroup.SetActive(true);
            interactionButtonGroup.transform.SetAsLastSibling();
        }
    }

    /// <summary>ปิดแผงเลือกประเภท QTE</summary>
    public void HideInteractionChoice()
    {
        if (interactionButtonGroup != null)
            interactionButtonGroup.SetActive(false);
    }

    public void StartQTE(int typeIndex)
    {
        if (!isInteracting)
        {
            HideInteractionChoice();
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

        // ส่งผลไปยัง RelationshipManager
        ApplyResult(success);

        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);
        HideInteractionChoice();
        isInteracting = false;
        ResetInputFlags();

        DayNightManager.Instance?.ForceResume();
        PlayerController2D.IsLocked = false;

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;

            if (success)
            {
                // เล่น Animation Perform ตามประเภท QTE (0 = Perform1, 1 = Perform2, 2 = Perform3)
                npcToClose.PlayPerformAndExit((int)qteType);
            }
            else
            {
                // ถ้า QTE ล้มเหลว ให้เดินออกจากร้านทันที
                npcToClose.FinishInteractionAtZone();
            }
        }
    }

    void ApplyResult(bool success)
    {
        float change = success ? 10f : -5f;

        if (currentNPC == null) return;

        if (success)
        {
            NPCInteract interact = currentNPC.GetComponent<NPCInteract>();
            if (interact != null) interact.PlayMeow();
        }

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
        HideInteractionChoice();

        DayNightManager.Instance?.ForceResume();
        PlayerController2D.IsLocked = false;

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;
            npcToClose.FinishInteractionAtZone();
        }
    }
}