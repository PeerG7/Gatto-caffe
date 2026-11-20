using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MainQuestManager : MonoBehaviour
{
    public static MainQuestManager Instance;

    [Serializable]
    public struct QuestStepTextEntry
    {
        public QuestStep step;
        [Tooltip("ลาก GameObject ของ TMP_Text สำหรับขั้นเควสนี้มาใส่")]
        public GameObject textObject;
    }

    [Header("Quest Text Objects (เปิด/ปิดตามขั้นเควส)")]
    [Tooltip("ใส่ให้ครบทุก QuestStep เพื่อให้ระบบสลับเปิด/ปิดอัตโนมัติ")]
    public List<QuestStepTextEntry> questStepTexts = new List<QuestStepTextEntry>();

    [Header("Quest Panel Fade (ตอนเควสเสร็จหมดแล้ว)")]
    public GameObject questPanelRoot;
    public GameObject[] additionalFadeObjects;
    public float completedShowDuration = 3f;
    public float fadeOutDuration = 1.5f;

    public enum QuestStep
    {
        BuyFurniture,
        PlaceFurniture,
        InviteCat1,
        InviteCat2,
        GuideCat2ToTable,
        CookFood,
        ServeFood,
        CarryCat2ToZone,
        Completed
    }

    public QuestStep currentQuest = QuestStep.BuyFurniture;

    private const string QuestSaveKey = "SavedMainQuestStep";
    private const string DaySaveKey = "SavedGameDay";
    private bool _holdingTutorialPause = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadQuestProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateQuestUI();

        if (currentQuest == QuestStep.Completed)
            HideQuestPanelImmediate();

        if (currentQuest != QuestStep.Completed && DayNightManager.Instance != null)
        {
            DayNightManager.Instance.PauseGame();
            _holdingTutorialPause = true;
        }

        PlayerController2D.SetLock(false);
    }

    void Update()
    {
        if (currentQuest == QuestStep.BuyFurniture &&
            FurnitureInventory.Instance != null &&
            FurnitureInventory.Instance.storedInstances.Count > 0)
        {
            AdvanceQuest();
        }
    }

    void LoadQuestProgress()
    {
        bool isReturningPlayer = PlayerPrefs.HasKey(DaySaveKey);
        bool questAlreadyCompleted = PlayerPrefs.GetInt(QuestSaveKey, 0) == (int)QuestStep.Completed
                                     && PlayerPrefs.HasKey(QuestSaveKey);

        currentQuest = (isReturningPlayer || questAlreadyCompleted)
            ? QuestStep.Completed
            : QuestStep.BuyFurniture;
    }

    void SaveQuestProgress()
    {
        PlayerPrefs.SetInt(QuestSaveKey, (int)currentQuest);
        PlayerPrefs.Save();
    }

    // 🟢 เปลี่ยนจากการเขียน .text เป็นการเปิดเฉพาะ Text ของขั้นปัจจุบัน และปิดตัวอื่น
    public void UpdateQuestUI()
    {
        foreach (var entry in questStepTexts)
        {
            if (entry.textObject == null) continue;

            bool shouldShow = (entry.step == currentQuest);
            if (entry.textObject.activeSelf != shouldShow)
            {
                entry.textObject.SetActive(shouldShow);
            }
        }
    }

    public void AdvanceQuest()
    {
        bool justCompleted = false;

        if (currentQuest == QuestStep.BuyFurniture) currentQuest = QuestStep.PlaceFurniture;
        else if (currentQuest == QuestStep.PlaceFurniture) currentQuest = QuestStep.InviteCat1;
        else if (currentQuest == QuestStep.InviteCat1) currentQuest = QuestStep.InviteCat2;
        else if (currentQuest == QuestStep.InviteCat2) currentQuest = QuestStep.GuideCat2ToTable;
        else if (currentQuest == QuestStep.GuideCat2ToTable) currentQuest = QuestStep.CookFood;
        else if (currentQuest == QuestStep.CookFood) currentQuest = QuestStep.ServeFood;
        else if (currentQuest == QuestStep.ServeFood) currentQuest = QuestStep.CarryCat2ToZone;
        else if (currentQuest == QuestStep.CarryCat2ToZone)
        {
            currentQuest = QuestStep.Completed;
            justCompleted = true;
            if (_holdingTutorialPause && DayNightManager.Instance != null)
            {
                DayNightManager.Instance.ResumeGame();
                _holdingTutorialPause = false;
            }
        }

        SaveQuestProgress();
        UpdateQuestUI();

        if (justCompleted && DayNightManager.Instance != null)
            DayNightManager.Instance.SaveDayProgress();

        if (TutorialPointerManager.Instance != null)
            TutorialPointerManager.Instance.UpdatePointerState();

        if (currentQuest == QuestStep.Completed)
            StartCoroutine(FadeOutQuestPanelRoutine());
    }

    // ── Quest Panel helpers ────────────────────────────────────────
    GameObject GetQuestPanelRoot()
    {
        if (questPanelRoot != null) return questPanelRoot;

        // ใช้ GameObject ของข้อความที่ตรงกับ currentQuest เป็นตัวอ้างอิงแทน questDisplayText
        foreach (var entry in questStepTexts)
        {
            if (entry.step == currentQuest && entry.textObject != null)
            {
                Transform parent = entry.textObject.transform.parent;
                if (parent != null && parent.GetComponent<Canvas>() == null)
                    return parent.gameObject;
                return entry.textObject;
            }
        }

        return null;
    }

    List<GameObject> GetFadeTargets()
    {
        List<GameObject> targets = new List<GameObject>();

        GameObject main = GetQuestPanelRoot();
        if (main != null) targets.Add(main);

        if (additionalFadeObjects != null)
        {
            foreach (var go in additionalFadeObjects)
                if (go != null && !targets.Contains(go)) targets.Add(go);
        }
        return targets;
    }

    void DeactivateTarget(GameObject go, CanvasGroup cg)
    {
        if (cg != null) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
        if (!transform.IsChildOf(go.transform)) go.SetActive(false);
    }

    void HideQuestPanelImmediate()
    {
        foreach (var go in GetFadeTargets())
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            DeactivateTarget(go, cg);
        }
    }

    System.Collections.IEnumerator FadeOutQuestPanelRoutine()
    {
        yield return new WaitForSecondsRealtime(completedShowDuration);

        List<GameObject> targets = GetFadeTargets();
        if (targets.Count == 0) yield break;

        List<CanvasGroup> groups = new List<CanvasGroup>();
        foreach (var go in targets)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            groups.Add(cg);
        }

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(1f - t / fadeOutDuration);
            foreach (var cg in groups) if (cg != null) cg.alpha = a;
            yield return null;
        }

        for (int i = 0; i < targets.Count; i++)
            if (targets[i] != null) DeactivateTarget(targets[i], groups[i]);
    }
}