using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MainQuestManager : MonoBehaviour
{
    public static MainQuestManager Instance;

    [Header("UI Reference")]
    public TMP_Text questDisplayText;

    [Header("Quest Panel Fade (ตอนเควสเสร็จหมดแล้ว)")]
    [Tooltip("กล่องเควสทั้งกล่อง (พื้นหลัง + ข้อความ) ที่จะ Fade ออก — แนะนำให้ลากใส่เอง ถ้าเว้นว่างจะใช้ parent ของ questDisplayText")]
    public GameObject questPanelRoot;
    [Tooltip("ชิ้นส่วน UI อื่นของเควสที่ต้องหายไปพร้อมกัน เช่น Board (กรอบสีน้ำตาลที่มีคำว่า Quest) — ลากมาใส่ได้หลายตัว")]
    public GameObject[] additionalFadeObjects;
    [Tooltip("โชว์ข้อความ 'ทำเควสเสร็จแล้ว' ค้างไว้กี่วินาทีก่อนเริ่ม Fade")]
    public float completedShowDuration = 3f;
    [Tooltip("ระยะเวลา Fade ออก (วินาที)")]
    public float fadeOutDuration = 1.5f;

    // 🌟 เพิ่มสถานะ InviteCat2 เข้ามาเป็นลำดับที่ 2
    // 🟢 เพิ่ม GuideCat2ToTable (จูงแมวตัวที่ 2 ไปโต๊ะ) และ CarryCat2ToZone (อุ้มแมวตัวที่ 2 ไปวางที่ Zone)
    // ที่ NPCController อ้างถึงอยู่ — ถ้า Enum ไม่มีค่าเหล่านี้จะขึ้นแดง
    public enum QuestStep { BuyFurniture, InviteCat1, InviteCat2, GuideCat2ToTable, CookFood, ServeFood, CarryCat2ToZone, Completed }
    public QuestStep currentQuest = QuestStep.BuyFurniture;

    // 🔑 ต้องตรงกับ Key ที่ MainMenuManager / NewGameIntroDialogue ลบตอนกด New Game
    private const string QuestSaveKey = "SavedMainQuestStep";
    private const string DaySaveKey = "SavedGameDay"; // ต้องตรงกับ DayNightManager

    // true = เราเป็นคนเรียก PauseGame() ไว้ และยังไม่ได้ ResumeGame() (กัน Pause ค้าง/Resume เกิน)
    private bool _holdingTutorialPause = false;

    void Awake()
    {
        // ↩️ คืนเป็นพฤติกรรมเดิม (ตัวแรกชนะ) — ดูคำอธิบายที่ DayNightManager.Awake()
        if (Instance == null)
        {
            Instance = this;
            LoadQuestProgress(); // Continue = ข้าม Tutorial / New Game = เริ่ม Tutorial ใหม่
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateQuestUI();

        // 🟢 Continue หลังเควสเสร็จหมดแล้ว → ซ่อนกล่องเควสทันที ไม่ต้องโชว์ซ้ำ
        if (currentQuest == QuestStep.Completed) HideQuestPanelImmediate();

        // 🟢 FIX: Pause เกมเฉพาะตอน Tutorial ยังไม่จบเท่านั้น
        // (เดิม Pause ทุกครั้งที่เข้าเกม รวมถึง Continue ทำให้เวลาหยุดค้างตลอดเพราะเควสเริ่มใหม่เสมอ)
        if (currentQuest != QuestStep.Completed && DayNightManager.Instance != null)
        {
            DayNightManager.Instance.PauseGame();
            _holdingTutorialPause = true;
        }
        PlayerController2D.SetLock(false);
    }

    void LoadQuestProgress()
    {
        // 🟢 SKIP TUTORIAL: ถ้ามีเซฟวันอยู่ (= เคยเล่นแล้วและกลับมากด Continue) หรือเควสเคยจบแล้ว → ข้าม Tutorial ทั้งหมด
        // ส่วน New Game จะลบทั้ง SavedGameDay และ SavedMainQuestStep ไปแล้ว จึงเริ่ม Tutorial ใหม่ตามปกติ
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

    public void UpdateQuestUI()
    {
        if (questDisplayText == null) return;

        switch (currentQuest)
        {
            case QuestStep.BuyFurniture:
                questDisplayText.text = "MainQuest: Buy your first table or piece of furniture from Computer On Counter desk";
                break;
            case QuestStep.InviteCat1:
                questDisplayText.text = "MainQuest: Invite the Cats in front Cafe";
                break;
            case QuestStep.InviteCat2:
                questDisplayText.text = "Main mission: Invite the second cat to the Cafe. (NeedyCat)";
                break;
            case QuestStep.GuideCat2ToTable:
                questDisplayText.text = "MainQuest: Press E near the cat to guide it, then lead it to a table.";
                break;
            case QuestStep.CookFood:
                questDisplayText.text = "MainQuest: Cook the food according to the cat's order";
                break;
            case QuestStep.ServeFood:
                questDisplayText.text = "MainQuest: Serve the cat food at the table.";
                break;
            case QuestStep.CarryCat2ToZone:
                questDisplayText.text = "MainQuest: Pick up the cat and place it in the Interaction Zone.";
                break;
            case QuestStep.Completed:
                questDisplayText.text = "All initial missions have been successfully completed!";
                break;
        }
    }

    public void AdvanceQuest()
    {
        bool justCompleted = false;

        if (currentQuest == QuestStep.BuyFurniture) currentQuest = QuestStep.InviteCat1;
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

        SaveQuestProgress(); // 🟢 FIX: เซฟทุกครั้งที่เควสขยับ
        UpdateQuestUI();

        // 🟢 CHECKPOINT วันแรก: จบ Tutorial แล้วเซฟ วัน/เงิน/เฟอร์นิเจอร์ ณ ตอนนี้
        // (Continue ในวันแรกจะกลับมาที่จุดนี้ ไม่ใช่เงินที่หาได้หลังจากนั้น)
        if (justCompleted && DayNightManager.Instance != null)
            DayNightManager.Instance.SaveDayProgress();

        // 🟢 สั่งให้ Pointer ชี้เป้าใหม่ / ซ่อนทันทีเมื่อเควสเปลี่ยน
        if (TutorialPointerManager.Instance != null)
            TutorialPointerManager.Instance.UpdatePointerState();

        // 🟢 เควสเสร็จหมด → โชว์ข้อความสักพักแล้วค่อยๆ Fade กล่องเควสออก
        if (currentQuest == QuestStep.Completed)
            StartCoroutine(FadeOutQuestPanelRoutine());
    }

    // ── Quest Panel helpers ────────────────────────────────────────
    GameObject GetQuestPanelRoot()
    {
        if (questPanelRoot != null) return questPanelRoot;
        if (questDisplayText == null) return null;

        // ถ้า parent เป็น Canvas หลัก ห้ามใช้ (จะ Fade UI ทั้งหน้าจอ) ให้ใช้ตัวข้อความเอง
        Transform parent = questDisplayText.transform.parent;
        if (parent != null && parent.GetComponent<Canvas>() == null)
            return parent.gameObject;
        return questDisplayText.gameObject;
    }

    // รวมทุกชิ้นของ UI เควสที่ต้องซ่อน: Panel หลัก + Board/ชิ้นอื่นที่ลากใส่ additionalFadeObjects
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

    // ถ้า UI เควสเป็นพ่อของ GameObject ที่มีสคริปต์นี้ ห้ามปิด (ไม่งั้น Coroutine/สคริปต์หยุดทำงาน) ให้ซ่อนด้วย alpha แทน
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