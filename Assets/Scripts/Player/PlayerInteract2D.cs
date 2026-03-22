using UnityEngine;

public class PlayerInteract2D : MonoBehaviour
{
    public float range = 1.5f;

    void Update()
    {
        if (TimeManager.Instance.isPaused) return; // 🔥 หยุดทุก input

        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

            // 🔧 1. Repair ก่อน
            foreach (var hit in hits)
            {
                DamageableObject obj = hit.GetComponent<DamageableObject>();

                if (obj != null && obj.IsBroken())
                {
                    Debug.Log("🔧 Repair");
                    obj.Repair();
                    return;
                }
            }

            // 🤖 2. หา NPC ที่ใกล้ที่สุด
            NPCInteract closestNPC = null;
            float minDist = Mathf.Infinity;

            foreach (var hit in hits)
            {
                NPCInteract npc = hit.GetComponent<NPCInteract>();

                if (npc == null) continue;

                float dist = Vector2.Distance(transform.position, npc.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    closestNPC = npc;
                }
            }

            if (closestNPC != null)
            {
                if (closestNPC.CanInteract())
                {
                    Debug.Log("➡ Open QTE");
                    closestNPC.RelationShip();
                }
                else
                {
                    Debug.Log("➡ Invite NPC");
                    closestNPC.Interact();
                }
            }
            else
            {
                Debug.Log("❌ Nothing to interact");
            }
        }
    }

    // 🎯 Debug ระยะ
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}