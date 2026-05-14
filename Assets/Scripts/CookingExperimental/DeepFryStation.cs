using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// DeepFryStation — v3 (Shared Canvas with CookingManager)
//
// เปลี่ยนจากเดิม:
//   - ไม่ manage OpenCanvas / CloseCanvas เองอีกต่อไป
//     เพราะ canvas เดียวกับ CookingManager — CookingManager จัดการแทน
//   - OnFryButtonClicked() ยัง public อยู่ — CookingManager เรียกผ่าน hotkey F
//   - mouse click บน deepfry button ยังทำงานปกติ
//   - FryCoroutine ทำงานอิสระ ไม่ยุ่งกับ PlayerController2D.IsLocked
//     เพราะ CookingManager เป็นคนล็อกอยู่แล้ว
//
// Setup ใน Inspector:
//   - ลาก DeepFryStation component นี้ไปผูกกับ CookingManager.deepFryStation
//   - ลาก fryButton ไปผูกกับ CookingManager.deepFryButton ด้วย
//     เพื่อให้ CookingManager ตรวจ interactable ก่อนกด F
// =====================================================================
public class DeepFryStation : MonoBehaviour
{
    [Header("UI Elements")]
    public Image fillImage;
    public Button fryButton;

    [Header("Settings")]
    public float fryDuration = 4f;
    public string foodName = "Tempura";
    public Sprite foodSprite;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip frySoundClip;
    public AudioClip completeSoundClip;

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

    // ── ยังคง OpenCanvas / CloseCanvas ไว้เผื่อใช้แบบ standalone ──
    // ถ้า DeepFryStation อยู่ใน canvas เดียวกับ CookingManager
    // ไม่จำเป็นต้องเรียก method เหล่านี้ — CookingManager จัดการแทน
    public void OpenCanvas() { /* จัดการโดย CookingManager */ }
    public void CloseCanvas() { /* จัดการโดย CookingManager */ }

    // ── OnFryButtonClicked: เรียกได้จากทั้ง mouse click และ hotkey F ─
    public void OnFryButtonClicked()
    {
        if (isProcessing) return;
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

    // ── helper สำหรับ CookingManager ตรวจสอบสถานะ ─────────────────
    public bool IsProcessing => isProcessing;
}