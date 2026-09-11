using System.Collections.Generic;
using UnityEngine;

public class BuildingFader : MonoBehaviour
{
    [Header("กดเครื่องหมาย + เพื่อเพิ่มช่อง แล้วลากกำแพงมาใส่ทีละชิ้นได้เลย")]
    public List<SpriteRenderer> wallsToFade = new List<SpriteRenderer>();

    [Header("ระดับความจาง (0 = หายไปเลย, 0.2 = โปร่งใสลางๆ)")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0f;

    [Header("ความเร็วในการจาง")]
    public float fadeSpeed = 5f;

    private float targetAlpha = 1f;

    void Update()
    {
        // วนลูปปรับความโปร่งใสของกำแพงทุกชิ้นในลิสต์
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
        if (collision.CompareTag("Player"))
        {
            targetAlpha = transparentAlpha;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            targetAlpha = 1f;
        }
    }

}