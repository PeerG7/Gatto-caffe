using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;
    private NPCController currentNPC;
    private bool isInteracting = false;

    [Header("Interaction Hierarchy Root")]
    [Tooltip("Parent นอกสุด (GroupOfInteraction)")]
    public GameObject groupOfInteraction;

    [Tooltip("QTEPanel หลัก")]
    public GameObject qtePanel;

    [Header("QTE Child UI Components")]
    [Tooltip("ButtonGroup (กลุ่มปุ่มเลือกกิจกรรม 3 ช้อยส์)")]
    public GameObject buttonGroup;

    [Tooltip("ExitButton (ปุ่มกดยกเลิก/ออก)")]
    public GameObject exitButton;

    [Tooltip("Reputation Bar")]
    public GameObject reputationBar;

    [Tooltip("ProgressBar (หลอดแสดงความคืบหน้า)")]
    public GameObject progressBar;

    [Tooltip("QTE interact (ปุ่มกด Input QTE ตัวใหญ่ที่ขึ้นมาระหว่างเล่น)")]
    public GameObject qteInteractButton;

    [Tooltip("PRGBAR (Fill Image ด้านใน ProgressBar)")]
    public Image qteProgressBarFill;

    [Header("QTE Settings")]
    public float interactionWindow = 5.0f;
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;

    [Header("Tutorial Assist (หยุดเวลา QTE ให้ผู้เล่นปรับตัว)")]
    [Tooltip("เปิด = ช่วง Tutorial / QTE ครั้งแรกๆ ของวันที่ 1 เวลา QTE จะหยุดนิ่ง จนกว่าผู้เล่นจะกดปุ่มครั้งแรก แล้วค่อยเริ่มนับเวลา")]
    public bool freezeQTEUntilFirstInput = true;
    [Tooltip("จำนวน QTE แรกของวันที่ 1 (หลังจบ Tutorial) ที่ยังให้หยุดรอ — QTE ตัวแรกมักเกิดหลังวางแมวที่ Zone ซึ่งเควสจบไปแล้ว | 0 = ใช้เฉพาะตอนเควสยังไม่จบ")]
    public int assistQTECountOnDay1 = 3;

    private static int _assistedQTECount = 0; // นับต่อการเปิดเกม 1 ครั้ง

    private bool _pressReceived = false;
    private bool _holdActive = false;

    /// <summary>🛡️ มี session ที่เปิดอยู่กับแมวตัวอื่น (ไม่ใช่ npc ที่ถามมา) หรือไม่</summary>
    public bool IsBusyWithOther(NPCController npc)
    {
        return currentNPC != null && currentNPC != npc;
    }

    public void NotifyPress() { if (isInteracting) _pressReceived = true; _holdActive = true; }
    public void NotifyRelease() { _holdActive = false; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        HideAllQTEUI();
    }

    public void StartInteraction(NPCController npc)
    {
        currentNPC = npc;
        isInteracting = false;
        StopAllCoroutines();
        ResetInputFlags();
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        HideAllQTEUI();
    }

    /// <summary>
    /// 🟢 1. สเตจเปิดเมนูตัวเลือก (ตอนแมวถึง InteractionZone): แสดงเฉพาะกลุ่มปุ่มตัวเลือก 3 ช้อยส์
    /// </summary>
    public void ShowInteractionChoice(NPCController npc)
    {
        // 🟢 กันเรียกซ้ำ: กำลังเล่น QTE หรือ UI ของแมวตัวนี้เปิดอยู่แล้ว -> เมินเงียบๆ
        // (ห้าม Unlock, ห้าม Reset, ห้าม StopAllCoroutines)
        if (isInteracting) return;
        if (currentNPC == npc && groupOfInteraction != null && groupOfInteraction.activeSelf) return;

        // 🟢 Guard Clause: กรองสถานะสิทธิ์การเปิด Canvas อย่างเข้มงวด
        if (npc == null || !npc.isInteractionAllowed || npc.currentState != NPCController.NPCState.AtInteractionZone)
        {
            Debug.LogWarning($"[CatSystemManager] บล็อก QTE! แมว {npc?.name} ไม่มีสิทธิ์เปิด Canvas (isAllowed: {npc?.isInteractionAllowed}, State: {npc?.currentState})");

            PlayerController2D.SetLock(false);
            HideAllQTEUI();
            return;
        }

        StartInteraction(npc);
        npc.StopInteractionTimeout();
        QTEZoomController.Instance?.ZoomInToQTE(npc.transform, true);

        PlayerController2D.SetLock(true);

        // 👻 Player Fade หายไปตอนเริ่ม Interaction (ครอบคลุมทุกทางเข้า)
        PlayerGhostFade.BeginInteraction(npc);

        // 🟢 เปิด Root หลัก และ Panel
        if (groupOfInteraction != null) groupOfInteraction.SetActive(true);
        if (qtePanel != null) qtePanel.SetActive(true);

        // 🟢 เปิดเฉพาะกลุ่มปุ่ม Choice และ Bar ตกแต่ง
        if (buttonGroup != null) buttonGroup.SetActive(true);
        if (exitButton != null) exitButton.SetActive(true);
        if (reputationBar != null) reputationBar.SetActive(true);

        // 🔴 ปิด ProgressBar และปุ่ม QTE interact ไว้ก่อนจนกว่าจะเลือกช้อยส์
        if (progressBar != null) progressBar.SetActive(false);
        if (qteInteractButton != null) qteInteractButton.SetActive(false);
    }

    public void HideInteractionChoice()
    {
        if (buttonGroup != null) buttonGroup.SetActive(false);
    }

    /// <summary>
    /// 🟢 2. สเตจเริ่มทำกิจกรรม QTE: ซ่อน 3 ช้อยส์ แล้วเปิด ProgressBar + ปุ่ม QTE interact ค้างไว้
    /// </summary>
    public void StartQTE(int typeIndex)
    {
        if (!isInteracting)
        {
            // 🟢 ปิดกลุ่มปุ่มเลือกช้อยส์
            HideInteractionChoice();

            // 🟢 เปิดหลอดคืบหน้า และปุ่มกด QTE interact สำหรับใช้ทำกิจกรรม
            if (groupOfInteraction != null) groupOfInteraction.SetActive(true);
            if (qtePanel != null) qtePanel.SetActive(true);
            if (progressBar != null) progressBar.SetActive(true);
            if (qteInteractButton != null) qteInteractButton.SetActive(true);
            if (exitButton != null) exitButton.SetActive(true);

            if (currentNPC != null)
            {
                currentNPC.StopInteractionTimeout();
            }

            ResetInputFlags();
            StartCoroutine(RunInteractionQTE((QTEType)typeIndex));
        }
    }

    // true = QTE รอบนี้ให้หยุดเวลารอจนกว่าผู้เล่นจะกดครั้งแรก
    //  - เควสยังไม่ Completed (ช่วง Tutorial)
    //  - หรือเป็น QTE ช่วงแรกๆ ของวันที่ 1 (QTE ตัวแรกเกิดหลังเควสจบไปแล้ว จึงต้องนับเพิ่ม)
    bool ShouldFreezeQTEUntilInput()
    {
        if (!freezeQTEUntilFirstInput) return false;

        if (MainQuestManager.Instance != null &&
            MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed)
            return true;

        if (assistQTECountOnDay1 > 0 &&
            DayNightManager.Instance != null && DayNightManager.Instance.currentDay == 1 &&
            _assistedQTECount < assistQTECountOnDay1)
        {
            _assistedQTECount++;
            return true;
        }
        return false;
    }

    void ResetInputFlags()
    {
        _pressReceived = false;
        _holdActive = false;
    }

    IEnumerator RunInteractionQTE(QTEType qteType)
    {
        isInteracting = true;

        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;

        float timeLeft = interactionWindow;
        bool success = false;
        float currentHoldTime = 0f;
        int currentClicks = 0;

        // 🟢 Tutorial Assist: หยุดเวลา QTE จนกว่าผู้เล่นจะกดครั้งแรก
        bool waitForFirstInput = ShouldFreezeQTEUntilInput();
        if (waitForFirstInput && qteType == QTEType.SinglePress && qteProgressBarFill != null)
            qteProgressBarFill.fillAmount = 1f; // SinglePress หลอดเต็มค้างไว้ ไม่ลดลงระหว่างรอ

        while (timeLeft > 0)
        {
            if (waitForFirstInput)
            {
                if (_pressReceived || _holdActive)
                {
                    waitForFirstInput = false; // กดแล้ว → เริ่มนับเวลาตามปกติตั้งแต่เฟรมนี้
                }
                else
                {
                    yield return null; // ยังไม่กด → เวลาหยุดนิ่ง (ไม่ลด timeLeft)
                    continue;
                }
            }

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

        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;

        // 🟢 เมื่อเล่นเสร็จ ปิด UI ทั้งหมด
        HideAllQTEUI();
        isInteracting = false;
        ResetInputFlags();

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;

            if (success)
            {
                if (CafeRatingManager.Instance != null) CafeRatingManager.Instance.RegisterInteractSuccess(); // ⭐ โบนัส Rating
                npcToClose.PlayPerformAndExit((int)qteType);
            }
            else
            {
                PlayerController2D.SetLock(false);
                QTEZoomController.Instance?.UnlockQTEZoom();
                PlayerGhostFade.EndInteraction(); // 👻 QTE พลาด → Player กลับมาเป็นร่างลางๆ
                npcToClose.FinishInteractionAtZone();
            }
        }
        else
        {
            PlayerController2D.SetLock(false);
            QTEZoomController.Instance?.UnlockQTEZoom();
            PlayerGhostFade.EndInteraction();
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

    /// <summary>
    /// 🔴 คำสั่งปิด UI ทุกชิ้นใน Hierarchy ของ GroupOfInteraction อย่างเด็ดขาด
    /// </summary>
    public void HideAllQTEUI()
    {
        if (buttonGroup != null) buttonGroup.SetActive(false);
        if (progressBar != null) progressBar.SetActive(false);
        if (qteInteractButton != null) qteInteractButton.SetActive(false);
        if (exitButton != null) exitButton.SetActive(false);
        if (reputationBar != null) reputationBar.SetActive(false);
        if (qtePanel != null) qtePanel.SetActive(false);
        if (groupOfInteraction != null) groupOfInteraction.SetActive(false);
    }

    /// <summary>NPC ที่กำลังใช้ระบบอยู่ถูกลบ → ปิดระบบและปลดล็อกให้ผู้เล่น</summary>
    public void NotifyNPCRemoved(NPCController npc)
    {
        if (npc == null || currentNPC != npc) return;

        currentNPC = null; // เคลียร์ก่อน กัน ForceCloseSystem ไปเรียกเมธอดของแมวที่กำลังถูกลบ
        ForceCloseSystem();
    }

    public void ForceCloseSystem()
    {
        isInteracting = false;
        StopAllCoroutines();
        ResetInputFlags();
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;

        HideAllQTEUI();

        PlayerController2D.SetLock(false);
        QTEZoomController.Instance?.UnlockQTEZoom();
        PlayerGhostFade.EndInteraction(); // 👻 ปิดระบบ/กดออก/แมวถูกลบ → Player กลับมา

        if (currentNPC != null)
        {
            NPCController npcToClose = currentNPC;
            currentNPC = null;
            npcToClose.FinishInteractionAtZone();
        }
    }
}