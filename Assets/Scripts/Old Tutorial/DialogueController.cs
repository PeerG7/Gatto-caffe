using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance;

    [Header("UI Components")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;

    [Header("Settings")]
    public float typingSpeed = 0.04f;

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

    void Update()
    {
        // 🟢 เพิ่มตรงนี้: ให้เช็กว่าถ้ากล่องข้อความเปิดอยู่ และผู้เล่นคลิกเมาส์ซ้าย หรือกด Space/Enter
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                DisplayNextSentence();
            }
        }
    }

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
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = currentSentence;
            isTyping = false;
            return;
        }

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

        // ตรงนี้สามารถเรียกให้ไปทำขั้นตอนต่อไปได้ (เช่น โหลดฉาก Tutorial ต่อไป)
    }
}