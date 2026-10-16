using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;
    private NPCController currentNPC;
    private bool isInteracting = false;

    [Header("QTE UI")]
    public GameObject qtePanel;
    public Image qteProgressBarFill;

    [Header("Interaction Choice UI (Global)")]
    public GameObject interactionButtonGroup;

    [Header("QTE Settings")]
    public float interactionWindow = 5.0f; // 🟢 ปรับเพิ่มเวลาหน้า QTE เล็กน้อย
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;

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

    public void StartInteraction(NPCController npc)
    {
        currentNPC = npc;
        isInteracting = false;
        StopAllCoroutines();
        ResetInputFlags();
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);
    }

    public void ShowInteractionChoice(NPCController npc)
    {
        StartInteraction(npc);

        // 🟢 หยุด Timeout ของ NPC ทันทีที่เปิดหน้าเลือก Choice
        if (npc != null)
        {
            npc.StopInteractionTimeout();
            QTEZoomController.Instance?.ZoomInToQTE(npc.transform, true);
        }

        PlayerController2D.SetLock(true);

        if (qtePanel != null)
            qtePanel.SetActive(true);

        if (interactionButtonGroup != null)
        {
            interactionButtonGroup.SetActive(true);
            interactionButtonGroup.transform.SetAsLastSibling();
        }
    }

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

            // 🟢 ปิด Timeout ของ NPC ทันที เพื่อไม่ให้สคริปต์ NPC สั่งปิด UI ตัดหน้าขณะกำลังทำ QTE
            if (currentNPC != null)
            {
                currentNPC.StopInteractionTimeout();
                currentNPC.OpenQTECanvas();
            }

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

        // 🟢 บังคับเปิด UI Panel ค้างไว้แน่นอน
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
                        // หากปล่อยมือ หลอดจะค่อยๆ ลด แต่ UI จะไม่หายไป
                        currentHoldTime = Mathf.Max(0f, currentHoldTime - Time.deltaTime * 1.5f);
                        if (qteProgressBarFill != null)
                            qteProgressBarFill.fillAmount = currentHoldTime / holdRequiredTime;
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

        ApplyResult(success);

        // 🟢 เคลียร์ UI เมื่อ QTE ทำงานเสร็จสมบูรณ์เท่านั้น
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);
        HideInteractionChoice();
        isInteracting = false;
        ResetInputFlags();

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;

            if (success)
            {
                npcToClose.PlayPerformAndExit((int)qteType);
            }
            else
            {
                PlayerController2D.SetLock(false);
                QTEZoomController.Instance?.UnlockQTEZoom();
                npcToClose.FinishInteractionAtZone();
            }
        }
        else
        {
            PlayerController2D.SetLock(false);
            QTEZoomController.Instance?.UnlockQTEZoom();
        }
    }

    void ApplyResult(bool success)
    {
        float change = success ? 10f : -5f;

        if (currentNPC == null) return;

        if (success)
        {
            NPCInteract interact = currentNPC.GetComponent<NPCInteract>();
            if (interact != null)
                interact.PlayHappy();
            else if (AudioManager.instance != null)
                AudioManager.instance.Cathappy();
        }

        CatIdentity identity = currentNPC.GetComponent<CatIdentity>();
        if (identity == null || identity.catData == null) return;

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

        PlayerController2D.SetLock(false);
        QTEZoomController.Instance?.UnlockQTEZoom();

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;
            npcToClose.FinishInteractionAtZone();
        }
    }
}