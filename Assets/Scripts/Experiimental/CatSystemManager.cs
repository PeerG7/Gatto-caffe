using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

public class CatSystemManager : MonoBehaviour
{
    [Header("UI Stages")]
    public GameObject mainInteractButton; // ปุ่มหลัก "เริ่มคุยกับแมว"
    public GameObject selectionMenu;      // เมนูที่มี 3 ปุ่ม (Pet, Hug, Play)
    public GameObject qtePanel;           // แถบ Progress Bar

    [Header("Reputation & Visuals")]
    public Slider reputationSlider;
    public float currentReputation = 0;
    public GameObject goodCat;
    public GameObject badCat;

    [Header("QTE Settings")]
    public Slider qteProgressBar;
    public float interactionWindow = 3.0f;
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;

    private bool isInteracting = false;

    void Start()
    {
        // เริ่มต้น: เปิดแค่ปุ่มหลัก ปิดอย่างอื่นให้หมด
        ShowMainButton();
        UpdateUI();
    }

    // 1. เมื่อกดปุ่มหลัก (Main Button)
    public void OpenSelectionMenu()
    {
        mainInteractButton.SetActive(false);
        selectionMenu.SetActive(true);
    }

    // 2. เมื่อเลือก 1 ใน 3 วิธีจากเมนู
    public void TriggerInteraction(int typeIndex)
    {
        if (!isInteracting)
        {
            selectionMenu.SetActive(false); // ซ่อนเมนูเลือกทันที
            StartCoroutine(RunInteractionQTE((QTEType)typeIndex));
        }
    }

    IEnumerator RunInteractionQTE(QTEType qteType)
    {
        isInteracting = true;
        qtePanel.SetActive(true);

        float timeLeft = interactionWindow;
        bool success = false;
        float currentHoldTime = 0f;
        int currentClicks = 0;
        qteProgressBar.value = 0;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            switch (qteType)
            {
                case QTEType.SinglePress:
                    qteProgressBar.value = timeLeft / interactionWindow;
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    { success = true; timeLeft = 0; }
                    break;

                case QTEType.Hold:
                    qteProgressBar.value = currentHoldTime / holdRequiredTime;
                    if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                    {
                        currentHoldTime += Time.deltaTime;
                        if (currentHoldTime >= holdRequiredTime) { success = true; timeLeft = 0; }
                    }
                    else { currentHoldTime = 0f; }
                    break;

                case QTEType.ButtonMash:
                    qteProgressBar.value = (float)currentClicks / mashRequiredClicks;
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    {
                        currentClicks++;
                        if (currentClicks >= mashRequiredClicks) { success = true; timeLeft = 0; }
                    }
                    break;
            }
            yield return null;
        }

        // 3. จบ QTE: ซ่อนทุกอย่างและกลับไปเริ่มต้นใหม่
        qtePanel.SetActive(false);
        ApplyResult(success);
        isInteracting = false;
        ShowMainButton();
    }

    void ShowMainButton()
    {
        mainInteractButton.SetActive(true);
        selectionMenu.SetActive(false);
        qtePanel.SetActive(false);
    }

    void ApplyResult(bool success)
    {
        currentReputation += success ? 0.1f : -0.1f;
        currentReputation = Mathf.Clamp(currentReputation, 0f, 1f);
        UpdateUI();
    }

    void UpdateUI()
    {
        reputationSlider.value = currentReputation;
        bool isHappy = currentReputation >= 0;
        goodCat.SetActive(isHappy);
        badCat.SetActive(!isHappy);
    }
}