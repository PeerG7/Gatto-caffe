using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;
    private CustomerTable currentTable;
    private bool isInteracting = false;

    [Header("QTE UI (Image Fill)")]
    public GameObject qtePanel;
    public Image qteProgressBarFill;

    [Header("Reputation UI (Image Fill)")]
    public Image reputationBarFill;
    private float currentReputation = 0.0f;

    [Header("QTE Settings")]
    public float interactionWindow = 3.0f;
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;

    // =====================================================================
    // FIX: แทน Input.GetMouseButtonDown/GetKey ใช้ state flags เหล่านี้แทน
    // ปุ่มบน NPC prefab จะเรียก method NotifyPress/NotifyHold/NotifyRelease
    // ทำให้ input ถูกรับเฉพาะจากปุ่มที่ถูกต้องเท่านั้น
    // =====================================================================
    private bool _pressReceived = false;   // สำหรับ SinglePress และ ButtonMash (แต่ละ click)
    private bool _holdActive = false;   // สำหรับ Hold — true ตลอดที่กดค้าง

    // เรียกจาก QTEInteractButton.cs (ปุ่มบน NPC prefab) — OnPointerDown
    public void NotifyPress()
    {
        if (!isInteracting) return;
        _pressReceived = true;
        _holdActive = true;
    }

    // เรียกจาก QTEInteractButton.cs — OnPointerUp
    public void NotifyRelease()
    {
        _holdActive = false;
    }
    // =====================================================================

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        currentReputation = 0.0f;
        UpdateReputationUI();
        if (qtePanel != null) qtePanel.SetActive(false);
    }

    void UpdateReputationUI()
    {
        if (reputationBarFill != null)
            reputationBarFill.fillAmount = currentReputation;
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
                // -----------------------------------------------------------
                // SinglePress: กดปุ่มบน NPC ครั้งเดียว ก็สำเร็จ
                // -----------------------------------------------------------
                case QTEType.SinglePress:
                    if (qteProgressBarFill != null)
                        qteProgressBarFill.fillAmount = timeLeft / interactionWindow;

                    if (_pressReceived)
                    {
                        success = true;
                        timeLeft = 0;
                    }
                    break;

                // -----------------------------------------------------------
                // Hold: กดค้างปุ่มบน NPC จนเต็ม holdRequiredTime
                // -----------------------------------------------------------
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

                // -----------------------------------------------------------
                // ButtonMash: รัวปุ่มบน NPC จนครบ mashRequiredClicks
                // -----------------------------------------------------------
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

            // ล้าง _pressReceived ทุก frame หลังจากอ่านแล้ว
            _pressReceived = false;

            yield return null;
        }

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
        float change = success ? 0.1f : -0.1f;
        currentReputation = Mathf.Clamp01(currentReputation + change);
        UpdateReputationUI();
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