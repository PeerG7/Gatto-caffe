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
        // 🟢 เปิดสถานีทันทีโดยไม่ต้องเช็คที่นี่ 
        // (เพราะการสุ่มคราบเลอะจะถูกจัดการต่อภายใน CookingManager.OpenCanvas แทนแล้ว)
        ExecuteOpenCanvas();
    }

    private void ExecuteOpenCanvas()
    {
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

        if (stationCanvas != null)
        {
            stationCanvas.SetActive(true);
            DayNightManager.Instance?.PauseGame();
            PlayerController2D.IsLocked = true;
        }
    }
}