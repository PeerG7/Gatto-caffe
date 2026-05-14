using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// DeepFryStation — v2 (Gamepad Canvas Navigation Added)
//
// ของเดิม: mouse/touch ทุกอย่างทำงานปกติ
// เพิ่มใหม่:
//   - implement IGamepadNavigable
//   - OpenCanvas() register / CloseCanvas() unregister
//   - กด B / Escape เพื่อปิด canvas
//   - กด Confirm (E / A) ขณะ canvas เปิด → เรียก OnFryButtonClicked()
//   - ตั้ง initial focus ที่ fryButton เมื่อ canvas เปิด
// =====================================================================
public class DeepFryStation : MonoBehaviour, IGamepadNavigable
{
    [Header("UI Elements")]
    public Image fillImage;
    public Button fryButton;

    [Header("Settings")]
    public float fryDuration = 4f;
    public string foodName = "Tempura";
    public Sprite foodSprite;

    public GameObject stationFryCanvas;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip frySoundClip;
    public AudioClip completeSoundClip;

#if ENABLE_INPUT_SYSTEM
    [Header("Gamepad Navigation (ใหม่)")]
    [Tooltip("InputActionReference สำหรับปุ่ม Back/Cancel (B button / Escape)")]
    public UnityEngine.InputSystem.InputActionReference backAction;
#endif

    private Coroutine fryCoroutine = null;
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
        if (stationFryCanvas == null || !stationFryCanvas.activeSelf) return;
        if (isProcessing) return;

        bool backPressed = false;
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
        if (!isProcessing) OnFryButtonClicked();
    }

    public void OnBack() => CloseCanvas();

    public void OnNavigate(Vector2 direction) { /* EventSystem จัดการให้ */ }

    // ── OpenCanvas / CloseCanvas ───────────────────────────────────
    public void OpenCanvas()
    {
        if (stationFryCanvas != null)
            stationFryCanvas.SetActive(true);

        PlayerController2D.IsLocked = true;
        PlayerInteract2D.RegisterActiveCanvas(this);

        // ✅ focus ที่ fryButton ทันทีเมื่อเปิด
        if (EventSystem.current != null && fryButton != null)
            EventSystem.current.SetSelectedGameObject(fryButton.gameObject);
    }

    public void CloseCanvas()
    {
        if (stationFryCanvas != null)
            stationFryCanvas.SetActive(false);

        PlayerInteract2D.UnregisterActiveCanvas(this);

        if (isProcessing)
        {
            if (fryCoroutine != null) { StopCoroutine(fryCoroutine); fryCoroutine = null; }
            if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();
            ResetStation();
        }

        PlayerController2D.IsLocked = false;
    }

    // ── ของเดิมทั้งหมด (ไม่มีการแก้ไข) ───────────────────────────
    public void OnFryButtonClicked()
    {
        if (isProcessing) return;
        PlayerController2D.IsLocked = true;
        fryCoroutine = StartCoroutine(FryCoroutine());
    }

    private IEnumerator FryCoroutine()
    {
        isProcessing = true;
        if (fryButton != null) fryButton.interactable = false;

        if (fillImage != null)
        {
            fillImage.gameObject.SetActive(true);
            fillImage.fillAmount = 0;
        }

        if (sfxSource != null && frySoundClip != null)
        {
            sfxSource.clip = frySoundClip;
            sfxSource.loop = false;
            sfxSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < fryDuration)
        {
            elapsed += Time.deltaTime;
            if (fillImage != null)
                fillImage.fillAmount = elapsed / fryDuration;
            yield return null;
        }

        fryCoroutine = null;

        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Stop();

        PlayCompleteSound();

        PlayerInventory player = FindObjectOfType<PlayerInventory>();
        if (player != null)
            player.PickUpItem(foodName, foodSprite);

        ResetStation();

        if (stationFryCanvas != null)
            stationFryCanvas.SetActive(false);

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
        if (fryButton != null) fryButton.interactable = true;
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.gameObject.SetActive(false);
        }
    }
}