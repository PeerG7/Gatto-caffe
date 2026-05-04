using UnityEngine;

public class RelationshipMinigame : MonoBehaviour
{
    public int maxInteraction = 3;
    private int currentInteraction = 0;
    public float relationshipGain = 10f;

    [Header("SFX")]
    public AudioSource sfxSource;     // AudioSource ใน RelationshipScene
    public AudioClip meowClip;        // ลากไฟล์เสียงแมวมาใส่

    void Awake()
    {
        // Canvas ไม่มี AudioSource ในตัว — หาบน GameObject นี้ก่อน
        // ถ้าไม่มีให้สร้างขึ้นมาอัตโนมัติ ไม่ต้องลากใส่ใน Inspector
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void OnEnable()
    {
        currentInteraction = 0;
    }

    public void OnClickCat()
    {
        if (currentInteraction >= maxInteraction) return;

        currentInteraction++;

        // ✅ เล่นเสียงแมวทันทีที่ค่าความสัมพันธ์เพิ่ม
        if (sfxSource != null && meowClip != null)
            sfxSource.PlayOneShot(meowClip);

        // ✅ เพิ่มค่าความสัมพันธ์ผ่าน RelationshipManager
        //    เพื่อให้บันทึก PlayerPrefs และผ่าน maxRelationship clamp ด้วย
        var npc = RelationshipManager.Instance.GetCurrentNPC();
        if (npc != null)
        {
            var relation = npc.GetComponent<NPCRelationship>();
            if (relation != null)
                RelationshipManager.Instance.AddRelationship(relation.npcName, relationshipGain);
        }

        Debug.Log($"❤️ +{relationshipGain} Relationship | ({currentInteraction}/{maxInteraction})");

        if (currentInteraction >= maxInteraction)
            Debug.Log("✅ Interaction Limit Reached");
    }

    public void BackToShop()
    {
        var npc = RelationshipManager.Instance.GetCurrentNPC();
        if (npc != null) npc.GoExit();

        RelationshipManager.Instance.ClearNPC();
        SceneLoader.Instance.CloseRelationshipScene();
    }
}