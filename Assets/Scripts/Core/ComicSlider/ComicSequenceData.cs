using System;
using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// ComicSequenceData — ข้อมูลฉากเนื้อเรื่องแบบ Comic Slide 1 ชุด (ScriptableObject)
// สร้างโดย: คลิกขวาใน Project → Create → Comic → Comic Sequence
// 1 ชุด = หลายแผง (Panel) ที่สไลด์ไปทางขวาเรื่อยๆ ทีละแผง
// =====================================================================
[Serializable]
public class ComicPanel
{
    [Tooltip("รูปของแผงนี้")]
    public Sprite image;

    [Tooltip("ข้อความใต้ภาพ (เว้นว่างได้)")]
    [TextArea(2, 5)] public string caption;

    [Tooltip("true = รอผู้เล่นกดไปแผงต่อไป | false = ไปเองหลัง Auto Advance Seconds (ผู้เล่นกดข้ามได้ก่อน)")]
    public bool waitForInput = true;

    [Tooltip("ใช้เมื่อ Wait For Input = false")]
    public float autoAdvanceSeconds = 3f;

    [Tooltip("ระยะเวลาสไลด์เข้ามาที่แผงนี้ (วินาที) | -1 = ใช้ค่า Default ของ Sequence")]
    public float slideDuration = -1f;

    [Tooltip("ภาพค่อยๆ ลอยไปทางขวาช้าๆ ระหว่างอ่าน (พิกเซล/วินาที) | -1 = ใช้ค่า Default | 0 = ไม่ขยับ")]
    public float driftPixelsPerSecond = -1f;

    [Tooltip("เสียงที่เล่นตอนแผงนี้ขึ้น (เว้นว่างได้)")]
    public AudioClip sfx;
}

[CreateAssetMenu(fileName = "NewComicSequence", menuName = "Comic/Comic Sequence")]
public class ComicSequenceData : ScriptableObject
{
    [Tooltip("ID ไม่ซ้ำกัน ใช้บันทึกว่าเล่นไปแล้ว เช่น intro, day2_rating_up | เว้นว่าง = ใช้ชื่อไฟล์")]
    public string sequenceId;

    public List<ComicPanel> panels = new List<ComicPanel>();

    [Header("Default Timing")]
    [Tooltip("ระยะเวลาสไลด์ระหว่างแผง (วินาที)")]
    public float defaultSlideDuration = 0.7f;
    [Tooltip("ความเร็วภาพลอยไปทางขวาระหว่างอ่าน (พิกเซล/วินาที)")]
    public float defaultDriftPixelsPerSecond = 12f;
    [Tooltip("ลอยได้ไกลสุดกี่พิกเซลต่อแผง (ค่อยๆ ช้าลงจนหยุด)")]
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
