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
        if (notificationText == null) return;

        StopAllCoroutines(); // หยุดการแจ้งเตือนเก่าถ้ามี
        StartCoroutine(DisplayRoutine(message));
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