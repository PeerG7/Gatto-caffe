using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Boba station.
///  Display layers (stay at the slot):  glassImage, teaFillImage
///  Fly proxies (hidden until click):   flyGlassImage, flyBobaImage
///  Stream:                             streamImage (behind everything except the background)
///
/// Click -> glass + boba proxies fly to the slot -> display glass shows "empty", then "glass + boba"
///       -> stream pours (behind all layers) while the tea fills -> final cup -> inventory.
/// </summary>
public class DrinkStation : MonoBehaviour, IGamepadNavigable
{
    [Header("Canvas / Button")]
    public GameObject stationDrinksCanvas;
    public Button emptyGlassButton;
    [Tooltip("Where the boba flies from (the boba jar)")]
    public RectTransform bobaSource;

    [Header("Display layers (placed at the slot, same size & pivot)")]
    public Image glassImage;      // empty glass -> glass+boba -> final cup
    public Image teaFillImage;    // Cup sprite, revealed bottom -> top

    [Header("Fly proxies (hidden by default, same size & pivot as glassImage)")]
    public Image flyGlassImage;
    public Image flyBobaImage;

    [Header("Stream (pivot = top-center, sits behind every layer except the background)")]
    public Image streamImage;
    public Sprite[] streamFrames;
    public Sprite streamStaticSprite;
    public float streamFps = 12f;
    public float streamGrowTime = 0.2f;
    public float streamRetractTime = 0.15f;
    [Tooltip("OFF: stream retracts up into the tap. ON: the top is cut and the tail falls into the glass")]
    public bool streamTailFalls = false;
    // NOTE: the stream keeps the size you set in the scene. It is never resized or squashed;
    // it is only revealed with Image fill (top -> bottom), so it always looks like the sprite.

    [Header("Sprites")]
    public Sprite emptyGlassSprite;
    public Sprite bobaSprite;
    public Sprite glassWithBobaSprite;
    public Sprite cupSprite;

    [Header("Fill mapping (fraction of the cup sprite height, 0 = bottom)")]
    public float fillStart = 0.10f;
    public float fillTop = 0.57f;

    [Header("Timing")]
    public float glassFlyDuration = 0.45f;
    public float bobaFlyDelay = 0.15f;      // 0 = glass and boba fly together
    public float bobaFlyDuration = 0.4f;
    public float waitAfterLand = 0.3f;
    public float fillDuration = 3f;
    public float finishDelay = 0.5f;
    public float flyArcHeight = 80f;
    [Tooltip("Flying objects start at this fraction of the slot size and grow to 100%")]
    [Range(0.2f, 1f)] public float flyStartScale = 0.7f;

    [Header("Sizing")]
    [Tooltip("Copies GlassImage's size/pivot/scale onto the fly proxies and the tea layer at runtime")]
    public bool matchSlotSize = true;

    [Header("Layer order")]
    [Tooltip("Reorders the siblings: stream (back) > glass > tea > fly proxies (front). All must share one parent")]
    public bool autoOrderLayers = true;

    [Header("Drink")]
    public string drinkName = "Boba Milk Tea";

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip glassPlaceClip;
    public AudioClip bobaDropClip;
    public AudioClip pourSoundClip;
    public AudioClip completeSoundClip;

#if ENABLE_INPUT_SYSTEM
    [Header("Gamepad Navigation")]
    public InputActionReference backAction;
    public InputActionReference confirmAction;
#endif

    private bool isProcessing;
    public bool IsProcessing => isProcessing;

    private bool glassLanded, bobaLanded;
    private int pendingFlights;

    void Start()
    {
        if (autoOrderLayers) OrderLayers();
        ResetStation();
        if (emptyGlassButton != null) emptyGlassButton.onClick.AddListener(OnEmptyGlassClicked);
    }

    // stream at the back, then glass, tea, and the flying proxies on top.
    // Uses the lowest sibling index of the group so the background stays behind them.
    void OrderLayers()
    {
        Image[] order = { streamImage, glassImage, teaFillImage, flyGlassImage, flyBobaImage };
        Transform parent = null; int min = int.MaxValue;
        foreach (var img in order)
        {
            if (img == null) continue;
            if (parent == null) parent = img.transform.parent;
            else if (img.transform.parent != parent)
            { Debug.LogWarning("DrinkStation: layer images must share one parent to auto-order."); return; }
            min = Mathf.Min(min, img.transform.GetSiblingIndex());
        }
        if (parent == null) return;
        int i = min;
        foreach (var img in order) if (img != null) img.transform.SetSiblingIndex(i++);
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (backAction != null) backAction.action.Enable();
        if (confirmAction != null) confirmAction.action.Enable();
    }
#endif

    void Update()
    {
        if (stationDrinksCanvas == null || !stationDrinksCanvas.activeSelf) return;

        bool back = Input.GetKeyDown(KeyCode.Escape);
#if ENABLE_INPUT_SYSTEM
        if (!back && backAction != null && backAction.action.WasPressedThisFrame()) back = true;
#endif
        if (back) { CloseCanvas(); return; }

        if (!isProcessing)
        {
            bool confirm = Input.GetKeyDown(KeyCode.E);
#if ENABLE_INPUT_SYSTEM
            if (!confirm && confirmAction != null && confirmAction.action.WasPressedThisFrame()) confirm = true;
#endif
            if (confirm) OnEmptyGlassClicked();
        }
    }

    public void OnConfirm() { if (!isProcessing) OnEmptyGlassClicked(); }
    public void OnBack() => CloseCanvas();
    public void OnNavigate(Vector2 direction) { }

    // ---------- Open / Close ----------
    public void OpenCanvas()
    {
        if (stationDrinksCanvas != null) stationDrinksCanvas.SetActive(true);
        ResetStation();
        PlayerController2D.IsLocked = true;
        PlayerInteract2D.RegisterActiveCanvas(this);
        DayNightManager.Instance?.PauseGame();

        if (EventSystem.current != null && emptyGlassButton != null)
            EventSystem.current.SetSelectedGameObject(emptyGlassButton.gameObject);
    }

    public void CloseCanvas()
    {
        StopAllCoroutines();
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
        if (stationDrinksCanvas != null) stationDrinksCanvas.SetActive(false);
        ResetStation();
        PlayerInteract2D.UnregisterActiveCanvas(this);
        PlayerController2D.IsLocked = false;
        DayNightManager.Instance?.ResumeGame();
    }

    // ---------- Main flow ----------
    public void OnEmptyGlassClicked()
    {
        if (isProcessing) return;
        StartCoroutine(MakeDrinkRoutine());
    }

    private IEnumerator MakeDrinkRoutine()
    {
        isProcessing = true;
        if (emptyGlassButton != null) emptyGlassButton.interactable = false;
        glassLanded = bobaLanded = false;
        pendingFlights = 2;

        Vector3 slot = glassImage.rectTransform.position;   // the display layer IS the slot
        if (matchSlotSize)
        {
            MatchToSlot(flyGlassImage);
            MatchToSlot(flyBobaImage);
            MatchToSlot(teaFillImage);
        }

        // 1) both proxies fly to the slot (boba can start a moment later)
        flyGlassImage.sprite = emptyGlassSprite;
        flyBobaImage.sprite = bobaSprite;
        StartCoroutine(Fly(flyGlassImage.rectTransform, emptyGlassButton.transform.position, slot,
                           glassFlyDuration, flyArcHeight, 0f, OnGlassLanded));
        StartCoroutine(Fly(flyBobaImage.rectTransform, bobaSource.position, slot,
                           bobaFlyDuration, flyArcHeight * 0.6f, bobaFlyDelay, OnBobaLanded));
        while (pendingFlights > 0) yield return null;

        yield return WaitUnscaled(waitAfterLand);

        // 2) tea fill, stream behind everything
        teaFillImage.sprite = cupSprite;
        teaFillImage.type = Image.Type.Filled;
        teaFillImage.fillMethod = Image.FillMethod.Vertical;
        teaFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        teaFillImage.fillAmount = fillStart;
        teaFillImage.rectTransform.position = slot;
        teaFillImage.gameObject.SetActive(true);

        if (sfxSource != null && pourSoundClip != null)
        { sfxSource.clip = pourSoundClip; sfxSource.loop = false; sfxSource.Play(); }

        StartStream();
        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fillDuration));
            teaFillImage.fillAmount = Mathf.Lerp(fillStart, fillTop, p);
            UpdateStream(elapsed, 1f);
            yield return null;
        }

        teaFillImage.fillAmount = 1f;   // show the whole cup sprite
        float r = 0f;
        if (streamTailFalls && streamImage != null) streamImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        while (r < streamRetractTime)
        {
            r += Time.unscaledDeltaTime;
            UpdateStream(elapsed + r, 1f - Mathf.Clamp01(r / Mathf.Max(0.01f, streamRetractTime)));
            yield return null;
        }
        StopStream();
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();

        // 3) done
        glassImage.sprite = cupSprite;
        teaFillImage.gameObject.SetActive(false);
        PlaySfx(completeSoundClip);
        yield return WaitUnscaled(finishDelay);

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null) player.PickUpItem(drinkName, cupSprite);
        CloseCanvas();
    }

    // ---------- Landing ----------
    private void OnGlassLanded() { glassLanded = true; PlaySfx(glassPlaceClip); RefreshDisplay(); }
    private void OnBobaLanded() { bobaLanded = true; PlaySfx(bobaDropClip); RefreshDisplay(); }

    // display glass shows empty first, then swaps to glass+boba; proxies hide once they landed
    private void RefreshDisplay()
    {
        if (glassLanded)
        {
            glassImage.sprite = bobaLanded ? glassWithBobaSprite : emptyGlassSprite;
            glassImage.gameObject.SetActive(true);
            flyGlassImage.gameObject.SetActive(false);
        }
        if (glassLanded && bobaLanded) flyBobaImage.gameObject.SetActive(false);
    }

    private IEnumerator Fly(RectTransform rt, Vector3 from, Vector3 to, float duration,
                            float arc, float delay, Action onLand)
    {
        rt.gameObject.SetActive(false);
        if (delay > 0f) yield return WaitUnscaled(delay);
        rt.gameObject.SetActive(true);
        Vector3 endScale = glassImage.rectTransform.localScale;   // same scale as the slot
        rt.position = from;
        rt.localScale = endScale * flyStartScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Vector3 p = Vector3.Lerp(from, to, e);
            p.y += Mathf.Sin(e * Mathf.PI) * arc;
            rt.position = p;
            rt.localScale = endScale * Mathf.Lerp(flyStartScale, 1f, e);
            yield return null;
        }
        rt.position = to;
        rt.localScale = endScale;
        pendingFlights--;
        onLand?.Invoke();
    }

    // make a proxy exactly the size of the display glass (works with any anchor setup)
    private void MatchToSlot(Image img)
    {
        if (img == null || glassImage == null) return;
        RectTransform src = glassImage.rectTransform, dst = img.rectTransform;
        dst.pivot = src.pivot;
        dst.localScale = src.localScale;
        dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, src.rect.width);
        dst.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, src.rect.height);
        img.preserveAspect = glassImage.preserveAspect;
        img.raycastTarget = false;
    }

    // ---------- Stream ----------
    private void StartStream()
    {
        if (streamImage == null) return;
        streamImage.sprite = (streamFrames != null && streamFrames.Length > 0) ? streamFrames[0] : streamStaticSprite;
        // reveal with fill instead of resizing, so the sprite is never squashed
        streamImage.type = Image.Type.Filled;
        streamImage.fillMethod = Image.FillMethod.Vertical;
        streamImage.fillOrigin = (int)Image.OriginVertical.Top;
        streamImage.fillAmount = 0f;
        streamImage.gameObject.SetActive(streamImage.sprite != null);
    }

    private void UpdateStream(float time, float revealScale)
    {
        if (streamImage == null || !streamImage.gameObject.activeSelf) return;
        if (streamFrames != null && streamFrames.Length > 0)
            streamImage.sprite = streamFrames[Mathf.FloorToInt(time * streamFps) % streamFrames.Length];

        float grow = Mathf.Clamp01(time / Mathf.Max(0.01f, streamGrowTime));
        streamImage.fillAmount = Mathf.Min(grow, revealScale);
    }

    private void StopStream() { if (streamImage != null) streamImage.gameObject.SetActive(false); }

    // ---------- Helpers ----------
    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private void PlaySfx(AudioClip clip) { if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip); }

    private void ResetStation()
    {
        isProcessing = false;
        glassLanded = bobaLanded = false;
        pendingFlights = 0;
        if (emptyGlassButton != null) emptyGlassButton.interactable = true;
        if (glassImage != null) glassImage.gameObject.SetActive(false);
        if (flyGlassImage != null) flyGlassImage.gameObject.SetActive(false);
        if (flyBobaImage != null) flyBobaImage.gameObject.SetActive(false);
        if (teaFillImage != null) { teaFillImage.fillAmount = 0f; teaFillImage.gameObject.SetActive(false); }
        StopStream();
    }
}