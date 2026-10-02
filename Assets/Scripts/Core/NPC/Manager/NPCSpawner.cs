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
    [Tooltip("โอกาส % ที่ NPC จะเกิดพร้อมกันเป็นกลุ่ม")]
    [Range(0, 100)] public int groupSpawnChance = 40;
    [Tooltip("ขนาดกลุ่มขั้นต่ำ")] public int minGroupSize = 2;
    [Tooltip("ขนาดกลุ่มสูงสุด")] public int maxGroupSize = 2;

    [Header("VIP Cat Settings")]
    public GameObject[] vipNpcPrefabs;
    [Range(0, 100)] public int vipSpawnChance = 10;

    [Header("Scene References")]
    public GameObject relationshipCanvas;

    public float spawnInterval = 5f;
    private float timer;
    public float minInterval = 3f;
    public float maxInterval = 7f;

    void Awake() => Instance = this;

    void Start() => spawnInterval = Random.Range(minInterval, maxInterval);

    void Update()
    {
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

        Vector3 spawnOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0);
        GameObject npc = Instantiate(prefabToSpawn, spawnPoint.position + spawnOffset, Quaternion.identity);

        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null) agent.Warp(spawnPoint.position + spawnOffset);

        NPCController controller = npc.GetComponent<NPCController>();
        if (controller != null)
        {
            controller.exitPoint = exitPoint;
            controller.groupID = groupID;
            if (spawnVIP) controller.isVIP = true;

            // ส่งแมวเข้าคิวรอเสมอ เพื่อให้แมวหยุดรอผู้เล่นมากดคิว/กด E
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