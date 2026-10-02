using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls 2D lights based on in-game time.
/// Syncs with DayNightManager automatically with smooth fade transitions.
/// </summary>
public class TimeBasedLight : MonoBehaviour
{
    [Header("Target Lights")]
    [Tooltip("List of Light 2D components to turn on during the night.")]
    public List<Light2D> lights = new List<Light2D>();

    [Header("Schedule (24-Hour Clock)")]
    [Tooltip("Hour when lights begin turning on (e.g., 17 = 5 PM).")]
    public float turnOnHour = 17f;

    [Tooltip("Hour when lights begin turning off (e.g., 6 = 6 AM).")]
    public float turnOffHour = 6f;

    [Header("Fade Settings")]
    [Tooltip("Fade duration for turning on or off (seconds).")]
    public float fadeDuration = 2.5f;

    private List<float> maxIntensities = new List<float>();

    void Start()
    {
        // Cache original intensities configured in the Inspector
        foreach (var l in lights)
        {
            if (l != null)
            {
                maxIntensities.Add(l.intensity);
                // Start with lights completely off to allow smooth fade-in
                l.intensity = 0f;
            }
            else
            {
                maxIntensities.Add(0f);
            }
        }
    }

    void Update()
    {
        if (DayNightManager.Instance == null) return;

        // Retrieve current in-game hour
        float currentHour = DayNightManager.Instance.GetCurrentGameHour24();

        // Check if the current time falls within operating hours
        bool shouldTurnOn = IsNightTime(currentHour);

        // Smoothly adjust intensity for each assigned light
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] == null) continue;

            float targetIntensity = shouldTurnOn ? maxIntensities[i] : 0f;

            // Calculate fade speed so all lights finish fading within fadeDuration
            float stepSpeed = (fadeDuration > 0f) ? (maxIntensities[i] / fadeDuration) : 999f;

            lights[i].intensity = Mathf.MoveTowards(
                lights[i].intensity,
                targetIntensity,
                stepSpeed * Time.deltaTime
            );
        }
    }

    private bool IsNightTime(float hour)
    {
        // Spans across midnight (e.g., 17:00 to 06:00)
        if (turnOnHour > turnOffHour)
        {
            return hour >= turnOnHour || hour < turnOffHour;
        }
        else
        {
            return hour >= turnOnHour && hour < turnOffHour;
        }
    }
}