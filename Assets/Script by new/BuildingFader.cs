using System.Collections.Generic;
using UnityEngine;

public class BuildingFader : MonoBehaviour
{
    [Header("Walls or roofs to fade")]
    public List<SpriteRenderer> wallsToFade = new List<SpriteRenderer>();

    [Header("Transparency level (0 = fully invisible, 0.2 = faint)")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0f;

    [Header("Fade transition speed")]
    public float fadeSpeed = 5f;

    private float targetAlpha = 1f;

    void Update()
    {
        // Smoothly adjust alpha for all walls in the list
        foreach (var wall in wallsToFade)
        {
            if (wall != null)
            {
                Color c = wall.color;
                c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
                wall.color = c;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Trigger entered: " + collision.gameObject.name + " | Tag: " + collision.tag);

        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player detected: Fading walls out");
            targetAlpha = transparentAlpha;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player left: Restoring wall opacity");
            targetAlpha = 1f;
        }
    }
}