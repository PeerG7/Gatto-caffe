using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UINotificationManager : MonoBehaviour
{
    public static UINotificationManager Instance;

    [Header("UI References")]
    public GameObject notificationPanelRoot;
    public TextMeshProUGUI notificationText;
    public Image mascotImage;
    public Image expandableBackground;
    public MascotAnimator mascotAnimator;    // 🌟 ลากคอมโพเนนต์ MascotAnimator มาใส่ตรงนี้

    [Header("Settings")]
    public float displayDuration = 2.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (notificationPanelRoot != null) notificationPanelRoot.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);

        // ค้นหาอัตโนมัติถ้าไม่ได้ลากใส่ช่อง
        if (mascotAnimator == null && mascotImage != null)
            mascotAnimator = mascotImage.GetComponent<MascotAnimator>();
    }

    public void ShowNotification(string message)
    {
        if (notificationPanelRoot != null) notificationPanelRoot.SetActive(true);
        this.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(DisplayRoutine(message));
    }

    IEnumerator DisplayRoutine(string message)
    {
        if (notificationText == null) yield break;

        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        SetAlpha(1.0f);

        // 🌟 เริ่มอนิเมชันขยับปากพูดตอนข้อความเริ่มแสดง
        if (mascotAnimator != null) mascotAnimator.StartTalking();

        yield return new WaitForSeconds(displayDuration);

        // 🌟 หยุดขยับปากเมื่อถึงเวลาจางหาย
        if (mascotAnimator != null) mascotAnimator.StopTalking();

        // ค่อยๆ จางหายลง
        float elapsed = 0;
        float fadeDuration = 1.0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1.0f, 0f, elapsed / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0f);
        notificationText.gameObject.SetActive(false);
        if (notificationPanelRoot != null) notificationPanelRoot.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        if (notificationText != null)
        {
            Color tColor = notificationText.color;
            tColor.a = alpha;
            notificationText.color = tColor;
        }

        if (mascotImage != null)
        {
            Color mColor = mascotImage.color;
            mColor.a = alpha;
            mascotImage.color = mColor;
        }

        if (expandableBackground != null)
        {
            Color bColor = expandableBackground.color;
            bColor.a = alpha;
            expandableBackground.color = bColor;
        }
    }
}