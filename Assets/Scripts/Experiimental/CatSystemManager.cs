using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }

public class CatSystemManager : MonoBehaviour
{
    public static CatSystemManager Instance;

    [Header("UI Stages")]
    public GameObject mainInteractButton;
    public GameObject selectionMenu;
    public GameObject qtePanel;

    [Header("Reputation (Fill Image)")]
    public Image reputationFill;
    public float currentReputation = 0f;

    [Header("Reputation Visual")]
    public Color lowColor = Color.red;
    public Color highColor = Color.green;
    public float fillSpeed = 5f;
    private float targetReputation;

    [Header("QTE (Fill Image)")]
    public Image qteFill;
    public float interactionWindow = 3.0f;
    public float holdRequiredTime = 1.5f;
    public int mashRequiredClicks = 10;

    private bool isInteracting = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        targetReputation = currentReputation;
        ShowMainButton();
        UpdateUIInstant();
    }

    void Update()
    {
        // Smooth reputation fill (ไม่โดน TimeScale)
        if (reputationFill != null)
        {
            reputationFill.fillAmount = Mathf.Lerp(
                reputationFill.fillAmount,
                targetReputation,
                Time.unscaledDeltaTime * fillSpeed
            );

            reputationFill.color = Color.Lerp(
                lowColor,
                highColor,
                reputationFill.fillAmount
            );
        }
    }

    // ===== ENTRY =====
    public void StartInteraction()
    {
        ShowMainButton();
        UpdateUIInstant();
    }

    // ===== UI FLOW =====
    public void OpenSelectionMenu()
    {
        mainInteractButton.SetActive(false);
        selectionMenu.SetActive(true);
    }

    public void TriggerInteraction(int typeIndex)
    {
        if (!isInteracting)
        {
            selectionMenu.SetActive(false);
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

        if (qteFill != null)
            qteFill.fillAmount = 0f;

        while (timeLeft > 0)
        {
            timeLeft -= Time.unscaledDeltaTime;

            switch (qteType)
            {
                case QTEType.SinglePress:
                    if (qteFill != null)
                        qteFill.fillAmount = timeLeft / interactionWindow;

                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    {
                        success = true;
                        timeLeft = 0;
                    }
                    break;

                case QTEType.Hold:
                    if (qteFill != null)
                        qteFill.fillAmount = currentHoldTime / holdRequiredTime;

                    if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
                    {
                        currentHoldTime += Time.unscaledDeltaTime;

                        if (currentHoldTime >= holdRequiredTime)
                        {
                            success = true;
                            timeLeft = 0;
                        }
                    }
                    else
                    {
                        currentHoldTime = 0f;
                    }
                    break;

                case QTEType.ButtonMash:
                    if (qteFill != null)
                        qteFill.fillAmount = (float)currentClicks / mashRequiredClicks;

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

            yield return null;
        }

        qtePanel.SetActive(false);
        ApplyResult(success);
        isInteracting = false;

        UIManager.Instance.CloseRelationshipPanel();
    }

    // ===== RESULT =====
    void ApplyResult(bool success)
    {
        float amount = success ? 0.1f : -0.1f;

        currentReputation += amount;
        currentReputation = Mathf.Clamp(currentReputation, 0f, 1f);

        targetReputation = currentReputation;
    }

    // ===== UI STATE =====
    void ShowMainButton()
    {
        mainInteractButton.SetActive(true);
        selectionMenu.SetActive(false);
        qtePanel.SetActive(false);
    }

    void UpdateUIInstant()
    {
        if (reputationFill != null)
        {
            reputationFill.fillAmount = currentReputation;
            reputationFill.color = Color.Lerp(lowColor, highColor, currentReputation);
        }
    }
}