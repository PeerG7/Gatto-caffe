using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance;

    public GameObject[] npcPrefabs;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Group Spawn Settings")]
    [Range(0, 100)] public int groupSpawnChance = 40;
    public int minGroupSize = 2;
    public int maxGroupSize = 2;

    [Header("VIP Cat Settings")]
    public GameObject[] vipNpcPrefabs;
    [Range(0, 100)] public int vipSpawnChance = 10;
    [Range(0, 100)] public int maxVipSpawnChance = 40;

    [Header("Tutorial Spawn Settings (ตั้งค่าแมวเจาะจงช่วง Tutorial)")]
    [Tooltip("Element 0 = แมวตัวที่ 1 | Element 1 = แมวตัวที่ 2 (SHO IKEDA)")]
    public GameObject[] tutorialOrderedPrefabs;

    private bool cat1Spawned = false;
    private bool cat2Spawned = false;

    [Header("Scene References")]
    public GameObject relationshipCanvas;

    public float spawnInterval = 5f;
    private float timer;
    public float minInterval = 3f;
    public float maxInterval = 7f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[NPCSpawner] พบ Spawner ซ้ำ ลบตัวนี้ทิ้ง: " + name);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() => spawnInterval = Random.Range(minInterval, maxInterval);

    void Update()
    {
        if (MainQuestManager.Instance != null)
        {
            var currentQ = MainQuestManager.Instance.currentQuest;

            if (currentQ == MainQuestManager.QuestStep.InviteCat1 && !cat1Spawned)
            {
                if (tutorialOrderedPrefabs != null && tutorialOrderedPrefabs.Length > 0 && tutorialOrderedPrefabs[0] != null)
                    SpawnSpecificNPC(tutorialOrderedPrefabs[0], System.Guid.NewGuid().ToString());
                else
                    Debug.LogError("[NPCSpawner] ยังไม่ได้ใส่ Tutorial Prefab ตัวที่ 1 (Element 0)!");

                cat1Spawned = true;
                return;
            }

            // 2. สเต็ป InviteCat2: เสกแมวตัวที่ 2 (SHO IKEDA)
            if (currentQ == MainQuestManager.QuestStep.InviteCat2 && !cat2Spawned)
            {
                if (tutorialOrderedPrefabs != null && tutorialOrderedPrefabs.Length > 1 && tutorialOrderedPrefabs[1] != null)
                {
                    SpawnSpecificNPC(tutorialOrderedPrefabs[1], System.Guid.NewGuid().ToString());
                    Debug.Log("[NPCSpawner] 🐱 แมวตัวที่ 2 (SHO IKEDA) ปรากฏตัวแล้ว!");
                }
                cat2Spawned = true;
                return;
            }

            if (currentQ != MainQuestManager.QuestStep.Completed)
            {
                return;
            }
        }

        // --- การทำงานปกติหลังจบ Tutorial ---
        bool isPaused = DayNightManager.Instance != null && DayNightManager.Instance.isPaused;
        bool isNotWork = DayNightManager.Instance != null && !DayNightManager.Instance.isWorkTime;

        if (isPaused || isNotWork) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnNPCGroupLogic();
            timer = 0f;
            spawnInterval = Random.Range(minInterval, maxInterval);
        }
    }

    void SpawnNPCGroupLogic()
    {
        bool isGroup = Random.Range(0, 100) < groupSpawnChance;
        int count = isGroup ? Random.Range(minGroupSize, maxGroupSize + 1) : 1;
        string newGroupID = System.Guid.NewGuid().ToString();

        for (int i = 0; i < count; i++)
        {
            SpawnSingleNPC(newGroupID);
        }
    }

    NPCController SpawnSingleNPC(string groupID)
    {
        if (npcPrefabs.Length == 0) return null;

        bool spawnVIP = vipNpcPrefabs.Length > 0 && Random.Range(0, 100) < vipSpawnChance;
        GameObject prefabToSpawn = spawnVIP
            ? vipNpcPrefabs[Random.Range(0, vipNpcPrefabs.Length)]
            : npcPrefabs[Random.Range(0, npcPrefabs.Length)];

        return SpawnSpecificNPC(prefabToSpawn, groupID);
    }

    NPCController SpawnSpecificNPC(GameObject prefabToSpawn, string groupID)
    {
        if (prefabToSpawn == null) return null;

        Vector3 spawnOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0);
        GameObject npc = Instantiate(prefabToSpawn, spawnPoint.position + spawnOffset, Quaternion.identity);

        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null) agent.Warp(spawnPoint.position + spawnOffset);

        NPCController controller = npc.GetComponent<NPCController>();
        if (controller != null)
        {
            controller.exitPoint = exitPoint;
            controller.groupID = groupID;

            bool isInTutorialQuest = MainQuestManager.Instance != null &&
                                     MainQuestManager.Instance.currentQuest != MainQuestManager.QuestStep.Completed;
            if (isInTutorialQuest)
            {
                controller.ignorePauseForWalking = true;
            }

            if (QueueManager.Instance != null)
            {
                QueueManager.Instance.AddToQueue(controller);
            }
        }

        NPCInteract interact = npc.GetComponent<NPCInteract>();
        if (interact != null && relationshipCanvas != null)
        {
            interact.relationshipCanvas = relationshipCanvas;
        }

        return controller;
    }

    public void IncreaseVIPChance(int amount)
    {
        vipSpawnChance = Mathf.Clamp(vipSpawnChance + amount, 0, 100);
    }
}