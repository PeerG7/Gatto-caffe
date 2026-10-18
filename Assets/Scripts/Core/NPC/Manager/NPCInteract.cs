using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCInteract : MonoBehaviour
{
    private NPCController npc;

    public bool isInStore = false;

    [Header("Relationship Canvas (ลาก RelationshipCanvas หรือ BookCanvas ใน Scene มาใส่ตรงนี้)")]
    public GameObject relationshipCanvas;

    [Header("Audio Settings (เสียงเฉพาะตัว NPC นี้ — Optional)")]
    public AudioSource audioSource;
    public AudioClip meowSound;
    public AudioClip angrySound;

    [Header("VIP Sounds (Optional — สำหรับแมว VIP ที่มีเสียงเฉพาะตัว)")]
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

    /// <summary>
    /// 🟢 เช็คว่าแมวตัวนี้อยู่ในจุด InteractionZone และอยู่ใน Playing Phase เรียบร้อยแล้วหรือไม่
    /// </summary>
    public bool CanRequestZoneInteraction()
    {
        if (npc == null) return false;

        // 🟢 2-PHASE GUARD: สิทธิ์เปิด QTE เกิดขึ้นได้เฉพาะเมื่ออยู่ใน PlayingPhase เท่านั้น
        return npc.CurrentPhase == NPCController.CatPhase.PlayingPhase &&
               npc.currentState == NPCController.NPCState.AtInteractionZone &&
               npc.isInteractionAllowed;
    }

    /// <summary>
    /// 🟢 เรียกสั่งเปิดหน้า UI Choice 3 ปุ่มผ่าน CatSystemManager
    /// </summary>
    public void RequestZoneInteraction()
    {
        if (npc == null) return;

        if (!CanRequestZoneInteraction())
        {
            Debug.LogWarning($"[NPCInteract] บล็อกการเปิด Canvas บน {gameObject.name} (currentState: {npc.currentState}, isAllowed: {npc.isInteractionAllowed})");
            PlayerController2D.SetLock(false);
            return;
        }

        npc.StopInteractionTimeout();
        CatSystemManager.Instance?.ShowInteractionChoice(npc);
    }

    public void Interact()
    {
        if (npc == null) return;

        if (npc.currentState != NPCController.NPCState.InQueue)
        {
            return;
        }

        NPCController frontNPC = QueueManager.Instance?.GetFrontNPC();

        bool isFrontOrSameGroup = (frontNPC == npc) ||
                                  (frontNPC != null && !string.IsNullOrEmpty(frontNPC.groupID) && frontNPC.groupID == npc.groupID);

        if (!isFrontOrSameGroup)
        {
            Debug.Log("❌ Not front of queue");
            return;
        }

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

    /// <summary>
    /// 🟢 เรียกเปิดหน้า Relationship (ตัดขาดโหมด Dining บล็อกไม่ให้เปิดระหว่างแมวนั่งโต๊ะอาหารเด็ดขาด)
    /// </summary>
    public void RelationShip()
    {
        if (npc == null) return;

        // 🔴 2-PHASE HARD GUARD: บล็อกการเปิด Relationship Canvas เด็ดขาดถ้า NPC ยังอยู่ใน Dining Phase (ช่วงสั่งอาหาร/อ่านเมนู/คุยกัน)
        if (npc.CurrentPhase != NPCController.CatPhase.PlayingPhase || !npc.isInteractionAllowed)
        {
            Debug.Log($"⏳ [NPCInteract] บล็อก Relationship! แมว {gameObject.name} ยังอยู่ในช่วง Dining Phase หรือยังไม่อยู่ ณ จุด Playing Zone");
            return;
        }

        // สั่งเคลียร์ปิด UI QTE ทั้งหมดใน Scene ทันที
        if (CatSystemManager.Instance != null)
        {
            CatSystemManager.Instance.HideAllQTEUI();
        }

        GameObject targetCanvas = null;
        if (RelationshipManager.Instance != null)
        {
            targetCanvas = RelationshipManager.Instance.relationshipCanvas;
        }

        if (targetCanvas == null)
        {
            Debug.LogError($"❌ [NPCInteract] ยังไม่ได้ลากใส่ RelationshipCanvas ใน RelationshipManager (Inspector)!");
            return;
        }

        if (targetCanvas.activeSelf) return;

        Debug.Log("❤️ OPEN RELATIONSHIP for: " + gameObject.name);

        RelationshipManager.Instance?.SetCurrentNPC(npc);

        targetCanvas.SetActive(true);
        PlayerController2D.SetLock(true);

        if (DayNightManager.Instance != null && !DayNightManager.Instance.isPaused)
            DayNightManager.Instance.PauseGame();
    }

    /// <summary>
    /// 🟢 สั่งปิดหน้า Relationship และ Resume เวลาในเกมกลับสู่สภาวะปกติ
    /// </summary>
    public void CloseRelationship()
    {
        GameObject targetCanvas = null;
        if (RelationshipManager.Instance != null)
        {
            targetCanvas = RelationshipManager.Instance.relationshipCanvas;
        }

        if (targetCanvas != null)
        {
            targetCanvas.SetActive(false);
            PlayerController2D.SetLock(false);

            if (DayNightManager.Instance != null && DayNightManager.Instance.isPaused)
            {
                DayNightManager.Instance.ResumeGame();
            }
        }
    }
}