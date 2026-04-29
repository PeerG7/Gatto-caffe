using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; // 🔥 เพิ่มเพื่อใช้งาน Image

public class UISummaryController : MonoBehaviour
{
    [Header("UI Components")]
    public CanvasGroup canvasGroup;
    public Image blackOverlay; // 🔥 ลาก Image สีดำที่สร้างขึ้นมาใส่ในช่องนี้
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI servedText;
    public TextMeshProUGUI earningsText;

    [Header("Settings")]
    public float fadeDuration = 1.5f;

    public void StartSummarySequence(int day, int served, int earnings)
    {
        gameObject.SetActive(true);
        StartCoroutine(EndOfDayRoutine(day, served, earnings));
    }

    private IEnumerator EndOfDayRoutine(int day, int served, int earnings)
    {
        Debug.Log("Day End: Checking for remaining active customers..."); //

        // 1. รอจนกว่า NPCController ทุกตัวจะถูกทำลาย (ออกจากฉากหมด)
        while (GameObject.FindObjectsOfType<NPCController>().Length > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 🔥 เพิ่ม Log นี้เพื่อให้คุณรู้ว่าเงื่อนไข "ลูกค้าหมดร้าน" ผ่านแล้ว
        Debug.Log("All active customers cleared. Starting Fade Sequence."); //

        // 2. ตั้งค่าข้อมูล UI
        if (dayText != null) dayText.text = "Day " + day + " Finished!";
        if (servedText != null) servedText.text = "Cats Served: " + served;
        if (earningsText != null) earningsText.text = "Total Earnings: " + earnings + " $";

        // 3. เริ่มกระบวนการ Fade In ทั้งม่านดำและหน้าจอสรุป
        float timer = 0;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alphaProgress = Mathf.Lerp(0, 1, timer / fadeDuration);

            // ปรับความชัดของตัวหนังสือและปุ่ม
            if (canvasGroup != null) canvasGroup.alpha = alphaProgress;

            // 🔥 ปรับความมืดของหน้าจอ (Black Overlay)
            if (blackOverlay != null)
            {
                Color c = blackOverlay.color;
                c.a = alphaProgress;
                blackOverlay.color = c;
            }

            yield return null;
        }

        // 4. เปิดให้คลิกปุ่มได้เมื่อ Fade เสร็จ
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}