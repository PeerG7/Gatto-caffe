using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// ComicStoryManager — ตัดสินใจว่า "ฉากไหนควรเล่นเมื่อไหร่" ตามเงื่อนไข
//
//  เพิ่มฉากใหม่ = สร้าง ComicSequenceData แล้วเพิ่ม 1 แถวใน Triggers (ไม่ต้องแก้โค้ด)
//
//  ชนิด Trigger
//   • NewGame    : เริ่ม New Game (เข้า Scene เกมโดยไม่มีเซฟวัน)
//   • DayStarted : เริ่มวันที่ N (Day = 0 → ทุกวัน) — เช็กตอนขึ้นวันใหม่ และตอนเข้า Scene ด้วย Continue
//   • Custom     : ระบบอื่นเรียก ComicStoryManager.Instance.RaiseEvent("ชื่อ") เช่น "first_vip"
//
//  เงื่อนไขเสริม (ต้องผ่านทั้งหมด): Min Rating / Rating เพิ่มขึ้นจากเมื่อวาน / เสิร์ฟเมื่อวานได้ขั้นต่ำ
//
//  "เล่นไปแล้ว" บันทึกใน PlayerPrefs (ComicSeen_<id>) และล้างเองเมื่อเข้าเกมโดยไม่มีเซฟวัน (= New Game)
//  → ฉากของวันที่ 2 ฯลฯ จึงเล่นใหม่ใน New Game รอบถัดไป โดยไม่ต้องแก้ MainMenuManager
// =====================================================================
public enum ComicTriggerType { NewGame, DayStarted, Custom }

[Serializable]
public class ComicTrigger
{
    [Tooltip("โน้ตให้ตัวเองอ่าน เช่น 'วันที่ 2 ถ้า Rating ขึ้น'")]
    public string note;
    public ComicSequenceData sequence;
    public ComicTriggerType type = ComicTriggerType.DayStarted;

    [Tooltip("DayStarted: วันที่ต้องตรง (0 = ทุกวัน)")]
    public int day = 2;
    [Tooltip("Custom: ชื่อ event ที่ระบบอื่นส่งมา")]
    public string eventKey;
    [Tooltip("เล่นครั้งเดียวต่อเซฟ (ถ้าไม่ติ๊ก = เล่นทุกครั้งที่เงื่อนไขผ่าน)")]
    public bool playOnce = true;

    [Header("เงื่อนไขเสริม (ค่า default = ไม่เช็ก)")]
    [Tooltip("Rating ปัจจุบันต้อง >= ค่านี้ | ติดลบ = ไม่เช็ก")]
    public float minRating = -1f;
    [Tooltip("Rating ต้องเพิ่มขึ้นจากการคิดคะแนนวันก่อน (ใช้ได้ตอนขึ้นวันใหม่ในรอบการเล่นเดียวกัน)")]
    public bool requireRatingIncreased = false;
    [Tooltip("ต้องเสิร์ฟได้อย่างน้อยกี่ตัวเมื่อวาน | ติดลบ = ไม่เช็ก")]
    public int minServedLastDay = -1;
}

public class ComicStoryManager : MonoBehaviour
{
    public static ComicStoryManager Instance { get; private set; }

    [Header("References")]
    [Tooltip("เว้นว่าง = หา ComicSequencePlayer ใน Scene เอง")]
    public ComicSequencePlayer player;

    [Header("Triggers (เรียงลำดับ = ลำดับที่เล่นถ้าผ่านพร้อมกัน)")]
    public List<ComicTrigger> triggers = new List<ComicTrigger>();

    [Tooltip("ฉากที่เล่นด้วยวิธีอื่น (เรียก PlayNow) แต่ต้องการให้ล้างสถานะ 'เล่นแล้ว' ตอน New Game ด้วย")]
    public List<ComicSequenceData> extraSequencesToReset = new List<ComicSequenceData>();

    [Header("Settings")]
    [Tooltip("รอกี่วินาทีหลังเข้า Scene ก่อนเริ่มเช็ก (ให้ระบบอื่นโหลดวัน/เควสเสร็จก่อน)")]
    public float sceneStartDelay = 0.8f;
    [Tooltip("Continue: ตอนเข้า Scene ให้เช็ก DayStarted ของวันปัจจุบันด้วย (กรณีออกเกมตอนฉากยังไม่ได้เล่น)")]
    public bool checkDayStartOnSceneStart = true;
    [Tooltip("ไม่เล่น Trigger ชนิดอื่นนอกจาก NewGame ระหว่าง Tutorial")]
    public bool skipTriggersDuringTutorial = true;

    private struct QueuedComic { public ComicSequenceData sequence; public bool markSeen; }

    private readonly Queue<QueuedComic> _queue = new Queue<QueuedComic>();
    private bool _running;
    private DayNightManager _dayNight;

    private const string SeenPrefix = "ComicSeen_";
    private const string DaySaveKey = "SavedGameDay"; // ต้องตรงกับ DayNightManager

    // ───────────────────────── Lifecycle ─────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        // ไม่มีเซฟวัน = New Game → ล้างสถานะ "เล่นแล้ว" ทั้งหมด (เหมือนที่ MainMenu ล้างเซฟอื่นๆ)
        if (!PlayerPrefs.HasKey(DaySaveKey)) ClearSeenFlags();
    }

    void Start()
    {
        if (player == null) player = FindObjectOfType<ComicSequencePlayer>();

        _dayNight = DayNightManager.Instance;
        if (_dayNight != null) _dayNight.OnNewDayStarted += HandleNewDayStarted;

        StartCoroutine(SceneStartRoutine());
    }

    void OnDestroy()
    {
        if (_dayNight != null) _dayNight.OnNewDayStarted -= HandleNewDayStarted;
        if (Instance == this) Instance = null;
    }

    private IEnumerator SceneStartRoutine()
    {
        yield return new WaitForSecondsRealtime(sceneStartDelay);

        if (!PlayerPrefs.HasKey(DaySaveKey)) EvaluateNewGame();
        else if (checkDayStartOnSceneStart) EvaluateDayStart();
    }

    private void HandleNewDayStarted()
    {
        EvaluateDayStart();
    }

    // ───────────────────────── Evaluate ─────────────────────────
    private void EvaluateNewGame()
    {
        foreach (ComicTrigger t in triggers)
        {
            if (t == null || t.type != ComicTriggerType.NewGame) continue;
            if (!Passes(t, false)) continue;
            Enqueue(t.sequence, false); // NewGame ไม่บันทึกสถานะ — New Game ทุกครั้งต้องเล่นใหม่
        }
    }

    private void EvaluateDayStart()
    {
        int day = _dayNight != null ? _dayNight.currentDay : 1;

        foreach (ComicTrigger t in triggers)
        {
            if (t == null || t.type != ComicTriggerType.DayStarted) continue;
            if (t.day != 0 && t.day != day) continue;
            if (!Passes(t, true)) continue;
            Enqueue(t.sequence, t.playOnce);
        }
    }

    /// <summary>ระบบอื่นเรียกเมื่อเกิดเหตุการณ์ เช่น ComicStoryManager.Instance?.RaiseEvent("first_vip");</summary>
    public void RaiseEvent(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        foreach (ComicTrigger t in triggers)
        {
            if (t == null || t.type != ComicTriggerType.Custom) continue;
            if (t.eventKey != key) continue;
            if (!Passes(t, true)) continue;
            Enqueue(t.sequence, t.playOnce);
        }
    }

    /// <summary>สั่งเล่นฉากทันที (เข้าคิวถ้ามีฉากอื่นกำลังเล่น) โดยไม่ผ่านเงื่อนไข</summary>
    public void PlayNow(ComicSequenceData sequence, bool markSeen = false)
    {
        if (sequence != null) Enqueue(sequence, markSeen);
    }

    // ───────────────────────── Conditions ─────────────────────────
    private bool Passes(ComicTrigger t, bool checkSeenAndTutorial)
    {
        if (t.sequence == null) return false;

        if (checkSeenAndTutorial)
        {
            if (skipTriggersDuringTutorial && TutorialActive()) return false;
            if (t.playOnce && HasSeen(t.sequence)) return false;
        }

        return CheckExtraConditions(t);
    }

    // ← เพิ่มเงื่อนไขใหม่ๆ ตรงนี้ (แล้วเพิ่ม field ใน ComicTrigger)
    private bool CheckExtraConditions(ComicTrigger t)
    {
        CafeRatingManager rating = CafeRatingManager.Instance;

        if (t.minRating >= 0f)
        {
            if (rating == null || rating.Rating < t.minRating) return false;
        }

        if (t.requireRatingIncreased)
        {
            if (rating == null || rating.LastResult == null || rating.LastResult.change <= 0.001f) return false;
        }

        if (t.minServedLastDay >= 0)
        {
            if (rating == null || rating.LastResult == null || rating.LastResult.served < t.minServedLastDay) return false;
        }

        return true;
    }

    private bool TutorialActive()
    {
        return MainQuestManager.Instance != null &&
               MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;
    }

    // ───────────────────────── Queue ─────────────────────────
    private void Enqueue(ComicSequenceData seq, bool markSeen)
    {
        foreach (QueuedComic q in _queue)
            if (q.sequence == seq) return; // ไม่ใส่คิวซ้ำ

        _queue.Enqueue(new QueuedComic { sequence = seq, markSeen = markSeen });
        if (!_running) StartCoroutine(RunQueue());
    }

    private IEnumerator RunQueue()
    {
        _running = true;

        while (_queue.Count > 0)
        {
            QueuedComic item = _queue.Dequeue();

            if (player == null) player = FindObjectOfType<ComicSequencePlayer>();
            if (player == null)
            {
                Debug.LogWarning("[ComicStoryManager] ไม่พบ ComicSequencePlayer ใน Scene — ข้ามฉาก");
                _queue.Clear();
                break;
            }

            while (ComicSequencePlayer.IsPlaying) yield return null; // รอฉากอื่นจบก่อน

            bool done = false;
            if (player.Play(item.sequence, () => done = true))
            {
                while (!done) yield return null;
                if (item.markSeen) MarkSeen(item.sequence);
            }
        }

        _running = false;
    }

    // ───────────────────────── Seen flags ─────────────────────────
    public static bool HasSeen(ComicSequenceData seq)
    {
        return seq != null && PlayerPrefs.GetInt(SeenPrefix + seq.Id, 0) == 1;
    }

    public static void MarkSeen(ComicSequenceData seq)
    {
        if (seq == null) return;
        PlayerPrefs.SetInt(SeenPrefix + seq.Id, 1);
        PlayerPrefs.Save();
    }

    [ContextMenu("Clear Seen Flags (ทดสอบเล่นซ้ำ)")]
    public void ClearSeenFlags()
    {
        foreach (ComicTrigger t in triggers)
            if (t != null && t.sequence != null) PlayerPrefs.DeleteKey(SeenPrefix + t.sequence.Id);

        foreach (ComicSequenceData s in extraSequencesToReset)
            if (s != null) PlayerPrefs.DeleteKey(SeenPrefix + s.Id);

        PlayerPrefs.Save();
    }
}
