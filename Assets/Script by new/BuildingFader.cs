using System.Collections.Generic;
using UnityEngine;

public class BuildingFader : MonoBehaviour
{
    [Header("ใส่รูปกำแพง/หลังคาที่ต้องการให้จาง (กี่ชิ้นก็ได้)")]
    public List<SpriteRenderer> wallsToFade = new List<SpriteRenderer>();

    [Header("ระดับความจาง (0 = หายไปเลย, 0.2 = โปร่งใสลางๆ)")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0f;

    [Header("ความเร็วในการจาง")]
    public float fadeSpeed = 5f;

    private float targetAlpha = 1f;

    void Update()
    {
        // ค่อยๆ ปรับค่าความโปร่งใส (Alpha) ของกำแพงทุกชิ้นในลิสต์
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
        // แจ้งเตือนเมื่อมีอะไรเดินเข้ามาในโซนตรวจจับ
        Debug.Log("Trigger แตะโดน: " + collision.gameObject.name + " | Tag: " + collision.tag);

        if (collision.CompareTag("Player"))
        {
            Debug.Log(">>> ตรวจพบ Player: กำแพงเริ่มจางหาย <<<");
            targetAlpha = transparentAlpha;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log(">>> Player ออกจากห้อง: กำแพงกลับมาทึบแสง <<<");
            targetAlpha = 1f;
        }
    }
}