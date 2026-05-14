using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;

    public bool isInStore = false;

    [Header("Relationship Canvas")]
    [Tooltip("ลาก RelationshipCanvas (GameObject ใน Scene) มาใส่ตรงนี้ทุก NPC Prefab")]
    public GameObject relationshipCanvas;

    [Header("Audio Settings (เสียงเฉพาะตัว NPC นี้ — Optional)")]
    [Tooltip("ถ้าใส่จะใช้ AudioSource ของ NPC นี้แทน AudioManager")]
    public AudioSource audioSource;
    public AudioClip meowSound;     // ✅ เสียงแมวเฉพาะตัว (override AudioManager.sfxMeow)
    public AudioClip angrySound;    // ✅ เสียงโกรธเฉพาะตัว (override AudioManager.sfxAngry)

    void Awake()
    {
        npc = GetComponent<NPCController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    // ── SFX Helpers ───────────────────────────────────────

    /// <summary>เล่นเสียงแมวร้อง — ใช้ AudioSource ของ NPC ถ้ามี ไม่งั้นใช้ AudioManager</summary>
    public void PlayMeow()
    {
        if (audioSource != null && meowSound != null)
            audioSource.PlayOneShot(meowSound);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayMeow();
        else
            Debug.LogWarning("PlayMeow: ไม่มี AudioSource หรือ AudioManager บน " + gameObject.name);
    }

    /// <summary>เล่นเสียงโกรธ — ใช้ AudioSource ของ NPC ถ้ามี ไม่งั้นใช้ AudioManager</summary>
    public void PlayAngry()
    {
        if (audioSource != null && angrySound != null)
            audioSource.PlayOneShot(angrySound);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayAngry();
        else
            Debug.LogWarning("PlayAngry: ไม่มี AudioSource หรือ AudioManager บน " + gameObject.name);
    }

    // ── State Checks ──────────────────────────────────────

    /// <summary>true = NPC กำลังนั่งอยู่ → พร้อมเปิด Relationship Minigame</summary>
    public bool CanInteract()
    {
        return npc != null && npc.currentState == NPCController.NPCState.Sitting;
    }

    // ── Interactions ─────────────────────────────────────

    /// <summary>Invite NPC จาก Queue เข้าร้าน + เล่นเสียงแมวร้อง</summary>
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

            // ✅ เล่นเสียงแมวร้องตอนถูกเรียกเข้าร้าน
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

    /// <summary>เปิด Relationship Canvas (ไม่ใช้ SceneLoader แล้ว)</summary>
    public void RelationShip()
    {
        if (npc == null) return;

        // ✅ Guard: NPC ต้องนั่งอยู่ก่อนเท่านั้น
        if (npc.currentState != NPCController.NPCState.Sitting)
        {
            Debug.Log("❌ NPC not sitting — state: " + npc.currentState);
            return;
        }

        Debug.Log("❤️ OPEN RELATIONSHIP for: " + gameObject.name);

        RelationshipManager.Instance.SetCurrentNPC(npc);

        // ✅ เปิด Canvas แทน LoadScene
        if (relationshipCanvas != null)
        {
            relationshipCanvas.SetActive(true);
            TimeManager.Instance.PauseGame();
        }
        else
        {
            Debug.LogError("❌ relationshipCanvas is NULL บน " + gameObject.name +
                           " — กรุณาลาก RelationshipCanvas มาใส่ใน Inspector");
        }
    }
}