using UnityEngine;

public class StationInteract : MonoBehaviour
{
    [Header("UI Canvas")]
    public GameObject stationCanvas; // ลาก Canvas ทำอาหาร หรือ Canvas ตู้กดน้ำ มาใส่ที่นี่

    public void OpenCanvas()
    {
        if (stationCanvas != null)
        {
            stationCanvas.SetActive(true);

            // (เพิ่มเติม) คุณอาจจะใส่โค้ดเพื่อหยุดการเดินของผู้เล่นตรงนี้ 
            // เพื่อไม่ให้ผู้เล่นเดินทะลุจอขณะทำอาหาร
        }
    }
}