using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CatChatBubble : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bubbleCanvas;      // Canvas/Panel รูปก้อนเมฆ
    public Image iconImage;              // Image ตรงกลางก้อนเมฆ
    public Sprite[] chatIcons;           // สุ่มรูปไอคอนสนทนา (ปลา, แก้วน้ำ, หัวใจ ฯลฯ)
    public float displayDuration = 3.5f; // ระยะเวลาแสดงผล

    private Coroutine hideCoroutine;

    void Start()
    {
        if (bubbleCanvas != null) bubbleCanvas.SetActive(false);
    }

    public void ShowChatBubble()
    {
        if (bubbleCanvas == null || chatIcons == null || chatIcons.Length == 0) return;

        // สุ่มไอคอน 1 รูป
        Sprite randomSprite = chatIcons[Random.Range(0, chatIcons.Length)];
        if (iconImage != null) iconImage.sprite = randomSprite;

        bubbleCanvas.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        yield return new WaitForSeconds(displayDuration);
        if (bubbleCanvas != null) bubbleCanvas.SetActive(false);
    }
}