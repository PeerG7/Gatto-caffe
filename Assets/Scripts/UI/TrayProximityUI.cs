using UnityEngine;

public class TrayProximityUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก TraySlot ที่อยู่คู่กันมาใส่ตรงนี้")]
    public TraySlot traySlot;

    [Header("Proximity Icon")]
    [Tooltip("ลาก GameObject ของ Icon อาหาร (หรือปุ่ม E) มาใส่ตรงนี้")]
    public GameObject proximityIcon;

    private void Start()
    {
        // ถ้าไม่ได้ลาก TraySlot มา ให้ลองหาจาก GameObject เดียวกัน
        if (traySlot == null)
        {
            traySlot = GetComponent<TraySlot>();
        }

        // อัปเดตสถานะเริ่มต้นตอนเปิดเกม
        UpdateUIVisible();
    }

    private void Update()
    {
        // เช็คตลอดเวลาว่าถาดมีอาหารหรือไม่
        UpdateUIVisible();
    }

    private void UpdateUIVisible()
    {
        if (proximityIcon != null && traySlot != null)
        {
            // 🌟 แสดงไอคอนก็ต่อเมื่อ "มีอาหารอยู่บนถาด" เท่านั้น (ไม่สนว่าผู้เล่นจะอยู่ใกล้หรือไกล)
            bool shouldShow = !traySlot.IsEmpty();
            proximityIcon.SetActive(shouldShow);
        }
    }
}