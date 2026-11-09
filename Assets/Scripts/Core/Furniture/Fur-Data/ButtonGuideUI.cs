using UnityEngine;
using System.Collections;

public class ButtonGuideUI : MonoBehaviour
{
    public static ButtonGuideUI Instance;

    [Header("UI Panel & RectTransform")]
    public GameObject guidePanel;          // Panel หลักของปุ่มแนะนำ
    public RectTransform panelRect;        // RectTransform ของ Panel (ควรตั้ง Pivot เป็น Top-Center หรือ Top)

    [Header("Y Position Settings (Top Center)")]
    public float shownPosY = -30f;         // ตำแหน่ง Y ตอนโผล่มาแสดง (อยู่ด้านบนจอ)
    public float hiddenPosY = 100f;        // ตำแหน่ง Y ตอนซ่อน (เลื่อนขึ้นไปซ่อนพ้นขอบจอด้านบน)
    public float moveSpeed = 12f;          // ความเร็วในการเลื่อน Smooth

    private float targetPosY;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (panelRect == null && guidePanel != null)
        {
            panelRect = guidePanel.GetComponent<RectTransform>();
        }

        // เริ่มต้นด้วยการซ่อนไว้ด้านบน
        targetPosY = hiddenPosY;
        if (panelRect != null)
        {
            Vector2 pos = panelRect.anchoredPosition;
            pos.y = hiddenPosY;
            panelRect.anchoredPosition = pos;
        }

        if (guidePanel != null) guidePanel.SetActive(false);
    }

    private void Update()
    {
        if (panelRect != null && guidePanel.activeSelf)
        {
            Vector2 currentPos = panelRect.anchoredPosition;
            if (Mathf.Abs(currentPos.y - targetPosY) > 0.01f)
            {
                currentPos.y = Mathf.Lerp(currentPos.y, targetPosY, Time.deltaTime * moveSpeed);
                panelRect.anchoredPosition = currentPos;
            }
            else if (targetPosY == hiddenPosY && Mathf.Abs(currentPos.y - hiddenPosY) <= 0.01f)
            {
                // ปิด GameObject เมื่อสไลด์ออกจนสุดแล้ว เพื่อประหยัดการประมวลผล
                guidePanel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// สั่งแสดงปุ่มแนะนำ (Slide In) ลงมาที่หน้าจอ
    /// </summary>
    public void ShowGuide(bool show)
    {
        if (guidePanel == null) return;

        if (show)
        {
            guidePanel.SetActive(true);
            targetPosY = shownPosY; // สไลด์ลงมาแสดง
        }
        else
        {
            targetPosY = hiddenPosY; // สไลด์ขึ้นไปซ่อน
            // ตัว Update จะจัดการปิด GameObject ให้เองหลังจากสไลด์ขึ้นไปสุดแล้ว
        }
    }
}