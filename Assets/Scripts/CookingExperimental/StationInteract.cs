using UnityEngine;

public class StationInteract : MonoBehaviour
{
    [Header("UI Canvas")]
    public GameObject stationCanvas;

    [Header("Station Manager (ผูก CookingManager หรือ DrinkStation)")]
    public CookingManager cookingManager;
    public DrinkStation drinkStation;

    public void OpenCanvas()
    {
        // ✅ ถ้าผูก CookingManager หรือ DrinkStation ไว้ ให้เรียกผ่านตัวนั้น
        // เพราะ OpenCanvas() ของพวกนั้นมี PauseGame() อยู่แล้ว
        if (cookingManager != null)
        {
            cookingManager.OpenCanvas();
            return;
        }

        if (drinkStation != null)
        {
            drinkStation.OpenCanvas();
            return;
        }

        // Fallback: ถ้าไม่ได้ผูกอะไรไว้ ก็เปิด Canvas ตรงๆ พร้อม Pause
        if (stationCanvas != null)
        {
            stationCanvas.SetActive(true);
            DayNightManager.Instance?.PauseGame();
            PlayerController2D.IsLocked = true;
        }
    }
}