using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    public GameObject[] npcPrefabs;
    public Transform spawnPoint;
    public Transform exitPoint;

    public float spawnInterval = 5f;
    private float timer;
    public float minInterval = 3f;
    public float maxInterval = 7f;

    void Start()
    {
        spawnInterval = Random.Range(minInterval, maxInterval);
    }

    void Update()
    {
        // 🔥 FIX: เช็ค Null ของ Manager ทั้งสองตัว
        bool isPaused = TimeManager.Instance != null && TimeManager.Instance.isPaused;
        bool isNotWorkTime = DayNightManager.Instance != null && !DayNightManager.Instance.isWorkTime;

        if (isPaused || isNotWorkTime) return;

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

        int index = Random.Range(0, npcPrefabs.Length);
        GameObject selectedPrefab = npcPrefabs[index];

        GameObject npc = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);
        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();

        if (agent != null) agent.Warp(spawnPoint.position);

        NPCController controller = npc.GetComponent<NPCController>();
        if (controller != null)
        {
            controller.exitPoint = exitPoint;
            if (QueueManager.Instance != null) QueueManager.Instance.AddToQueue(controller);
        }
    }
}