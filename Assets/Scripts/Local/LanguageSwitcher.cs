using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageSwitcher : MonoBehaviour
{
    private bool isChanging = false;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        int saved = PlayerPrefs.GetInt("lang", 0);
        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.Locales[saved];
    }

    public void ToggleLocale()
    {
        if (isChanging) return;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        int next = (locales.IndexOf(LocalizationSettings.SelectedLocale) + 1) % locales.Count;
        StartCoroutine(SetLocale(next));
    }

    private IEnumerator SetLocale(int index)
    {
        isChanging = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        PlayerPrefs.SetInt("lang", index);
        PlayerPrefs.Save();
        isChanging = false;
    }
}