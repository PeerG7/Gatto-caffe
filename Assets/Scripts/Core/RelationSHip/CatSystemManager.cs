using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;
    private CustomerTable currentTable;
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

    public void StartInteraction(CustomerTable table)
    {
        currentTable = table;
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
            if (currentTable != null)
                currentTable.OpenNPCInteractCanvas();
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

        if (currentTable != null)
        {
            CustomerTable tableToClose = currentTable;
            currentTable = null;
            tableToClose.CloseInteractionUI();
        }
    }

    void ApplyResult(bool success)
    {
        // QTE สำเร็จ +10, ล้มเหลว -5
        float change = success ? 10f : -5f;

        if (currentTable?.sittingNPC == null) return;

        // ✅ เล่นเสียงแมวเฉพาะตอน QTE สำเร็จ
        if (success)
        {
            NPCInteract interact = currentTable.sittingNPC.GetComponent<NPCInteract>();
            if (interact != null) interact.PlayMeow();
        }

        // ดึง catID จาก CatRelationshipData ที่ผูกไว้บน NPC prefab
        CatIdentity identity = currentTable.sittingNPC.GetComponent<CatIdentity>();
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

        if (currentTable != null)
        {
            CustomerTable tableToClose = currentTable;
            currentTable = null;
            tableToClose.CloseInteractionUI();
        }
    }
}