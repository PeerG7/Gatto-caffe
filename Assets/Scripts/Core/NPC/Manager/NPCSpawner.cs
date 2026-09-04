using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance;

    public GameObject[] npcPrefabs;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("VIP Cat Settings")]
    [Tooltip("Prefab แมว VIP (สีพิเศษ) แยกจากแมวธรรมดา — ถ้าไม่ใส่ ระบบจะไม่มีแมว VIP spawn เลย")]
    public GameObject[] vipNpcPrefabs;
    [Range(0, 100)]
    [Tooltip("โอกาส % ที่จะ Spawn แมว VIP แทนแมวธรรมดาในแต่ละรอบ")]
    public int vipSpawnChance = 10;

    [Header("Scene References — Inject to NPC after Spawn")]
    [Tooltip("ลาก RelationshipCanvas (GameObject ใน Scene) มาใส่ตรงนี้ที่ NPCSpawner แทน")]
    public GameObject relationshipCanvas;

    public float spawnInterval = 5f;
    private float timer;
    public float minInterval = 3f;
    public float maxInterval = 7f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        spawnInterval = Random.Range(minInterval, maxInterval);
    }

    void Update()
    {
        // ✅ ใช้ DayNightManager แทน TimeManager
        bool isPaused = DayNightManager.Instance != null && DayNightManager.Instance.isPaused;
        bool isNotWork = DayNightManager.Instance != null && !DayNightManager.Instance.isWorkTime;

        if (isPaused || isNotWork) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnNPC();
            timer = 0f;
            spawnInterval = Random.Range(minInterval, maxInterval);
        }
    }

    void SpawnNPC()
    {
        if (npcPrefabs.Length == 0) return;

        // ✅ สุ่มว่ารอบนี้จะเป็นแมว VIP หรือแมวธรรมดา
        bool spawnVIP = vipNpcPrefabs.Length > 0 && Random.Range(0, 100) < vipSpawnChance;

        GameObject prefabToSpawn;
        if (spawnVIP)
        {
            int vipIndex = Random.Range(0, vipNpcPrefabs.Length);
            prefabToSpawn = vipNpcPrefabs[vipIndex];
        }
        else
        {
            int index = Random.Range(0, npcPrefabs.Length);
            prefabToSpawn = npcPrefabs[index];
        }

        GameObject npc = Instantiate(prefabToSpawn, spawnPoint.position, Quaternion.identity);

        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null) agent.Warp(spawnPoint.position);

        NPCController controller = npc.GetComponent<NPCController>();
        if (controller != null)
        {
            controller.exitPoint = exitPoint;

            // ✅ บังคับ flag ให้แน่ใจว่าเป็น VIP แม้ Prefab จะลืมติ๊ก isVIP ไว้
            if (spawnVIP) controller.isVIP = true;

            if (QueueManager.Instance != null) QueueManager.Instance.AddToQueue(controller);
        }

        // ✅ Inject relationshipCanvas จาก Scene เข้า NPC ที่ Spawn ใหม่
        NPCInteract interact = npc.GetComponent<NPCInteract>();
        if (interact != null)
        {
            if (relationshipCanvas != null)
                interact.relationshipCanvas = relationshipCanvas;
            else
                Debug.LogWarning("⚠️ NPCSpawner: relationshipCanvas ยังไม่ได้ assign — กรุณาลากมาใส่ใน Inspector");
        }
    }

    /// <summary>เรียกจาก FurnitureObject เมื่อ Upgrade โต๊ะเป็นหินอ่อนสำเร็จ — เพิ่มโอกาส Spawn แมว VIP ทั้งร้าน</summary>
    public void IncreaseVIPChance(int amount)
    {
        vipSpawnChance = Mathf.Clamp(vipSpawnChance + amount, 0, 100);
        Debug.Log($"✅ Marble Table Upgrade! VIP Spawn Chance เพิ่มขึ้นเป็น {vipSpawnChance}%");
    }
}