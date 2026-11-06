using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IntroSequenceManager : MonoBehaviour
{
    public static IntroSequenceManager Instance;

    public enum IntroStep
    {
        None,
        Step1_OpeningDialogue,   // 1. กด New Game แล้วมี UI เด้งคุยเปิดฉาก (กำลังทำอันนี้)
        Step2_PlayerWalkIn,      // 2. ผู้เล่นเดินเข้าฉากมาในร้าน Cafe (ยังไม่ได้ทำ)
        Step3_TeachBuyFurniture, // 3. สอนซื้อเฟอร์นิเจอร์/โต๊ะ (ยังไม่ได้ทำ)
        Step4_TeachInviteAndCook,// 4. สอนเชิญแมวและทำอาหาร (ยังไม่ได้ทำ)
        Completed                // จบอินโทร เริ่มเล่นเกมปกติ
    }

    public IntroStep currentStep = IntroStep.None;

    [Header("UI References")]
    public GameObject dialogueBoxUI; // หน้าต่าง UI บทสนทนาแบบ Stardew
    public GameObject darkDimPanel;  // ฉากหลังมืดสลัว (ถ้าต้องการใช้เน้นโฟกัส)

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // เริ่มต้นเกมใหม่ให้เรียกขั้นตอนที่ 1 ทันที
        StartIntroSequence();
    }

    public void StartIntroSequence()
    {
        currentStep = IntroStep.Step1_OpeningDialogue;
        TriggerStep1_Dialogue();
    }

    // ──────────────────────────────────────────
    //  ขั้นตอนที่ 1: บทสนทนาเปิดเกม (Dialogue UI เด้งขึ้นมา)
    // ──────────────────────────────────────────
    void TriggerStep1_Dialogue()
    {
        Debug.Log("[Intro] ขั้นตอนที่ 1: แสดง UI บทสนทนาเปิดเกมสไตล์ Stardew");
        if (dialogueBoxUI != null) dialogueBoxUI.SetActive(true);

        // ผูกปุ่มกด "Next" ในบทสนทนา เมื่อกดจนจบบทสนทนา ให้สั่งข้ามไปขั้นตอนที่ 2
    }

    public void OnDialogueFinished()
    {
        if (dialogueBoxUI != null) dialogueBoxUI.SetActive(false);

        // เมื่อคุยเสร็จ ไปต่อขั้นตอนที่ 2
        ProceedToStep2_PlayerWalkIn();
    }

    // ──────────────────────────────────────────
    //  ขั้นตอนที่ 2: Player เดินเข้าฉากมาในร้าน Cafe (เตรียมทำต่อ)
    // ──────────────────────────────────────────
    void ProceedToStep2_PlayerWalkIn()
    {
        currentStep = IntroStep.Step2_PlayerWalkIn;
        Debug.Log("[Intro] ขั้นตอนที่ 2: สั่งให้ Player เดินเข้าฉากมาประจำที่ในร้าน Cafe");

        // TODO: เขียนโค้ดสั่งให้ Player เดินทางเข้าหน้าจอ (เช่น ใช้ NavMesh หรือ MoveTowards)
        // เมื่อเดินถึงจุดที่กำหนด ค่อยปลดล็อกให้ไปขั้นตอนที่ 3 ต่อ
    }

    // ──────────────────────────────────────────
    //  ขั้นตอนที่ 3: สอนซื้อ Furniture / โต๊ะ (เตรียมทำต่อ)
    // ──────────────────────────────────────────
    public void ProceedToStep3_TeachFurniture()
    {
        currentStep = IntroStep.Step3_TeachBuyFurniture;
        Debug.Log("[Intro] ขั้นตอนที่ 3: สอนผู้เล่นซื้อโต๊ะหรือเฟอร์นิเจอร์");

        // TODO: เปิดไฟ Spotlight โฟกัสไปที่ปุ่มเปิดร้าน/ซื้อของ
    }

    // ──────────────────────────────────────────
    //  ขั้นตอนที่ 4: สอนเชิญแมวและทำอาหาร
    // ──────────────────────────────────────────
    public void ProceedToStep4_TeachCooking()
    {
        currentStep = IntroStep.Step4_TeachInviteAndCook;
        Debug.Log("[Intro] ขั้นตอนที่ 4: สอนเชิญแมว ทำอาหาร และเสิร์ฟ");
    }

    public void CompleteIntro()
    {
        currentStep = IntroStep.Completed;
        Debug.Log("[Intro] จบขั้นตอนเริ่มต้น เข้าสู่ Main Stage เต็มรูปแบบ!");
    }
}