using UnityEngine;

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
        GameObject npcObj = Instantiate(npcPrefab, spawnPoint.position, Quaternion.identity);

        NPCController npc = npcObj.GetComponent<NPCController>();

        npc.exitPoint = exitPoint;

        QueueManager.Instance.AddToQueue(npc);
    }
}