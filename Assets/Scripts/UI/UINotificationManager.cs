using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using System.Collections;

public class UINotificationManager : MonoBehaviour
{
    public static UINotificationManager Instance;

    [Header("UI References")]
    public GameObject notificationPanelRoot;
    public TextMeshProUGUI notificationText;
    public Image mascotImage;
    public Image expandableBackground;
    public MascotAnimator mascotAnimator;

    [Header("Localization")]
    [Tooltip("ชื่อ String Table ใน Unity Localization เช่น StringPasaa")]
    public string defaultStringTable = "";

    [Tooltip("เปิดเพื่อค้นหาคำแปลจาก Key ใน Table อัตโนมัติ และ Fallback เป็นข้อความเดิมหากไม่พบ")]
    public bool autoLocalizeByKey = true;

    [Header("Settings")]
    public float displayDuration = 2.0f;
    public float fadeDuration = 1.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (notificationPanelRoot != null) notificationPanelRoot.SetActive(false);
        if (notificationText != null) notificationText.gameObject.SetActive(false);

        if (mascotAnimator == null && mascotImage != null)
            mascotAnimator = mascotImage.GetComponent<MascotAnimator>();
    }

    public void ShowNotification(string message)
    {
        ShowNotification(message, null);
    }

    public void ShowNotification(string keyOrMessage, params object[] args)
    {
        if (string.IsNullOrEmpty(keyOrMessage)) return;

        StopAllCoroutines();

        if (autoLocalizeByKey && !string.IsNullOrEmpty(defaultStringTable))
        {
            StartCoroutine(ResolveAndShowRoutine(defaultStringTable, keyOrMessage, args));
        }
        else
        {
            StartDisplayRoutine(FormatText(keyOrMessage, args));
        }
    }

    private IEnumerator ResolveAndShowRoutine(string tableName, string keyOrFallbackText, object[] args)
    {
        yield return LocalizationSettings.InitializationOperation;

        var tableOp = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
        yield return tableOp;

        var table = tableOp.Result;
        if (table != null)
        {
            var entry = table.GetEntry(keyOrFallbackText);
            string localized = entry != null ? entry.GetLocalizedString() : null;
            if (!string.IsNullOrEmpty(localized))
            {
                StartDisplayRoutine(FormatText(localized, args));
                yield break;
            }
        }

        StartDisplayRoutine(FormatText(keyOrFallbackText, args));
    }

    private string FormatText(string template, object[] args)
    {
        if (string.IsNullOrEmpty(template) || args == null || args.Length == 0)
            return template;

        try
        {
            return string.Format(template, args);
        }
        catch (System.FormatException)
        {
            return template;
        }
    }

    private void StartDisplayRoutine(string finalMessage)
    {
        if (notificationPanelRoot != null) notificationPanelRoot.SetActive(true);
        this.gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(DisplayRoutine(finalMessage));
    }

    IEnumerator DisplayRoutine(string message)
    {
        if (notificationText == null) yield break;

        notificationText.text = message;
        notificationText.gameObject.SetActive(true);
        SetAlpha(1.0f);

        if (mascotAnimator != null) mascotAnimator.StartTalking();

        yield return new WaitForSeconds(displayDuration);

        if (mascotAnimator != null) mascotAnimator.StopTalking();

        float elapsed = 0f;
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