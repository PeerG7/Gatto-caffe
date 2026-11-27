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
    [Tooltip("ชุด Object ชื่อของแต่ละหน้า (เรียงตามลำดับแมว/หน้าในสมุด)")]
    public GameObject[] nameObjects;
    [Tooltip("ชุด Object ของ Label ตามระดับความสัมพันธ์ (เรียงจากระดับต่ำไปสูง)")]
    public GameObject[] labelObjects;
    [Tooltip("ขอบเขตค่า Fill ขั้นต่ำของแต่ละระดับ (เช่น 0, 0.34, 0.67 — ถ้าปล่อยว่างจะหารเท่ากันตามจำนวน labelObjects)")]
    public float[] labelThresholds;
    public Image relBarFill;
    public TMP_Text pageText;

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
            Vector2 nav = GameInput.Navigate;
            if (Input.GetKeyDown(prevKey) || Input.GetKeyDown(KeyCode.LeftArrow) || GameInput.PadPrevDown || nav.x < -0.5f)
            { PrevCat(); navCooldown = NAV_COOLDOWN_TIME; }
            else if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.RightArrow) || GameInput.PadNextDown || nav.x > 0.5f)
            { NextCat(); navCooldown = NAV_COOLDOWN_TIME; }
        }

        if (Input.GetKeyDown(KeyCode.Escape) || GameInput.PadBackDown) { GameInput.MarkBackHandled(); CloseBook(); }
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
        UpdateNameVisuals(currentIndex);

        float fillValue = 0f;
        if (relBarFill != null)
        {
            float currentRel = RelationshipManager.Instance.GetRelationship(cat.catID);
            float maxRel = cat.maxRelationship;
            fillValue = maxRel > 0f ? Mathf.Clamp01((float)currentRel / (float)maxRel) : 0f;
            relBarFill.fillAmount = fillValue;
        }

        UpdateLabelVisualsByFill(fillValue);

        if (pageText != null) pageText.text = $"{currentIndex + 1} / {cats.Count}";

        bool moreThanOne = cats.Count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(moreThanOne);
        if (nextButton != null) nextButton.gameObject.SetActive(moreThanOne);
    }

    private void UpdateNameVisuals(int pageIndex)
    {
        if (nameObjects == null || nameObjects.Length == 0)
        {
            Debug.LogWarning("[RelationshipBookUI] nameObjects ว่างหรือยังไม่ได้ตั้งค่า");
            return;
        }

        for (int i = 0; i < nameObjects.Length; i++)
        {
            if (nameObjects[i] == null) continue;
            bool shouldBeActive = (i == pageIndex);
            nameObjects[i].SetActive(shouldBeActive);
        }

        if (pageIndex < nameObjects.Length && nameObjects[pageIndex] != null)
            Debug.Log($"[RelationshipBookUI] Page {pageIndex} -> Name activeSelf={nameObjects[pageIndex].activeSelf}, activeInHierarchy={nameObjects[pageIndex].activeInHierarchy}");
    }

    private void UpdateLabelVisualsByFill(float fill)
    {
        if (labelObjects == null || labelObjects.Length == 0)
        {
            Debug.LogWarning("[RelationshipBookUI] labelObjects ว่างหรือยังไม่ได้ตั้งค่า");
            return;
        }

        int activeIndex = GetLabelIndexFromFill(fill, labelObjects.Length);

        for (int i = 0; i < labelObjects.Length; i++)
        {
            if (labelObjects[i] == null) continue;
            labelObjects[i].SetActive(i == activeIndex);
        }

        if (activeIndex < labelObjects.Length && labelObjects[activeIndex] != null)
            Debug.Log($"[RelationshipBookUI] Fill={fill:0.00}, LabelIndex={activeIndex} -> activeSelf={labelObjects[activeIndex].activeSelf}, activeInHierarchy={labelObjects[activeIndex].activeInHierarchy}");
    }

    private int GetLabelIndexFromFill(float fill, int count)
    {
        if (count <= 1) return 0;

        // ถ้าตั้ง labelThresholds ไว้ ให้ใช้ค่าขอบเขตตามที่กำหนด (เช่น [0, 0.34, 0.67])
        if (labelThresholds != null && labelThresholds.Length == count)
        {
            int chosen = 0;
            for (int i = 0; i < labelThresholds.Length; i++)
            {
                if (fill >= labelThresholds[i])
                    chosen = i;
            }
            return chosen;
        }

        // ถ้าไม่ได้ตั้ง labelThresholds ให้แบ่งช่วงเท่าๆ กันตามจำนวน Label
        int index = Mathf.FloorToInt(fill * count);
        return Mathf.Clamp(index, 0, count - 1);
    }
}