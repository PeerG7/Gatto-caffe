using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// ComicSequenceData — ข้อมูลฉากเนื้อเรื่องแบบ Comic 1 ชุด (ScriptableObject)
// สร้างโดย: คลิกขวาใน Project → Create → Comic → Comic Sequence
//
// มี 2 รูปแบบ (Layout)
//   • SlideStrip : 1 แผง = 1 รูป สไลด์ไปทางขวาทีละแผง
//   • PageFocus  : ใช้รูป "ทั้งหน้ากระดาษ" รูปเดียว แล้วกล้องซูม/เลื่อนไปทีละช่อง
//                  (1 Panel = 1 จุดที่กล้องไปหยุด — ระบุตำแหน่งด้วย Focus Rect)
// =====================================================================
public enum ComicLayout { SlideStrip, PageFocus }

[Serializable]
public class ComicPanel
{
    [Header("SlideStrip: รูปของแผง")]
    [Tooltip("(SlideStrip เท่านั้น) รูปของแผงนี้")]
    public Sprite image;

    [Header("PageFocus: จุดที่กล้องซูมไป")]
    [Tooltip("(PageFocus เท่านั้น) พื้นที่ที่จะซูมไป เป็นสัดส่วน 0–1 ของรูปทั้งหน้า\n" +
             "X, Y = มุมซ้ายบนของพื้นที่ (0,0 = มุมซ้ายบนของรูป)  |  W, H = กว้าง/สูง\n" +
             "เช่น ครึ่งซ้ายบน = X 0, Y 0, W 0.5, H 0.5\n" +
             "ซูม 2 ช่องพร้อมกัน = ใส่พื้นที่ครอบทั้ง 2 ช่อง")]
    public Rect focusRect = new Rect(0f, 0f, 1f, 1f);

    [Header("ทั้งสองแบบ")]
    [Tooltip("ข้อความใต้ภาพ (เว้นว่างได้)")]
    [TextArea(2, 5)] public string caption;

    [Tooltip("true = รอผู้เล่นกดไปต่อ | false = ไปเองหลัง Auto Advance Seconds (ผู้เล่นกดข้ามได้ก่อน)")]
    public bool waitForInput = true;

    [Tooltip("ใช้เมื่อ Wait For Input = false")]
    public float autoAdvanceSeconds = 3f;

    [Tooltip("ระยะเวลาเคลื่อนมาที่แผง/จุดนี้ (วินาที) | -1 = ใช้ค่า Default ของ Sequence")]
    public float slideDuration = -1f;

    [Tooltip("(SlideStrip) ภาพลอยไปทางขวาช้าๆ ระหว่างอ่าน (พิกเซล/วินาที) | -1 = ใช้ค่า Default | 0 = ไม่ขยับ")]
    public float driftPixelsPerSecond = -1f;

    [Tooltip("เสียงที่เล่นตอนขึ้นแผง/จุดนี้ (เว้นว่างได้)")]
    public AudioClip sfx;
}

[CreateAssetMenu(fileName = "NewComicSequence", menuName = "Comic/Comic Sequence")]
public class ComicSequenceData : ScriptableObject
{
    [Tooltip("ID ไม่ซ้ำกัน ใช้บันทึกว่าเล่นไปแล้ว เช่น intro, day2_rating_up | เว้นว่าง = ใช้ชื่อไฟล์")]
    public string sequenceId;

    public ComicLayout layout = ComicLayout.SlideStrip;

    [Tooltip("SlideStrip = รูปต่อแผง | PageFocus = 1 Panel ต่อ 1 จุดที่กล้องซูมไป (เรียงลำดับที่จะไป)")]
    public List<ComicPanel> panels = new List<ComicPanel>();

    // ───────────── PageFocus ─────────────
    [Header("PageFocus (ใช้เมื่อ Layout = PageFocus)")]
    [Tooltip("รูปทั้งหน้ากระดาษ")]
    public Sprite pageImage;
    [Tooltip("เริ่มด้วยการโชว์ทั้งหน้าก่อนกี่วินาที แล้วค่อยซูมเข้าช่องแรก (0 = ซูมเข้าเลย)")]
    public float overviewHoldSeconds = 1.2f;
    [Tooltip("จบด้วยการซูมออกมาโชว์ทั้งหน้าอีกครั้ง")]
    public bool endWithOverview = false;
    [Tooltip("1 = ซูมให้พื้นที่พอดีจอ | น้อยกว่า 1 = เผื่อขอบรอบๆ (เช่น 0.95)")]
    [Range(0.5f, 1f)] public float focusPadding = 0.96f;
    [Tooltip("ไม่ให้กล้องเลื่อนออกนอกขอบรูป (ไม่เห็นพื้นดำ)")]
    public bool clampToPage = true;
    [Tooltip("ระหว่างอ่านแต่ละจุด กล้องค่อยๆ ซูมเข้าเพิ่มเล็กน้อย (0.03 = 3%) | 0 = นิ่ง")]
    [Range(0f, 0.2f)] public float holdPushIn = 0.03f;

    // ───────────── Default Timing ─────────────
    [Header("Default Timing")]
    [Tooltip("ระยะเวลาสไลด์/เลื่อนกล้องระหว่างแผง (วินาที) — PageFocus แนะนำ 0.9–1.2")]
    public float defaultSlideDuration = 0.7f;
    [Tooltip("(SlideStrip) ความเร็วภาพลอยไปทางขวาระหว่างอ่าน (พิกเซล/วินาที)")]
    public float defaultDriftPixelsPerSecond = 12f;
    [Tooltip("(SlideStrip) ลอยได้ไกลสุดกี่พิกเซลต่อแผง (ค่อยๆ ช้าลงจนหยุด)")]
    public float maxDriftPixels = 60f;

    [Header("Behaviour")]
    [Tooltip("ผู้เล่นกดข้ามทั้งชุดได้ (ปุ่มกำหนดที่ ComicSequencePlayer)")]
    public bool allowSkip = true;
    [Tooltip("หยุดเวลาในเกมระหว่างเล่น (DayNightManager.PauseGame)")]
    public bool pauseGameTime = true;
    [Tooltip("ล็อกผู้เล่นระหว่างเล่น")]
    public bool lockPlayer = true;

    public string Id => string.IsNullOrEmpty(sequenceId) ? name : sequenceId;
}