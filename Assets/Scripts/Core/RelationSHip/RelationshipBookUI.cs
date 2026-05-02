using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =====================================================================
// Page-based Relationship Book — ไม่ใช้ ScrollRect หรือ spawn prefab
// แสดงแมวทีละตัว กด Prev/Next เพื่อเปลี่ยนหน้า
//
// Setup ใน Scene (ทำใน BookCanvas เลย ไม่ต้องมี prefab):
//   - Portrait    : Image แสดงรูปแมว
//   - NameText    : TMP_Text ชื่อแมว
//   - LabelText   : TMP_Text เช่น "Friend"
//   - RelBarFill  : Image type Filled (progress bar)
//   - PageText    : TMP_Text เช่น "1 / 3"
//   - PrevButton  : Button ลูกศรซ้าย
//   - NextButton  : Button ลูกศรขวา
//   - CloseButton : Button ปิด
// =====================================================================
public class RelationshipBookUI : MonoBehaviour
{
    [Header("Book Canvas")]
    public GameObject bookCanvas;

    [Header("Display Elements — ผูกตรงๆ ใน Inspector")]
    public Image portrait;
    public TMP_Text nameText;
    public TMP_Text labelText;
    public Image relBarFill;
    public TMP_Text pageText;     // แสดง "1 / 3"

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

    private List<CatRelationshipData> cats;
    private int currentIndex = 0;

    void Start()
    {
        if (bookCanvas != null) bookCanvas.SetActive(false);
        if (prevButton != null) prevButton.onClick.AddListener(PrevCat);
        if (nextButton != null) nextButton.onClick.AddListener(NextCat);
        if (closeButton != null) closeButton.onClick.AddListener(CloseBook);
    }

    public void OpenBook()
    {
        if (RelationshipManager.Instance == null) return;

        cats = RelationshipManager.Instance.GetAllCats();
        if (cats == null || cats.Count == 0) return;

        currentIndex = 0;

        if (bookCanvas != null) bookCanvas.SetActive(true);
        PlayerController2D.IsLocked = true;

        ShowCurrentCat();
    }

    public void CloseBook()
    {
        if (bookCanvas != null) bookCanvas.SetActive(false);
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

        // ซ่อนปุ่มถ้ามีแมวแค่ตัวเดียว
        bool moreThanOne = cats.Count > 1;
        if (prevButton != null) prevButton.gameObject.SetActive(moreThanOne);
        if (nextButton != null) nextButton.gameObject.SetActive(moreThanOne);
    }
}