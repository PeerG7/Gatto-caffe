using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
// RelationshipBookUI — v2 (Keyboard + Gamepad Navigation Added)
//
// ของเดิม: Prev/Next/Close button ยังทำงานด้วย mouse ปกติ
// เพิ่มใหม่:
//   - กด A/D หรือ Left/Right บน D-pad เพื่อเปลี่ยนหน้า
//   - กด Escape / B button เพื่อปิด book
//   - implement IGamepadNavigable
// =====================================================================
public class RelationshipBookUI : MonoBehaviour, IGamepadNavigable
{
    [Header("Book Canvas")]
    public GameObject bookCanvas;

    [Header("Display Elements — ผูกตรงๆ ใน Inspector")]
    public Image portrait;
    public TMP_Text nameText;
    public TMP_Text labelText;
    public Image relBarFill;
    public TMP_Text pageText;

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

#if ENABLE_INPUT_SYSTEM
    [Header("Gamepad Navigation (ใหม่)")]
    [Tooltip("InputActionReference สำหรับ Navigate (D-pad / Left Stick)")]
    public UnityEngine.InputSystem.InputActionReference navigateAction;
    [Tooltip("InputActionReference สำหรับ Back/Cancel (B button / Escape)")]
    public UnityEngine.InputSystem.InputActionReference backAction;
#endif

    // Legacy keyboard ─ ใช้ A/D หรือ Arrow keys เปลี่ยนหน้า
    [Header("Legacy Keyboard (ใหม่)")]
    public KeyCode prevKey = KeyCode.A;
    public KeyCode nextKey = KeyCode.D;

    private List<CatRelationshipData> cats;
    private int currentIndex = 0;
    private bool isOpen = false;

    // กัน navigate เร็วเกินไปด้วย cooldown เล็กน้อย
    private float navCooldown = 0f;
    private const float NAV_COOLDOWN_TIME = 0.25f;

    void Start()
    {
        if (bookCanvas != null) bookCanvas.SetActive(false);
        if (prevButton != null) prevButton.onClick.AddListener(PrevCat);
        if (nextButton != null) nextButton.onClick.AddListener(NextCat);
        if (closeButton != null) closeButton.onClick.AddListener(CloseBook);
    }

#if ENABLE_INPUT_SYSTEM
    void OnEnable()
    {
        if (navigateAction != null) navigateAction.action.Enable();
        if (backAction != null) backAction.action.Enable();
    }

    void OnDisable()
    {
        if (navigateAction != null) navigateAction.action.Disable();
        if (backAction != null) backAction.action.Disable();
    }
#endif

    void Update()
    {
        if (!isOpen) return;

        navCooldown -= Time.deltaTime;

        // ── Legacy keyboard ────────────────────────────────────────
        if (navCooldown <= 0f)
        {
            if (Input.GetKeyDown(prevKey) || Input.GetKeyDown(KeyCode.LeftArrow))
            { PrevCat(); navCooldown = NAV_COOLDOWN_TIME; }
            else if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.RightArrow))
            { NextCat(); navCooldown = NAV_COOLDOWN_TIME; }
        }

        if (Input.GetKeyDown(KeyCode.Escape)) CloseBook();

#if ENABLE_INPUT_SYSTEM
        // ── New Input System ───────────────────────────────────────
        if (navCooldown <= 0f && navigateAction != null)
        {
            Vector2 nav = navigateAction.action.ReadValue<Vector2>();
            if (nav.x < -0.5f) { PrevCat(); navCooldown = NAV_COOLDOWN_TIME; }
            else if (nav.x > 0.5f) { NextCat(); navCooldown = NAV_COOLDOWN_TIME; }
        }

        if (backAction != null && backAction.action.WasPressedThisFrame())
            CloseBook();
#endif
    }

    // ── IGamepadNavigable ──────────────────────────────────────────
    public void OnConfirm() { /* Book ไม่ต้องการ confirm action */ }
    public void OnBack() => CloseBook();
    public void OnNavigate(Vector2 direction)
    {
        if (navCooldown > 0f) return;
        if (direction.x < -0.5f) { PrevCat(); navCooldown = NAV_COOLDOWN_TIME; }
        else if (direction.x > 0.5f) { NextCat(); navCooldown = NAV_COOLDOWN_TIME; }
    }

    // ── ของเดิม + เพิ่ม register/unregister ─────────────────────
    public void OpenBook()
    {
        if (RelationshipManager.Instance == null) return;

        cats = RelationshipManager.Instance.GetAllCats();
        if (cats == null || cats.Count == 0) return;

        currentIndex = 0;
        isOpen = true;

        if (bookCanvas != null) bookCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;

        // ✅ ใหม่: register
        PlayerInteract2D.RegisterActiveCanvas(this);

        // ✅ ใหม่: focus ที่ closeButton เพื่อให้ gamepad มี anchor
        if (EventSystem.current != null && closeButton != null)
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);

        ShowCurrentCat();
    }

    public void CloseBook()
    {
        isOpen = false;
        if (bookCanvas != null) bookCanvas.SetActive(false);

        // ✅ ใหม่: unregister
        PlayerInteract2D.UnregisterActiveCanvas(this);

        PlayerController2D.IsLocked = false;
    }

    void PrevCat()
    {
        if (cats == null || cats.Count == 0) return;
        currentIndex = (currentIndex - 1 + cats.Count) % cats.Count;
        ShowCurrentCat();
    }

    void NextCat()
    {
        if (cats == null || cats.Count == 0) return;
        currentIndex = (currentIndex + 1) % cats.Count;
        ShowCurrentCat();
    }

    void ShowCurrentCat()
    {
        if (cats == null || currentIndex >= cats.Count) return;

        CatRelationshipData cat = cats[currentIndex];
        Debug.Log($"[Book] showing: {cat.catName} | portrait null: {cat.catPortrait == null}");
        Debug.Log($"[Book] portrait field null: {portrait == null} | nameText null: {nameText == null}");

        if (portrait != null) portrait.sprite = cat.catPortrait;
        if (nameText != null) nameText.text = cat.catName;
        if (labelText != null) labelText.text = RelationshipManager.Instance.GetRelationshipLabel(cat.catID);
        if (pageText != null) pageText.text = $"{currentIndex + 1} / {cats.Count}";

        if (relBarFill != null)
            relBarFill.fillAmount = RelationshipManager.Instance.GetRelationship(cat.catID) / cat.maxRelationship;

        bool moreThanOne = cats.Count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(moreThanOne);
        if (nextButton != null) nextButton.gameObject.SetActive(moreThanOne);
    }
}