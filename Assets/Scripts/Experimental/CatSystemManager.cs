using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ประกาศประเภทกิจกรรม QTE
public enum QTEType { SinglePress, Hold, ButtonMash }

public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;
    private CustomerTable currentTable;
    private bool isInteracting = false;

    [Header("QTE UI (Image Fill)")]
    public GameObject qtePanel;
    public Image qteProgressBarFill; // ใน Inspector: เปลี่ยน Image Type เป็น Filled และ Fill Method เป็น Horizontal

    [Header("Reputation UI (Image Fill)")]
    public Image reputationBarFill;  // ใน Inspector: เปลี่ยน Image Type เป็น Filled
    private float currentReputation = 0.0f;

    [Header("QTE Settings")]
    public float interactionWindow = 3.0f;
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;
    void Start()
    {
        // กำหนดค่าเริ่มต้นในสคริปต์ (0.0f คือg)
        currentReputation = 0.0f;

        // สั่งให้ Image Fill อัปเดตตามค่าด้านบนทันที
        UpdateReputationUI();
    }

    void UpdateReputationUI()
    {
        if (reputationBarFill != null)
        {
            // บรรทัดนี้จะบังคับให้ Image Fill ขยับตามค่า currentReputation (0.0 - 1.0)
            reputationBarFill.fillAmount = currentReputation;
        }
    }
    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // เริ่มระบบเมื่อมีการคลิกเลือกแมว
    public void StartInteraction(CustomerTable table)
    {
        currentTable = table;
        isInteracting = false;
        StopAllCoroutines();

        // Reset แถบพลังเป็น 0 เสมอเมื่อเริ่มใหม่
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
        if (qtePanel != null) qtePanel.SetActive(false);

        this.gameObject.SetActive(true);
    }

    // เรียกจากปุ่ม Pet, Hug หรือ Play
    public void StartQTE(int typeIndex)
    {
        if (gameObject.activeInHierarchy && !isInteracting)
        {
            StartCoroutine(RunInteractionQTE((QTEType)typeIndex));
        }
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
                    // แถบวิ่งลดลงตามเวลา
                    if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = timeLeft / interactionWindow;
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) { success = true; timeLeft = 0; }
                    break;

                case QTEType.Hold:
                    // แถบเพิ่มขึ้นตามระยะเวลาที่กดค้าง
                    if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                    {
                        currentHoldTime += Time.deltaTime;
                        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = currentHoldTime / holdRequiredTime;
                        if (currentHoldTime >= holdRequiredTime) { success = true; timeLeft = 0; }
                    }
                    else
                    {
                        currentHoldTime = 0f;
                        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;
                    }
                    break;

                case QTEType.ButtonMash:
                    // แถบเพิ่มขึ้นตามจำนวนครั้งที่รัว
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    {
                        currentClicks++;
                        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = (float)currentClicks / mashRequiredClicks;
                        if (currentClicks >= mashRequiredClicks) { success = true; timeLeft = 0; }
                    }
                    break;
            }
            yield return null;
        }

        ApplyResult(success);

        // Reset ค่าเป็น 0 ทันทีหลังจบกิจกรรม เพื่อให้แมวตัวต่อไปเริ่มที่ 0
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;

        isInteracting = false;
        if (qtePanel != null) qtePanel.SetActive(false);

        // ปิดระบบและส่งแมวออกอัตโนมัติ
        ForceCloseSystem();
    }

    void ApplyResult(bool success)
    {
        float change = success ? 0.1f : -0.1f;
        currentReputation = Mathf.Clamp01(currentReputation + change);
        if (reputationBarFill != null) reputationBarFill.fillAmount = currentReputation;
    }

    public void ForceCloseSystem()
    {
        isInteracting = false;
        StopAllCoroutines();

        // Reset ค่าอีกครั้งเพื่อความปลอดภัย
        if (qteProgressBarFill != null) qteProgressBarFill.fillAmount = 0;

        if (currentTable != null)
        {
            currentTable.CloseInteractionUI();
        }
        this.gameObject.SetActive(false);
    }
}