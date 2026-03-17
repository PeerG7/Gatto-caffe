using UnityEngine;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
    public GameObject npcPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;

    public float spawnInterval = 5f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnNPC();
            timer = 0f;
        }
    }

    void SpawnNPC()
    {

        GameObject npc = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);

        Debug.Log(npc.GetComponent<NavMeshAgent>()); //ตัวเช็ค

        NavMeshAgent agent = npc.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.Warp(spawnPoint.position);
        }

        NPCController controller = npc.GetComponent<NPCController>();

        controller.exitPoint = exitPoint;

        QueueManager.Instance.AddToQueue(controller);
    }
}