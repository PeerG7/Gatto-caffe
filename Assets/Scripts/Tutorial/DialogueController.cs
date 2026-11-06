using System.Collections;
using System.Collections.Generic;
using TMPro; // ใช้ TextMeshPro
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance;

    [Header("UI Components")]
    public GameObject dialoguePanel;       // ลาก Panel กล่องข้อความมาใส่
    public TextMeshProUGUI dialogueText;   // ลาก Text ("New Text") มาใส่
    public Image portraitImage;            // ลาก Image ช่องรูปภาพมาใส่

    [Header("Settings")]
    public float typingSpeed = 0.04f;      // ความเร็วในการพิมพ์ข้อความทีละตัวอักษร

    private Queue<string> sentences = new Queue<string>();
    private bool isTyping = false;
    private string currentSentence = "";
    private Coroutine typingCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // ฟังก์ชันเริ่มบทสนทนา (ส่ง Array ของประโยคเข้ามา)
    public void StartDialogue(string[] newSentences, Sprite portrait = null)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (portraitImage != null)
        {
            if (portrait != null)
            {
                portraitImage.gameObject.SetActive(true);
                portraitImage.sprite = portrait;
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        sentences.Clear();
        foreach (string sentence in newSentences)
        {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        // ถ้ากำลังพิมพ์อยู่แล้วผู้เล่นกดคลิก/กดปุ่ม ให้แสดงข้อความทั้งหมดทันที
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = currentSentence;
            isTyping = false;
            return;
        }

        // ถ้าหมดประโยคแล้ว ให้ปิดกล่องข้อความ
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    void EndDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        Debug.Log("[Dialogue] จบบทสนทนาแล้ว!");

        // ตรงนี้สามารถเรียกให้ไปทำขั้นตอนต่อไป (เช่น เริ่มเข้าสเต็ปที่ 2) ได้เลย
        // เช่น IntroSequenceManager.Instance.OnDialogueFinished();
    }
}