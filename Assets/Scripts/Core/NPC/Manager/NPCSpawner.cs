using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    public GameObject[] npcPrefabs; // มี 3 ตัว
    public Transform spawnPoint;
    public Transform exitPoint;

    public float spawnInterval = 5f;
    private float timer;
    public float minInterval = 3f;
    public float maxInterval = 7f;
    private float currentInterval;
    void Start()
    {
        SetNextInterval();
    }

    void Update()
    {
        if (TimeManager.Instance.isPaused) return; // 🔥 หยุด spawn

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnNPC();
            timer = 0f;
        }
    }
    void SetNextInterval()
    {
        currentInterval = Random.Range(minInterval, maxInterval);
    }

    void SpawnNPC()
    {
        if (npcPrefabs.Length == 0) return;

        // 🎲 สุ่ม NPC
        int index = Random.Range(0, npcPrefabs.Length);
        GameObject selectedPrefab = npcPrefabs[index];

        GameObject npc = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);

        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.Warp(spawnPoint.position);
        }

        NPCController controller = npc.GetComponent<NPCController>();

        if (controller != null)
        {
            controller.exitPoint = exitPoint;
            QueueManager.Instance.AddToQueue(controller);
        }
        else
        {
            Debug.LogWarning("NPC ไม่มี NPCController!");
        }
    }
}