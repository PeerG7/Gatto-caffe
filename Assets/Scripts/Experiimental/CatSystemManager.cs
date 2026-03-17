using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum QTEType { SinglePress, Hold, ButtonMash }
public class CatSystemManager : MonoBehaviour
{
    [Header("ข้อมูลแมวปัจจุบัน (Active Cat)")]
    public CatProfile activeCat; // ลากไฟล์ Cat Profile ที่สร้างไว้มาใส่ช่องนี้

    [Header("UI Stages")]
    public GameObject mainInteractButton;
    public GameObject selectionMenu;
    public GameObject qtePanel;

    [Header("Reputation & Visuals")]
    public Slider reputationSlider;
    public float currentReputation = 0;
    public GameObject goodCat;
    public GameObject badCat;

    [Header("QTE Settings")]
    public Slider qteProgressBar;
    private bool isInteracting = false;

    void Start()
    {
        // ดึงโปรไฟล์แมวจาก SceneLoader
        if (SceneLoader.Instance != null && SceneLoader.Instance.currentCatInConversation != null)
        {
            activeCat = SceneLoader.Instance.currentCatInConversation;
        }

        ShowMainButton();
        UpdateUI();
    }

    public void SetupCat(CatProfile profile)
    {
        activeCat = profile;
        isInteracting = false; // ต้องเป็น false เพื่อให้ปุ่มเริ่มทำงานได้
        Debug.Log("Manager พร้อมทำงาน: " + activeCat.catName);
        ShowMainButton();
    }

    public void OpenSelectionMenu()
    {
        mainInteractButton.SetActive(false);
        selectionMenu.SetActive(true);
    }

    // ฟังก์ชันนี้จะรับ Index (0, 1, 2) จากปุ่มในหน้าต่าง Selection Menu
    public void TriggerInteraction(int typeIndex)
    {
        if (!isInteracting && activeCat != null)
        {
            selectionMenu.SetActive(false);
            // ดึงข้อมูลการโต้ตอบตาม Index จาก Profile ของแมวตัวนั้น
            StartCoroutine(RunInteractionQTE(activeCat.interactions[typeIndex]));
        }
    }

    IEnumerator RunInteractionQTE(CatInteraction data)
    {
        isInteracting = true;
        qtePanel.SetActive(true);

        float timeLeft = data.timeLimit;
        bool success = false;
        float currentHoldTime = 0f;
        int currentClicks = 0;
        qteProgressBar.value = 0;

        // การตั้งค่าความแรง/ความเร็วของ QTE (ดึงมาจาก Manager เดิม)
        float holdRequired = 1.5f;
        int mashRequired = 10;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            switch (data.qteType)
            {
                case QTEType.SinglePress:
                    qteProgressBar.value = timeLeft / data.timeLimit;
                    // ตรวจสอบปุ่มเฉพาะของแมวตัวนี้
                    if (Input.GetKeyDown(data.preferredKey))
                    { success = true; timeLeft = 0; }
                    break;

                case QTEType.Hold:
                    qteProgressBar.value = currentHoldTime / holdRequired;
                    // ต้องกดปุ่ม preferredKey ค้างไว้
                    if (Input.GetKey(data.preferredKey))
                    {
                        currentHoldTime += Time.deltaTime;
                        if (currentHoldTime >= holdRequired) { success = true; timeLeft = 0; }
                    }
                    else { currentHoldTime = 0f; } // ถ้าปล่อยปุ่มให้เริ่มนับใหม่
                    break;

                case QTEType.ButtonMash:
                    qteProgressBar.value = (float)currentClicks / mashRequired;
                    // รัวปุ่ม preferredKey ให้ครบจำนวน
                    if (Input.GetKeyDown(data.preferredKey))
                    {
                        currentClicks++;
                        if (currentClicks >= mashRequired) { success = true; timeLeft = 0; }
                    }
                    break;
            }
            yield return null;
        }

        qtePanel.SetActive(false);
        ApplyResult(success, data.reputationChange);
        isInteracting = false;
        ShowMainButton();
    }

    void ShowMainButton()
    {
        mainInteractButton.SetActive(true);
        selectionMenu.SetActive(false);
        qtePanel.SetActive(false);
    }

    void ApplyResult(bool success, float repChange)
    {
        // บวกหรือลบค่าความสัมพันธ์ตามข้อมูลของแมว
        currentReputation += success ? repChange : -repChange;
        currentReputation = Mathf.Clamp(currentReputation, 0f, 1f);
        UpdateUI();
    }

    void UpdateUI()
    {
        reputationSlider.value = currentReputation;
        bool isHappy = currentReputation >= 0.5f; // สมมติว่าเกินครึ่งคืออารมณ์ดี
        if (goodCat) goodCat.SetActive(isHappy);
        if (badCat) badCat.SetActive(!isHappy);
    }
}