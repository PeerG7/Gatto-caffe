using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Define our 3 interaction types
public enum QTEType
{
    SinglePress, // Click once
    Hold,        // Hold down the button
    ButtonMash   // Click multiple times quickly
}

public class CatSystemManager : MonoBehaviour
{
    [Header("Reputation Settings")]
    public Slider reputationSlider; // Ranges from -100 to 100
    public float currentReputation = 0;
    public GameObject goodCat;
    public GameObject badCat;

    [Header("QTE Base Settings")]
    public Slider qteProgressBar; // Ranges from 0 to 1
    public GameObject qtePanel;   // Holds the progress bar UI
    public float interactionWindow = 2.0f; // Seconds to react/complete

    [Header("Extended QTE Settings")]
    public float holdRequiredTime = 1.0f; // Seconds needed to hold the button
    public int mashRequiredClicks = 5;    // Clicks needed for Button Mash mode

    private bool isInteracting = false;

    void Start()
    {
        UpdateUI();
        qtePanel.SetActive(false);
    }

    // Call this from a UI Button and pass 0 (SinglePress), 1 (Hold), or 2 (Mash)
    public void TriggerInteraction(int typeIndex)
    {
        if (!isInteracting)
        {
            QTEType selectedType = (QTEType)typeIndex;
            StartCoroutine(RunInteractionQTE(selectedType));
        }
    }

    IEnumerator RunInteractionQTE(QTEType qteType)
    {
        isInteracting = true;
        qtePanel.SetActive(true);

        float timeLeft = interactionWindow;
        bool success = false;

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
                    }
                    break;
            }

            yield return null; // Wait for the next frame
        }

        qtePanel.SetActive(false);
        ApplyResult(success);
        isInteracting = false;
    }

    void ApplyResult(bool success)
    {
        // Adjusted reputation gain/loss to match the -100 to 100 slider range
        currentReputation += success ? 10f : -10f;
        currentReputation = Mathf.Clamp(currentReputation, -100f, 100f);

        UpdateUI();
    }

    void UpdateUI()
    {
        reputationSlider.value = currentReputation;

        // Swap Prefabs based on score
        bool isHappy = currentReputation >= 0;
        goodCat.SetActive(isHappy);
        badCat.SetActive(!isHappy);
    }
}