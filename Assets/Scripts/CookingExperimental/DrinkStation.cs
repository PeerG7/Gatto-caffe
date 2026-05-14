using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// DrinkStation — v2 (Keyboard + Gamepad Navigation Added)
//
// ของเดิม: mouse/touch ทุกอย่างทำงานปกติ
// เพิ่มใหม่:
//   - implement IGamepadNavigable
//   - OpenCanvas() → RegisterActiveCanvas + SetSelectedGameObject
//   - CloseCanvas() → UnregisterActiveCanvas
//   - กด Escape / B button ขณะ canvas เปิด → CloseCanvas()
//   - กด Confirm (E / A) ขณะ canvas เปิด → OnDrinkButtonClicked()
// =====================================================================
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
    [Header("Gamepad Navigation (ใหม่)")]
    [Tooltip("InputActionReference สำหรับปุ่ม Back/Cancel (B button / Escape)")]
    public UnityEngine.InputSystem.InputActionReference backAction;
#endif

    private Coroutine fillCoroutine = null;
    private bool isProcessing = false;

    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable() { if (backAction != null) backAction.action.Enable(); }
    void OnDisable() { if (backAction != null) backAction.action.Disable(); }
#endif

    void Update()
    {
        // ตรวจ back input เฉพาะตอน canvas เปิดและไม่ได้กำลัง process
        if (stationDrinksCanvas == null || !stationDrinksCanvas.activeSelf) return;
        if (isProcessing) return;

        bool backPressed = false;

        // Legacy keyboard
        if (Input.GetKeyDown(KeyCode.Escape)) backPressed = true;

#if ENABLE_INPUT_SYSTEM
        if (!backPressed && backAction != null && backAction.action.WasPressedThisFrame())
            backPressed = true;
#endif

        if (backPressed) CloseCanvas();
    }

    // ── IGamepadNavigable ──────────────────────────────────────────
    public void OnConfirm()
    {
        if (!isProcessing) OnDrinkButtonClicked();
    }

    public void OnBack() => CloseCanvas();

    public void OnNavigate(Vector2 direction) { /* single-button canvas ไม่ต้องการ navigate */ }

    // ── OpenCanvas / CloseCanvas ───────────────────────────────────
    public void OpenCanvas()
    {
        if (stationDrinksCanvas != null)
            stationDrinksCanvas.SetActive(true);

        PlayerController2D.IsLocked = true;

        // ✅ register ให้ PlayerInteract2D รู้ว่า canvas นี้กำลังเปิดอยู่
        PlayerInteract2D.RegisterActiveCanvas(this);

        // ✅ focus ที่ drinkButton ทันทีเมื่อเปิด
        if (EventSystem.current != null && drinkButton != null)
            EventSystem.current.SetSelectedGameObject(drinkButton.gameObject);
    }

    public void CloseCanvas()
    {
        if (stationDrinksCanvas != null)
            stationDrinksCanvas.SetActive(false);

        // ✅ unregister
        PlayerInteract2D.UnregisterActiveCanvas(this);

        if (isProcessing)
        {
            if (fillCoroutine != null)
            {
                StopCoroutine(fillCoroutine);
                fillCoroutine = null;
            }

            if (sfxSource != null && sfxSource.isPlaying)
                sfxSource.Stop();

            ResetStation();
        }

        PlayerController2D.IsLocked = false;
    }

    // ── ของเดิมทั้งหมด (ไม่มีการแก้ไข) ───────────────────────────
    public void OnDrinkButtonClicked()
    {
        if (isProcessing) return;
        PlayerController2D.IsLocked = true;
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

        fillCoroutine = null;

        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();

        PlayCompleteSound();

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
            player.PickUpItem(drinkName, drinkSprite);

        ResetStation();

        if (stationDrinksCanvas != null)
            stationDrinksCanvas.SetActive(false);

        // ✅ unregister เมื่อ canvas ปิดหลัง fill เสร็จ
        PlayerInteract2D.UnregisterActiveCanvas(this);

        PlayerController2D.IsLocked = false;
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