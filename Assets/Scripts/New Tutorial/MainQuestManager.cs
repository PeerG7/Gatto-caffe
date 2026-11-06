using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class MainQuestManager : MonoBehaviour
{
    public static MainQuestManager Instance;

    [Header("UI Reference")]
    public TMP_Text questDisplayText;
    public GameObject questBackground; // 🌟 Background image/panel for the main quest

    public enum QuestStep { BuyFurniture, InviteCat1, InviteCat2, GuideCat2ToTable, CookFood, ServeFood, CarryCat2ToZone, Completed }
    public QuestStep currentQuest = QuestStep.BuyFurniture;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (questBackground != null) questBackground.SetActive(true);
        UpdateQuestUI();

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.PauseGame();
        }
        PlayerController2D.SetLock(false);
    }

    public void UpdateQuestUI()
    {
        if (questDisplayText == null) return;

        // รีเซ็ตค่า Alpha ของข้อความให้ชัดเจน (1) เสมอก่อนเปลี่ยนข้อความสเต็ปใหม่
        Color c = questDisplayText.color;
        c.a = 1f;
        questDisplayText.color = c;

        if (questBackground != null)
        {
            questBackground.SetActive(currentQuest != QuestStep.Completed);
        }

        switch (currentQuest)
        {
            case QuestStep.BuyFurniture:
                questDisplayText.text = "MainQuest: Buy your first table or piece of furniture from Computer On Counter desk";
                break;
            case QuestStep.InviteCat1:
                questDisplayText.text = "MainQuest: Invite the Cats in front Cafe";
                break;
            case QuestStep.InviteCat2:
                questDisplayText.text = "MainQuest: Invite the second cat to the Cafe.";
                break;
            case QuestStep.GuideCat2ToTable:
                questDisplayText.text = "MainQuest: Guide the second cat to the table and verify it has taken a seat.";
                break;
            case QuestStep.CookFood:
                questDisplayText.text = "MainQuest: Cook the food according to the cat's order";
                break;
            case QuestStep.ServeFood:
                questDisplayText.text = "MainQuest: Serve the cat food at the table.";
                break;
            case QuestStep.CarryCat2ToZone:
                questDisplayText.text = "MainQuest: Carry the second cat to the Interaction Zone!";
                break;
            case QuestStep.Completed:
                questDisplayText.text = "All initial missions have been successfully completed!";
                StartCoroutine(FadeOutQuestTextRoutine()); // 🌟 สั่งให้ข้อความและ Background ค่อยๆ Fade ออกเมื่อเควสเสร็จสมบูรณ์
                break;
        }

        if (TutorialPointerManager.Instance != null)
        {
            TutorialPointerManager.Instance.UpdatePointerState();
        }
    }

    // 🌟 Coroutine สำหรับทำเอฟเฟกต์ค่อยๆ จางหาย (Fade Out) ของตัวอักษรและ Background
    private IEnumerator FadeOutQuestTextRoutine()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Color startColor = questDisplayText.color;

        Image bgImage = questBackground != null ? questBackground.GetComponent<Image>() : null;
        Color bgStartColor = bgImage != null ? bgImage.color : Color.white;

        yield return new WaitForSecondsRealtime(1.0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

            questDisplayText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            if (bgImage != null)
            {
                bgImage.color = new Color(bgStartColor.r, bgStartColor.g, bgStartColor.b, alpha);
            }

            yield return null;
        }

        questDisplayText.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        if (questBackground != null) questBackground.SetActive(false);
    }

    public void AdvanceQuest()
    {
        if (currentQuest == QuestStep.BuyFurniture) currentQuest = QuestStep.InviteCat1;
        else if (currentQuest == QuestStep.InviteCat1) currentQuest = QuestStep.InviteCat2;
        else if (currentQuest == QuestStep.InviteCat2) currentQuest = QuestStep.GuideCat2ToTable;
        else if (currentQuest == QuestStep.GuideCat2ToTable) currentQuest = QuestStep.CookFood;
        else if (currentQuest == QuestStep.CookFood) currentQuest = QuestStep.ServeFood;
        else if (currentQuest == QuestStep.ServeFood) currentQuest = QuestStep.CarryCat2ToZone;
        else if (currentQuest == QuestStep.CarryCat2ToZone)
        {
            currentQuest = QuestStep.Completed;
            if (DayNightManager.Instance != null)
            {
                DayNightManager.Instance.ResumeGame();
            }
        }

        UpdateQuestUI();
    }
}