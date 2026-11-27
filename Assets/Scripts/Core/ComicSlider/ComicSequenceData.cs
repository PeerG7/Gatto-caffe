using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// ComicSequenceData — ข้อมูลฉากเนื้อเรื่องแบบ Comic 1 ชุด (ScriptableObject)
// สร้างโดย: คลิกขวาใน Project → Create → Comic → Comic Sequence
//
// มี 3 รูปแบบ (Layout)
//   • SlideStrip : 1 แผง = 1 รูป สไลด์ไปทางขวาทีละแผง
//   • PageFocus  : ใช้รูป "ทั้งหน้ากระดาษ" รูปเดียว แล้วกล้องซูม/เลื่อนไปทีละช่อง
//                  (1 Panel = 1 จุดที่กล้องไปหยุด — ระบุตำแหน่งด้วย Focus Rect)
//   • PanelReveal: ใช้รูป "ทั้งหน้ากระดาษ" รูปเดียว กล้องนิ่ง แล้วแต่ละช่อง "ค่อยๆ ขึ้นมา" ตามลำดับ
//                  (1 Panel = 1 ช่องที่จะโผล่ — ระบุตำแหน่งช่องด้วย Focus Rect เหมือน PageFocus)
// =====================================================================

public enum ComicLayout { SlideStrip, PageFocus, PanelReveal }

// วิธีที่ช่องจะโผล่ขึ้นมา (ใช้กับ Layout = PanelReveal)
public enum ComicRevealStyle { Fade, SlideFromRight }

[Serializable]
public class ComicPanel
{
    [Header("SlideStrip: รูปของแผง")]
    [Tooltip("(SlideStrip เท่านั้น) รูปของแผงนี้")]
    public Sprite image;

    [Header("PageFocus / PanelReveal: พื้นที่ของช่อง")]
    [Tooltip("(PageFocus / PanelReveal) PageFocus = พื้นที่ที่จะซูมไป | PanelReveal = พื้นที่ของช่องที่จะค่อยๆ โผล่ขึ้นมา (ควรครอบเส้นขอบช่องด้วย)\n" +
             "เป็นสัดส่วน 0–1 ของรูปทั้งหน้า\n" +
             "X, Y = มุมซ้ายบนของพื้นที่ (0,0 = มุมซ้ายบนของรูป)  |  W, H = กว้าง/สูง\n" +
             "เช่น ครึ่งซ้ายบน = X 0, Y 0, W 0.5, H 0.5\n" +
             "ซูม 2 ช่องพร้อมกัน = ใส่พื้นที่ครอบทั้ง 2 ช่อง")]
    public Rect focusRect = new Rect(0f, 0f, 1f, 1f);

    [Tooltip("(PanelReveal เท่านั้น) วิธีที่ช่องนี้โผล่ขึ้นมา\n" +
             "Fade = ค่อยๆ จางขึ้นมา | SlideFromRight = ช่องสไลด์เข้ามาจากขอบจอด้านขวา (ระหว่างนั้นตำแหน่งช่องเป็นหน้าขาว)")]
    public ComicRevealStyle revealStyle = ComicRevealStyle.Fade;

    [Tooltip("(PanelReveal เท่านั้น) ติ๊ก = ช่องนี้ \"อยู่บนหน้าตั้งแต่แรก\" ไม่มีตัวบัง ไม่ค่อยๆ ขึ้น (แต่ยังมีแคปชัน/เสียง/รอกดไปต่อตามปกติ)\n" +
             "ใช้กับช่องแรกที่อยากให้เห็นทันที แล้วช่องถัดไปค่อยๆ เข้าฉากมา")]
    public bool visibleFromStart = false;

    [Header("PanelReveal: ซูมเข้าช่องนี้ (ไม่บังคับ)")]
    [Tooltip("(PanelReveal เท่านั้น) ติ๊ก = พอช่องนี้โผล่แล้ว กล้องซูมเข้าไปที่ Focus Rect ของช่อง")]
    public bool zoomIn = false;

    [Tooltip("(PanelReveal เท่านั้น) เวลาที่ใช้ซูมเข้า/ซูมออก (วินาที) | 0 = 1 วินาที")]
    public float zoomDuration = 1f;

    [Tooltip("(PanelReveal เท่านั้น) ติ๊ก = ผู้เล่นกดไปต่อแล้วกล้องซูมออกมาเห็นทั้งหน้าก่อนช่องถัดไปโผล่ (ถ้ามีช่องต่อไป ควรติ๊กไว้)")]
    public bool zoomOutAfter = true;

    [Header("ทั้งสองแบบ")]
    [Tooltip("ข้อความใต้ภาพ (เว้นว่างได้)")]
    [TextArea(2, 5)] public string caption;

    [Tooltip("ติ๊ก = รอผู้เล่นกดไปต่อ | ไม่ติ๊ก = ไปเองหลัง Auto Advance Seconds (ถ้า Auto Advance Seconds เป็น 0 จะถือว่ารอผู้เล่นกดเหมือนกัน)")]
    public bool waitForInput = true;

    [Tooltip("วินาทีที่รอก่อนไปต่อเอง — ใช้เมื่อไม่ติ๊ก Wait For Input (ผู้เล่นกดข้ามได้ก่อน)")]
    public float autoAdvanceSeconds = 3f;

    [Tooltip("ระยะเวลาเคลื่อนมาที่แผง/จุดนี้ (วินาที) — PanelReveal = เวลาที่ช่องนี้ค่อยๆ ขึ้นมา | 0 = ใช้ Default Slide Duration ของ Sequence")]
    public float slideDuration = 0f;

    [Tooltip("(SlideStrip) ภาพลอยไปทางขวาช้าๆ ระหว่างอ่าน (พิกเซล/วินาที) | 0 = ใช้ค่า Default ของ Sequence (ปิดลอยทั้งชุดที่ Max Drift Pixels = 0)")]
    public float driftPixelsPerSecond = 0f;

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

    // ───────────── PanelReveal ─────────────
    [Header("PanelReveal (ใช้เมื่อ Layout = PanelReveal — ใช้ Page Image ตัวเดียวกับ PageFocus)")]
    [Tooltip("รอกี่วินาทีก่อนเริ่มลำดับแรก (โชว์หน้าก่อน — ช่องที่ติ๊ก Visible From Start จะเห็นอยู่แล้ว ส่วนช่องที่เหลือยังถูกบังไว้) | 0 = เริ่มเลย")]
    public float revealStartDelay = 0.5f;
    [Tooltip("สีที่ใช้บังช่องที่ยังไม่ขึ้น — ควรตรงกับสีพื้นของหน้ากระดาษ (ขาว)")]
    public Color revealMaskColor = Color.white;

    // ───────────── Default Timing ─────────────
    [Header("Default Timing")]
    [Tooltip("ระยะเวลาสไลด์/เลื่อนกล้องระหว่างแผง (วินาที) — PageFocus แนะนำ 0.9–1.2 | PanelReveal = เวลาที่แต่ละช่องค่อยๆ ขึ้น แนะนำ 0.8–1.5")]
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