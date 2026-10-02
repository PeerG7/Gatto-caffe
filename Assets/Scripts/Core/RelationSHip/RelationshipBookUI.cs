using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RelationshipBookUI : MonoBehaviour, IGamepadNavigable
{
    [Header("Book Canvas")]
    public GameObject bookCanvas;

    [Header("Display Elements — Inspector Connections")]
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
    [Header("Gamepad Navigation")]
    public UnityEngine.InputSystem.InputActionReference navigateAction;
    public UnityEngine.InputSystem.InputActionReference backAction;
#endif

    [Header("Legacy Keyboard Navigation")]
    public KeyCode prevKey = KeyCode.A;
    public KeyCode nextKey = KeyCode.D;

    private List<CatRelationshipData> cats;
    private int currentIndex = 0;
    private bool isOpen = false;

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

        if (navCooldown <= 0f)
        {
            if (Input.GetKeyDown(prevKey) || Input.GetKeyDown(KeyCode.LeftArrow))
            { PrevCat(); navCooldown = NAV_COOLDOWN_TIME; }
            else if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.RightArrow))
            { NextCat(); navCooldown = NAV_COOLDOWN_TIME; }
        }

        if (Input.GetKeyDown(KeyCode.Escape)) CloseBook();

#if ENABLE_INPUT_SYSTEM
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

    public void OnConfirm() { }
    public void OnBack() => CloseBook();
    public void OnNavigate(Vector2 direction)
    {
        if (navCooldown > 0f) return;
        if (direction.x < -0.5f) { PrevCat(); navCooldown = NAV_COOLDOWN_TIME; }
        else if (direction.x > 0.5f) { NextCat(); navCooldown = NAV_COOLDOWN_TIME; }
    }

    public void OpenBook()
    {
        if (RelationshipManager.Instance == null) return;

        cats = RelationshipManager.Instance.GetAllCats();
        if (cats == null || cats.Count == 0) return;

        currentIndex = 0;
        isOpen = true;

        if (bookCanvas != null) bookCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;

        PlayerInteract2D.RegisterActiveCanvas(this);

        if (EventSystem.current != null && closeButton != null)
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);

        ShowCurrentCat();
    }

    public void CloseBook()
    {
        isOpen = false;
        if (bookCanvas != null) bookCanvas.SetActive(false);

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

        if (portrait != null) portrait.sprite = cat.catPortrait;
        if (nameText != null) nameText.text = cat.catName;
        if (labelText != null) labelText.text = RelationshipManager.Instance.GetRelationshipLabel(cat.catID);
        if (pageText != null) pageText.text = $"{currentIndex + 1} / {cats.Count}";

        // คำนวณและอัปเดตหลอด Bar
        if (relBarFill != null)
        {
            float currentRel = RelationshipManager.Instance.GetRelationship(cat.catID);
            float maxRel = cat.maxRelationship;
            relBarFill.fillAmount = maxRel > 0f ? (float)currentRel / (float)maxRel : 0f;
        }

        bool moreThanOne = cats.Count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(moreThanOne);
        if (nextButton != null) nextButton.gameObject.SetActive(moreThanOne);
    }
}