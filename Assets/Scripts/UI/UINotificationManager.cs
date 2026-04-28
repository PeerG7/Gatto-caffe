using UnityEngine;
using TMPro; // อย่าลืมติดตั้ง TextMeshPro
using System.Collections;

public class UINotificationManager : MonoBehaviour
{
    public static UINotificationManager Instance;
    public TextMeshProUGUI notificationText;
    public float displayDuration = 2.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (notificationText != null) notificationText.gameObject.SetActive(false);
    }

    public void ShowNotification(string message)
    {
        // 1. เปิด GameObject ของตัวเองก่อนเริ่มทำงาน (ป้องกัน Error Inactive)
        this.gameObject.SetActive(true);

        // 2. หยุด Coroutine เก่า (ถ้ามี) เพื่อไม่ให้ข้อความทับกัน
        StopAllCoroutines();

        // 3. เริ่มรัน Coroutine ใหม่
        StartCoroutine(NotificationSequence(message));
    }

    private IEnumerator NotificationSequence(string message)
    {
        // โค้ดการแสดงผลข้อความของคุณ...
        // เช่น textDisplay.text = message;
        yield return new WaitForSeconds(2f);

        // เมื่อแสดงผลเสร็จ จะปิดตัวเองไปก็ได้ หรือจะค้างไว้ก็ได้
        // this.gameObject.SetActive(false); 
    }

    IEnumerator DisplayRoutine(string message)
    {
        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        notificationText.alpha = 1.0f;

        yield return new WaitForSeconds(displayDuration);

        // ค่อยๆ จางหาย (Fade Out)
        float elapsed = 0;
        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            notificationText.alpha = Mathf.Lerp(1.0f, 0f, elapsed);
            yield return null;
        }

        notificationText.gameObject.SetActive(false);
    }
}