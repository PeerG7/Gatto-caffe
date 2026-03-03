using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CatSystemManager : MonoBehaviour
{
    [Header("Reputation Settings")]
    public Slider reputationSlider; // Ranges from -100 to 100
    public float currentReputation = 0;
    public GameObject goodCat;
    public GameObject badCat;

    [Header("QTE Settings")]
    public Slider qteProgressBar; // Ranges from 0 to 1
    public GameObject qtePanel;   // Holds the progress bar UI
    public float interactionWindow = 2.0f; // Seconds to react

    private bool isInteracting = false;

    void Start()
    {
        UpdateUI();
        qtePanel.SetActive(false);
    }

    public void TriggerInteraction()
    {
        if (!isInteracting) StartCoroutine(RunInteractionQTE());
    }

    IEnumerator RunInteractionQTE()
    {
        isInteracting = true;
        qtePanel.SetActive(true);

        float timeLeft = interactionWindow;
        bool success = false;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            // Update the Progress Bar visually (Time Left / Total Time)
            qteProgressBar.value = timeLeft / interactionWindow;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                success = true;
                break;
            }
            yield return null;
        }

        qtePanel.SetActive(false);
        ApplyResult(success);
        isInteracting = false;
    }

    void ApplyResult(bool success)
    {
        // Adjust reputation based on success or failure
        currentReputation += success ? 20 : -20;
        currentReputation = Mathf.Clamp(currentReputation, -100, 100);

        UpdateUI();
    }

    void UpdateUI()
    {
        // Update the main reputation slider
        reputationSlider.value = currentReputation;

        // Swap Prefabs based on score
        bool isHappy = currentReputation >= 0;
        goodCat.SetActive(isHappy);
        badCat.SetActive(!isHappy);
    }
}