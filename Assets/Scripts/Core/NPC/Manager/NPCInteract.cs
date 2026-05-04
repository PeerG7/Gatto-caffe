using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;

    public bool isInStore = false;

// เพิ่มตัวแปรเหล่านี้ใน NPCInteract.cs
[Header("Audio Settings")]
public AudioSource audioSource; 
public AudioClip meowSound;    

void Awake()
{
    npc = GetComponent<NPCController>();
    // ค้นหา AudioSource อัตโนมัติถ้าไม่ได้ลากใส่
    if (audioSource == null) audioSource = GetComponent<AudioSource>();
}

// สร้าง Method ใหม่สำหรับสั่งให้แมวร้อง
public void PlayMeow()
{
    if (audioSource != null && meowSound != null)
    {
        audioSource.PlayOneShot(meowSound);
        Debug.Log("Playing Meow Sound!");
    }
    else
    {
        Debug.LogError("AudioSource or MeowSound is missing!");
    }
}

    // 🎯 ใช้เช็คเปิด QTE
    public bool CanInteract()
    {
        return npc != null && npc.currentState == NPCController.NPCState.Sitting;
    }

    // 🤖 Invite
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
        // ค้นหา NPCController ที่อยู่ในตัวแมวตัวนี้แล้วสั่งให้หาโต๊ะ
        NPCController controller = GetComponent<NPCController>();
        if (controller != null)
        {
            controller.GoToTableDirectly();
        }
    }

// ❤️ QTE / Relationship
    public void RelationShip()
    {
        if (npc == null) return;

        if (npc.currentState != NPCController.NPCState.Sitting)
        {
            Debug.Log("❌ NPC not sitting");
            return;
        }

        // ✅ เพิ่มส่วนการเล่นเสียงตรงนี้
        if (audioSource != null && meowSound != null)
        {
            audioSource.PlayOneShot(meowSound);
        }

        Debug.Log("❤️ OPEN RELATIONSHIP");

        RelationshipManager.Instance.SetCurrentNPC(npc);
        SceneLoader.Instance.LoadRelationshipScene();
    }
}