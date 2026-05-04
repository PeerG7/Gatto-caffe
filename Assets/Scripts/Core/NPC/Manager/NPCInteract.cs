using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;

    public bool isInStore = false;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip meowSound;

    void Awake()
    {
        npc = GetComponent<NPCController>();
        // ค้นหา AudioSource อัตโนมัติถ้าไม่ได้ลากใส่
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    // เล่นเสียงแมวโดยตรง (เรียกจากภายนอกได้)
    public void PlayMeow()
    {
        if (audioSource != null && meowSound != null)
        {
            audioSource.PlayOneShot(meowSound);
            Debug.Log("🔊 Playing Meow Sound!");
        }
        else
        {
            Debug.LogError("AudioSource or MeowSound is missing on " + gameObject.name);
        }
    }

    // ✅ true = NPC กำลังนั่งอยู่ → พร้อมเปิด Relationship Minigame
    public bool CanInteract()
    {
        return npc != null && npc.currentState == NPCController.NPCState.Sitting;
    }

    // 🤖 Invite NPC จาก Queue เข้าร้าน
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
            npc.GoToTableDirectly();
        }
    }

    public void GoToTableDirectly()
    {
        NPCController controller = GetComponent<NPCController>();
        if (controller != null)
        {
            controller.GoToTableDirectly();
        }
    }

    // ❤️ เปิด Relationship Minigame
    public void RelationShip()
    {
        if (npc == null) return;

        if (npc.currentState != NPCController.NPCState.Sitting)
        {
            Debug.Log("❌ NPC not sitting");
            return;
        }

        Debug.Log("❤️ OPEN RELATIONSHIP for: " + gameObject.name);

        RelationshipManager.Instance.SetCurrentNPC(npc);
        SceneLoader.Instance.LoadRelationshipScene();
    }
}