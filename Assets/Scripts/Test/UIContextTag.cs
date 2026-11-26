using System.Collections.Generic;
using System.Text;
using UnityEngine;

// =====================================================================
// UIContextTag — แปะบน "Canvas ราก" ของหน้า UI ที่เปิด/ปิดด้วย Unity Event (OnClick → SetActive)
// หรือหน้าที่ไม่ได้ล็อกตัวละคร เช่น เมนู Pause, หน้า Tutorial, หน้าร้านค้า
//
// ตอน GameObject นี้ "เปิดอยู่" เกมจะถือว่าอยู่ในหน้า UI:
//   • ปุ่ม A = คลิกซ้ายตรงตำแหน่งเคอร์เซอร์ (แทนการเป็นปุ่ม E)
//   • เคอร์เซอร์จอยโผล่ขึ้นมาให้เอง
// ปิดเมื่อไหร่ ปุ่ม A ก็กลับไปเป็นปุ่มโต้ตอบ (E) ตามเดิม
//
// เวอร์ชันนี้เก็บเป็น "รายชื่อ tag ที่เปิดอยู่" แทนตัวนับ:
//   • Count ไม่เพี้ยน (ตัวที่ถูกทำลาย/ปิดจะหลุดจากรายชื่ออัตโนมัติ)
//   • Describe() บอกชื่อ GameObject ที่ทำให้ค้าง → หาตัวการได้ทันทีจาก Console
// =====================================================================
[DisallowMultipleComponent]
public class UIContextTag : MonoBehaviour
{
    static readonly HashSet<UIContextTag> active = new HashSet<UIContextTag>();

    public static int Count
    {
        get
        {
            active.RemoveWhere(t => t == null || !t.isActiveAndEnabled);   // เก็บกวาดตัวที่ตายแล้ว/ปิดแล้ว
            return active.Count;
        }
    }

    /// <summary>ชื่อ GameObject (พร้อม Scene) ของ tag ที่กำลังเปิดอยู่ — ใช้ debug</summary>
    public static string Describe()
    {
        var sb = new StringBuilder();
        foreach (var t in active)
        {
            if (t == null || !t.isActiveAndEnabled) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(t.gameObject.scene.name).Append('/').Append(t.name);
        }
        return sb.ToString();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { active.Clear(); }

    void OnEnable() { active.Add(this); }

    void OnDisable() { active.Remove(this); }

    void OnDestroy() { active.Remove(this); }
}