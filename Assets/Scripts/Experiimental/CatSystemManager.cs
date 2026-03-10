using UnityEngine;
using UnityEngine.UI;
using System.Collections;

<<<<<<< HEAD
public enum QTEType { SinglePress, Hold, ButtonMash }
=======
// Define our 3 interaction types
public enum QTEType
{
    SinglePress, // Click once
    Hold,        // Hold down the button
    ButtonMash   // Click multiple times quickly
}
>>>>>>> main

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

<<<<<<< HEAD
    [Header("QTE Settings")]
    public Slider qteProgressBar;
    public float interactionWindow = 3.0f;
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;
=======
    [Header("QTE Base Settings")]
    public Slider qteProgressBar; // Ranges from 0 to 1
    public GameObject qtePanel;   // Holds the progress bar UI
    public float interactionWindow = 2.0f; // Seconds to react/complete

    [Header("Extended QTE Settings")]
    public float holdRequiredTime = 1.0f; // Seconds needed to hold the button
    public int mashRequiredClicks = 5;    // Clicks needed for Button Mash mode
>>>>>>> main

    private bool isInteracting = false;

    void Start()
    {
        // เริ่มต้น: เปิดแค่ปุ่มหลัก ปิดอย่างอื่นให้หมด
        ShowMainButton();
        UpdateUI();
    }

<<<<<<< HEAD
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

=======
    // Call this from a UI Button and pass 0 (SinglePress), 1 (Hold), or 2 (Mash)
    public void TriggerInteraction(int typeIndex)
    {
        if (!isInteracting)
        {
            QTEType selectedType = (QTEType)typeIndex;
            StartCoroutine(RunInteractionQTE(selectedType));
        }
    }

>>>>>>> main
    IEnumerator RunInteractionQTE(QTEType qteType)
    {
        isInteracting = true;
        qtePanel.SetActive(true);

        float timeLeft = interactionWindow;
        bool success = false;
        float currentHoldTime = 0f;
        int currentClicks = 0;
        qteProgressBar.value = 0;

        // Variables to track progress for Hold and Mash
        float currentHoldTime = 0f;
        int currentClicks = 0;

        // Reset progress bar at the start
        qteProgressBar.value = 0;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            switch (qteType)
            {
                case QTEType.SinglePress:
<<<<<<< HEAD
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
=======
                    // For Single Press, the bar shows time running out
                    qteProgressBar.value = timeLeft / interactionWindow;
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    {
                        success = true;
                        timeLeft = 0; // Force loop to end
                    }
                    break;

                case QTEType.Hold:
                    // For Hold, the bar shows hold progress
                    qteProgressBar.value = currentHoldTime / holdRequiredTime;

                    if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                    {
                        currentHoldTime += Time.deltaTime;
                        if (currentHoldTime >= holdRequiredTime)
                        {
                            success = true;
                            timeLeft = 0;
                        }
                    }
                    else
                    {
                        // Reset progress if the player lets go early
                        currentHoldTime = 0f;
                    }
                    break;

                case QTEType.ButtonMash:
                    // For Mash, the bar shows click progress
                    qteProgressBar.value = (float)currentClicks / mashRequiredClicks;

                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    {
                        currentClicks++;
                        if (currentClicks >= mashRequiredClicks)
                        {
                            success = true;
                            timeLeft = 0;
                        }
>>>>>>> main
                    }
                    break;
            }

            yield return null; // Wait for the next frame
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
<<<<<<< HEAD
        currentReputation += success ? 0.1f : -0.1f;
        currentReputation = Mathf.Clamp(currentReputation, 0f, 1f);
=======
        // Adjusted reputation gain/loss to match the -100 to 100 slider range
        currentReputation += success ? 10f : -10f;
        currentReputation = Mathf.Clamp(currentReputation, -100f, 100f);

>>>>>>> main
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