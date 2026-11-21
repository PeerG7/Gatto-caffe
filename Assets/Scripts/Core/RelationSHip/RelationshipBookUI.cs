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
    [Header("Debug")]
    public bool debugManualBarFill = false;

    [Header("Ordered Text Objects (เปิด/ปิดตามลำดับหน้าและระดับ Bar)")]
    [Tooltip("ลาก GameObject ของชื่อแมวเรียงตามลำดับหน้า (0, 1, 2, 3...)")]
    public List<GameObject> catNameTextObjects = new List<GameObject>();

    [Tooltip("ลาก GameObject ของ Label ระดับความสัมพันธ์ เรียงจาก Bar น้อยไปมาก (0, 1, 2, 3, 4...)")]
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

        // อัปเดตหลอด Bar และ Label แบบเรียลไทม์ขณะที่เปิดหน้าสมุดอยู่
        RefreshBarAndLabel();

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
        if (cats == null || cats.Count == 0)
        {
            SetActiveByIndex(catNameTextObjects, -1);
            SetActiveByIndex(relationshipTierTextObjects, -1);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, cats.Count - 1);
        CatRelationshipData cat = cats[currentIndex];

        if (portrait != null) portrait.sprite = cat.catPortrait;

        if (catNameTextObjects != null && catNameTextObjects.Count > 0)
            SetActiveByIndex(catNameTextObjects, currentIndex);
        else
            ToggleChildrenByPrefix("Name", currentIndex);

        if (pageText != null) pageText.text = $"{currentIndex + 1} / {cats.Count}";

        RefreshBarAndLabel();

        bool moreThanOne = cats.Count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(moreThanOne);
        if (nextButton != null) nextButton.gameObject.SetActive(moreThanOne);
    }

    void RefreshBarAndLabel()
    {
        if (cats == null || cats.Count == 0) return;

        int safeIndex = Mathf.Clamp(currentIndex, 0, cats.Count - 1);
        CatRelationshipData cat = cats[safeIndex];

        float fill = 0f;
        if (relBarFill != null)
        {
            if (!debugManualBarFill)
            {
                float currentRel = RelationshipManager.Instance != null
                    ? RelationshipManager.Instance.GetRelationship(cat.catID)
                    : 0f;
                float maxRel = cat.maxRelationship;
                fill = maxRel > 0f ? Mathf.Clamp01(currentRel / maxRel) : 0f;
                relBarFill.fillAmount = fill;
            }
            else
            {
                fill = Mathf.Clamp01(relBarFill.fillAmount);
            }
        }

        if (relationshipTierTextObjects != null && relationshipTierTextObjects.Count > 0)
        {
            int labelIndex = GetLabelIndexFromBar(fill, relationshipTierTextObjects.Count);
            SetActiveByIndex(relationshipTierTextObjects, labelIndex);
        }
        else
        {
            int labelIndex = GetLabelIndexFromBar(fill, CountChildrenByPrefix("Label"));
            ToggleChildrenByPrefix("Label", labelIndex);
        }
    }

    int GetLabelIndexFromBar(float barFillAmount, int labelCount)
    {
        if (labelCount <= 0) return -1;

        float clamped = Mathf.Clamp01(barFillAmount);
        if (clamped >= 1f) return labelCount - 1;

        return Mathf.Clamp(Mathf.FloorToInt(clamped * labelCount), 0, labelCount - 1);
    }

    void SetActiveByIndex(List<GameObject> objects, int activeIndex)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Count; i++)
        {
            GameObject target = objects[i];
            if (target == null) continue;
            target.SetActive(i == activeIndex);
        }
    }

    private List<GameObject> GetChildrenByPrefix(string prefix)
    {
        List<GameObject> matched = new List<GameObject>();
        if (bookCanvas == null) return matched;

        Transform[] allTransforms = bookCanvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t != null && t.name.StartsWith(prefix))
                matched.Add(t.gameObject);
        }

        matched.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return matched;
    }

    private int CountChildrenByPrefix(string prefix)
    {
        return GetChildrenByPrefix(prefix).Count;
    }

    private void ToggleChildrenByPrefix(string prefix, int activeIndex)
    {
        List<GameObject> matched = GetChildrenByPrefix(prefix);
        for (int i = 0; i < matched.Count; i++)
        {
            matched[i].SetActive(i == activeIndex);
        }
    }
}