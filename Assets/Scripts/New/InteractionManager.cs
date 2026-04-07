using UnityEngine;

public enum InteractionMode
{
    None,
    Pet,
    Feed,
    Play
}

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance;

    public InteractionMode currentMode = InteractionMode.None;

    void Awake()
    {
        Instance = this;
    }

    public void SetMode(InteractionMode mode)
    {
        currentMode = mode;
        Debug.Log("Mode: " + mode);
    }
}