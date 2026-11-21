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
// หน้า Cooking / Drink / สมุด / ร้านคอม / QTE / วางเฟอร์นิเจอร์ ไม่ต้องแปะ (ตรวจเองอยู่แล้ว)
// =====================================================================
[DisallowMultipleComponent]
public class UIContextTag : MonoBehaviour
{
    public static int Count { get; private set; }

    private bool counted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Count = 0; }

    void OnEnable()
    {
        if (counted) return;
        counted = true;
        Count++;
    }

    void OnDisable()
    {
        if (!counted) return;
        counted = false;
        Count--;
    }
}
