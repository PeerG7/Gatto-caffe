using System.Collections;
using UnityEngine;

public class ComputerStation : MonoBehaviour
{
    [Header("Shop UI Panel")]
    public GameObject shopCanvas;

    [Header("Open/Close Animation")]
    public float animDuration = 0.2f;
    public float startScale = 0.9f;

    public static bool IsShopOpen { get; private set; }

    CanvasGroup group;
    Coroutine anim;

    void Start()
    {
        if (shopCanvas != null)
        {
            group = shopCanvas.GetComponent<CanvasGroup>();
            if (group == null) group = shopCanvas.AddComponent<CanvasGroup>();
            shopCanvas.SetActive(false);
        }
        IsShopOpen = false;
    }

    public void OpenComputerShop()
    {
        if (FurnitureManager.Instance != null && FurnitureManager.Instance.isEditMode) return;

        if (shopCanvas != null)
        {
            if (!IsShopOpen)
                AudioManager.instance?.PlayComputerOpen();

            IsShopOpen = true;
            PlayerController2D.SetLock(true);
            CameraController2D.SetPanMode(true);
            DayNightManager.Instance?.PauseGame();

            PlayAnim(true);
        }
    }

    public void CloseComputerShop()
    {
        if (shopCanvas != null)
        {
            IsShopOpen = false;
            PlayerController2D.SetLock(false);
            CameraController2D.SetPanMode(false);
            DayNightManager.Instance?.ResumeGame();

            PlayAnim(false);
        }
    }

    void PlayAnim(bool open)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimRoutine(open));
    }

    IEnumerator AnimRoutine(bool open)
    {
        Transform t = shopCanvas.transform;
        if (open) shopCanvas.SetActive(true);

        float from = open ? 0f : group.alpha;
        float to = open ? 1f : 0f;
        float elapsed = 0f;

        group.interactable = false;

        while (elapsed < animDuration)
        {
            // ใช้ unscaledDeltaTime เพราะเกมอาจ Pause ด้วย timeScale = 0
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);

            group.alpha = Mathf.Lerp(from, to, k);
            float s = Mathf.Lerp(startScale, 1f, group.alpha);
            t.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        group.alpha = to;
        t.localScale = Vector3.one;
        group.interactable = open;

        if (!open) shopCanvas.SetActive(false);
        anim = null;
    }
}