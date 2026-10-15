using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("VIP Sounds (Optional — สำหรับแมว VIP เช่น Alien ที่มีเสียงเฉพาะตัว)")]
    [Tooltip("cat_happy_alien เป็นต้น — ถ้าไม่ใส่ จะใช้เสียง default จาก AudioManager แทน")]
    public AudioClip happySound;
    [Tooltip("cat_sad_new_alien เป็นต้น")]
    public AudioClip sadSound;
    [Tooltip("cat_wants_food_alien เป็นต้น")]
    public AudioClip wantsFoodSound;

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

    public void PlayHappy()
    {
        if (audioSource != null && happySound != null)
            audioSource.PlayOneShot(happySound);
        else if (AudioManager.instance != null)
            AudioManager.instance.Cathappy();
        else
            Debug.LogWarning("PlayHappy: ไม่มี AudioSource หรือ AudioManager บน " + gameObject.name);
    }

    public void PlaySad()
    {
        if (audioSource != null && sadSound != null)
            audioSource.PlayOneShot(sadSound);
        else if (AudioManager.instance != null)
            AudioManager.instance.Catsad();
        else
            Debug.LogWarning("PlaySad: ไม่มี AudioSource หรือ AudioManager บน " + gameObject.name);
    }

    public void PlayWantsFood()
    {
        if (audioSource != null && wantsFoodSound != null)
            audioSource.PlayOneShot(wantsFoodSound);
        else if (AudioManager.instance != null)
            AudioManager.instance.PlayWantsFood();
        else
            Debug.LogWarning("PlayWantsFood: ไม่มี AudioSource หรือ AudioManager บน " + gameObject.name);
    }

    public bool CanInteract()
    {
        return npc != null && npc.currentState == NPCController.NPCState.Sitting;
    }

    public bool CanRequestZoneInteraction()
    {
        return npc != null && npc.CanRequestInteractionChoice();
    }

    public void RequestZoneInteraction()
    {
        if (npc == null) return;
        npc.RequestInteractionChoice();
    }

    public void Interact()
    {
        if (npc == null) return;

        NPCController frontNPC = QueueManager.Instance.GetFrontNPC();

        // อนุญาตให้คลิกต้อนรับได้ ถ้าตัวที่คลิกคือหัวแถว หรือเป็นเพื่อนร่วมกลุ่มที่ต่อแถวอยู่
        bool isFrontOrSameGroup = (frontNPC == npc) ||
                                  (frontNPC != null && !string.IsNullOrEmpty(frontNPC.groupID) && frontNPC.groupID == npc.groupID);

        if (!isFrontOrSameGroup)
        {
            Debug.Log("❌ Not front of queue"); 
            return;
        }

        if (npc.currentState == NPCController.NPCState.InQueue)
        {
            int groupSize = npc.GetGroupSize();

            // ถ้าเป็นกลุ่ม 2 ตัวขึ้นไป แต่ไม่มีโต๊ะที่นั่งเพียงพอว่างอยู่เลย
            if (groupSize >= 2 && !NPCController.HasAvailableTableForGroup(groupSize))
            {
                Debug.Log("❌ ไม่มีโต๊ะเพียงพอสำหรับกลุ่มนี้ — เล่นอนิเมชันหงุดหงิดแล้วเดินออก"); 
                StartCoroutine(HandleGroupRejectAndExit(frontNPC.groupID));
                return;
            }

            Debug.Log("✅ Invite NPC Group"); 
            PlayMeow();

            if (QueueManager.Instance != null)
            {
                QueueManager.Instance.InviteGroup(frontNPC);
            }
            else
            {
                npc.GoToTableDirectly();
            }
        }
    }

    private IEnumerator HandleGroupRejectAndExit(string targetGroupID)
    {
        // ค้นหาแมวทุกตัวในกลุ่มที่มี groupID นี้
        List<NPCController> groupMembers = new List<NPCController>();
        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (var n in allNPCs)
        {
            if (n != null && n.groupID == targetGroupID)
            {
                groupMembers.Add(n);
            }
        }

        // ปิดหลอดรอคิว และสั่งเล่นเสียง/อนิเมชันหงุดหงิดของทุกคนในกลุ่มพร้อมกัน
        foreach (var member in groupMembers)
        {
            if (member.queuePatienceBarRoot != null) member.queuePatienceBarRoot.SetActive(false);
            NPCInteract inter = member.GetComponent<NPCInteract>();
            if (inter != null) inter.PlayAngry();
        }

        // ยืนรอ 2 วินาที
        yield return new WaitForSeconds(2f);

        // นำออกจากคิวและสั่งเดินออกพร้อมกันทั้งคู่
        foreach (var member in groupMembers)
        {
            if (QueueManager.Instance != null)
            {
                QueueManager.Instance.RemoveFromQueue(member);
            }
            member.GoExit();
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
            PlayerController2D.SetLock(true);

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