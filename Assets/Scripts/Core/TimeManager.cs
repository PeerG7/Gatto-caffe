using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    //public static class TimeHelper
    //{
    //    public static float DeltaTime
    //    {
    //        get
    //        {
    //            return TimeManager.Instance.isPaused ? 0f : Time.deltaTime;
    //        }
    //    }
    //}

    public bool isPaused = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void PauseGame()
    {
        isPaused = true;
        Debug.Log("⏸ Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Debug.Log("▶ Game Resumed");
    }
}