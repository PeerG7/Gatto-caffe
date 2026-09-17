using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;

    public bool isInStore = false;

    [Header("Relationship Canvas")]
    [Tooltip("ลาก RelationshipCanvas (GameObject ใน Scene) มาใส่ตรงนี้ทุก NPC Prefab")]
    public GameObject relationshipCanvas;

    [Header("Audio Settings (เสียงเฉพาะตัว NPC นี้ — Optional)")]
    public AudioSource audioSource;
    public AudioClip meowSound;
    public AudioClip angrySound;

    void Awake()
    {
        npc = GetComponent<NPCController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void PlayMeow()
    {
        if (audioSource != null && meowSound != null)
            audioSource.PlayOneShot(meowSound);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayMeow();
        else
            Debug.LogWarning("PlayMeow: ไม่มี AudioSource หรือ AudioManager บน " + gameObject.name);
    }

    public void PlayAngry()
    {
        if (audioSource != null && angrySound != null)
            audioSource.PlayOneShot(angrySound);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayAngry();
        else
            Debug.LogWarning("PlayAngry: ไม่มี AudioSource หรือ AudioManager บน " + gameObject.name);
    }

    public bool CanInteract()
    {
        return npc != null && npc.currentState == NPCController.NPCState.Sitting;
    }

    /// <summary>เช็คว่าแมวตัวนี้กำลังยืนรออยู่ที่ Interaction Zone (รอผู้เล่นกด E) หรือไม่</summary>
    public bool CanRequestZoneInteraction()
    {
        return npc != null && npc.CanRequestInteractionChoice();
    }

    /// <summary>เรียกจาก PlayerInteract2D ตอนกด E ใกล้แมวที่ยืนรออยู่ที่ Zone — เปิดเมนูเลือก QTE</summary>
    public void RequestZoneInteraction()
    {
        if (npc == null) return;
        npc.RequestInteractionChoice();
    }

    public void Interact()
    {
        if (npc == null) return;

        NPCController frontNPC = QueueManager.Instance.GetFrontNPC();
        if (frontNPC != npc)
        {
            Debug.Log("❌ Not front of queue");
            return;
        }

        if (npc.currentState == NPCController.NPCState.InQueue)
        {
            Debug.Log("✅ Invite NPC");
            isInStore = true;
            PlayMeow();
            npc.GoToTableDirectly();
        }
    }

    public void GoToTableDirectly()
    {
        NPCController controller = GetComponent<NPCController>();
        if (controller != null)
            controller.GoToTableDirectly();
    }

    public void RelationShip()
    {
        if (npc == null) return;

        if (npc.currentState != NPCController.NPCState.Sitting)
        {
            Debug.Log("❌ NPC not sitting — state: " + npc.currentState);
            return;
        }

        Debug.Log("❤️ OPEN RELATIONSHIP for: " + gameObject.name);

        RelationshipManager.Instance.SetCurrentNPC(npc);

        if (relationshipCanvas != null)
        {
            relationshipCanvas.SetActive(true);

            // ✅ Fix บั๊ก: ล็อก Player ตอนเปิด Relationship canvas
            //    (เดิมมีแค่ Pause DayNightManager แต่ Player ยังเดินได้)
            PlayerController2D.IsLocked = true;

            // ✅ Pause เฉพาะตอนที่ยังไม่ได้ pause อยู่
            if (DayNightManager.Instance != null && !DayNightManager.Instance.isPaused)
                DayNightManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogError("❌ relationshipCanvas is NULL บน " + gameObject.name +
                           " — กรุณาลาก RelationshipCanvas มาใส่ใน Inspector ของ NPCSpawner");
        }
    }
}