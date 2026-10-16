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
    public AudioClip happySound;
    public AudioClip sadSound;
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
        List<NPCController> groupMembers = new List<NPCController>();
        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        foreach (var n in allNPCs)
        {
            if (n != null && n.groupID == targetGroupID)
            {
                groupMembers.Add(n);
            }
        }

        foreach (var member in groupMembers)
        {
            if (member != null)
            {
                if (member.queuePatienceBarRoot != null) member.queuePatienceBarRoot.SetActive(false);
                NPCInteract inter = member.GetComponent<NPCInteract>();
                if (inter != null) inter.PlayAngry();
            }
        }

        yield return new WaitForSeconds(2f);

        foreach (var member in groupMembers)
        {
            // 🟢 ตรวจสอบว่าแมวยังไม่ถูก Destroy ก่อนสั่ง Remove และ GoExit
            if (member != null)
            {
                if (QueueManager.Instance != null)
                {
                    QueueManager.Instance.RemoveFromQueue(member);
                }
                member.GoExit();
            }
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