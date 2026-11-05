using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class GuidanceZoneManager : MonoBehaviour
{
    public static GuidanceZoneManager Instance;

    [Header("Guidance Points / Positions")]
    [Tooltip("จุดสำหรับยืนรอในโซนนี้ (สามารถสร้างหลายๆ จุดย่อยในโซนได้)")]
    public List<Transform> guidancePoints = new List<Transform>();

    private Dictionary<NPCController, Transform> assignedPoints = new Dictionary<NPCController, Transform>();

    void Awake()
    {
        Instance = this;
        // ถ้าไม่ได้สร้างจุดย่อยไว้ ให้ใช้ตำแหน่งศูนย์กลางของ Collider แทน
        if (guidancePoints == null || guidancePoints.Count == 0)
        {
            guidancePoints = new List<Transform> { transform };
        }
    }

    /// <summary>
    /// หาจุดว่างในโซนพักรอเพื่อให้แมวเดินมาหลบพัก
    /// </summary>
    public Transform GetAvailablePoint(NPCController npc)
    {
        foreach (var point in guidancePoints)
        {
            if (!assignedPoints.ContainsValue(point))
            {
                assignedPoints[npc] = point;
                return point;
            }
        }
        return guidancePoints[0]; // ถ้าเต็ม ใช้จุดแรกสำรอง
    }

    public void ReleasePoint(NPCController npc)
    {
        if (assignedPoints.ContainsKey(npc))
        {
            assignedPoints.Remove(npc);
        }
    }
}