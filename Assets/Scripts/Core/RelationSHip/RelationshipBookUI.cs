using System;
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
    [Serializable]
    public struct CatNameTextEntry
    {
        public string catID;
        [Tooltip("GameObject ของ TMP_Text ชื่อแมวที่ตรงกับ catID นี้")]
        public GameObject nameTextObject;
    }

    [Serializable]
    public struct RelationshipLabelTextEntry
    {
        [Tooltip("ค่า Label ที่ตรงกับผลลัพธ์จาก RelationshipManager.GetRelationshipLabel(catID)")]
        public string labelKeyOrText;
        [Tooltip("GameObject ของ TMP_Text สำหรับระดับความสัมพันธ์นี้")]
        public GameObject labelTextObject;
    }

    [Header("Book Canvas")]
    public GameObject bookCanvas;

    [Header("Display Elements — Inspector Connections")]
    public Image portrait;
    public Image relBarFill;
    public TMP_Text pageText;

    [Header("Localized / Toggleable Text Objects")]
    [Tooltip("แยก TMP_Text ชื่อแมวต่อตัว แล้วเปิด-ปิดตาม catID")]
    public List<CatNameTextEntry> catNameTexts = new List<CatNameTextEntry>();

    [Tooltip("แยก TMP_Text ระดับความสัมพันธ์ต่อระดับ แล้วเปิด-ปิดตาม GetRelationshipLabel")]
    public List<RelationshipLabelTextEntry> relationshipLabelTexts = new List<RelationshipLabelTextEntry>();

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

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

    void Update()
    {
        if (!isOpen) return;

        navCooldown -= Time.deltaTime;

        if (navCooldown <= 0f)
        {
            if (Input.GetKeyDown(prevKey) || Input.GetKeyDown(KeyCode.LeftArrow) || GameInput.PadPrevDown)
            { PrevCat(); navCooldown = NAV_COOLDOWN_TIME; }
            else if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.RightArrow) || GameInput.PadNextDown)
            { NextCat(); navCooldown = NAV_COOLDOWN_TIME; }
        }

        if (Input.GetKeyDown(KeyCode.Escape) || GameInput.PadBackDown) CloseBook();
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

        AudioManager.instance?.PlayPaperFold();

        if (bookCanvas != null) bookCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;

        PlayerInteract2D.RegisterActiveCanvas(this);

        if (EventSystem.current != null && closeButton != null)
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);

        ShowCurrentCat();
    }

    public void CloseBook()
    {
        if (isOpen)
            AudioManager.instance?.PlayBookClose();

        isOpen = false;
        if (bookCanvas != null) bookCanvas.SetActive(false);

        PlayerInteract2D.UnregisterActiveCanvas(this);
        PlayerController2D.IsLocked = false;
    }

    void PrevCat()
    {
        if (cats == null || cats.Count == 0) return;
        if (cats.Count > 1) AudioManager.instance?.PlayPaperFold();
        currentIndex = (currentIndex - 1 + cats.Count) % cats.Count;
        ShowCurrentCat();
    }

    void NextCat()
    {
        if (cats == null || cats.Count == 0) return;
        if (cats.Count > 1) AudioManager.instance?.PlayPaperFold();
        currentIndex = (currentIndex + 1) % cats.Count;
        ShowCurrentCat();
    }

    void ShowCurrentCat()
    {
        if (cats == null || currentIndex >= cats.Count) return;

        CatRelationshipData cat = cats[currentIndex];

        if (portrait != null) portrait.sprite = cat.catPortrait;
        UpdateCatNameUI(cat.catID);

        string currentLabel = RelationshipManager.Instance != null
            ? RelationshipManager.Instance.GetRelationshipLabel(cat.catID)
            : string.Empty;
        UpdateRelationshipLabelUI(currentLabel);

        if (pageText != null) pageText.text = $"{currentIndex + 1} / {cats.Count}";

        if (relBarFill != null && RelationshipManager.Instance != null)
        {
            float currentRel = RelationshipManager.Instance.GetRelationship(cat.catID);
            float maxRel = cat.maxRelationship;
            relBarFill.fillAmount = maxRel > 0f ? (float)currentRel / (float)maxRel : 0f;
        }

        bool moreThanOne = cats.Count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(moreThanOne);
        if (nextButton != null) nextButton.gameObject.SetActive(moreThanOne);
    }

    void UpdateCatNameUI(string catID)
    {
        foreach (var entry in catNameTexts)
        {
            if (entry.nameTextObject == null) continue;
            bool shouldShow = (entry.catID == catID);
            if (entry.nameTextObject.activeSelf != shouldShow)
                entry.nameTextObject.SetActive(shouldShow);
        }
    }

    void UpdateRelationshipLabelUI(string currentLabel)
    {
        foreach (var entry in relationshipLabelTexts)
        {
            if (entry.labelTextObject == null) continue;
            bool shouldShow = (entry.labelKeyOrText == currentLabel);
            if (entry.labelTextObject.activeSelf != shouldShow)
                entry.labelTextObject.SetActive(shouldShow);
        }
    }
}