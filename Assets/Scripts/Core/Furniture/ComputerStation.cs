using UnityEngine;

public class ComputerStation : MonoBehaviour
{
    [Header("Shop UI Panel")]
    public GameObject shopCanvas;

    void Start()
    {
        if (shopCanvas != null)
        {
            shopCanvas.SetActive(false);
        }
    }

    public void OpenComputerShop()
    {
        if (shopCanvas != null)
        {
            shopCanvas.SetActive(true);
            PlayerController2D.SetLock(true); // ล็อก Player ไม่ให้เดิน
            CameraController2D.SetPanMode(true); // ✅ สั่งเปิดโหมดคลิกขวาเลื่อนกล้อง
        }
    }

    public void CloseComputerShop()
    {
        if (shopCanvas != null)
        {
            shopCanvas.SetActive(false);
            PlayerController2D.SetLock(false); // ปลดล็อก Player
            CameraController2D.SetPanMode(false); // ✅ สั่งกลับสู่โหมดกล้องตาม Player
        }
    }
}