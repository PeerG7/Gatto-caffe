using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DrinkStation : MonoBehaviour, IGamepadNavigable
{
    [Header("UI Elements")]
    public Image fillImage;
    public Button drinkButton;

    [Header("Settings")]
    public float fillDuration = 3f;
    public string drinkName = "Milk";
    public Sprite drinkSprite;

    public GameObject stationDrinksCanvas;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip pourSoundClip;
    public AudioClip completeSoundClip;

#if ENABLE_INPUT_SYSTEM
    [Header("Gamepad Navigation")]
    public UnityEngine.InputSystem.InputActionReference backAction;
    public UnityEngine.InputSystem.InputActionReference confirmAction;
#endif

    private Coroutine fillCoroutine = null;
    private bool isProcessing = false;
    public bool IsProcessing => isProcessing;

    void Start()
    {
        if (fillImage != null) { fillImage.fillAmount = 0; fillImage.gameObject.SetActive(false); }
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

        bool backPressed = Input.GetKeyDown(KeyCode.Escape);
#if ENABLE_INPUT_SYSTEM
        if (!backPressed && backAction != null && backAction.action.WasPressedThisFrame())
            backPressed = true;
#endif
        if (backPressed) { CloseCanvas(); return; }

        if (!isProcessing)
        {
            bool confirmPressed = Input.GetKeyDown(KeyCode.E);
#if ENABLE_INPUT_SYSTEM
            if (!confirmPressed && confirmAction != null && confirmAction.action.WasPressedThisFrame())
                confirmPressed = true;
#endif
            if (confirmPressed) OnDrinkButtonClicked();
        }
    }

    public void OnConfirm() { if (!isProcessing) OnDrinkButtonClicked(); }
    public void OnBack() => CloseCanvas();
    public void OnNavigate(Vector2 direction) { }

    public void OpenCanvas()
    {
        if (stationDrinksCanvas != null) stationDrinksCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;
        PlayerInteract2D.RegisterActiveCanvas(this);
        DayNightManager.Instance?.PauseGame();  // ✅ pause

        if (EventSystem.current != null && drinkButton != null)
            EventSystem.current.SetSelectedGameObject(drinkButton.gameObject);
    }

    public void CloseCanvas()
    {
        if (fillCoroutine != null) { StopCoroutine(fillCoroutine); fillCoroutine = null; }
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
        if (stationDrinksCanvas != null) stationDrinksCanvas.SetActive(false);
        ResetStation();
        PlayerInteract2D.UnregisterActiveCanvas(this);
        PlayerController2D.IsLocked = false;
        DayNightManager.Instance?.ResumeGame();  // ✅ resume
    }

    public void OnDrinkButtonClicked()
    {
        if (isProcessing) return;
        fillCoroutine = StartCoroutine(FillDrinkCoroutine());
    }

    private IEnumerator FillDrinkCoroutine()
    {
        isProcessing = true;
        if (drinkButton != null) drinkButton.interactable = false;
        if (fillImage != null) { fillImage.gameObject.SetActive(true); fillImage.fillAmount = 0; }
        if (sfxSource != null && pourSoundClip != null) { sfxSource.clip = pourSoundClip; sfxSource.loop = false; sfxSource.Play(); }

        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null) fillImage.fillAmount = elapsed / fillDuration;
            yield return null;
        }

        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
        PlayCompleteSound();

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null) player.PickUpItem(drinkName, drinkSprite);

        CloseCanvas();  // CloseCanvas เรียก ResumeGame() แล้ว
    }

    void PlayCompleteSound()
    {
        if (sfxSource != null && completeSoundClip != null) sfxSource.PlayOneShot(completeSoundClip);
        else if (AudioManager.instance != null) AudioManager.instance.PlayComplete();
    }

    void ResetStation()
    {
        isProcessing = false;
        if (drinkButton != null) drinkButton.interactable = true;
        if (fillImage != null) { fillImage.fillAmount = 0; fillImage.gameObject.SetActive(false); }
    }
}