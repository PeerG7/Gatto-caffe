using UnityEngine;

public class ComputerStation : MonoBehaviour
{
    [Header("Shop UI Panel")]
    public GameObject shopCanvas;

    public static bool IsShopOpen { get; private set; }

    void Start()
    {
        if (shopCanvas != null)
        {
            shopCanvas.SetActive(false);
        }
        IsShopOpen = false;
    }

    public void OpenComputerShop()
    {
        if (FurnitureManager.Instance != null && FurnitureManager.Instance.isEditMode) return;

        if (shopCanvas != null)
        {
            // ถ้าเปิดอยู่แล้ว ไม่เล่นเสียงซ้ำ
            if (!IsShopOpen)
                AudioManager.instance?.PlayComputerOpen();

            shopCanvas.SetActive(true);
            IsShopOpen = true;
            PlayerController2D.SetLock(true); // ล็อก Player ไม่ให้เดิน
            CameraController2D.SetPanMode(true); // เปิดโหมดกล้อง Shop
        }
    }

    public void CloseComputerShop()
    {
        if (shopCanvas != null)
        {
            shopCanvas.SetActive(false);
            IsShopOpen = false;
            PlayerController2D.SetLock(false); // ✅ ปลดล็อก Player ให้กลับมาเดินได้
            CameraController2D.SetPanMode(false); // ✅ ปรับกล้องกลับติดตาม Player
        }
    }
}