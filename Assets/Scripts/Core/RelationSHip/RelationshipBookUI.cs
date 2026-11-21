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
    public Image relBarFill;
    public TMP_Text pageText;

    [Header("Ordered Text Objects (เรียงลำดับตามข้อมูล)")]
    [Tooltip("ลาก GameObject ของ TMP_Text ชื่อแมว เรียงตามลำดับแมวใน GetAllCats() (0, 1, 2...)")]
    public List<GameObject> catNameTextObjects = new List<GameObject>();

    [Tooltip("ลาก GameObject ของ TMP_Text ระดับความสัมพันธ์ เรียงจากระดับต่ำสุดไปสูงสุด (0, 1, 2...)")]
    public List<GameObject> relationshipTierTextObjects = new List<GameObject>();

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
        UpdateOrderedTextList(catNameTextObjects, currentIndex);

        float currentRel = 0f;
        float maxRel = cat.maxRelationship;
        if (RelationshipManager.Instance != null)
        {
            currentRel = RelationshipManager.Instance.GetRelationship(cat.catID);
        }

        int tierIndex = GetRelationshipTierIndex(currentRel, maxRel, relationshipTierTextObjects.Count);
        UpdateOrderedTextList(relationshipTierTextObjects, tierIndex);

        if (pageText != null) pageText.text = $"{currentIndex + 1} / {cats.Count}";

        if (relBarFill != null)
        {
            relBarFill.fillAmount = maxRel > 0f ? currentRel / maxRel : 0f;
        }

        bool moreThanOne = cats.Count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(moreThanOne);
        if (nextButton != null) nextButton.gameObject.SetActive(moreThanOne);
    }

    int GetRelationshipTierIndex(float currentRel, float maxRel, int tierCount)
    {
        if (tierCount <= 0) return -1;
        if (maxRel <= 0f) return 0;

        float ratio = Mathf.Clamp01(currentRel / maxRel);
        if (ratio >= 1f) return tierCount - 1;

        return Mathf.FloorToInt(ratio * tierCount);
    }

    void UpdateOrderedTextList(List<GameObject> objects, int activeIndex)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] == null) continue;
            bool shouldShow = (i == activeIndex);
            if (objects[i].activeSelf != shouldShow)
                objects[i].SetActive(shouldShow);
        }
    }
}