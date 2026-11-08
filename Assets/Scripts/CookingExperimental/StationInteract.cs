using UnityEngine;

public class StationInteract : MonoBehaviour
{
    [Header("UI Canvas")]
    public GameObject stationCanvas;

    [Header("Proximity Icons")]
    [Tooltip("Drag the Cooking Icon child object here")]
    public GameObject cookingIcon;

    [Tooltip("Drag the Beverage Icon child object here")]
    public GameObject drinkIcon;

    [Header("Station Manager (ผูก CookingManager หรือ DrinkStation)")]
    public CookingManager cookingManager;
    public DrinkStation drinkStation;

    private void Start()
    {
        // Hide both icons when the game starts
        HideAllIcons();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ShowActiveIcon();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HideAllIcons();
        }
    }

    public void OpenCanvas()
    {
        // Hide icons when the player interacts and opens the UI
        HideAllIcons();
        ExecuteOpenCanvas();
    }

    private void ShowActiveIcon()
    {
        // Display only the icon that matches the assigned manager
        if (cookingManager != null && cookingIcon != null)
        {
            cookingIcon.SetActive(true);
        }
        else if (drinkStation != null && drinkIcon != null)
        {
            drinkIcon.SetActive(true);
        }
    }

    private void HideAllIcons()
    {
        if (cookingIcon != null) cookingIcon.SetActive(false);
        if (drinkIcon != null) drinkIcon.SetActive(false);
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