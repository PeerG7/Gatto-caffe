using System.Collections.Generic;
using UnityEngine;

public class BuildingFader : MonoBehaviour
{
    [Header("飜霤ﾙｻ｡ﾓ眄ｧ/ﾋﾅﾑｧ､ﾒｷﾕ襍鯱ｧ｡ﾒﾃ耆鬨ﾒｧ (｡ﾕ隱ﾔ鮖｡鈕ｴ・")]
    public List<SpriteRenderer> wallsToFade = new List<SpriteRenderer>();

    [Header("ﾃﾐｴﾑｺ､ﾇﾒﾁｨﾒｧ (0 = ﾋﾒﾂ莉倏ﾂ, 0.2 = 篏ﾃ隗飜ﾅﾒｧ・")]
    [Range(0f, 1f)]
    public float transparentAlpha = 0f;

    [Header("､ﾇﾒﾁ狹酩羯｡ﾒﾃｨﾒｧ")]
    public float fadeSpeed = 5f;

    private float targetAlpha = 1f;

    void Update()
    {
        // ､靉ﾂ・ｻﾃﾑｺ､靨､ﾇﾒﾁ篏ﾃ隗飜 (Alpha) ｢ﾍｧ｡ﾓ眄ｧｷﾘ｡ｪﾔ鮖羯ﾅﾔﾊｵ・
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
        // 皖鬧犒ﾗﾍｹ狠ﾗ靉ﾁﾕﾍﾐ菘犇ﾔｹ爐鰓ﾁﾒ羯筬ｹｵﾃﾇｨｨﾑｺ
        Debug.Log("Trigger 盞ﾐ箒ｹ: " + collision.gameObject.name + " | Tag: " + collision.tag);

        if (collision.CompareTag("Player"))
        {
            Debug.Log(">>> ｵﾃﾇｨｾｺ Player: ｡ﾓ眄ｧ狹ﾔ霖ｨﾒｧﾋﾒﾂ <<<");
            targetAlpha = transparentAlpha;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log(">>> Player ﾍﾍ｡ｨﾒ｡ﾋ鯱ｧ: ｡ﾓ眄ｧ｡ﾅﾑｺﾁﾒｷﾖｺ睫ｧ <<<");
            targetAlpha = 1f;
        }
    }
}