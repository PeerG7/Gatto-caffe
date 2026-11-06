using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public enum TutorialState { None, Cutscene, Dialogue, PlayerWalkIn, TeachBuyFurniture, TeachInviteCat, TeachCooking, TeachServeAndInteract, Completed }
    public TutorialState currentState;

    [Header("UI & Spotlights")]
    public GameObject darkDimPanel; // Panel สีดำโปร่งแสง คลุมทั้งจอ
    public GameObject menuSpotlightUI; // วงกลมสลัวๆ ที่จะเอามา Focus เมนู

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // เริ่มต้นด้วยการเล่น Cutscene หรือ Dialogue เปิดฉาก
        StartCoroutine(RunTutorialSequence());
    }

    IEnumerator RunTutorialSequence()
    {
        currentState = TutorialState.Cutscene;
        // 1. เล่น Cutscene เปิดร้าน
        yield return new WaitForSeconds(2f);

        currentState = TutorialState.Dialogue;
        // 2. แสดงบทสนทนาต้อนรับ
        // รอจนกว่าผู้เล่นกดคลิกจบ Dialogue...

        currentState = TutorialState.PlayerWalkIn;
        // 3. สั่งให้ Player เดินเข้าฉากมาประจำที่ในร้าน Cafe

        // 4. เข้าสู่โหมดสอนซื้อโต๊ะ / เฟอร์นิเจอร์
        StartTeachBuyFurniture();
    }

    public void StartTeachBuyFurniture()
    {
        currentState = TutorialState.TeachBuyFurniture;
        Debug.Log("[Tutorial] สอนวิธีซื้อโต๊ะหรือเฟอร์นิเจอร์ (เปิด Shop หรือกดซื้อ)");
        // รอให้ผู้เล่นซื้อเฟอร์นิเจอร์สำเร็จ แล้วเรียก NextStep()
    }

    public void StartTeachInviteCat()
    {
        currentState = TutorialState.TeachInviteCat;
        Debug.Log("[Tutorial] สอนวิธีเชิญแมวเข้าร้าน (ใช้ AutoInviter หรือคลิกเรียก)");
    }

    public void StartTeachCooking(Transform menuTarget)
    {
        currentState = TutorialState.TeachCooking;
        Debug.Log("[Tutorial] เริ่มสอนทำอาหาร - เปิดโหมดมืดสลัวและวงกลมโฟกัสเมนู");

        if (darkDimPanel != null) darkDimPanel.SetActive(true);
        if (menuSpotlightUI != null)
        {
            menuSpotlightUI.SetActive(true);
            // ย้ายตำแหน่งวงกลม Spotlight ไปที่เมนูที่แมวสั่ง
            menuSpotlightUI.transform.position = menuTarget.position;
        }
    }

    public void OnCookingFinished()
    {
        if (darkDimPanel != null) darkDimPanel.SetActive(false);
        if (menuSpotlightUI != null) menuSpotlightUI.SetActive(false);

        StartTeachServeAndInteract();
    }

    public void StartTeachServeAndInteract()
    {
        currentState = TutorialState.TeachServeAndInteract;
        Debug.Log("[Tutorial] สอนวิธีหยิบอาหารไป Serve ที่โต๊ะ และ Interact กับแมว");
    }

    public void CompleteTutorial()
    {
        currentState = TutorialState.Completed;
        Debug.Log("[Tutorial] จบ Tutorial เรียบร้อย เริ่มเกมเพลย์ปกติ!");
    }
}