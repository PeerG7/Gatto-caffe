using UnityEngine;
using System.Collections.Generic;

// =====================================================================
// InteractionZoneManager — คุมการจัดสรรโซน Interaction ทั้งร้าน
//
// หน้าที่:
//   - เก็บ list ของ InteractionZone ทั้งหมดในร้าน
//   - ถ้า NPC ขอใช้โซนแล้วมีว่าง -> จองให้ทันที
//   - ถ้าไม่มีว่าง -> เข้าคิวรอ พอมีโซนว่าง (Update ทุกเฟรมเช็คให้) จะจับคู่ให้อัตโนมัติ
//   - กันปัญหาแมว 2+ ตัวแย่งจุดเดียวกันพร้อมกัน (race condition)
//
// Setup:
//   1. สร้าง Empty GameObject ชื่อ "InteractionZoneManager" ในซีน (ตัวเดียวพอ)
//   2. ใส่ component นี้
//   3. ลาก InteractionZone ทุกตัวที่มีในร้าน มาใส่ list "Zones"
// =====================================================================
public class InteractionZoneManager : MonoBehaviour
{
    public static InteractionZoneManager Instance;

    [Tooltip("ลาก InteractionZone ทุกจุด/โซนในร้านมาใส่ที่นี่")]
    public List<InteractionZone> zones = new List<InteractionZone>();

    private class PendingRequest
    {
        public NPCController npc;
        public System.Action<InteractionZone> onZoneReady;
    }

    private readonly Queue<PendingRequest> waitingQueue = new Queue<PendingRequest>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // มีคนรอคิวอยู่ไหม แล้วตอนนี้มีโซนว่างหรือยัง -> จับคู่ทันที
        while (waitingQueue.Count > 0)
        {
            InteractionZone freeZone = GetAvailableZone();
            if (freeZone == null) break;

            PendingRequest req = waitingQueue.Dequeue();

            // เผื่อ NPC ตัวที่รอคิวโดน Destroy ไปแล้ว (โกรธ/ออกจากร้านระหว่างรอ)
            if (req.npc == null) continue;

            freeZone.Occupy(req.npc);
            req.onZoneReady?.Invoke(freeZone);
        }
    }

    public InteractionZone GetAvailableZone()
    {
        foreach (var zone in zones)
        {
            if (zone != null && zone.IsAvailable())
                return zone;
        }
        return null;
    }

    /// <summary>
    /// ✅ ใหม่: ขอจองโซนแบบ "ทันทีเท่านั้น" — ไม่เข้าคิวรอ
    /// มีว่าง -> จองให้แล้ว return zone นั้น
    /// ไม่มีว่าง -> return null ทันที (ให้ฝั่งผู้เรียกตัดสินใจเอง เช่น ให้แมวออกจากร้านไปเลย)
    /// </summary>
    public InteractionZone TryOccupyZoneImmediate(NPCController npc)
    {
        InteractionZone zone = GetAvailableZone();
        if (zone == null) return null;

        zone.Occupy(npc);
        return zone;
    }

    /// <summary>
    /// ขอใช้โซน Interaction ให้ NPC ตัวหนึ่ง
    /// มีว่าง -> เรียก callback ทันที
    /// ไม่มีว่าง -> เข้าคิว รอจนกว่าจะมีโซนว่าง แล้วค่อยเรียก callback ทีหลัง
    /// (ตอนนี้ NPCController ไม่ได้ใช้เมธอดนี้แล้ว — เก็บไว้เผื่อระบบอื่นที่ต้องการคิวจริงๆ)
    /// </summary>
    public void RequestZone(NPCController npc, System.Action<InteractionZone> onZoneReady)
    {
        InteractionZone zone = GetAvailableZone();
        if (zone != null)
        {
            zone.Occupy(npc);
            onZoneReady?.Invoke(zone);
            return;
        }

        waitingQueue.Enqueue(new PendingRequest { npc = npc, onZoneReady = onZoneReady });
    }

    /// <summary>ยกเลิกคำขอที่ยังค้างอยู่ในคิว (เช่น NPC โดนบังคับออกจากร้านระหว่างรอคิว)</summary>
    public void CancelRequest(NPCController npc)
    {
        if (waitingQueue.Count == 0) return;

        var remaining = new Queue<PendingRequest>();
        foreach (var req in waitingQueue)
        {
            if (req.npc != npc) remaining.Enqueue(req);
        }
        waitingQueue.Clear();
        foreach (var r in remaining) waitingQueue.Enqueue(r);
    }

    public void ReleaseZone(InteractionZone zone)
    {
        zone?.Release();
    }
}