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
    public UnityEngine.InputSystem.InputActionReference confirmAction; // เพิ่มสำหรับปุ่ม E/A
#endif

    private Coroutine fillCoroutine = null;
    private bool isProcessing = false;

    public bool IsProcessing => isProcessing;

    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
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
        // ตรวจสอบเฉพาะตอนเปิด Canvas อยู่เท่านั้น
        if (stationDrinksCanvas == null || !stationDrinksCanvas.activeSelf) return;

        // 1. ตรวจสอบการกดปุ่ม ปิด (Back / Escape) - ให้ปิดได้เสมอแม้กำลังเติมน้ำ
        bool backPressed = Input.GetKeyDown(KeyCode.Escape);
#if ENABLE_INPUT_SYSTEM
        if (!backPressed && backAction != null && backAction.action.WasPressedThisFrame())
            backPressed = true;
#endif
        if (backPressed)
        {
            CloseCanvas();
            return;
        }

        // 2. ตรวจสอบการกดปุ่ม เริ่ม (Confirm / E) - ทำงานเฉพาะตอนที่ยังไม่ได้เติมน้ำ
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

    // ── IGamepadNavigable (เผื่อไว้สำหรับระบบจัดการรวม) ──────────
    public void OnConfirm() { if (!isProcessing) OnDrinkButtonClicked(); }
    public void OnBack() => CloseCanvas();
    public void OnNavigate(Vector2 direction) { }

    // ── ระบบเปิด/ปิด ──────────────────────────────────────────
    public void OpenCanvas()
    {
        if (stationDrinksCanvas != null) stationDrinksCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;
        PlayerInteract2D.RegisterActiveCanvas(this);

        if (EventSystem.current != null && drinkButton != null)
            EventSystem.current.SetSelectedGameObject(drinkButton.gameObject);
    }

    public void CloseCanvas()
    {
        // หยุด Coroutine และเสียงทันทีถ้ามีการปิดหน้าจอ
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();

        if (stationDrinksCanvas != null) stationDrinksCanvas.SetActive(false);

        ResetStation();
        PlayerInteract2D.UnregisterActiveCanvas(this);
        PlayerController2D.IsLocked = false;
    }

    // ── ระบบการเติมน้ำ (เหมือน DeepFry แต่ใช้ E) ──────────────────
    public void OnDrinkButtonClicked()
    {
        if (isProcessing) return;
        fillCoroutine = StartCoroutine(FillDrinkCoroutine());
    }

    private IEnumerator FillDrinkCoroutine()
    {
        isProcessing = true;
        if (drinkButton != null) drinkButton.interactable = false;

        if (fillImage != null)
        {
            fillImage.gameObject.SetActive(true);
            fillImage.fillAmount = 0;
        }

        if (sfxSource != null && pourSoundClip != null)
        {
            sfxSource.clip = pourSoundClip;
            sfxSource.loop = false;
            sfxSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
                fillImage.fillAmount = elapsed / fillDuration;
            yield return null;
        }

        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
        PlayCompleteSound();

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null) player.PickUpItem(drinkName, drinkSprite);

        // เมื่อเสร็จแล้วให้ปิดหน้าจออัตโนมัติ
        CloseCanvas();
    }

    void PlayCompleteSound()
    {
        if (sfxSource != null && completeSoundClip != null)
            sfxSource.PlayOneShot(completeSoundClip);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayComplete();
    }

    void ResetStation()
    {
        isProcessing = false;
        if (drinkButton != null) drinkButton.interactable = true;
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }
}